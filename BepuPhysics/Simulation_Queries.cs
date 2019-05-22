﻿using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace BepuPhysics
{
    public interface ISweepHitHandler
    {
        /// <summary>
        /// Checks whether to run a detailed sweep test against a target collidable.
        /// </summary>
        /// <param name="collidable">Collidable to check.</param>
        /// <returns>True if the sweep test should be attempted, false otherwise.</returns>
        bool AllowTest(CollidableReference collidable);
        /// <summary>
        /// Checks whether to run a detailed sweep test against a target collidable's child.
        /// </summary>
        /// <param name="collidable">Collidable to check.</param>
        /// <param name="child">Index of the child in the collidable to check.</param>
        /// <returns>True if the sweep test should be attempted, false otherwise.</returns>
        bool AllowTest(CollidableReference collidable, int child);
        /// <summary>
        /// Called when a swep test detects a hit with nonzero T value.
        /// </summary>
        /// <param name="maximumT">Reference to maximumT passed to the traversal.</param>
        /// <param name="t">Impact time of the sweep test.</param>
        /// <param name="hitLocation">Location of the first hit detected by the sweep.</param>
        /// <param name="hitNormal">Surface normal at the hit location.</param>
        /// <param name="collidable">Collidable hit by the traversal.</param>
        void OnHit(ref float maximumT, float t, in Vector3 hitLocation, in Vector3 hitNormal, CollidableReference collidable);
        /// <summary>
        /// Called when a swept test detects a hit at T = 0, meaning that no location or normal can be computed.
        /// </summary>
        /// <param name="maximumT">Reference to maximumT passed to the traversal.</param>
        /// <param name="collidable">Collidable hit by the traversal.</param>
        void OnHitAtZeroT(ref float maximumT, CollidableReference collidable);
    }

    partial class Simulation
    {
        //TODO: This is all sensitive to pose precision. If you change broadphase or pose precision, this will have to change.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void GetPoseAndShape(CollidableReference reference, out RigidPose* pose, out TypedIndex shape)
        {
            if (reference.Mobility == CollidableMobility.Static)
            {
                var index = Statics.HandleToIndex[reference.Handle];
                pose = (RigidPose*)Statics.Poses.Memory + index;
                shape = Statics.Collidables[index].Shape;
            }
            else
            {
                ref var location = ref Bodies.HandleToLocation[reference.Handle];
                ref var set = ref Bodies.Sets[location.SetIndex];
                pose = (RigidPose*)set.Poses.Memory + location.Index;
                shape = set.Collidables[location.Index].Shape;
            }
        }
        struct RayHitDispatcher<TRayHitHandler> : IBroadPhaseRayTester where TRayHitHandler : IRayHitHandler
        {
            public Simulation Simulation;
            public TRayHitHandler HitHandler;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void RayTest(CollidableReference reference, RayData* rayData, float* maximumT)
            {
                if (HitHandler.AllowTest(reference))
                {
                    Simulation.GetPoseAndShape(reference, out var pose, out var shape);
                    if (Simulation.Shapes[shape.Type].RayTest(shape.Index, *pose, rayData->Origin, rayData->Direction, *maximumT, out var t, out var normal) && t < *maximumT)
                    {
                        HitHandler.OnRayHit(*rayData, ref *maximumT, t, normal, reference);
                    }
                }
            }
        }

        /// <summary>
        /// Intersects a ray against the simulation.
        /// </summary>
        /// <typeparam name="THitHandler">Type of the callbacks to execute on ray-object intersections.</typeparam>
        /// <param name="origin">Origin of the ray to cast.</param>
        /// <param name="direction">Direction of the ray to cast.</param>
        /// <param name="maximumT">Maximum length of the ray traversal in units of the direction's length.</param>
        /// <param name="hitHandler">callbacks to execute on ray-object intersections.</param>
        /// <param name="id">User specified id of the ray.</param>
        public unsafe void RayCast<THitHandler>(in Vector3 origin, in Vector3 direction, float maximumT, ref THitHandler hitHandler, int id = 0) where THitHandler : IRayHitHandler
        {
            TreeRay.CreateFrom(origin, direction, maximumT, id, out var rayData, out var treeRay);
            RayHitDispatcher<THitHandler> dispatcher;
            dispatcher.HitHandler = hitHandler;
            dispatcher.Simulation = this;
            BroadPhase.RayCast(origin, direction, maximumT, ref dispatcher, id);
            //The hit handler was copied to pass it into the child processing; since the user may (and probably does) rely on mutations, copy it back to the original reference.
            hitHandler = dispatcher.HitHandler;
        }

        unsafe struct SweepHitDispatcher<TSweepHitHandler> : IBroadPhaseSweepTester, ISweepFilter where TSweepHitHandler : ISweepHitHandler
        {
            public Simulation Simulation;
            public BufferPool Pool;
            public void* ShapeData;
            public int ShapeType;
            public RigidPose Pose;
            public BodyVelocity Velocity;
            public TSweepHitHandler HitHandler;
            public CollidableReference CollidableBeingTested;
            public float MinimumProgression;
            public float ConvergenceThreshold;
            public int MaximumIterationCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowTest(int childA, int childB)
            {
                //Note that the simulation sweep does not permit nonconvex sweeps, so we don't need to worry about childA.
                return HitHandler.AllowTest(CollidableBeingTested, childB);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void Test(CollidableReference reference, ref float maximumT)
            {
                if (HitHandler.AllowTest(reference))
                {
                    Simulation.GetPoseAndShape(reference, out var targetPose, out var shape);
                    Simulation.Shapes[shape.Type].GetShapeData(shape.Index, out var targetShapeData, out _);
                    //Note that the velocity of the target shape is treated as zero for the purposes of a simulation wide cast.
                    //If you wanted to create a simulation velocity aware sweep, you would want to pull the velocity of the target from the Bodies set for collidable references
                    //that are associated with non-statics. It would look like this:
                    //BodyVelocity targetVelocity;
                    //if (reference.Mobility != CollidableMobility.Static)
                    //{
                    //    ref var location = ref Simulation.Bodies.HandleToLocation[reference.Handle];
                    //    //If the body is inactive, even though they can have small nonzero velocities, you probably should treat it as having zero velocity.
                    //    //Otherwise, you might get some unintuitive results where the sweep integrated the inactive body's velocity forward, but the simulation didn't.
                    //    if (location.SetIndex == 0)
                    //        targetVelocity = Simulation.Bodies.ActiveSet.Velocities[location.Index];
                    //    else
                    //        targetVelocity = new BodyVelocity();
                    //}
                    //else
                    //{
                    //    targetVelocity = new BodyVelocity();
                    //}
                    CollidableBeingTested = reference;
                    var task = Simulation.NarrowPhase.SweepTaskRegistry.GetTask(ShapeType, shape.Type);
                    if (task != null)
                    {
                        var result = task.Sweep(
                            ShapeData, ShapeType, Pose.Orientation, Velocity,
                            targetShapeData, shape.Type, targetPose->Position - Pose.Position, targetPose->Orientation, new BodyVelocity(),
                            maximumT, MinimumProgression, ConvergenceThreshold, MaximumIterationCount,
                            ref this, Simulation.Shapes, Simulation.NarrowPhase.SweepTaskRegistry, Pool, out var t0, out var t1, out var hitLocation, out var hitNormal);
                        if (result)
                        {
                            if (t1 > 0)
                            {
                                hitLocation += Pose.Position;
                                HitHandler.OnHit(ref maximumT, t1, hitLocation, hitNormal, reference);
                            }
                            else
                            {
                                //At t1 == 0, hitLocation and hitNormal do not have valid values, so don't imply that they exist.
                                HitHandler.OnHitAtZeroT(ref maximumT, reference);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Sweeps a shape against the simulation.
        /// </summary>
        /// <typeparam name="TShape">Type of the shape to sweep.</typeparam>
        /// <typeparam name="TSweepHitHandler">Type of the callbacks executed when a sweep impacts an object in the scene.</typeparam>
        /// <param name="shape">Shape to sweep.</param>
        /// <param name="pose">Starting pose of the sweep.</param>
        /// <param name="velocity">Velocity of the swept shape.</param>
        /// <param name="maximumT">Maximum length of the sweep in units of time used to integrate the velocity.</param>
        /// <param name="pool">Pool to allocate any temporary resources in during execution.</param>
        /// <param name="hitHandler">Callbacks executed when a sweep impacts an object in the scene.</param>
        /// <remarks>Simulation objects are treated as stationary during the sweep.</remarks>
        /// <param name="minimumProgression">Minimum amount of progress in terms of t parameter that any iterative sweep tests should make for each sample.</param>
        /// <param name="convergenceThreshold">Threshold in terms of t parameter under which iterative sweep tests are permitted to exit in collision.</param>
        /// <param name="maximumIterationCount">Maximum number of iterations to use in iterative sweep tests.</param>
        public unsafe void Sweep<TShape, TSweepHitHandler>(TShape shape, in RigidPose pose, in BodyVelocity velocity, float maximumT, BufferPool pool, ref TSweepHitHandler hitHandler,
            float minimumProgression, float convergenceThreshold, int maximumIterationCount)
            where TShape : IConvexShape where TSweepHitHandler : ISweepHitHandler
        {
            //Build a bounding box.
            shape.ComputeAngularExpansionData(out var maximumRadius, out var maximumAngularExpansion);
            shape.ComputeBounds(pose.Orientation, out var min, out var max);
            BoundingBoxHelpers.GetAngularBoundsExpansion(velocity.Angular, maximumT, maximumRadius, maximumAngularExpansion, out var angularExpansion);
            min = min - angularExpansion + pose.Position;
            max = max + angularExpansion + pose.Position;
            var direction = velocity.Linear;
            SweepHitDispatcher<TSweepHitHandler> dispatcher;
            dispatcher.HitHandler = hitHandler;
            dispatcher.Pose = pose;
            dispatcher.Velocity = velocity;
            //Note that the shape was passed by copy, and that all shape types are required to be blittable. No GC hole.
            dispatcher.ShapeData = Unsafe.AsPointer(ref shape);
            dispatcher.ShapeType = shape.TypeId;
            dispatcher.Simulation = this;
            dispatcher.Pool = pool;
            dispatcher.CollidableBeingTested = default;
            dispatcher.MinimumProgression = minimumProgression;
            dispatcher.ConvergenceThreshold = convergenceThreshold;
            dispatcher.MaximumIterationCount = maximumIterationCount;
            BroadPhase.Sweep(min, max, direction, maximumT, ref dispatcher);
            //The hit handler was copied to pass it into the child processing; since the user may (and probably does) rely on mutations, copy it back to the original reference.
            hitHandler = dispatcher.HitHandler;
        }

        /// <summary>
        /// Sweeps a shape against the simulation.
        /// </summary>
        /// <typeparam name="TShape">Type of the shape to sweep.</typeparam>
        /// <typeparam name="TSweepHitHandler">Type of the callbacks executed when a sweep impacts an object in the scene.</typeparam>
        /// <param name="shape">Shape to sweep.</param>
        /// <param name="pose">Starting pose of the sweep.</param>
        /// <param name="velocity">Velocity of the swept shape.</param>
        /// <param name="maximumT">Maximum length of the sweep in units of time used to integrate the velocity.</param>
        /// <param name="pool">Pool to allocate any temporary resources in during execution.</param>
        /// <param name="hitHandler">Callbacks executed when a sweep impacts an object in the scene.</param>
        /// <remarks>Simulation objects are treated as stationary during the sweep.</remarks>
        public unsafe void Sweep<TShape, TSweepHitHandler>(in TShape shape, in RigidPose pose, in BodyVelocity velocity, float maximumT, BufferPool pool, ref TSweepHitHandler hitHandler)
            where TShape : IConvexShape where TSweepHitHandler : ISweepHitHandler
        {
            //Estimate some reasonable termination conditions for iterative sweeps based on the input shape size.
            shape.ComputeAngularExpansionData(out var maximumRadius, out var maximumAngularExpansion);
            var minimumRadius = maximumRadius - maximumAngularExpansion;
            var sizeEstimate = Math.Max(minimumRadius, maximumRadius * 0.25f);
            //By default, lean towards precision. This may often trip the maximum iteration count, but that's okay. Performance sensitive users can tune it down with the other overload.
            //It would be far more disconcerting for new users to use a 'fast' default tuning and get visibly incorrect results.
            var minimumProgressionDistance = .1f * sizeEstimate;
            var convergenceThresholdDistance = 1e-5f * sizeEstimate;
            var tangentVelocity = Math.Min(velocity.Angular.Length() * maximumRadius, maximumAngularExpansion / maximumT);
            var inverseVelocity = 1f / (velocity.Linear.Length() + tangentVelocity);
            var minimumProgressionT = minimumProgressionDistance * inverseVelocity;
            var convergenceThresholdT = convergenceThresholdDistance * inverseVelocity;
            var maximumIterationCount = 25;
            Sweep(shape, pose, velocity, maximumT, pool, ref hitHandler, minimumProgressionT, convergenceThresholdT, maximumIterationCount);
        }
    }
}
