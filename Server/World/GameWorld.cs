// ================================================================================================================================
// File:        GameWorld.cs
// Description: Handles the current game world simulation and everything within
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using System.Collections.Generic;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuUtilities;
using BepuUtilities.Memory;
using Quaternion = BepuUtilities.Quaternion;
using ContentLoader;
using ContentRenderer;
using ContentRenderer.UI;
using ServerUtilities;
using Server.Logging;
using Server.Logic;
using Server.Interface;
using Server.GameItems;
using Server.Networking;
using Server.Networking.PacketSenders;
using OpenTK.Input;

namespace Server.World
{
    public class GameWorld
    {
        public Simulation WorldSimulation = null;   //Bepu Physics Engine Simulation containing all characters colliders, enemy entities, item pickups etc
        public BufferPool BufferPool = null;    //Used to managed and assign chunks of memory to the server application as it needs them during runtime
        SimpleThreadDispatcher ThreadDispatcher = null; //Used to assign extra processing threads to the server application during runtime

        public Window ApplicationWindow = null; //The window displaying all server information when the program is running
        public Input UserInput = null;  //Current input recieved while controlling the server application during runtime
        private Controls UserControls;  //Control settings for receiving input

        public ObservationCamera ObservationCamera = new ObservationCamera();   //The camera used to view the game world during server runtime

        private ContentArchive Content = null;  //Assets used during the servers runtime
        private SimulationTimeSamples TimeSamples = null;   //Timesamples used to evaluate and display performance metric on the graph during runtime
        private Font UIFont = null; //Font type used to draw strings to the application window

        private float UITextSize = 16;  //Size of the font used to display messages to the UI
        private Vector3 UITextColor = new Vector3(1);

        private CommandInput CommandInputField = new CommandInput(); //Allows user to type messages into the server application for custom command execution

        public bool PacketQueueEnabled = true;

        //Constructor which sets up the whole game world scene
        public GameWorld(GameLoop Loop, ContentArchive Content)
        {
            //Store references from the GameLoop class
            ApplicationWindow = Loop.Window;
            UserInput = Loop.Input;

            //Assign the camera reference
            ObservationCamera.SetCamera(Loop);

            this.Content = Content;
            TimeSamples = new SimulationTimeSamples(512, Loop.Pool);
            UserControls = Controls.Default;

            //Load font from the content archive
            var FontContent = Content.Load<FontContent>(@"Content\Carlito-Regular.ttf");
            UIFont = new Font(Loop.Surface.Device, Loop.Surface.Context, FontContent);

            //Position the camera
            ObservationCamera.PositionCamera(new Vector3(6, 2.5f, -8), -3.14f, 0);

            //Setup character controller and world simulation
            BufferPool = new BufferPool();
            ThreadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
            WorldSimulation = Simulation.Create(BufferPool, new CharacterNarrowphaseCallbacks(new CharacterControllers(BufferPool)), new ScenePoseIntegratorCallbacks(new Vector3(0, -10, 0)));
            
            //Place a ground plane to walk on
            WorldSimulation.Statics.Add(new StaticDescription(new Vector3(0), new CollidableDescription(WorldSimulation.Shapes.Add(new Box(200, 1, 200)), 0.1f)));

            CommandInputField.Initialize();

            //Make sure the window size is correct relative to the current resolution
            OnResize(ApplicationWindow.Resolution);
        }

        //Allow the window to be resized
        public void OnResize(Int2 Resolution)
        {
            
        }

        private void ProcessInput(bool WindowFocused, float DeltaTime)
        {
            //Ignore user input if the application window is not in focus
            if (WindowFocused)
            {
                //Allow user to type messages into the command input window
                CommandInputField.Update(UserInput, DeltaTime);

                //Allow the user to control the camera if the command input field is inactive
                if (!CommandInputField.InputEnabled)
                    ObservationCamera.UpdateCamera(UserControls, UserInput, DeltaTime);

                //Toggle packet transmission with G
                if(UserInput.WasPushed(Key.G))
                {
                    PacketQueueEnabled = !PacketQueueEnabled;
                    MessageLog.Print("Packet Queue " + (PacketQueueEnabled ? "Enabled." : "Disabled."));
                }
            }
            else
                UserInput.MouseLocked = false;
        }

        public void UpdateWorld(float DeltaTime)
        {
            //Perform any actions required based on user input, move camera around the scene etc
            ProcessInput(ApplicationWindow.Focused, DeltaTime);

            //Remove any item pickups from the physics scene which have been queued up to be removed
            ItemManager.ClearRemoveQueue(WorldSimulation);

            //Calling this will have the ConnectionManager keep track of how much time has passed since we last heard from each client connection
            //Then automatically cleans up and connections which have been inactive for too long
            ConnectionManager.CheckConnections(DeltaTime);
            //Remove any character colliders from the physics scene which have been queued up to be removed
            ConnectionManager.CleanDeadClients(WorldSimulation);

            //Perform attacks by players that said they performed an attack since the last world update
            PerformPlayerAttacks(WorldSimulation);
            RespawnDeadPlayers(WorldSimulation);

            //Update the positions of any character colliders who have sent us a new position update since the last world update
            ConnectionManager.UpdateClientPositions(WorldSimulation);

            //Add any new clients characters into the game world who have recently logged in
            AddNewClients();

            PacketQueue.UpdateQueue(DeltaTime, PacketQueueEnabled);

            //Simulate physics and record frame data for performance monitor
            WorldSimulation.Timestep(DeltaTime, ThreadDispatcher);
            TimeSamples.RecordFrame(WorldSimulation);
        }

        private void RespawnDeadPlayers(Simulation WorldSimulation)
        {
            //Get all the clients waiting to be respawned
            List<ClientConnection> RespawningClients = ClientSubsetFinder.GetClientsAwaitingRespawn();

            //Loop through and respawn each of the clients
            foreach(ClientConnection RespawningClient in RespawningClients)
            {
                //Reset the characters values to their default
                RespawningClient.Character.SetDefaultValues();

                //Add the clients body back into the physics simulation
                RespawningClient.InitializePhysicsBody(WorldSimulation, RespawningClient.Character.Position);

                //Tell the client to respawn their character with all these values
                CombatPacketSenders.SendLocalPlayerRespawn(RespawningClient.NetworkID, RespawningClient.Character);
                //And the other clients to respawn this character in their world
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(RespawningClient.NetworkID))
                    CombatPacketSenders.SendRemotePlayerRespawn(OtherClient.NetworkID, RespawningClient.Character);

                RespawningClient.WaitingToRespawn = false;
                RespawningClient.Character.IsAlive = true;
            }
        }

        private void PerformPlayerAttacks(Simulation WorldSimulation)
        {
            //Get all the clients who's players are performing an attack this frame
            List<ClientConnection> AttackingClients = ClientSubsetFinder.GetClientsAttacking();

            //Loop through and perform each clients attack
            foreach(ClientConnection AttackingClient in AttackingClients)
            {
                //Get a list of all the other players currently in the game (not including this one who is performing the attack)
                List<ClientConnection> OtherPlayers = ClientSubsetFinder.GetInGameClientsExceptFor(AttackingClient.NetworkID);

                //Check if the attack is close enough to any of these players to hit them
                foreach(ClientConnection OtherPlayer in OtherPlayers)
                {
                    //Check the distance from the attack location to this player
                    float AttackDistance = Vector3.Distance(OtherPlayer.Character.Position, AttackingClient.AttackPosition);
                    //If the attack is close enough to hit, then it should deal damage to that player
                    if(AttackDistance < 2f)
                    {
                        //Attack hit, remove 1 health point from the player who recieved the attack and alert that client they have taken damage
                        OtherPlayer.Character.CurrentHealth -= 1;

                        //If the player is still alive we just alert all clients with their new HP value
                        if(OtherPlayer.Character.CurrentHealth > 0)
                        {
                            //Tell the client their character has taken damage
                            CombatPacketSenders.SendLocalPlayerTakeHit(OtherPlayer.NetworkID, OtherPlayer.Character.CurrentHealth);
                            //And other clients this other character has taken damage
                            foreach (ClientConnection OtherOtherPlayer in ClientSubsetFinder.GetInGameClientsExceptFor(OtherPlayer.NetworkID))
                                CombatPacketSenders.SendRemotePlayerTakeHit(OtherOtherPlayer.NetworkID, OtherPlayer.Character.Name, OtherPlayer.Character.CurrentHealth);
                        }
                        //Otherwise we remove their character from the physics scene and tell everyone they are now dead
                        else
                        {
                            //Mark the clients character as being dead and remove their characters collider from the physics scene
                            OtherPlayer.Character.IsAlive = false;
                            WorldSimulation.Bodies.Remove(OtherPlayer.BodyHandle);
                            WorldSimulation.Shapes.Remove(OtherPlayer.ShapeIndex);
                            //Tell the client their character is now dead
                            CombatPacketSenders.SendLocalPlayerDead(OtherPlayer.NetworkID);
                            //And other clients this other character is now dead
                            foreach (ClientConnection OtherOtherPlayer in ClientSubsetFinder.GetInGameClientsExceptFor(OtherPlayer.NetworkID))
                                CombatPacketSenders.SendRemotePlayerDead(OtherOtherPlayer.NetworkID, OtherPlayer.Character.Name);
                        }
                    }
                }

                //Attack is finished
                AttackingClient.AttackPerformed = false;
            }
        }

        public void RenderWorld(Renderer Renderer)
        {
            //Clear the frame buffer
            Renderer.Shapes.ClearInstances();
            Renderer.Lines.ClearInstances();

            //Draws various text message windows to the UI displaying a range of useful information
            RenderUI(Renderer);

            //Render all the shapes in the scene
            Renderer.Shapes.AddInstances(WorldSimulation, ThreadDispatcher);
            Renderer.Lines.Extract(WorldSimulation.Bodies, WorldSimulation.Solver, WorldSimulation.BroadPhase, false, false, false, ThreadDispatcher);
        }

        private void RenderUI(Renderer Renderer)
        {
            //Define the locations where each element of the UI will be rendered
            Vector2 MessageLogPos = new Vector2(10, 450);
            Vector2 CommandInputPos = new Vector2(10, 430);
            Vector2 PacketOutPos = new Vector2(800, 25);
            Vector2 PacketInPos = new Vector2(550, 25);
            Vector2 ClientsInfoPos = new Vector2(10, 25);

            //Render each element to the window UI
            CommandInputField.Render(Renderer, CommandInputPos, UITextSize, UITextColor, UIFont);
            MessageLog.RenderLog(Renderer, MessageLogPos, UITextSize, UITextColor, UIFont);
            CommunicationLog.RenderOutgoingLog(Renderer, PacketOutPos, UITextSize, UITextColor, UIFont);
            CommunicationLog.RenderIncomingLog(Renderer, PacketInPos, UITextSize, UITextColor, UIFont);
            ConnectionManager.RenderClientsInfo(Renderer, ClientsInfoPos, UITextSize, UITextColor, UIFont);
        }

        //Adds any clients character into the game world who have just logged in and are waiting to be added before they are allowed to start playing
        private void AddNewClients()
        {
            //Fetch any clients that are waiting to be entered into the game world, add them into the physics scene then tell them they can start playing
            foreach (ClientConnection ClientToAdd in ClientSubsetFinder.GetClientsReadyToEnter())
            {
                Vector3 SpawnLocation = new Vector3(ClientToAdd.Character.Position.X, ClientToAdd.Character.Position.Y + 2, ClientToAdd.Character.Position.Z);
                ClientToAdd.InitializePhysicsBody(WorldSimulation, SpawnLocation);

                //Set this clients flags so they are now known to be ingame, and no longer waiting to be added
                ClientToAdd.WaitingToEnter = false;
                ClientToAdd.InGame = true;

                //Tell this client they have been added into the game world and they may now start playing
                PlayerManagementPacketSender.SendPlayerBegin(ClientToAdd.NetworkID);

                //Tell all other ingame clients they need to have this new player spawned into the game worlds
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(ClientToAdd.NetworkID))
                    PlayerManagementPacketSender.SendAddRemotePlayer(OtherClient.NetworkID, ClientToAdd.Character);

                //Display a message showing that the clients character has been spawned into the game world
                MessageLog.Print(ClientToAdd.Character.Name + " has entered into the game world");
            }
        }
    }
}