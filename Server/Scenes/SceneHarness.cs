// ================================================================================================================================
// File:        SceneHarness.cs
// Description: Maintains current scene, and handles changing between different scenes during runtime, taken from the bepu demos
// ================================================================================================================================

using System;
using System.Numerics;
using BepuUtilities;
using Quaternion = BepuUtilities.Quaternion;
using ContentLoader;
using ContentRenderer;
using ContentRenderer.UI;
using ServerUtilities;
using Server.Logic;

namespace Server.Scenes
{
    public class SceneHarness : IDisposable
    {
        Window window;
        ContentArchive content;
        internal Input input;
        Camera camera;
        Grabber grabber;
        internal Controls controls;
        Font font;

        bool showControls;
        bool showConstraints = true;
        bool showContacts;
        bool showBoundingBoxes;
        int frameCount;

        enum TimingDisplayMode
        {
            Regular,
            Big,
            Minimized
        }

        TimingDisplayMode timingDisplayMode;
        Graph timingGraph;

        SceneSwapper swapper;
        internal SceneSet sceneSet;
        public static Scene CurrentScene;
        public static GameWorldScene GameScene;
        internal void TryChangeToScene(int sceneIndex)
        {
            if(sceneIndex >= 0 && sceneIndex < sceneSet.Count)
            {
                CurrentScene.Dispose();
                CurrentScene = sceneSet.Build(sceneIndex, content, camera);
                GC.Collect(int.MaxValue, GCCollectionMode.Forced, true, true);
            }
        }

        SimulationTimeSamples timeSamples;

        public SceneHarness(GameLoop loop, ContentArchive content,
            Controls? controls = null)
        {
            this.window = loop.Window;
            this.input = loop.Input;
            this.camera = loop.Camera;
            this.content = content;
            timeSamples = new SimulationTimeSamples(512, loop.Pool);
            if (controls == null)
                this.controls = Controls.Default;

            var fontContent = content.Load<FontContent>(@"Content\Carlito-Regular.ttf");
            font = new Font(loop.Surface.Device, loop.Surface.Context, fontContent);

            timingGraph = new Graph(new GraphDescription
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
                Font = font,

                LineSpacingMultiplier = 1f,

                ForceVerticalAxisMinimumToZero = true
            });
            timingGraph.AddSeries("Total", new Vector3(1, 1, 1), 0.75f, timeSamples.Simulation);
            timingGraph.AddSeries("Pose Integrator", new Vector3(0, 0, 1), 0.25f, timeSamples.PoseIntegrator);
            timingGraph.AddSeries("Sleeper", new Vector3(0.5f, 0, 1), 0.25f, timeSamples.Sleeper);
            timingGraph.AddSeries("Broad Update", new Vector3(1, 1, 0), 0.25f, timeSamples.BroadPhaseUpdate);
            timingGraph.AddSeries("Collision Test", new Vector3(0, 1, 0), 0.25f, timeSamples.CollisionTesting);
            timingGraph.AddSeries("Narrow Flush", new Vector3(1, 0, 1), 0.25f, timeSamples.NarrowPhaseFlush);
            timingGraph.AddSeries("Solver", new Vector3(1, 0, 0), 0.5f, timeSamples.Solver);

            timingGraph.AddSeries("Body Opt", new Vector3(1, 0.5f, 0), 0.125f, timeSamples.BodyOptimizer);
            timingGraph.AddSeries("Constraint Opt", new Vector3(0, 0.5f, 1), 0.125f, timeSamples.ConstraintOptimizer);
            timingGraph.AddSeries("Batch Compress", new Vector3(0, 0.5f, 0), 0.125f, timeSamples.BatchCompressor);

            sceneSet = new SceneSet();
            CurrentScene = sceneSet.Build(0, content, camera);
            GameScene = (GameWorldScene)CurrentScene;

            OnResize(window.Resolution);
        }

        private void UpdateTimingGraphForMode(TimingDisplayMode newDisplayMode)
        {
            timingDisplayMode = newDisplayMode;
            ref var description = ref timingGraph.Description;
            var resolution = window.Resolution;
            switch (timingDisplayMode)
            {
                case TimingDisplayMode.Big:
                    {
                        const float inset = 150;
                        description.BodyMinimum = new Vector2(inset);
                        description.BodySpan = new Vector2(resolution.X, resolution.Y) - description.BodyMinimum - new Vector2(inset);
                        description.LegendMinimum = description.BodyMinimum - new Vector2(110, 0);
                        description.TargetVerticalTickCount = 5;
                    }
                    break;
                case TimingDisplayMode.Regular:
                    {
                        const float inset = 50;
                        var targetSpan = new Vector2(400, 150);
                        description.BodyMinimum = new Vector2(resolution.X - targetSpan.X - inset, inset);
                        description.BodySpan = targetSpan;
                        description.LegendMinimum = description.BodyMinimum - new Vector2(130, 0);
                        description.TargetVerticalTickCount = 3;
                    }
                    break;
            }
            //In a minimized state, the graph is just not drawn.
        }

        public void OnResize(Int2 resolution)
        {
            UpdateTimingGraphForMode(timingDisplayMode);
        }

        enum CameraMoveSpeedState
        {
            Regular,
            Slow,
            Fast
        }
        CameraMoveSpeedState cameraSpeedState;
        Int2? grabberCachedMousePosition;

        public void Update(float dt)
        {
            //Don't bother responding to input if the window isn't focused.
            if (window.Focused)
            {
                if (controls.Exit.WasTriggered(input))
                {
                    window.Close();
                    return;
                }

                if (controls.MoveFaster.WasTriggered(input))
                {
                    switch (cameraSpeedState)
                    {
                        case CameraMoveSpeedState.Slow:
                            cameraSpeedState = CameraMoveSpeedState.Regular;
                            break;
                        case CameraMoveSpeedState.Regular:
                            cameraSpeedState = CameraMoveSpeedState.Fast;
                            break;
                    }
                }
                if (controls.MoveSlower.WasTriggered(input))
                {
                    switch (cameraSpeedState)
                    {
                        case CameraMoveSpeedState.Regular:
                            cameraSpeedState = CameraMoveSpeedState.Slow;
                            break;
                        case CameraMoveSpeedState.Fast:
                            cameraSpeedState = CameraMoveSpeedState.Regular;
                            break;
                    }
                }

                var cameraOffset = new Vector3();
                if (controls.MoveForward.IsDown(input))
                    cameraOffset += camera.Forward;
                if (controls.MoveBackward.IsDown(input))
                    cameraOffset += camera.Backward;
                if (controls.MoveLeft.IsDown(input))
                    cameraOffset += camera.Left;
                if (controls.MoveRight.IsDown(input))
                    cameraOffset += camera.Right;
                if (controls.MoveUp.IsDown(input))
                    cameraOffset += camera.Up;
                if (controls.MoveDown.IsDown(input))
                    cameraOffset += camera.Down;
                var length = cameraOffset.Length();

                if (length > 1e-7f)
                {
                    float cameraMoveSpeed;
                    switch (cameraSpeedState)
                    {
                        case CameraMoveSpeedState.Slow:
                            cameraMoveSpeed = controls.CameraSlowMoveSpeed;
                            break;
                        case CameraMoveSpeedState.Fast:
                            cameraMoveSpeed = controls.CameraFastMoveSpeed;
                            break;
                        default:
                            cameraMoveSpeed = controls.CameraMoveSpeed;
                            break;
                    }
                    cameraOffset *= dt * cameraMoveSpeed / length;
                }
                else
                    cameraOffset = new Vector3();
                camera.Position += cameraOffset;

                var grabRotationIsActive = controls.Grab.IsDown(input) && controls.GrabRotate.IsDown(input);

                //Don't turn the camera while rotating a grabbed object.
                if (!grabRotationIsActive)
                {
                    if (input.MouseLocked)
                    {
                        var delta = input.MouseDelta;
                        if (delta.X != 0 || delta.Y != 0)
                        {
                            camera.Yaw += delta.X * controls.MouseSensitivity;
                            camera.Pitch += delta.Y * controls.MouseSensitivity;
                        }
                    }
                }
                if (controls.LockMouse.WasTriggered(input))
                {
                    input.MouseLocked = !input.MouseLocked;
                }

                Quaternion incrementalGrabRotation;
                if (grabRotationIsActive)
                {
                    if (grabberCachedMousePosition == null)
                        grabberCachedMousePosition = input.MousePosition;
                    var delta = input.MouseDelta;
                    var yaw = delta.X * controls.MouseSensitivity;
                    var pitch = delta.Y * controls.MouseSensitivity;
                    incrementalGrabRotation = Quaternion.Concatenate(Quaternion.CreateFromAxisAngle(camera.Right, pitch), Quaternion.CreateFromAxisAngle(camera.Up, yaw));
                    if (!input.MouseLocked)
                    {
                        //Undo the mouse movement if we're in freemouse mode.
                        input.MousePosition = grabberCachedMousePosition.Value;
                    }
                }
                else
                {
                    incrementalGrabRotation = Quaternion.Identity;
                    grabberCachedMousePosition = null;
                }
                grabber.Update(CurrentScene.Simulation, camera, input.MouseLocked, controls.Grab.IsDown(input), incrementalGrabRotation, window.GetNormalizedMousePosition(input.MousePosition));



                if (controls.ShowControls.WasTriggered(input))
                {
                    showControls = !showControls;
                }

                if (controls.ShowConstraints.WasTriggered(input))
                {
                    showConstraints = !showConstraints;
                }
                if (controls.ShowContacts.WasTriggered(input))
                {
                    showContacts = !showContacts;
                }
                if (controls.ShowBoundingBoxes.WasTriggered(input))
                {
                    showBoundingBoxes = !showBoundingBoxes;
                }
                if (controls.ChangeTimingDisplayMode.WasTriggered(input))
                {
                    var newDisplayMode = (int)timingDisplayMode + 1;
                    if (newDisplayMode > 2)
                        newDisplayMode = 0;
                    UpdateTimingGraphForMode((TimingDisplayMode)newDisplayMode);
                }
                swapper.CheckForSceneSwap(this);
            }
            else
            {
                input.MouseLocked = false;
            }
            ++frameCount;
            if (!controls.SlowTimesteps.IsDown(input) || frameCount % 20 == 0)
            {
                CurrentScene.Update(window, camera, input, dt);
            }
            timeSamples.RecordFrame(CurrentScene.Simulation);
        }

        TextBuilder uiText = new TextBuilder(128);

        public void Render(Renderer renderer)
        {
            //Clear first so that any demo-specific logic doesn't get lost.
            renderer.Shapes.ClearInstances();
            renderer.Lines.ClearInstances();

            CurrentScene.Render(renderer, camera, input, uiText, font);

            float textHeight = 16;
            float lineSpacing = textHeight * 1.0f;
            var textColor = new Vector3(1, 1, 1);
            if (showControls)
            {
                var penPosition = new Vector2(window.Resolution.X - textHeight * 6 - 25, window.Resolution.Y - 25);
                penPosition.Y -= 19 * lineSpacing;
                uiText.Clear().Append("Controls: ");
                var headerHeight = textHeight * 1.2f;
                renderer.TextBatcher.Write(uiText, penPosition - new Vector2(0.5f * GlyphBatch.MeasureLength(uiText, font, headerHeight), 0), headerHeight, textColor, font);
                penPosition.Y += lineSpacing;

                var controlPosition = penPosition;
                controlPosition.X += textHeight * 0.5f;

                void WriteName(string controlName, string control)
                {
                    uiText.Clear().Append(controlName).Append(":");
                    renderer.TextBatcher.Write(uiText, penPosition - new Vector2(GlyphBatch.MeasureLength(uiText, font, textHeight), 0), textHeight, textColor, font);
                    penPosition.Y += lineSpacing;

                    uiText.Clear().Append(control);
                    renderer.TextBatcher.Write(uiText, controlPosition, textHeight, textColor, font);
                    controlPosition.Y += lineSpacing;
                }

                //Conveniently, enum strings are cached. Every (Key).ToString() returns the same reference for the same key, so no garbage worries.
                WriteName(nameof(controls.LockMouse), controls.LockMouse.ToString());
                WriteName(nameof(controls.Grab), controls.Grab.ToString());
                WriteName(nameof(controls.GrabRotate), controls.GrabRotate.ToString());
                WriteName(nameof(controls.MoveForward), controls.MoveForward.ToString());
                WriteName(nameof(controls.MoveBackward), controls.MoveBackward.ToString());
                WriteName(nameof(controls.MoveLeft), controls.MoveLeft.ToString());
                WriteName(nameof(controls.MoveRight), controls.MoveRight.ToString());
                WriteName(nameof(controls.MoveUp), controls.MoveUp.ToString());
                WriteName(nameof(controls.MoveDown), controls.MoveDown.ToString());
                WriteName(nameof(controls.MoveSlower), controls.MoveSlower.ToString());
                WriteName(nameof(controls.MoveFaster), controls.MoveFaster.ToString());
                WriteName(nameof(controls.SlowTimesteps), controls.SlowTimesteps.ToString());
                WriteName(nameof(controls.Exit), controls.Exit.ToString());
                WriteName(nameof(controls.ShowConstraints), controls.ShowConstraints.ToString());
                WriteName(nameof(controls.ShowContacts), controls.ShowContacts.ToString());
                WriteName(nameof(controls.ShowBoundingBoxes), controls.ShowBoundingBoxes.ToString());
                WriteName(nameof(controls.ChangeTimingDisplayMode), controls.ChangeTimingDisplayMode.ToString());
                
                WriteName(nameof(controls.ChangeScene), controls.ChangeScene.ToString());
                WriteName(nameof(controls.ShowControls), controls.ShowControls.ToString());
            }
            else
            {
                uiText.Clear().Append("Press ").Append(controls.ShowControls.ToString()).Append(" for controls.");
                const float inset = 25;
                renderer.TextBatcher.Write(uiText,
                    new Vector2(window.Resolution.X - inset - GlyphBatch.MeasureLength(uiText, font, textHeight), window.Resolution.Y - inset),
                    textHeight, textColor, font);
            }

            swapper.Draw(uiText, renderer.TextBatcher, sceneSet, new Vector2(16, 16), textHeight, textColor, font);

            if (timingDisplayMode != TimingDisplayMode.Minimized)
            {
                timingGraph.Draw(uiText, renderer.UILineBatcher, renderer.TextBatcher);
            }
            else
            {
                const float timingTextSize = 14;
                const float inset = 25;
                renderer.TextBatcher.Write(
                    uiText.Clear().Append(1e3 * timeSamples.Simulation[timeSamples.Simulation.End - 1], timingGraph.Description.VerticalIntervalLabelRounding).Append(" ms/step"),
                    new Vector2(window.Resolution.X - inset - GlyphBatch.MeasureLength(uiText, font, timingTextSize), inset), timingTextSize, timingGraph.Description.TextColor, font);
            }
            grabber.Draw(renderer.Lines, camera, input.MouseLocked, controls.Grab.IsDown(input), window.GetNormalizedMousePosition(input.MousePosition));
            renderer.Shapes.AddInstances(CurrentScene.Simulation, CurrentScene.ThreadDispatcher);
            renderer.Lines.Extract(CurrentScene.Simulation.Bodies, CurrentScene.Simulation.Solver, CurrentScene.Simulation.BroadPhase, showConstraints, showContacts, showBoundingBoxes, CurrentScene.ThreadDispatcher);
        }

        public void Dispose()
        {
            CurrentScene?.Dispose();
            timeSamples.Dispose();
        }
    }
}
