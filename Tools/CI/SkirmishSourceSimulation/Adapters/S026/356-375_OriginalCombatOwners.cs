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
        readonly EntityManager manager;
readonly Owner<UnitScanOrderExecutionSystem> scanExecute;
readonly Owner<ScanIntelCommandSystem> scanCommands;
readonly Owner<UnitTransportRopeDisembarkSystem> ropeDisembark;
readonly Owner<UnitTransportRopeDisperseSystem> ropeDisperse;
readonly Owner<UnitAirMovementSystem> airMovement;
readonly Owner<UnitTransportDeployOrderSystem> transportDeploy;
readonly Owner<UnitTransportBoardingSystem> transportBoarding;
readonly Owner<TransportBoardingDiagnosticLogFlushSystem> transportDiagnostic;
readonly Owner<UnitTransportDeployAttackSystem> transportAttack;
readonly Owner<UnitTransportRopeDropSystem> ropeDrop;

        readonly Owner<UnitAttackOrderRequestSystem> orderRequests;
        readonly Owner<GroundMissileLauncherFireSystem> missileFire;
        readonly Owner<GroundMissileLauncherVisualSystem> missileVisual;
        readonly Owner<GroundMissileFlyingRocketVisualSystem> flyingVisual;
        readonly Owner<GroundMissileRocketTrailSystem> rocketTrail;
        readonly Owner<SkirmishWorldMovementSystem> expandedMovement;
        readonly Owner<GroundMissileProjectileFlightSystem> projectileFlight;
        readonly Owner<MissileFlightAudioSystem> flightAudio;
        readonly Owner<GroundMissileImpactSystem> missileImpact;

        readonly Owner<UnitDeathSystem> death;
        readonly Owner<BuildingDefenseAttackSystem> defense;

        internal OriginalCombatOwners(EntityManager em)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            manager=em;
scanExecute=new Owner<UnitScanOrderExecutionSystem>(em,(ref UnitScanOrderExecutionSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitScanOrderExecutionSystem s,ref SystemState st)=>s.OnUpdate(ref st));
scanCommands=new Owner<ScanIntelCommandSystem>(em,(ref ScanIntelCommandSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref ScanIntelCommandSystem s,ref SystemState st)=>s.OnUpdate(ref st));
ropeDisembark=new Owner<UnitTransportRopeDisembarkSystem>(em,(ref UnitTransportRopeDisembarkSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitTransportRopeDisembarkSystem s,ref SystemState st)=>s.OnUpdate(ref st));
ropeDisperse=new Owner<UnitTransportRopeDisperseSystem>(em,(ref UnitTransportRopeDisperseSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitTransportRopeDisperseSystem s,ref SystemState st)=>s.OnUpdate(ref st));
airMovement=new Owner<UnitAirMovementSystem>(em,(ref UnitAirMovementSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitAirMovementSystem s,ref SystemState st)=>s.OnUpdate(ref st));
transportDeploy=new Owner<UnitTransportDeployOrderSystem>(em,(ref UnitTransportDeployOrderSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitTransportDeployOrderSystem s,ref SystemState st)=>s.OnUpdate(ref st));
transportBoarding=new Owner<UnitTransportBoardingSystem>(em,(ref UnitTransportBoardingSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitTransportBoardingSystem s,ref SystemState st)=>s.OnUpdate(ref st));
transportDiagnostic=new Owner<TransportBoardingDiagnosticLogFlushSystem>(em,(ref TransportBoardingDiagnosticLogFlushSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref TransportBoardingDiagnosticLogFlushSystem s,ref SystemState st)=>s.OnUpdate(ref st));
transportAttack=new Owner<UnitTransportDeployAttackSystem>(em,(ref UnitTransportDeployAttackSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitTransportDeployAttackSystem s,ref SystemState st)=>s.OnUpdate(ref st));
ropeDrop=new Owner<UnitTransportRopeDropSystem>(em,(ref UnitTransportRopeDropSystem s,ref SystemState st)=>s.OnCreate(ref st),(ref UnitTransportRopeDropSystem s,ref SystemState st)=>s.OnUpdate(ref st));

            orderRequests=new Owner<UnitAttackOrderRequestSystem>(em,(ref UnitAttackOrderRequestSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref UnitAttackOrderRequestSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            missileFire=new Owner<GroundMissileLauncherFireSystem>(em,(ref GroundMissileLauncherFireSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref GroundMissileLauncherFireSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            missileVisual=new Owner<GroundMissileLauncherVisualSystem>(em,(ref GroundMissileLauncherVisualSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref GroundMissileLauncherVisualSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            flyingVisual=new Owner<GroundMissileFlyingRocketVisualSystem>(em,(ref GroundMissileFlyingRocketVisualSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref GroundMissileFlyingRocketVisualSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            rocketTrail=new Owner<GroundMissileRocketTrailSystem>(em,(ref GroundMissileRocketTrailSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref GroundMissileRocketTrailSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            expandedMovement=new Owner<SkirmishWorldMovementSystem>(em,(ref SkirmishWorldMovementSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref SkirmishWorldMovementSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            projectileFlight=new Owner<GroundMissileProjectileFlightSystem>(em,(ref GroundMissileProjectileFlightSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref GroundMissileProjectileFlightSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            flightAudio=new Owner<MissileFlightAudioSystem>(em,(ref MissileFlightAudioSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref MissileFlightAudioSystem s,ref SystemState state)=>s.OnUpdate(ref state));
            missileImpact=new Owner<GroundMissileImpactSystem>(em,(ref GroundMissileImpactSystem s,ref SystemState state)=>s.OnCreate(ref state),(ref GroundMissileImpactSystem s,ref SystemState state)=>s.OnUpdate(ref state));

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
        internal void AttackRequestsPhase()=>orderRequests.Run();
        internal void MissileFirePhase()=>missileFire.Run();
        internal void MissileVisualPhase()=>missileVisual.Run();
        internal void FlyingVisualPhase()=>flyingVisual.Run();
        internal void RocketTrailPhase()=>rocketTrail.Run();
        internal void ExpandedMovementPhase()=>expandedMovement.Run();
        internal void ProjectileFlightPhase()=>projectileFlight.Run();
        internal void MissileAudioPhase()=>flightAudio.Run();
        internal void MissileImpactPhase()=>missileImpact.Run();
        internal void ScanExecutionPhase()=>scanExecute.Run();
internal void ScanCommandsPhase()=>scanCommands.Run();
internal void RopeDisembarkPhase()=>ropeDisembark.Run();
internal void RopeDispersePhase()=>ropeDisperse.Run();
internal void AirMovementPhase()=>airMovement.Run();
internal void TransportDeployPhase()=>transportDeploy.Run();
internal void TransportBoardingPhase()=>transportBoarding.Run();
internal void TransportDiagnosticPhase()=>transportDiagnostic.Run();
internal void TransportAttackPhase()=>transportAttack.Run();
internal void RopeDropPhase()=>ropeDrop.Run();
internal void DeathPhase(){SourceNativeDeathBinding.ValidateActualUnits(manager);death.Run();}
        internal void DefensePhase() => defense.Run();
        public void Dispose()
        {
scanExecute.Dispose();
scanCommands.Dispose();
ropeDisembark.Dispose();
ropeDisperse.Dispose();
airMovement.Dispose();
transportDeploy.Dispose();
transportBoarding.Dispose();
transportDiagnostic.Dispose();
transportAttack.Dispose();
ropeDrop.Dispose();

            orderRequests.Dispose();
            missileFire.Dispose();
            missileVisual.Dispose();
            flyingVisual.Dispose();
            rocketTrail.Dispose();
            expandedMovement.Dispose();
            projectileFlight.Dispose();
            flightAudio.Dispose();
            missileImpact.Dispose();

            death.Dispose(); defense.Dispose(); attack.Dispose(); chase.Dispose(); synchronization.Dispose();
            acquisition.Dispose(); validation.Dispose(); traceState.Dispose(); attackState.Dispose(); observations.Dispose();
        }
    }
}
