// ================================================================================================================================
// File:        GameWorld.cs
// Description: Handles the current game world simulation and everything within
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using OpenTK.Input;
using ContentLoader;
using ContentRenderer;
using ContentRenderer.UI;
using ServerUtilities;
using Server.Logic;
using Server.Interface;
using Server.GameItems;
using Server.Networking;
using Server.Enums;

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
        public Camera SceneCamera = null;   //The camera used to view the game world during server runtime
        private ContentArchive Content = null;  //Assets used during the servers runtime
        private SimulationTimeSamples TimeSamples = null;   //Timesamples used to evaluate and display performance metric on the graph during runtime
        private Font UIFont = null; //Font type used to draw strings to the application window
        private TextBuilder UIText; //Used to create our Font for drawing strings to the UI

        CameraMoveSpeedState CameraSpeedState = CameraMoveSpeedState.Regular;   //The current speed value being used by the scene camera while monitoring the game world during runtime
        CharacterControllers Characters;    //Set of characters currently active in the game world
        bool CharacterActive = false;   //Tracks whether we are using freeclip mode or character controller mode to view whats happening in the game world
        CharacterInput Character;   //The character controller used while moving around the game world with that mode

        private PerformanceGraph PerformanceGraph = null;    //Graph displayed to the windows UI to monitor performance metrics during the servers runtime
        private bool ShowControls = false;  //Should the user controls be displayed on the UI
        private bool ShowConstraints = true;    //Should physics collider shapes be displayed in the servers view window
        private bool ShowContacts = false;  //Should the active physical contact points between physics objects be displayed in the servers view window
        private bool ShowBoundingBoxes = false; //Should the physical bounding box colliders edges be displayed in the servers view window

        //Constructor which sets up the whole game world scene
        public GameWorld(GameLoop Loop, ContentArchive Content)
        {
            //Store references from the GameLoop class
            ApplicationWindow = Loop.Window;
            UserInput = Loop.Input;
            SceneCamera = Loop.Camera;
            this.Content = Content;
            TimeSamples = new SimulationTimeSamples(512, Loop.Pool);
            UserControls = Controls.Default;

            //Load font from the content archive
            var FontContent = Content.Load<FontContent>(@"Content\Carlito-Regular.ttf");
            UIFont = new Font(Loop.Surface.Device, Loop.Surface.Context, FontContent);

            //Set up the performance graph
            PerformanceGraph = new PerformanceGraph(UIFont, TimeSamples);

            //Setup camera, character controller and world simulation
            SceneCamera.Position = new Vector3(6, 2.5f, -8);
            SceneCamera.Yaw = -3.14f;
            SceneCamera.Pitch = 0;
            BufferPool = new BufferPool();
            ThreadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
            Characters = new CharacterControllers(BufferPool);
            WorldSimulation = Simulation.Create(BufferPool, new CharacterNarrowphaseCallbacks(Characters), new ScenePoseIntegratorCallbacks(new Vector3(0, -10, 0)));
            
            //Place a ground plane to walk on
            WorldSimulation.Statics.Add(new StaticDescription(new Vector3(0), new CollidableDescription(WorldSimulation.Shapes.Add(new Box(200, 1, 200)), 0.1f)));
            
            //Setup text builder for rendering UI text components
            UIText = new TextBuilder(128);

            //Make sure the window size is correct relative to the current resolution
            OnResize(ApplicationWindow.Resolution);
        }

        //Allow the window to be resized
        public void OnResize(Int2 Resolution)
        {
            PerformanceGraph.UpdateGraphTimingMode(PerformanceGraph.GraphDisplayMode, ApplicationWindow);
        }

        private void ProcessInput(bool WindowFocused, float DeltaTime)
        {
            //Ignore user input if the application window is not in focus
            if (WindowFocused)
            {
                //Close down the server when escape key is pressed
                if (UserControls.Exit.WasTriggered(UserInput))
                {
                    ApplicationWindow.Close();
                    return;
                }

                //Adjust camera movement speed with the scrollwheel
                if (UserControls.MoveFaster.WasTriggered(UserInput))
                {
                    switch (CameraSpeedState)
                    {
                        case CameraMoveSpeedState.Slow:
                            CameraSpeedState = CameraMoveSpeedState.Regular;
                            break;
                        case CameraMoveSpeedState.Regular:
                            CameraSpeedState = CameraMoveSpeedState.Fast;
                            break;
                    }
                }
                if (UserControls.MoveSlower.WasTriggered(UserInput))
                {
                    switch (CameraSpeedState)
                    {
                        case CameraMoveSpeedState.Regular:
                            CameraSpeedState = CameraMoveSpeedState.Slow;
                            break;
                        case CameraMoveSpeedState.Fast:
                            CameraSpeedState = CameraMoveSpeedState.Regular;
                            break;
                    }
                }

                //Move camera around the world with WASD
                var CameraOffset = new Vector3();
                if (UserControls.MoveForward.IsDown(UserInput))
                    CameraOffset += SceneCamera.Forward;
                if (UserControls.MoveBackward.IsDown(UserInput))
                    CameraOffset += SceneCamera.Backward;
                if (UserControls.MoveLeft.IsDown(UserInput))
                    CameraOffset += SceneCamera.Left;
                if (UserControls.MoveRight.IsDown(UserInput))
                    CameraOffset += SceneCamera.Right;
                if (UserControls.MoveUp.IsDown(UserInput))
                    CameraOffset += SceneCamera.Up;
                if (UserControls.MoveDown.IsDown(UserInput))
                    CameraOffset += SceneCamera.Down;

                //Only update the camera position if its to be moved past a minimum distance
                float CameraMovement = CameraOffset.Length();
                if (CameraMovement > 1e-7f)
                {
                    //Get the current camera movement speed
                    float CameraMoveSpeed = 0;
                    switch (CameraSpeedState)
                    {
                        case CameraMoveSpeedState.Slow:
                            CameraMoveSpeed = UserControls.CameraSlowMoveSpeed;
                            break;
                        case CameraMoveSpeedState.Regular:
                            CameraMoveSpeed = UserControls.CameraMoveSpeed;
                            break;
                        case CameraMoveSpeedState.Fast:
                            CameraMoveSpeed = UserControls.CameraFastMoveSpeed;
                            break;
                    }
                    //Keep the movement speed framerate independant
                    CameraOffset *= DeltaTime * CameraMoveSpeed / CameraMovement;
                }
                else
                    CameraOffset = new Vector3();
                //Update the cameras position
                SceneCamera.Position += CameraOffset;

                //Use the mouse to turn the camera when the cursor is locked
                if (UserInput.MouseLocked)
                {
                    var Delta = UserInput.MouseDelta;
                    if (Delta.X != 0 || Delta.Y != 0)
                    {
                        SceneCamera.Yaw += Delta.X * UserControls.MouseSensitivity;
                        SceneCamera.Pitch += Delta.Y * UserControls.MouseSensitivity;
                    }
                }

                //Toggle character controller with C
                if (UserInput.WasPushed(Key.C))
                {
                    if (CharacterActive)
                    {
                        //Remove and disable character controller, going to noclip fly mode
                        Character.Dispose();
                        CharacterActive = false;
                    }
                    else
                    {
                        //Create and enable character controller
                        Character = new CharacterInput(Characters, SceneCamera.Position, new Capsule(0.5f, 1), 0.1f, 1, 20, 100, 6, 4, MathF.PI * 0.4f);
                        CharacterActive = true;
                    }
                }

                //Update the character controller whenever its active
                if (CharacterActive)
                    Character.UpdateCharacterGoals(UserInput, SceneCamera);

                //Toggle the mouse lock with TAB
                if (UserControls.LockMouse.WasTriggered(UserInput))
                    UserInput.MouseLocked = !UserInput.MouseLocked;
                //Toggle controls displayed on the UI
                if (UserControls.ShowControls.WasTriggered(UserInput))
                    ShowControls = !ShowControls;
                //Toggle physical restraints display
                if (UserControls.ShowConstraints.WasTriggered(UserInput))
                    ShowConstraints = !ShowConstraints;
                //Toggle physical contacts display
                if (UserControls.ShowContacts.WasTriggered(UserInput))
                    ShowContacts = !ShowContacts;
                //Toggle bounding boxes display
                if (UserControls.ShowBoundingBoxes.WasTriggered(UserInput))
                    ShowBoundingBoxes = !ShowBoundingBoxes;

                //Allow changing the timing display mode in the server performance graph display
                if (UserControls.ChangeTimingDisplayMode.WasTriggered(UserInput))
                    PerformanceGraph.ChangeToNextDisplayMode(ApplicationWindow);
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

            //PacketQueue will automatically transmit all the queued outgoing packets to their target clients each communication interval
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

            //Display the contents of all the message display windows to the UI
            Log.DebugMessageWindow.RenderMessages(Renderer, UIText, UIFont, new Vector2(10, 550));

            //Display controls to the UI if the flag has been set
            if(ShowControls)
            {
                //Display the controls to the UI
                float TextHeight = 16;
                Vector2 TextPosition = new Vector2(Renderer.Surface.Resolution.X - 500, Renderer.Surface.Resolution.Y - TextHeight * 10);
                Renderer.TextBatcher.Write(UIText.Clear().Append("Toggle character: C"), TextPosition, TextHeight, new Vector3(1), UIFont);
                TextPosition.Y += TextHeight * 1.2f;
                Character.RenderControls(TextPosition, TextHeight, Renderer.TextBatcher, UIText, UIFont);
            }

            //Update the characters camera if its active
            if (CharacterActive)
                Character.UpdateCameraPosition(SceneCamera);

            //Render the performance graph to the UI
            PerformanceGraph.RenderGraph(UIText, Renderer);

            //Render all the shapes in the scene
            Renderer.Shapes.AddInstances(WorldSimulation, ThreadDispatcher);
            Renderer.Lines.Extract(WorldSimulation.Bodies, WorldSimulation.Solver, WorldSimulation.BroadPhase, ShowConstraints, ShowContacts, ShowBoundingBoxes, ThreadDispatcher);
        }
    }
}
