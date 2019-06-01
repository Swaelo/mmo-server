// ================================================================================================================================
// File:        GameLoop.cs
// Description: Taken from the bepu demos
// ================================================================================================================================

using System;
using ServerUtilities;
using ContentRenderer;
using BepuUtilities;
using BepuUtilities.Memory;
using Server.Scenes;
using Server.World;

namespace Server.Logic
{
    public class GameLoop : IDisposable
    {
        public Window Window { get; private set; }
        public Input Input { get; private set; }
        public Camera Camera { get; private set; }
        public RenderSurface Surface { get; private set; }
        public Renderer Renderer { get; private set; }
        public GameWorld World { get; set; }
        public BufferPool Pool { get; } = new BufferPool();

        public GameLoop(Window window)
        {
            Window = window;
            Input = new Input(Window, Pool);

            Surface = new RenderSurface(window.Handle, window.Resolution, enableDeviceDebugLayer: true);
            Renderer = new Renderer(Surface);
            Camera = new Camera(window.Resolution.X / (float)window.Resolution.Y, (float)Math.PI / 3, .01f, 100000);
        }

        private void WorldUpdate(float DeltaTime)
        {
            Input.Start();

            World.UpdateWorld(DeltaTime);
            World.RenderWorld(Renderer);

            Renderer.Render(Camera);
            Surface.Present();
            Input.End();
        }

        public void Run(GameWorld World)
        {
            this.World = World;
            Window.Run(WorldUpdate, OnResize);
        }

        private void OnResize(Int2 resolution)
        {
            Renderer.Surface.Resize(resolution, false);
            Camera.AspectRatio = resolution.X / (float)resolution.Y;
            World.OnResize(resolution);
        }

        public void Dispose()
        {
            Input.Dispose();
            Renderer.Dispose();
            Pool.Clear();
        }
    }
}
