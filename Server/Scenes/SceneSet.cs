// ================================================================================================================================
// File:        SceneSet.cs
// Description: Maintains a list of game scenes that can be swapped between during runtime, taken from the bepu demos
// ================================================================================================================================

using ContentLoader;
using ContentRenderer;
using System;
using System.Collections.Generic;

namespace Server.Scenes
{
    public class SceneSet
    {
        struct Option
        {
            public string Name;
            public Func<ContentArchive, Camera, Scene> Builder;
        }

        List<Option> options = new List<Option>();

        void AddOption<T>() where T : Scene, new()
        {
            options.Add(new Option
            {
                Builder = (content, camera) =>
                {
                    var scene = new T();
                    scene.Initialize(content, camera);
                    return scene;
                },
                Name = typeof(T).Name
            });
        }

        public SceneSet()
        {
            AddOption<GameWorldScene>();
        }

        public int Count { get { return options.Count; } }

        public string GetName(int index)
        {
            return options[index].Name;
        }

        public Scene Build(int index, ContentArchive content, Camera camera)
        {
            return options[index].Builder(content, camera);
        }
    }
}
