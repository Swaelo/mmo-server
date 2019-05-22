// ================================================================================================================================
// File:        GameLoop.cs
// Description: 
// ================================================================================================================================

using System;
using System.Text;
using System.Collections.Generic;
using ServerUtilities;
using ContentRenderer;
using BepuUtilities;
using BepuUtilities.Memory;
using OpenTK;
using Server.Scenes;

namespace Server.Logic
{
    public class GameLoop : IDisposable
    {
        public Window Window { get; private set; }
        public Input Input { get; private set; }
        public Camera Camera { get; private set; }
        public RenderSurface Surface { get; private set; }
        public Renderer Renderer { get; private set; }
        public SceneHarness SceneHarness { get; set; }
        public BufferPool Pool { get; } = new BufferPool();

        public GameLoop(Window window)
        {
            Window = window;
            Input = new Input(window, Pool);

            Surface = new RenderSurface(window.Handle, window.Resolution, enableDeviceDebugLayer: true);
            Renderer = new Renderer(Surface);
            Camera = new Camera(window.Resolution.X / (float)window.Resolution.Y, (float)Math.PI / 3, .01f, 100000);
        }

        void Update(float DeltaTime)
        {
            Input.Start();
            if(SceneHarness != null)
            {
                SceneHarness.Update(DeltaTime);
                SceneHarness.Render(Renderer);
            }
            Renderer.Render(Camera);
            Surface.Present();
            Input.End();
        }

        public void Run(SceneHarness harness)
        {
            SceneHarness = harness;
            Window.Run(Update, OnResize);
        }

        private void OnResize(Int2 resolution)
        {
            Renderer.Surface.Resize(resolution, false);
            Camera.AspectRatio = resolution.X / (float)resolution.Y;
            SceneHarness?.OnResize(resolution);
        }

        public void Dispose()
        {
            Input.Dispose();
            Renderer.Dispose();
            Pool.Clear();
        }
    }
}
