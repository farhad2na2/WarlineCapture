using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    // Per-attempt ownership. It never discovers a map/definition by scene name,
    // catalog-number fallback, checkpoint contents or an unreviewed Resources key.
    public sealed class SkirmishNativeMissionLogicalBindingRecord : IComponentData
    {
        public SkirmishScenarioDefinitionConfig Definition { get; }
        public SkirmishMissionLogicalBindingConfig AuthoredBinding { get; }
        public SkirmishCompiledMissionLogicalBinding Compiled { get; }
        public uint SetupHash { get; }
        internal readonly SkirmishLaunchDefinitionRecord LaunchDefinition;
        public bool OpeningFocusQueued { get; internal set; }
        public string OpeningFocusActorId { get; internal set; }
        public float3 OpeningFocusWorld { get; internal set; }
        public float3 OpeningFocusGroundRequest { get; internal set; }
        public int OpeningFocusRequestFrame { get; internal set; }
        // Entities may construct an empty record. TryRead rejects Compiled == null.
        public SkirmishNativeMissionLogicalBindingRecord() {}
        internal SkirmishNativeMissionLogicalBindingRecord(SkirmishScenarioDefinitionConfig definition,
            SkirmishResolvedSetup setup,SkirmishCompiledMissionLogicalBinding compiled,SkirmishLaunchDefinitionRecord launch)
        {Definition=definition;AuthoredBinding=definition.MissionLogicalBinding;SetupHash=setup.SetupHash;Compiled=compiled;LaunchDefinition=launch;}
    }

    public static class SkirmishMissionLogicalBindingProvider
    {
        // Launch dispatch supplies its exact independently selected definition.
        // Call before changing any session, payload, bank or actor native state.
        public static bool TryCompileForLaunch(SkirmishScenarioDefinitionConfig definition,SkirmishResolvedSetup setup,
            out SkirmishCompiledMissionLogicalBinding compiled,out string error)
        {
            compiled=null;error="Actual non-BA definition/setup binding missing.";
            if(setup!=null&&!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)){error=null;return true;}
            return SkirmishMissionLogicalBindingCompiler.TryCompile(definition,setup,out compiled,out error) &&
                !string.IsNullOrEmpty(setup.MissionLogicalBindingFingerprint) && compiled.Fingerprint==setup.MissionLogicalBindingFingerprint;
        }

        public static bool TryAttachCompiled(EntityManager em,Entity session,SkirmishScenarioDefinitionConfig definition,
            SkirmishResolvedSetup setup,SkirmishCompiledMissionLogicalBinding compiled,out string error)
        {
            error="Actual queued native setup differs from reviewed logical binding.";
            if(setup!=null&&!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)){error=null;return true;}
            if(definition==null||compiled==null||!em.Exists(session)||
                em.HasComponent<SkirmishNativeMissionLogicalBindingRecord>(session)||
                em.HasComponent<SkirmishNativeObjectiveRecoveryBinding>(session)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session)||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||
                !ReferenceEquals(em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition,definition)||
                !ReferenceEquals(em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup,setup)||
                compiled.Fingerprint!=setup.MissionLogicalBindingFingerprint||
                (setup.ObjectiveKind==SkirmishObjectiveKind.BaseAssault ? compiled.Areas!=null : compiled.Areas==null||compiled.Areas.ObjectiveKind!=setup.ObjectiveKind)||
                !compiled.View.Matches(setup,definition))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(!em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Matches(setup,native))return false;
            if(native.IsLegacy!=0||native.SetupHash!=setup.SetupHash||native.CatalogId.ToString()!=setup.CatalogId||
                native.DefinitionId.ToString()!=setup.DefinitionId||native.ContentVersion!=setup.ContentVersion||
                native.Seed!=setup.Seed||native.SizeId!=setup.SizeId||native.DifficultyId!=setup.DifficultyId||native.ObjectiveKind!=setup.ObjectiveKind||
                native.SpawnComplete!=0||(native.Phase!=SkirmishSessionPhase.Queued&&native.Phase!=SkirmishSessionPhase.Spawning))return false;
            em.AddComponentObject(session,new SkirmishNativeMissionLogicalBindingRecord(definition,setup,compiled,
                em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session)));
            try
            {
                if(setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault)
                    em.AddComponentObject(session,new SkirmishNativeObjectiveRecoveryBinding(compiled.Areas,compiled.ViewFingerprint));
            }
            catch
            {
                em.RemoveComponent<SkirmishNativeMissionLogicalBindingRecord>(session);throw;
            }
            error=null;return true;
        }

        public static bool TryRead(EntityManager em,Entity session,out SkirmishCompiledMissionLogicalBinding compiled)
        {
            compiled=null;
            if(!em.Exists(session)||!em.HasComponent<SkirmishNativeMissionLogicalBindingRecord>(session)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            var record=em.GetComponentObject<SkirmishNativeMissionLogicalBindingRecord>(session);
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(record?.Compiled==null||setup==null||record.Definition==null||!ReferenceEquals(record.Definition.MissionLogicalBinding,record.AuthoredBinding)||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||
                !ReferenceEquals(em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session),record.LaunchDefinition)||
                !record.LaunchDefinition.Matches(setup,native)||!record.Compiled.MatchesCurrentViewValues()||
                record.SetupHash!=native.SetupHash||native.SetupHash!=setup.SetupHash||
                record.Compiled.Fingerprint!=setup.MissionLogicalBindingFingerprint||
                native.CatalogId.ToString()!=setup.CatalogId||native.DefinitionId.ToString()!=setup.DefinitionId||
                native.ContentVersion!=setup.ContentVersion||native.Seed!=setup.Seed||native.SizeId!=setup.SizeId||native.DifficultyId!=setup.DifficultyId||
                native.ObjectiveKind!=setup.ObjectiveKind||!record.Compiled.View.Matches(setup,record.Definition))return false;
            compiled=record.Compiled;return true;
        }

        // Exact shared-spawn completion seam, before authoritative clock advances.
        // Recovery uses the independent binding above and its native commit owner.
        public static bool TryAttachAfterActualSpawn(EntityManager em,Entity session,out string error)
        {
            error="Native objective logical provider unavailable after spawn.";
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(!em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)){error=null;return true;}
            if(em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(session)){error=null;return true;}
            if(!TryRead(em,session,out var compiled))return false;
            if(state.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault && !SkirmishNativeObjectiveLifecycleHooks.TryAttachAfterActualSpawn(em,session,compiled.Areas,
                compiled.Areas.LogicalFingerprint,compiled.ViewFingerprint,out error))return false;
            return TryQueueOpeningRoleFocus(em,session,out error);
        }

        public static bool TryQueueOpeningRoleFocus(EntityManager em,Entity session,out string error)
        {
            error="Exact owned opening focus actor/elevation unavailable.";
            if(!TryRead(em,session,out var compiled))return false;
            var record=em.GetComponentObject<SkirmishNativeMissionLogicalBindingRecord>(session);
            if(record.OpeningFocusQueued){error=null;return true;}
            if(!compiled.TryReadFraming(compiled.OpeningFocusRole,out var framing))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState });
            using var actors=query.ToEntityArray(Allocator.Temp);Entity found=Entity.Null;float3 position=default;
            foreach(var actor in actors)
            {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(owned.SessionId!=attempt||owned.StableObjectId.ToString()!=compiled.OpeningFocusStableActorId)continue;
                if(found!=Entity.Null||owned.FactionId!=1||owned.IsStructure!=0||owned.CapacityReleased!=0||owned.ReservationId!=0||
                    em.HasComponent<Parent>(actor)||em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                    em.HasComponent<Disabled>(actor)||em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor)||
                    !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1||
                    !em.HasComponent<SkirmishUnitRoleComponent>(actor)||em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role!=compiled.OpeningFocusRole||
                    !em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||
                    !em.HasComponent<LocalTransform>(actor))return false;
                position=em.GetComponentData<LocalTransform>(actor).Position;
                if(!math.all(math.isfinite(position))||position.y<framing.MinimumSubjectElevation||position.y>framing.MaximumSubjectElevation)return false;
                found=actor;
            }
            if(found==Entity.Null)return false;
            if(!TryProjectRoleFocus(position,compiled.View.NormalHeight,compiled.View.Pitch,compiled.View.Yaw,out var groundRequest))return false;
            // Existing World-owned one-shot request and its existing shell consumer.
            // This changes camera presentation only; no selection or troop request.
            var facade=new RuntimeGameplayStateSystem(em);facade.ReadCameraFocusRequest();
            using var requests=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if(requests.CalculateEntityCount()!=1)return false;
            var view=compiled.View;
            em.SetComponentData(requests.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent {
                Requested=1,Smooth=1,UseExplicitPerspective=1,World=groundRequest,
                Perspective=new float4(view.NormalHeight,view.Pitch,view.Yaw,view.NormalFieldOfView),
                SmoothTimeSeconds=RuntimeCameraFocusRequestUtility.EnsureMissionIntroSmoothTime(compiled.OpeningFocusSmoothSeconds) });
            record.OpeningFocusQueued=true;record.OpeningFocusActorId=compiled.OpeningFocusStableActorId;
            record.OpeningFocusWorld=position;record.OpeningFocusRequestFrame=Time.frameCount;
            record.OpeningFocusGroundRequest=groundRequest;
            error=null;return true;
        }
        public static bool OwnsOpeningIntroduction(EntityManager em,Entity session)=>TryRead(em,session,out _) &&
            em.GetComponentObject<SkirmishNativeMissionLogicalBindingRecord>(session).OpeningFocusQueued;

        public static bool TryProjectRoleFocus(Vector3 actualRoleWorld,float absoluteCameraHeight,float pitch,float yaw,out Vector3 groundRequest)
        {
            groundRequest=default;
            if(!float.IsFinite(actualRoleWorld.x)||!float.IsFinite(actualRoleWorld.y)||!float.IsFinite(actualRoleWorld.z)||
                !float.IsFinite(absoluteCameraHeight)||absoluteCameraHeight<=actualRoleWorld.y||
                !float.IsFinite(pitch)||pitch<=0||pitch>=90||!float.IsFinite(yaw))return false;
            var forward=Quaternion.Euler(pitch,yaw,0)*Vector3.forward;
            if(forward.y>=-.0001f)return false;
            groundRequest=actualRoleWorld-forward*(actualRoleWorld.y/forward.y);groundRequest.y=0;
            return float.IsFinite(groundRequest.x)&&float.IsFinite(groundRequest.z);
        }
    }
}
