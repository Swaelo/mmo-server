// ================================================================================================================================
// File:        GameWorldScene.cs
// Description: 
// ================================================================================================================================

using BepuUtilities;
using ContentRenderer;
using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using System;
using BepuPhysics.CollisionDetection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using BepuPhysics.Constraints;
using ContentLoader;
using ServerUtilities;
using BepuUtilities.Memory;
using static BepuUtilities.GatherScatter;
using BepuUtilities.Collections;
using ContentRenderer.UI;
using OpenTK.Input;
using Server.Logic;
using Server.Networking;
using Server.Interface;

namespace Server.Scenes
{
    public class GameWorldScene : Scene
    {
        //Track time passes, send outgoing packets once per second
        private float SendPacketInterval = 1.0f;    //once per second
        private float NextSendPacket = 1.0f;    //time until next send packets instruction

        //Simulation WorldSimulation;
        CharacterControllers characters;
        bool characterActive;
        CharacterInput character;
        void CreateCharacter(Vector3 position)
        {
            characterActive = true;
            character = new CharacterInput(characters, position, new Capsule(0.5f, 1), 0.1f, 1, 20, 100, 6, 4, MathF.PI * 0.4f);
        }

        public unsafe override void Initialize(ContentArchive content, Camera camera)
        {
            camera.Position = new Vector3(20, 10, 20);
            camera.Yaw = MathF.PI;
            camera.Pitch = 0;
            characters = new CharacterControllers(BufferPool);
            Simulation = Simulation.Create(BufferPool, new CharacterNarrowphaseCallbacks(characters), new ScenePoseIntegratorCallbacks(new Vector3(0, -10, 0)));

            CreateCharacter(new Vector3(0, 2, -4));
            //Prevent the character from falling into the void.
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0, 0), new CollidableDescription(Simulation.Shapes.Add(new Box(200, 1, 200)), 0.1f)));

            //AddLegoShapes();
            //AddSpinningBlades();
            //AddFrogModel(content);
            //AddSeeSaw();
        }

        private void AddLegoShapes()
        {
            //Create a bunch of legos to hurt your feet on.
            var random = new Random(5);
            var origin = new Vector3(-3f, 0.5f, 0);
            var spacing = new Vector3(0.5f, 0, -0.5f);
            for (int i = 0; i < 12; ++i)
            {
                for (int j = 0; j < 12; ++j)
                {
                    var position = origin + new Vector3(i, 0, j) * spacing;
                    var orientation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(0.0001f) + new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble())), 10 * (float)random.NextDouble());
                    var shape = new Box(0.1f + 0.3f * (float)random.NextDouble(), 0.1f + 0.3f * (float)random.NextDouble(), 0.1f + 0.3f * (float)random.NextDouble());
                    var collidable = new CollidableDescription(Simulation.Shapes.Add(shape), 0.1f);
                    shape.ComputeInertia(1, out var inertia);
                    var choice = (i + j) % 3;
                    switch (choice)
                    {
                        case 0:
                            Simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(position, orientation), inertia, collidable, new BodyActivityDescription(0.01f)));
                            break;
                        case 1:
                            Simulation.Bodies.Add(BodyDescription.CreateKinematic(new RigidPose(position, orientation), collidable, new BodyActivityDescription(0.01f)));
                            break;
                        case 2:
                            Simulation.Statics.Add(new StaticDescription(position, orientation, collidable));
                            break;

                    }
                }
            }
        }

        private void AddSpinningBlades()
        {
            //Add some spinning fans to get slapped by.
            var bladeDescription = BodyDescription.CreateConvexDynamic(new Vector3(), 3, Simulation.Shapes, new Box(10, 0.2f, 2));
            var bladeBaseDescription = BodyDescription.CreateConvexKinematic(new Vector3(), Simulation.Shapes, new Box(0.2f, 1, 0.2f));
            for (int i = 0; i < 3; ++i)
            {
                bladeBaseDescription.Pose.Position = new Vector3(-22, 1, i * 11);
                bladeDescription.Pose.Position = new Vector3(-22, 1.7f, i * 11);
                var baseHandle = Simulation.Bodies.Add(bladeBaseDescription);
                var bladeHandle = Simulation.Bodies.Add(bladeDescription);
                Simulation.Solver.Add(baseHandle, bladeHandle,
                    new Hinge
                    {
                        LocalHingeAxisA = Vector3.UnitY,
                        LocalHingeAxisB = Vector3.UnitY,
                        LocalOffsetA = new Vector3(0, 0.7f, 0),
                        LocalOffsetB = new Vector3(0, 0, 0),
                        SpringSettings = new SpringSettings(30, 1)
                    });
                Simulation.Solver.Add(baseHandle, bladeHandle,
                    new AngularAxisMotor
                    {
                        LocalAxisA = Vector3.UnitY,
                        TargetVelocity = (i + 1) * (i + 1) * (i + 1) * (i + 1) * 0.2f,
                        Settings = new MotorSettings(5 * (i + 1), 0.0001f)
                    });
            }
        }

        private void AddFrogModel(ContentArchive content)
        {
            //Include a giant newt to test character-newt behavior and to ensure thematic consistency.
            SceneMeshHelper.LoadModel(content, BufferPool, @"Content\newt.obj", new Vector3(15, 15, 15), out var newtMesh);
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, 0), new CollidableDescription(Simulation.Shapes.Add(newtMesh), 0.1f)));

            //Give the newt a tongue, I guess.
            var tongueBase = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(0, 8.4f, 24), default, default));
            var tongue = Simulation.Bodies.Add(BodyDescription.CreateConvexDynamic(new Vector3(0, 8.4f, 27.5f), 1, Simulation.Shapes, new Box(1, 0.1f, 6f)));
            Simulation.Solver.Add(tongueBase, tongue, new Hinge
            {
                LocalHingeAxisA = Vector3.UnitX,
                LocalHingeAxisB = Vector3.UnitX,
                LocalOffsetB = new Vector3(0, 0, -3f),
                SpringSettings = new SpringSettings(30, 1)
            });
            Simulation.Solver.Add(tongueBase, tongue, new AngularServo
            {
                TargetRelativeRotationLocalA = Quaternion.Identity,
                ServoSettings = ServoSettings.Default,
                SpringSettings = new SpringSettings(2, 0)
            });
        }

        private void AddSeeSaw()
        {
            //And a seesaw thing?
            var seesawBase = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(0, 1f, 34f), new CollidableDescription(Simulation.Shapes.Add(new Box(0.2f, 1, 0.2f)), 0.1f), new BodyActivityDescription(0.01f)));
            var seesaw = Simulation.Bodies.Add(BodyDescription.CreateConvexDynamic(new Vector3(0, 1.7f, 34f), 1, Simulation.Shapes, new Box(1, 0.1f, 6f)));
            Simulation.Solver.Add(seesawBase, seesaw, new Hinge
            {
                LocalHingeAxisA = Vector3.UnitX,
                LocalHingeAxisB = Vector3.UnitX,
                LocalOffsetA = new Vector3(0, 0.7f, 0),
                LocalOffsetB = new Vector3(0, 0, 0),
                SpringSettings = new SpringSettings(30, 1)
            });
            Simulation.Bodies.Add(BodyDescription.CreateConvexDynamic(new Vector3(0, 2.25f, 35.5f), 0.5f, Simulation.Shapes, new Box(1f, 1f, 1f)));
        }

        public override void Update(Window window, Camera camera, Input input, float dt)
        {
            //Count down timer until next interval to send all outgoing network packets
            NextSendPacket -= dt;
            if(NextSendPacket <= 0.0f)
            {
                //Send all queued packets and reset the timer
                PacketSender.SendQueuedPackets();
                NextSendPacket = SendPacketInterval;
            }

            if (input.WasPushed(Key.C))
            {
                if (characterActive)
                {
                    character.Dispose();
                    characterActive = false;
                }
                else
                {
                    CreateCharacter(camera.Position);
                }
            }
            if (characterActive)
            {
                character.UpdateCharacterGoals(input, camera);
            }
            base.Update(window, camera, input, dt);
        }

        public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
            Log.DebugMessageWindow.RenderMessages(renderer, text, font, new Vector2(10, 25));
            Log.IncomingPacketsWindow.RenderMessages(renderer, text, font, new Vector2(10, 225));
            Log.OutgoingPacketsWindow.RenderMessages(renderer, text, font, new Vector2(10, 425));

            float textHeight = 16;
            var position = new Vector2(renderer.Surface.Resolution.X - 500, renderer.Surface.Resolution.Y - textHeight * 10);
            renderer.TextBatcher.Write(text.Clear().Append("Toggle character: C"), position, textHeight, new Vector3(1), font);
            position.Y += textHeight * 1.2f;
            character.RenderControls(position, textHeight, renderer.TextBatcher, text, font);
            if (characterActive)
            {
                character.UpdateCameraPosition(camera);
            }
            base.Render(renderer, camera, input, text, font);
        }
    }
}
