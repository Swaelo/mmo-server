// ================================================================================================================================
// File:        CharacterInput.cs
// Description: Taken from the bepu demos
// Author:      Bepu Entertainment https://www.github.com/bepu/
// ================================================================================================================================

using ContentRenderer;
using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using System;
using BepuPhysics.CollisionDetection;
using System.Runtime.CompilerServices;
using BepuPhysics.Constraints;
using ServerUtilities;
using ContentRenderer.UI;
using OpenTK.Input;

namespace Server.Logic
{
    public struct CharacterInput
    {
        int bodyHandle;
        CharacterControllers characters;
        float speed;
        Capsule shape;

        public CharacterInput(CharacterControllers characters, Vector3 initialPosition, Capsule shape,
            float speculativeMargin, float mass, float maximumHorizontalForce, float maximumVerticalGlueForce,
            float jumpVelocity, float speed, float maximumSlope = MathF.PI * 0.25f)
        {
            this.characters = characters;
            var shapeIndex = characters.Simulation.Shapes.Add(shape);

            bodyHandle = characters.Simulation.Bodies.Add(BodyDescription.CreateDynamic(initialPosition, new BodyInertia { InverseMass = 1f / mass }, new CollidableDescription(shapeIndex, speculativeMargin), new BodyActivityDescription(shape.Radius * 0.02f)));
            ref var character = ref characters.AllocateCharacter(bodyHandle, out var characterIndex);
            character.LocalUp = new Vector3(0, 1, 0);
            character.CosMaximumSlope = MathF.Cos(maximumSlope);
            character.JumpVelocity = jumpVelocity;
            character.MaximumVerticalForce = maximumVerticalGlueForce;
            character.MaximumHorizontalForce = maximumHorizontalForce;
            character.MinimumSupportDepth = shape.Radius * -0.01f;
            character.MinimumSupportContinuationDepth = -speculativeMargin;
            this.speed = speed;
            this.shape = shape;
        }

        static Key MoveForward = Key.W;
        static Key MoveBackward = Key.S;
        static Key MoveRight = Key.D;
        static Key MoveLeft = Key.A;
        static Key Sprint = Key.LShift;
        static Key Jump = Key.Space;
        static Key JumpAlternate = Key.BackSpace; //I have a weird keyboard.

        public void UpdateCharacterGoals(Input input, Camera camera)
        {
            Vector2 movementDirection = default;
            if (input.IsDown(MoveForward))
            {
                movementDirection = new Vector2(0, 1);
            }
            if (input.IsDown(MoveBackward))
            {
                movementDirection += new Vector2(0, -1);
            }
            if (input.IsDown(MoveLeft))
            {
                movementDirection += new Vector2(-1, 0);
            }
            if (input.IsDown(MoveRight))
            {
                movementDirection += new Vector2(1, 0);
            }
            var lengthSquared = movementDirection.LengthSquared();
            if (lengthSquared > 0)
            {
                movementDirection /= MathF.Sqrt(lengthSquared);
            }

            ref var character = ref characters.GetCharacterByBodyHandle(bodyHandle);
            character.TryJump = input.WasPushed(Jump) || input.WasPushed(JumpAlternate);
            var characterBody = new BodyReference(bodyHandle, characters.Simulation.Bodies);
            var newTargetVelocity = movementDirection * (input.IsDown(Sprint) ? speed * 1.75f : speed);
            var viewDirection = camera.Forward;
            //Modifying the character's raw data does not automatically wake the character up, so we do so explicitly if necessary.
            //If you don't explicitly wake the character up, it won't respond to the changed motion goals.
            //(You can also specify a negative deactivation threshold in the BodyActivityDescription to prevent the character from sleeping at all.)
            if (!characterBody.IsActive &&
                ((character.TryJump && character.Supported) ||
                newTargetVelocity != character.TargetVelocity ||
                (newTargetVelocity != Vector2.Zero && character.ViewDirection != viewDirection)))
            {
                characters.Simulation.Awakener.AwakenBody(character.BodyHandle);
            }
            character.TargetVelocity = newTargetVelocity;
            character.ViewDirection = viewDirection;
        }

        public void UpdateCameraPosition(Camera camera, float cameraBackwardOffsetScale = 4)
        {
            //We'll override the demo harness's camera control by attaching the camera to the character controller body.
            ref var character = ref characters.GetCharacterByBodyHandle(bodyHandle);
            var characterBody = new BodyReference(bodyHandle, characters.Simulation.Bodies);
            //Use a simple sorta-neck model so that when the camera looks down, the center of the screen sees past the character.
            //Makes mouselocked ray picking easier.
            camera.Position = characterBody.Pose.Position + new Vector3(0, shape.HalfLength, 0) +
                camera.Up * (shape.Radius * 1.2f) -
                camera.Forward * (shape.HalfLength + shape.Radius) * cameraBackwardOffsetScale;
        }

        void RenderControl(ref Vector2 position, float textHeight, string controlName, string controlValue, TextBuilder text, TextBatcher textBatcher, Font font)
        {
            text.Clear().Append(controlName).Append(": ").Append(controlValue);
            textBatcher.Write(text, position, textHeight, new Vector3(1), font);
            position.Y += textHeight * 1.1f;
        }
        public void RenderControls(Vector2 position, float textHeight, TextBatcher textBatcher, TextBuilder text, Font font)
        {
            RenderControl(ref position, textHeight, nameof(MoveForward), MoveForward.ToString(), text, textBatcher, font);
            RenderControl(ref position, textHeight, nameof(MoveBackward), MoveBackward.ToString(), text, textBatcher, font);
            RenderControl(ref position, textHeight, nameof(MoveRight), MoveRight.ToString(), text, textBatcher, font);
            RenderControl(ref position, textHeight, nameof(MoveLeft), MoveLeft.ToString(), text, textBatcher, font);
            RenderControl(ref position, textHeight, nameof(Sprint), Sprint.ToString(), text, textBatcher, font);
            RenderControl(ref position, textHeight, nameof(Jump), Jump.ToString(), text, textBatcher, font);
        }


        /// <summary>
        /// Removes the character's body from the simulation and the character from the associated characters set.
        /// </summary>
        public void Dispose()
        {
            characters.Simulation.Shapes.Remove(new BodyReference(bodyHandle, characters.Simulation.Bodies).Collidable.Shape);
            characters.Simulation.Bodies.Remove(bodyHandle);
            characters.RemoveCharacterByBodyHandle(bodyHandle);
        }
    }

    public struct CharacterNarrowphaseCallbacks : INarrowPhaseCallbacks
    {
        public CharacterControllers Characters;

        public CharacterNarrowphaseCallbacks(CharacterControllers characters)
        {
            Characters = characters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetMaterial(out PairMaterialProperties pairMaterial)
        {
            pairMaterial = new PairMaterialProperties { FrictionCoefficient = 1, MaximumRecoveryVelocity = 2, SpringSettings = new SpringSettings(30, 1) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, ConvexContactManifold* manifold, out PairMaterialProperties pairMaterial)
        {
            GetMaterial(out pairMaterial);
            Characters.TryReportContacts(pair, ref *manifold, workerIndex, ref pairMaterial);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, NonconvexContactManifold* manifold, out PairMaterialProperties pairMaterial)
        {
            GetMaterial(out pairMaterial);
            Characters.TryReportContacts(pair, ref *manifold, workerIndex, ref pairMaterial);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ConvexContactManifold* manifold)
        {
            return true;
        }

        public void Dispose()
        {
            Characters.Dispose();
        }

        public void Initialize(Simulation simulation)
        {
            Characters.Initialize(simulation);
        }
    }
}
