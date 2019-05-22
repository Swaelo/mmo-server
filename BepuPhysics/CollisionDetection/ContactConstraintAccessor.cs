﻿using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace BepuPhysics.CollisionDetection
{
    /// <summary>
    /// Provides indirection for reading from and updating constraints in the narrow phase.
    /// </summary>
    /// <remarks>This, like many other similar constructions in the engine, could conceptually be replaced by static function pointers and a few supplementary data fields.
    /// We probably will do exactly that at some point.</remarks>
    public abstract class ContactConstraintAccessor
    {
        public int ConstraintTypeId { get; protected set; }

        protected int AccumulatedImpulseBundleStrideInBytes;
        public int ContactCount { get; protected set; }
        protected bool Convex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void GatherOldImpulses(ref ConstraintReference constraintReference, float* oldImpulses)
        {
            BundleIndexing.GetBundleIndices(constraintReference.IndexInTypeBatch, out var bundleIndex, out var inner);
            ref var buffer = ref constraintReference.TypeBatch.AccumulatedImpulses;
            if (Convex)
            {
                //Note that we do not modify the friction accumulated impulses. This is just for simplicity- the impact of accumulated impulses on friction *should* be relatively
                //hard to notice compared to penetration impulses. TODO: We should, however, test this assumption.
                //Note that we assume that the tangent friction impulses always come first. This should be safe for now, but it is important to keep in mind for later.
                ref var start = ref GatherScatter.GetOffsetInstance(ref Unsafe.As<byte, Vector<float>>(ref buffer[AccumulatedImpulseBundleStrideInBytes * bundleIndex + Unsafe.SizeOf<Vector2Wide>()]), inner);
                for (int i = 0; i < ContactCount; ++i)
                {
                    oldImpulses[i] = Unsafe.Add(ref start, i)[0];
                }
            }
            else
            {
                ref var start = ref GatherScatter.GetOffsetInstance(ref Unsafe.As<byte, NonconvexAccumulatedImpulses>(ref buffer[AccumulatedImpulseBundleStrideInBytes * bundleIndex]), inner);
                for (int i = 0; i < ContactCount; ++i)
                {
                    oldImpulses[i] = Unsafe.Add(ref start, i).Penetration[0];
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ScatterNewImpulses<TContactImpulses>(ref ConstraintReference constraintReference, ref TContactImpulses contactImpulses)
        {
            //Note that we do not modify the friction accumulated impulses. This is just for simplicity- the impact of accumulated impulses on friction *should* be relatively
            //hard to notice compared to penetration impulses. TODO: We should, however, test this assumption.
            BundleIndexing.GetBundleIndices(constraintReference.IndexInTypeBatch, out var bundleIndex, out var inner);
            ref var buffer = ref constraintReference.TypeBatch.AccumulatedImpulses;
            Debug.Assert(constraintReference.TypeBatch.TypeId == ConstraintTypeId);
            if (Convex)
            {
                //Note that we assume that the tangent friction impulses always come first. This should be safe for now, but it is important to keep in mind for later.
                ref var sourceStart = ref Unsafe.As<TContactImpulses, float>(ref contactImpulses);
                ref var targetStart = ref GatherScatter.GetOffsetInstance(ref Unsafe.As<byte, Vector<float>>(ref buffer[AccumulatedImpulseBundleStrideInBytes * bundleIndex + Unsafe.SizeOf<Vector2Wide>()]), inner);
                for (int i = 0; i < ContactCount; ++i)
                {
                    GatherScatter.GetFirst(ref Unsafe.Add(ref targetStart, i)) = Unsafe.Add(ref sourceStart, i);
                }
            }
            else
            {
                ref var sourceStart = ref Unsafe.As<TContactImpulses, float>(ref contactImpulses);
                ref var targetStart = ref GatherScatter.GetOffsetInstance(ref Unsafe.As<byte, NonconvexAccumulatedImpulses>(ref buffer[AccumulatedImpulseBundleStrideInBytes * bundleIndex]), inner);
                for (int i = 0; i < ContactCount; ++i)
                {
                    GatherScatter.GetFirst(ref Unsafe.Add(ref targetStart, i).Penetration) = Unsafe.Add(ref sourceStart, i);
                }
            }
        }

        public abstract void DeterministicallyAdd<TCallbacks>(
            int typeIndex, NarrowPhase<TCallbacks>.OverlapWorker[] overlapWorkers,
            ref QuickList<NarrowPhase<TCallbacks>.SortConstraintTarget> constraintsOfType,
            Simulation simulation, PairCache pairCache) where TCallbacks : struct, INarrowPhaseCallbacks;

        public abstract void FlushWithSpeculativeBatches<TCallbacks>(ref UntypedList list, int narrowPhaseConstraintTypeId,
            ref Buffer<Buffer<ushort>> speculativeBatchIndices, Simulation simulation, PairCache pairCache)
            where TCallbacks : struct, INarrowPhaseCallbacks;

        public abstract void FlushSequentially<TCallbacks>(ref UntypedList list, int narrowPhaseConstraintTypeId, Simulation simulation, PairCache pairCache)
            where TCallbacks : struct, INarrowPhaseCallbacks;

        public abstract unsafe void UpdateConstraintForManifold<TContactManifold, TCollisionCache, TCallBodyHandles, TCallbacks>(
            NarrowPhase<TCallbacks> narrowPhase, int manifoldTypeAsConstraintType, int workerIndex,
            ref CollidablePair pair, ref TContactManifold manifoldPointer, ref TCollisionCache collisionCache, ref PairMaterialProperties material, TCallBodyHandles bodyHandles)
            where TCallbacks : struct, INarrowPhaseCallbacks
            where TCollisionCache : IPairCacheEntry;
    }

    //Note that the vast majority of the 'work' done by these accessor implementations is just type definitions used to call back into some other functions that need that type knowledge.
    public abstract class ContactConstraintAccessor<TConstraintDescription, TBodyHandles, TAccumulatedImpulses, TContactImpulses, TConstraintCache> : ContactConstraintAccessor
        where TConstraintDescription : IConstraintDescription<TConstraintDescription>
        where TConstraintCache : IPairCacheEntry
    {
        protected ContactConstraintAccessor()
        {
            Debug.Assert(
                typeof(TContactImpulses) == typeof(ContactImpulses1) ||
                typeof(TContactImpulses) == typeof(ContactImpulses2) ||
                typeof(TContactImpulses) == typeof(ContactImpulses3) ||
                typeof(TContactImpulses) == typeof(ContactImpulses4) ||
                typeof(TContactImpulses) == typeof(ContactImpulses5) ||
                typeof(TContactImpulses) == typeof(ContactImpulses6) ||
                typeof(TContactImpulses) == typeof(ContactImpulses7) ||
                typeof(TContactImpulses) == typeof(ContactImpulses8));
            ContactCount = Unsafe.SizeOf<TContactImpulses>() / Unsafe.SizeOf<float>();

            Convex =
                typeof(TConstraintDescription) == typeof(Contact1) ||
                typeof(TConstraintDescription) == typeof(Contact2) ||
                typeof(TConstraintDescription) == typeof(Contact3) ||
                typeof(TConstraintDescription) == typeof(Contact4) ||
                typeof(TConstraintDescription) == typeof(Contact1OneBody) ||
                typeof(TConstraintDescription) == typeof(Contact2OneBody) ||
                typeof(TConstraintDescription) == typeof(Contact3OneBody) ||
                typeof(TConstraintDescription) == typeof(Contact4OneBody);
            if (Convex)
            {
                Debug.Assert((ContactCount + 3) * Unsafe.SizeOf<Vector<float>>() == Unsafe.SizeOf<TAccumulatedImpulses>(),
                    "The layout of convex accumulated impulses seems to have changed; the assumptions of impulse gather/scatter are probably no longer valid.");
            }
            else
            {
                Debug.Assert(ContactCount * 3 * Unsafe.SizeOf<Vector<float>>() == Unsafe.SizeOf<TAccumulatedImpulses>(),
                    "The layout of nonconvex accumulated impulses seems to have changed; the assumptions of impulse gather/scatter are probably no longer valid.");
            }
            //Note that this test has to special case count == 1; 1 contact manifolds have no feature ids.
            Debug.Assert(Unsafe.SizeOf<TConstraintCache>() == sizeof(int) * (1 + ContactCount) &&
                default(TConstraintCache).CacheTypeId == ContactCount - 1,
                "The type of the constraint cache should hold as many contacts as the contact impulses requires.");
            AccumulatedImpulseBundleStrideInBytes = Unsafe.SizeOf<TAccumulatedImpulses>();
            ConstraintTypeId = default(TConstraintDescription).ConstraintTypeId;
        }
        public override void DeterministicallyAdd<TCallbacks>(int typeIndex, NarrowPhase<TCallbacks>.OverlapWorker[] overlapWorkers,
            ref QuickList<NarrowPhase<TCallbacks>.SortConstraintTarget> constraintsOfType,
            Simulation simulation, PairCache pairCache)
        {
            for (int i = 0; i < constraintsOfType.Count; ++i)
            {
                NarrowPhase<TCallbacks>.PendingConstraintAddCache.DeterministicAdd<TBodyHandles, TConstraintDescription, TContactImpulses>(
                    typeIndex, ref constraintsOfType[i], overlapWorkers, simulation, ref pairCache);
            }
        }
        public override void FlushSequentially<TCallbacks>(ref UntypedList list, int narrowPhaseConstraintTypeId, Simulation simulation, PairCache pairCache)
        {
            NarrowPhase<TCallbacks>.PendingConstraintAddCache.SequentialAddToSimulation<TBodyHandles, TConstraintDescription, TContactImpulses>(
                ref list, narrowPhaseConstraintTypeId, simulation, pairCache);
        }

        public override void FlushWithSpeculativeBatches<TCallbacks>(ref UntypedList list, int narrowPhaseConstraintTypeId, ref Buffer<Buffer<ushort>> speculativeBatchIndices, Simulation simulation, PairCache pairCache)
        {
            NarrowPhase<TCallbacks>.PendingConstraintAddCache.SequentialAddToSimulationSpeculative<TBodyHandles, TConstraintDescription, TContactImpulses>(
                ref list, narrowPhaseConstraintTypeId, ref speculativeBatchIndices, simulation, pairCache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe void UpdateConstraint<TCallbacks, TCollisionCache, TCallBodyHandles>(
            NarrowPhase<TCallbacks> narrowPhase, int manifoldTypeAsConstraintType, int workerIndex,
            ref CollidablePair pair, ref TConstraintCache constraintCache, ref TCollisionCache collisionCache, ref TConstraintDescription description, TCallBodyHandles bodyHandles)
            where TCallbacks : struct, INarrowPhaseCallbacks where TCollisionCache : IPairCacheEntry
        {
            //Note that we let the user pass in a body handles type to a generic function, rather than requiring that the top level abstract class define the type.
            //That allows a type inconsistency, but it's easy to catch.
            Debug.Assert(typeof(TCallBodyHandles) == typeof(TBodyHandles), "Don't call an update with inconsistent body handle types.");
            narrowPhase.UpdateConstraint<TBodyHandles, TConstraintDescription, TContactImpulses, TCollisionCache, TConstraintCache>(
                workerIndex, ref pair, manifoldTypeAsConstraintType, ref constraintCache, ref collisionCache, ref description, Unsafe.As<TCallBodyHandles, TBodyHandles>(ref bodyHandles));
        }

        protected static void CopyContactData(ref ConvexContactManifold manifold, out TConstraintCache constraintCache, out TConstraintDescription description)
        {
            //TODO: Unnecessary zero inits. Should see if releasestrip strips these. Blittable could help us avoid this if the compiler doesn't realize.
            constraintCache = default;
            description = default;
            //TODO: Check codegen. This should be a compilation time constant. If it's not, just use the ContactCount that we cached.
            var contactCount = constraintCache.CacheTypeId + 1;
            Debug.Assert(contactCount == manifold.Count, "Relying on generic specialization; should be the same value!");
            //Contact data comes first in the constraint description memory layout.
            ref var targetContacts = ref Unsafe.As<TConstraintDescription, ConstraintContactData>(ref description);
            ref var targetFeatureIds = ref Unsafe.Add(ref Unsafe.As<TConstraintCache, int>(ref constraintCache), 1);
            for (int i = 0; i < contactCount; ++i)
            {
                ref var sourceContact = ref Unsafe.Add(ref manifold.Contact0, i);
                ref var targetContact = ref Unsafe.Add(ref targetContacts, i);
                Unsafe.Add(ref targetFeatureIds, i) = sourceContact.FeatureId;
                targetContact.OffsetA = sourceContact.Offset;
                targetContact.PenetrationDepth = sourceContact.Depth;
            }
        }
        protected static void CopyContactData(ref NonconvexContactManifold manifold, ref TConstraintCache constraintCache, ref NonconvexConstraintContactData targetContacts)
        {
            //TODO: Check codegen. This should be a compilation time constant. If it's not, just use the ContactCount that we cached.
            var contactCount = constraintCache.CacheTypeId + 1;
            Debug.Assert(contactCount == manifold.Count, "Relying on generic specialization; should be the same value!");
            ref var targetFeatureIds = ref Unsafe.Add(ref Unsafe.As<TConstraintCache, int>(ref constraintCache), 1);
            for (int i = 0; i < contactCount; ++i)
            {
                ref var sourceContact = ref Unsafe.Add(ref manifold.Contact0, i);
                ref var targetContact = ref Unsafe.Add(ref targetContacts, i);
                Unsafe.Add(ref targetFeatureIds, i) = sourceContact.FeatureId;
                targetContact.OffsetA = sourceContact.Offset;
                targetContact.Normal = sourceContact.Normal;
                targetContact.PenetrationDepth = sourceContact.Depth;
            }
        }
    }
    public class ConvexOneBodyAccessor<TConstraintDescription, TAccumulatedImpulses, TContactImpulses, TConstraintCache> :
        ContactConstraintAccessor<TConstraintDescription, int, TAccumulatedImpulses, TContactImpulses, TConstraintCache>
        where TConstraintDescription : IConvexOneBodyContactConstraintDescription<TConstraintDescription>
        where TConstraintCache : IPairCacheEntry
    {
        public override void UpdateConstraintForManifold<TContactManifold, TCollisionCache, TCallBodyHandles, TCallbacks>(
            NarrowPhase<TCallbacks> narrowPhase, int manifoldTypeAsConstraintType, int workerIndex,
            ref CollidablePair pair, ref TContactManifold manifoldPointer, ref TCollisionCache collisionCache, ref PairMaterialProperties material, TCallBodyHandles bodyHandles)
        {
            Debug.Assert(typeof(TCallBodyHandles) == typeof(int));
            ref var manifold = ref Unsafe.As<TContactManifold, ConvexContactManifold>(ref manifoldPointer);
            CopyContactData(ref manifold, out var constraintCache, out var description);
            description.CopyManifoldWideProperties(ref manifold.Normal, ref material);
            UpdateConstraint(narrowPhase, manifoldTypeAsConstraintType, workerIndex, ref pair, ref constraintCache, ref collisionCache, ref description, bodyHandles);
        }
    }

    public class ConvexTwoBodyAccessor<TConstraintDescription, TAccumulatedImpulses, TContactImpulses, TConstraintCache> :
        ContactConstraintAccessor<TConstraintDescription, TwoBodyHandles, TAccumulatedImpulses, TContactImpulses, TConstraintCache>
        where TConstraintDescription : IConvexTwoBodyContactConstraintDescription<TConstraintDescription>
        where TConstraintCache : IPairCacheEntry
    {
        public override void UpdateConstraintForManifold<TContactManifold, TCollisionCache, TCallBodyHandles, TCallbacks>(
            NarrowPhase<TCallbacks> narrowPhase, int manifoldTypeAsConstraintType, int workerIndex,
            ref CollidablePair pair, ref TContactManifold manifoldPointer, ref TCollisionCache collisionCache, ref PairMaterialProperties material, TCallBodyHandles bodyHandles)
        {
            Debug.Assert(typeof(TCallBodyHandles) == typeof(TwoBodyHandles));
            ref var manifold = ref Unsafe.As<TContactManifold, ConvexContactManifold>(ref manifoldPointer);
            CopyContactData(ref manifold, out var constraintCache, out var description);
            description.CopyManifoldWideProperties(ref manifold.OffsetB, ref manifold.Normal, ref material);
            UpdateConstraint(narrowPhase, manifoldTypeAsConstraintType, workerIndex, ref pair, ref constraintCache, ref collisionCache, ref description, bodyHandles);
        }
    }

    public class NonconvexOneBodyAccessor<TConstraintDescription, TAccumulatedImpulses, TContactImpulses, TConstraintCache> :
        ContactConstraintAccessor<TConstraintDescription, int, TAccumulatedImpulses, TContactImpulses, TConstraintCache>
        where TConstraintDescription : INonconvexOneBodyContactConstraintDescription<TConstraintDescription>
        where TConstraintCache : IPairCacheEntry
    {
        public override void UpdateConstraintForManifold<TContactManifold, TCollisionCache, TCallBodyHandles, TCallbacks>(
            NarrowPhase<TCallbacks> narrowPhase, int manifoldTypeAsConstraintType, int workerIndex,
            ref CollidablePair pair, ref TContactManifold manifoldPointer, ref TCollisionCache collisionCache, ref PairMaterialProperties material, TCallBodyHandles bodyHandles)
        {
            Debug.Assert(typeof(TCallBodyHandles) == typeof(int));
            ref var manifold = ref Unsafe.As<TContactManifold, NonconvexContactManifold>(ref manifoldPointer);
            //TODO: Unnecessary zero inits. Should see if releasestrip strips these. Blittable could help us avoid this if the compiler doesn't realize.
            TConstraintCache constraintCache = default;
            TConstraintDescription description = default;
            CopyContactData(ref manifold, ref constraintCache, ref description.GetFirstContact(ref description));
            description.CopyManifoldWideProperties(ref material);
            UpdateConstraint(narrowPhase, manifoldTypeAsConstraintType, workerIndex, ref pair, ref constraintCache, ref collisionCache, ref description, bodyHandles);
        }
    }

    public class NonconvexTwoBodyAccessor<TConstraintDescription, TAccumulatedImpulses, TContactImpulses, TConstraintCache> :
        ContactConstraintAccessor<TConstraintDescription, TwoBodyHandles, TAccumulatedImpulses, TContactImpulses, TConstraintCache>
        where TConstraintDescription : INonconvexTwoBodyContactConstraintDescription<TConstraintDescription>
        where TConstraintCache : IPairCacheEntry
    {
        public override void UpdateConstraintForManifold<TContactManifold, TCollisionCache, TCallBodyHandles, TCallbacks>(
            NarrowPhase<TCallbacks> narrowPhase, int manifoldTypeAsConstraintType, int workerIndex,
            ref CollidablePair pair, ref TContactManifold manifoldPointer, ref TCollisionCache collisionCache, ref PairMaterialProperties material, TCallBodyHandles bodyHandles)
        {
            Debug.Assert(typeof(TCallBodyHandles) == typeof(TwoBodyHandles));
            ref var manifold = ref Unsafe.As<TContactManifold, NonconvexContactManifold>(ref manifoldPointer);
            //TODO: Unnecessary zero inits. Should see if releasestrip strips these. Blittable could help us avoid this if the compiler doesn't realize.
            TConstraintCache constraintCache = default;
            TConstraintDescription description = default;
            CopyContactData(ref manifold, ref constraintCache, ref description.GetFirstContact(ref description));
            description.CopyManifoldWideProperties(ref manifold.OffsetB, ref material);
            UpdateConstraint(narrowPhase, manifoldTypeAsConstraintType, workerIndex, ref pair, ref constraintCache, ref collisionCache, ref description, bodyHandles);
        }
    }
}
