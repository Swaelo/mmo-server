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
        CharacterControllers Characters;

        private ContentArchive Content = null;  //Assets used during the servers runtime
        private SimulationTimeSamples TimeSamples = null;   //Timesamples used to evaluate and display performance metric on the graph during runtime
        private Font UIFont = null; //Font type used to draw strings to the application window

        private float UITextSize = 16;  //Size of the font used to display messages to the UI
        private Vector3 UITextColor = new Vector3(1);

        private CommandInput CommandInputField = new CommandInput(); //Allows user to type messages into the server application for custom command execution 

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
            Characters = new CharacterControllers(BufferPool);
            WorldSimulation = Simulation.Create(BufferPool, new CharacterNarrowphaseCallbacks(Characters), new ScenePoseIntegratorCallbacks(new Vector3(0, -10, 0)));
            
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

                if(UserInput.IsDown(MouseButton.Right))
                {
                    var Delta = UserInput.MouseDelta;
                    MessageLog.Print("Mouse: " + Delta.X + ", " + Delta.Y);
                }

                //Allow the user to control the camera if the command input field is inactive
                if (!CommandInputField.InputEnabled)
                    ObservationCamera.UpdateCamera(UserControls, UserInput, DeltaTime);
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

            //Update the positions of any character colliders who have sent us a new position update since the last world update
            ConnectionManager.UpdateClientPositions(WorldSimulation);

            //Add any new clients characters into the game world who have recently logged in
            AddNewClients();

            PacketQueue.UpdateQueue(DeltaTime);

            //Simulate physics and record frame data for performance monitor
            WorldSimulation.Timestep(DeltaTime, ThreadDispatcher);
            TimeSamples.RecordFrame(WorldSimulation);
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
            Vector2 MessageLogPos = new Vector2(10, 750);
            Vector2 CommandInputPos = new Vector2(10, 735);
            Vector2 PacketOutPos = new Vector2(800, 25);
            Vector2 PacketInPos = new Vector2(450, 25);
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
                //Add a new collider into the physics scene to represent where this clients player character is located in the game world
                ClientToAdd.PhysicsShape = new Capsule(0.5f, 1);
                ClientToAdd.ShapeIndex = WorldSimulation.Shapes.Add(ClientToAdd.PhysicsShape);
                ClientToAdd.PhysicsDescription = new CollidableDescription(ClientToAdd.ShapeIndex, 0.1f);
                ClientToAdd.PhysicsShape.ComputeInertia(1, out var Inertia);
                Vector3 SpawnLocation = new Vector3(ClientToAdd.Character.Position.X, ClientToAdd.Character.Position.Y + 2, ClientToAdd.Character.Position.Z);
                ClientToAdd.ShapePose = new RigidPose(SpawnLocation, Quaternion.Identity);
                ClientToAdd.ActivityDescription = new BodyActivityDescription(0.01f);
                ClientToAdd.PhysicsBody = BodyDescription.CreateDynamic(ClientToAdd.ShapePose, Inertia, ClientToAdd.PhysicsDescription, ClientToAdd.ActivityDescription);
                ClientToAdd.BodyHandle = WorldSimulation.Bodies.Add(ClientToAdd.PhysicsBody);

                //Set this clients flags so they are now known to be ingame, and no longer waiting to be added
                ClientToAdd.WaitingToEnter = false;
                ClientToAdd.InGame = true;

                //Tell this client they have been added into the game world and they may now start playing
                PlayerManagementPacketSender.SendPlayerBegin(ClientToAdd.NetworkID);

                //Tell all other ingame clients they need to have this new player spawned into the game worlds
                foreach (ClientConnection OtherClient in ClientSubsetFinder.GetInGameClientsExceptFor(ClientToAdd.NetworkID))
                    PlayerManagementPacketSender.SendAddRemotePlayer(OtherClient.NetworkID, ClientToAdd.Character.Name, ClientToAdd.Character.Position, ClientToAdd.Character.Movement, ClientToAdd.Character.Rotation);

                //Display a message showing that the clients character has been spawned into the game world
                MessageLog.Print(ClientToAdd.Character.Name + " has entered into the game world");
            }
        }
    }
}