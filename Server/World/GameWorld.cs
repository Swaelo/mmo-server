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
            }
            else
                UserInput.MouseLocked = false;
        }

        public void UpdateWorld(float DeltaTime)
        {
            ProcessInput(ApplicationWindow.Focused, DeltaTime);
            PacketQueue.UpdateQueue(DeltaTime);
            ConnectionManager.CleanDeadClients(WorldSimulation);
            ConnectionManager.AddNewClients(WorldSimulation);
            ConnectionManager.RespawnDeadPlayers(WorldSimulation);
            ConnectionManager.UpdateClientPositions(WorldSimulation);
            ConnectionManager.PerformPlayerAttacks(WorldSimulation);

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
            //ConnectionManager.RenderClientsInfo(Renderer, ClientsInfoPos, UITextSize, UITextColor, UIFont);
        }
    }
}