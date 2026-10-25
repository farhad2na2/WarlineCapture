using System;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    // Each phase calls the original owner's complete OnUpdate. The outer world
    // scheduler owns time and the single shared EndSimulation playback barrier.
    internal sealed class OriginalCombatOwners : IDisposable
    {
        internal delegate void SourceCall<T>(ref T source, ref SystemState state) where T : struct;
        sealed class Owner<T> : IDisposable where T : struct
        {
            T source;
            SystemState state;
            readonly SourceCall<T> update, destroy;
            bool disposed;
            internal Owner(EntityManager em, SourceCall<T> create, SourceCall<T> step, SourceCall<T> cleanup = null)
            {
                state = new SystemState { EntityManager = em, Enabled = true };
                update = step;
                destroy = cleanup;
                state.Begin();
                create(ref source, ref state);
                // Fresh original systems have not yet observed a simulation
                // update. Initial component writes must reach change filters.
                state.LastSystemVersion = 0;
            }
            internal void Run()
            {
                if (disposed) throw new ObjectDisposedException(typeof(T).Name);
                state.Begin();
                if (state.Enabled && state.RequiredQueriesPresent) update(ref source, ref state);
                state.End();
            }
            public void Dispose()
            {
                if (disposed) return;
                state.Begin();
                if (destroy != null) destroy(ref source, ref state);
                disposed = true;
            }
        }

        readonly Owner<Combat.CombatDamageObservationBootstrapSystem> observations;
        readonly Owner<UnitAttackStateEnsureSystem> attackState;
        readonly Owner<UnitAttackTraceStateEnsureSystem> traceState;
        readonly Owner<EngageTargetValidateSystem> validation;
        readonly Owner<UnitEngagementSystem> acquisition;
        readonly Owner<EngageTargetSyncSystem> synchronization;
        readonly Owner<UnitEngagedMovementSystem> chase;
        readonly Owner<UnitAttackSystem> attack;
        readonly Owner<UnitDeathSystem> death;
        readonly Owner<BuildingDefenseAttackSystem> defense;

        internal OriginalCombatOwners(EntityManager em)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            if (em.CreateEntityQuery(typeof(GridConfig)).CalculateEntityCount() != 1 ||
                em.CreateEntityQuery(typeof(EndSimulationEntityCommandBufferSystem.Singleton)).CalculateEntityCount() != 1)
                throw new NotSupportedException("Generated source grid and shared deferred owner required before combat construction");
            observations = new Owner<Combat.CombatDamageObservationBootstrapSystem>(em,
                (ref Combat.CombatDamageObservationBootstrapSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref Combat.CombatDamageObservationBootstrapSystem s, ref SystemState t) => s.OnUpdate(ref t));
            attackState = new Owner<UnitAttackStateEnsureSystem>(em,
                (ref UnitAttackStateEnsureSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref UnitAttackStateEnsureSystem s, ref SystemState t) => s.OnUpdate(ref t));
            traceState = new Owner<UnitAttackTraceStateEnsureSystem>(em,
                (ref UnitAttackTraceStateEnsureSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref UnitAttackTraceStateEnsureSystem s, ref SystemState t) => s.OnUpdate(ref t));
            validation = new Owner<EngageTargetValidateSystem>(em,
                (ref EngageTargetValidateSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref EngageTargetValidateSystem s, ref SystemState t) => s.OnUpdate(ref t));
            acquisition = new Owner<UnitEngagementSystem>(em,
                (ref UnitEngagementSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref UnitEngagementSystem s, ref SystemState t) => s.OnUpdate(ref t));
            synchronization = new Owner<EngageTargetSyncSystem>(em,
                (ref EngageTargetSyncSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref EngageTargetSyncSystem s, ref SystemState t) => s.OnUpdate(ref t));
            chase = new Owner<UnitEngagedMovementSystem>(em,
                (ref UnitEngagedMovementSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref UnitEngagedMovementSystem s, ref SystemState t) => s.OnUpdate(ref t));
            attack = new Owner<UnitAttackSystem>(em,
                (ref UnitAttackSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref UnitAttackSystem s, ref SystemState t) => s.OnUpdate(ref t),
                (ref UnitAttackSystem s, ref SystemState t) => s.OnDestroy(ref t));
            defense = new Owner<BuildingDefenseAttackSystem>(em,
                (ref BuildingDefenseAttackSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref BuildingDefenseAttackSystem s, ref SystemState t) => s.OnUpdate(ref t),
                (ref BuildingDefenseAttackSystem s, ref SystemState t) => s.OnDestroy(ref t));
            death = new Owner<UnitDeathSystem>(em,
                (ref UnitDeathSystem s, ref SystemState t) => s.OnCreate(ref t),
                (ref UnitDeathSystem s, ref SystemState t) => s.OnUpdate(ref t),
                (ref UnitDeathSystem s, ref SystemState t) => s.OnDestroy(ref t));
        }

        internal void EnsureStatePhase() { observations.Run(); attackState.Run(); traceState.Run(); }
        internal void ValidatePhase() => validation.Run();
        internal void AcquisitionPhase() => acquisition.Run();
        internal void SynchronizePhase() => synchronization.Run();
        internal void ChasePhase() => chase.Run();
        internal void AttackPhase() => attack.Run();
        internal void DeathPhase() => death.Run();
        internal void DefensePhase() => defense.Run();
        public void Dispose()
        {
            death.Dispose(); defense.Dispose(); attack.Dispose(); chase.Dispose(); synchronization.Dispose();
            acquisition.Dispose(); validation.Dispose(); traceState.Dispose(); attackState.Dispose(); observations.Dispose();
        }
    }
}
