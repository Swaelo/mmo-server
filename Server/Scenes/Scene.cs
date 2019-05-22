// ================================================================================================================================
// File:        Scene.cs
// Description: 
// ================================================================================================================================

using BepuUtilities.Memory;
using ContentRenderer;
using ServerUtilities;
using BepuPhysics;
using System;
using System.Collections.Generic;
using System.Text;
using ContentRenderer.UI;
using ContentLoader;
using Server.Logic;

namespace Server.Scenes
{
    public abstract class Scene : IDisposable
    {
        public Simulation Simulation { get; protected set; }

        public BufferPool BufferPool { get; private set; }
        
        public SimpleThreadDispatcher ThreadDispatcher { get; private set; }

        protected Scene()
        {
            BufferPool = new BufferPool();
            ThreadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
        }

        public abstract void Initialize(ContentArchive content, Camera camera);

        public virtual void Update(Window window, Camera camera, Input input, float dt)
        {
            Simulation.Timestep(1 / 60f, ThreadDispatcher);
        }

        public virtual void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {

        }

        protected virtual void OnDispose()
        {

        }

        public void Dispose()
        {
            OnDispose();
            Simulation.Dispose();
            BufferPool.Clear();
            ThreadDispatcher.Dispose();
        }
    }
}
