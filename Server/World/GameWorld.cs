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
using Server.Testing;
using Server.Interface;
using Server.Misc;
using Server.GameItems;
using Server.Networking;
using Server.Networking.PacketSenders;
using OpenTK.Input;
using CharacterData = Server.Data.CharacterData;

namespace Server.World
{
    public class GameWorld
    {
        public Simulation World = null;   //Bepu Physics Engine Simulation containing all characters colliders, enemy entities, item pickups etc
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

        private bool TransmitPackets = true;

        private int ArenaMeshHandle;

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
            var FontContent = Content.Load<FontContent>(@"Carlito-Regular.ttf");
            UIFont = new Font(Loop.Surface.Device, Loop.Surface.Context, FontContent);

            //Position the camera
            ObservationCamera.PositionCamera(new Vector3(2.7f, 6.48f, 9.76f));
            ObservationCamera.FaceCamera(0.269f, 0.15899f);

            //Setup character controller and world simulation
            BufferPool = new BufferPool();
            ThreadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
            World = Simulation.Create(BufferPool, new CharacterNarrowphaseCallbacks(new CharacterControllers(BufferPool)), new ScenePoseIntegratorCallbacks(new Vector3(0, -10, 0)));

            //Initialize the mesh loader
            MeshManager.Initialize(Content, BufferPool);

            //Load in the numbers display the direction of the X/Z axes
            AxisMarkers.LoadModels();
            AxisMarkers.AddModels(World);

            //Load in the PVP arena
            ArenaMeshHandle = MeshManager.LoadMesh(@"Arena.obj", new Vector3(1));
            //Define its starting position and location
            Vector3 ArenaPosition = new Vector3(29.82f, 3.62f, 0.94f);
            Quaternion ArenaRotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), Trig.DegreesToRadians(180));
            //Use those to add it into the world
            MeshManager.AddStatic(World, ArenaMeshHandle, ArenaPosition, ArenaRotation);

            //Place a ground plane to walk on
            World.Statics.Add(new StaticDescription(new Vector3(0, -0.5f, 0), new CollidableDescription(World.Shapes.Add(new Box(100, 1, 100)), 0.1f)));

            //Setup the command executor
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

                if (UserInput.WasPushed(Key.G))
                {
                    MessageLog.Print("Camera Pos: " + ObservationCamera.SceneCamera.Position.ToString() + ", Pitch: " + ObservationCamera.SceneCamera.Pitch + ", Yaw: " + ObservationCamera.SceneCamera.Yaw);
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
            //Poll User Input from the server window
            ProcessInput(ApplicationWindow.Focused, DeltaTime);

            //Manage all client connections and their player characters
            ConnectionManager.CheckClients(DeltaTime);
            ConnectionManager.CleanDeadClients(World);
            ConnectionManager.AddNewClients(World);
            ConnectionManager.UpdateClientPositions(World);
            ConnectionManager.RespawnDeadPlayers(World);

            //Track current inhabitants of the PVP Battle Arena, then process the players PVP attacks
            List<CharacterData> InGameCharacters = ClientSubsetFinder.GetInGameCharacters();
            PVPBattleArena.UpdateArenaInhabitants(InGameCharacters);
            PVPBattleArena.AlertTravellers();
            ConnectionManager.PerformPlayerAttacks(World);
            
            //Update the packet queue, transmitting all messages to the client connections
            PacketQueue.UpdateQueue(DeltaTime, TransmitPackets);
            
            //Update the physics simulation
            World.Timestep(DeltaTime, ThreadDispatcher);
            TimeSamples.RecordFrame(World);
        }

        public void RenderWorld(Renderer Renderer)
        {
            //Clear the frame buffer
            Renderer.Shapes.ClearInstances();
            Renderer.Lines.ClearInstances();

            //Draws various text message windows to the UI displaying a range of useful information
            RenderUI(Renderer);

            //Render all the shapes in the scene
            Renderer.Shapes.AddInstances(World, ThreadDispatcher);
            Renderer.Lines.Extract(World.Bodies, World.Solver, World.BroadPhase, false, false, false, ThreadDispatcher);
        }

        private void RenderUI(Renderer Renderer)
        {
            //Define the locations where each element of the UI will be rendered
            Vector2 MessageLogPos = new Vector2(10, 450);
            Vector2 CommandInputPos = new Vector2(10, 430);
            Vector2 PacketOutPos = new Vector2(800, 25);
            Vector2 PacketInPos = new Vector2(550, 25);

            //Render each element to the window UI
            CommandInputField.Render(Renderer, CommandInputPos, UITextSize, UITextColor, UIFont);
            MessageLog.RenderLog(Renderer, MessageLogPos, UITextSize, UITextColor, UIFont);
            CommunicationLog.RenderOutgoingLog(Renderer, PacketOutPos, UITextSize, UITextColor, UIFont);
            CommunicationLog.RenderIncomingLog(Renderer, PacketInPos, UITextSize, UITextColor, UIFont);
        }
    }
}