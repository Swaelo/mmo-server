// ================================================================================================================================
// File:        GameWorld.cs
// Description: Handles the current game world simulation and everything within
// ================================================================================================================================

using System;
using System.Numerics;
using System.Collections.Generic;
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
using Server.Scenes;
using Server.Networking;

namespace Server.World
{
    public class GameWorld
    {
        //The game worlds physics simulation
        public Simulation WorldSimulation = null;
        
        //Stuff coming from base scene class
        public BufferPool BufferPool = null;
        SimpleThreadDispatcher ThreadDispatcher = null;

        public Window ApplicationWindow = null;
        public Input UserInput = null;
        private Controls UserControls;
        public Camera SceneCamera = null;
        private ContentArchive Content = null;
        private SimulationTimeSamples TimeSamples = null;
        private Font UIFont = null;
        private TextBuilder UIText;
        
        //Server Performance Monitoring Graph Display
        private enum TimingDisplayMode
        {
            Regular,
            Big,
            Minimized
        }
        TimingDisplayMode PerformanceDisplayMode;
        private Graph PerformanceGraph = null;

        //scene camera settings
        enum CameraMoveSpeedState
        {
            Regular,
            Slow,
            Fast
        }
        CameraMoveSpeedState CameraSpeedState = CameraMoveSpeedState.Regular;

        //Track time passing, send outgoing packets once per second
        private float SendPacketInterval = 1.0f;
        private float NextSendPacket = 1.0f;

        //Character controller used to move around and observe the game world
        CharacterControllers Characters;
        bool CharacterActive = false;
        CharacterInput Character;

        //Toggle states of what UI components should be displayed
        private bool ShowControls = false;
        private bool ShowConstraints = true;
        private bool ShowContacts = false;
        private bool ShowBoundingBoxes = false;
        private int FrameCounter = 0;

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

            //Setup the performance display monitor
            PerformanceGraph = new Graph(new GraphDescription
            {
                BodyLineColor = new Vector3(1, 1, 1),
                AxisLabelHeight = 16,
                AxisLineRadius = 0.5f,
                HorizontalAxisLabel = "Frames",
                VerticalAxisLabel = "Time (ms)",
                VerticalIntervalValueScale = 1e3f,
                VerticalIntervalLabelRounding = 2,
                BackgroundLineRadius = 0.125f,
                IntervalTextHeight = 12,
                IntervalTickRadius = 0.25f,
                IntervalTickLength = 6f,
                TargetHorizontalTickCount = 5,
                HorizontalTickTextPadding = 0,
                VerticalTickTextPadding = 3,

                LegendMinimum = new Vector2(20, 200),
                LegendNameHeight = 12,
                LegendLineLength = 7,

                TextColor = new Vector3(1, 1, 1),
                Font = UIFont,

                LineSpacingMultiplier = 1f,

                ForceVerticalAxisMinimumToZero = true
            });
            PerformanceGraph.AddSeries("Total", new Vector3(1, 1, 1), 0.75f, TimeSamples.Simulation);
            PerformanceGraph.AddSeries("Pose Integrator", new Vector3(0, 0, 1), 0.25f, TimeSamples.PoseIntegrator);
            PerformanceGraph.AddSeries("Sleeper", new Vector3(0.5f, 0, 1), 0.25f, TimeSamples.Sleeper);
            PerformanceGraph.AddSeries("Broad Update", new Vector3(1, 1, 0), 0.25f, TimeSamples.BroadPhaseUpdate);
            PerformanceGraph.AddSeries("Collision Test", new Vector3(0, 1, 0), 0.25f, TimeSamples.CollisionTesting);
            PerformanceGraph.AddSeries("Narrow Flush", new Vector3(1, 0, 1), 0.25f, TimeSamples.NarrowPhaseFlush);
            PerformanceGraph.AddSeries("Solver", new Vector3(1, 0, 0), 0.5f, TimeSamples.Solver);
            PerformanceGraph.AddSeries("Body Opt", new Vector3(1, 0.5f, 0), 0.125f, TimeSamples.BodyOptimizer);
            PerformanceGraph.AddSeries("Constraint Opt", new Vector3(0, 0.5f, 1), 0.125f, TimeSamples.ConstraintOptimizer);
            PerformanceGraph.AddSeries("Batch Compress", new Vector3(0, 0.5f, 0), 0.125f, TimeSamples.BatchCompressor);

            SceneCamera.Position = new Vector3(20, 10, 20);
            SceneCamera.Yaw = MathF.PI;
            SceneCamera.Pitch = 0;
            BufferPool = new BufferPool();
            ThreadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
            Characters = new CharacterControllers(BufferPool);
            WorldSimulation = Simulation.Create(BufferPool, new CharacterNarrowphaseCallbacks(Characters), new ScenePoseIntegratorCallbacks(new Vector3(0, -10, 0)));
            
            //Place a ground plane to walk on
            WorldSimulation.Statics.Add(new StaticDescription(new Vector3(0), new CollidableDescription(WorldSimulation.Shapes.Add(new Box(200, 1, 200)), 0.1f)));

            //Add character controller used to navigate and observe the current scene
            CharacterActive = true;
            Character = new CharacterInput(Characters, new Vector3(0, 2, -4), new Capsule(0.5f, 1), 0.1f, 1, 20, 100, 6, 4, MathF.PI * 0.4f);

            //Add some item pickups into the game world
            Vector3 ItemSpawnLocation = new Vector3(-10, 1, 2.5f);
            for (int i = 1; i < 5; i++)
            {
                ItemManager.AddNewItemPickup(i, WorldSimulation, ItemSpawnLocation);
                ItemSpawnLocation.X += 1;
            }

            //Setup text builder for rendering UI text components
            UIText = new TextBuilder(128);

            //Make sure the window size is correct relative to the current resolution
            OnResize(ApplicationWindow.Resolution);
        }

        //Allow the window to be resized
        public void OnResize(Int2 Resolution)
        {
            UpdatePerformanceGraphTimingMode(PerformanceDisplayMode);
        }

        //Allow changing the display mode for the performance monitor
        private void UpdatePerformanceGraphTimingMode(TimingDisplayMode NewDisplayMode)
        {
            PerformanceDisplayMode = NewDisplayMode;
            ref var Description = ref PerformanceGraph.Description;
            var Resolution = ApplicationWindow.Resolution;
            switch (PerformanceDisplayMode)
            {
                case TimingDisplayMode.Big:
                    {
                        const float Inset = 150;
                        Description.BodyMinimum = new Vector2(Inset);
                        Description.BodySpan = new Vector2(Resolution.X, Resolution.Y) - Description.BodyMinimum - new Vector2(Inset);
                        Description.LegendMinimum = Description.BodyMinimum - new Vector2(110, 0);
                        Description.TargetVerticalTickCount = 5;
                    }
                    break;
                case TimingDisplayMode.Regular:
                    {
                        const float Inset = 50;
                        var TargetSpan = new Vector2(400, 150);
                        Description.BodyMinimum = new Vector2(Resolution.X - TargetSpan.X - Inset, Inset);
                        Description.BodySpan = TargetSpan;
                        Description.LegendMinimum = Description.BodyMinimum - new Vector2(130, 0);
                        Description.TargetVerticalTickCount = 3;
                    }
                    break;
            }
            //In minimized state the graph is hidden
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
                {
                    var NewDisplayMode = (int)PerformanceDisplayMode + 1;
                    if (NewDisplayMode > 2)
                        NewDisplayMode = 0;
                    UpdatePerformanceGraphTimingMode((TimingDisplayMode)NewDisplayMode);
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

            //Count down timer until next send packets interval
            NextSendPacket -= DeltaTime;
            if (NextSendPacket <= 0.0f)
            {
                //Send all the queued network packets and reset the timer
                PacketSender.SendQueuedPackets();
                NextSendPacket = SendPacketInterval;
            }

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
            Log.DebugMessageWindow.RenderMessages(Renderer, UIText, UIFont, new Vector2(10, 25));
            Log.NetworkPackets.RenderMessages(Renderer, UIText, UIFont, new Vector2(10, 250));
            Log.SQLCommands.RenderMessages(Renderer, UIText, UIFont, new Vector2(10, 475));

            //Display the controls to the UI
            float TextHeight = 16;
            Vector2 TextPosition = new Vector2(Renderer.Surface.Resolution.X - 500, Renderer.Surface.Resolution.Y - TextHeight * 10);
            Renderer.TextBatcher.Write(UIText.Clear().Append("Toggle character: C"), TextPosition, TextHeight, new Vector3(1), UIFont);
            TextPosition.Y += TextHeight * 1.2f;
            Character.RenderControls(TextPosition, TextHeight, Renderer.TextBatcher, UIText, UIFont);

            ////Update the characters camera if its active
            if (CharacterActive)
                Character.UpdateCameraPosition(SceneCamera);

            //Render the performance graph to the UI
            if (PerformanceDisplayMode != TimingDisplayMode.Minimized)
                PerformanceGraph.Draw(UIText, Renderer.UILineBatcher, Renderer.TextBatcher);

            //Render all the shapes in the scene
            Renderer.Shapes.AddInstances(WorldSimulation, ThreadDispatcher);
            Renderer.Lines.Extract(WorldSimulation.Bodies, WorldSimulation.Solver, WorldSimulation.BroadPhase, ShowConstraints, ShowContacts, ShowBoundingBoxes, ThreadDispatcher);
        }
    }
}
