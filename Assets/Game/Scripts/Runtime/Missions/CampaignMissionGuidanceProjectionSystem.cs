using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [BurstCompile, UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(CampaignMissionObjectiveProjectionSystem))]
    public partial struct CampaignMissionGuidanceProjectionSystem : ISystem
    {
        private static readonly FixedString64Bytes FindSquadTitle = "Find your squad";
        private static readonly FixedString64Bytes MoveToCoverTitle = "Move to cover";
        private static readonly FixedString64Bytes ConfirmThreatTitle = "Confirm the threat";
        private static readonly FixedString64Bytes EngagePatrolTitle = "Engage the patrol";
        private static readonly FixedString64Bytes SecureCorridorTitle = "Secure the corridor";
        private static readonly FixedString64Bytes OpenBuildTitle = "Open Build";
        private static readonly FixedString64Bytes SelectBarracksTitle = "Select Barracks";
        private static readonly FixedString64Bytes PlaceBarracksTitle = "Place the Barracks";
        private static readonly FixedString64Bytes ReviewResourceSpendTitle = "Review resource spend";
        private static readonly FixedString128Bytes FindSquadBody = "Select the command squad to begin.";
        private static readonly FixedString128Bytes MoveToCoverBody = "Move the squad to the marked cover position.";
        private static readonly FixedString128Bytes ConfirmThreatBody = "Inspect the armed patrol near the civilians.";
        private static readonly FixedString128Bytes EngagePatrolBody = "Attack the confirmed hostile patrol.";
        private static readonly FixedString128Bytes SecureCorridorBody = "Check the objective and secure the civilian route.";
        private static readonly FixedString128Bytes OpenBuildBody = "Open Build to restore the forward post.";
        private static readonly FixedString128Bytes SelectBarracksBody = "Select Barracks from the building catalog.";
        private static readonly FixedString128Bytes PlaceBarracksBody = "Place the Barracks inside the green footprint. The confirmation bar shows its exact cost.";
        private static readonly FixedString128Bytes ReviewResourceSpendBody = "Credits and Materials were spent by the real construction order. Check the resource bar.";
        private static readonly FixedString128Bytes ContextualTargetHint = " Use Show Me if you need the exact target.";
        private static readonly FixedString64Bytes DoItAction = "DO IT";
        private static readonly FixedString64Bytes ShowMeAction = "SHOW ME";
        private static readonly FixedString64Bytes ContinueAction = "CONTINUE";
        private static readonly FixedString64Bytes MoveTargetAnchor = "anchor.ch01.m01.move_target";
        private static readonly FixedString64Bytes PatrolObjectiveAnchor = "anchor.ch01.m01.patrol_objective";
        private static readonly FixedString64Bytes CivilianSafeZoneAnchor = "anchor.ch01.m01.civilian_safe_zone";
        private static readonly FixedString64Bytes EstablishBaseMissionId = "saga.ch01.m02.establish_base";
        private static readonly FixedString64Bytes EstablishBaseBuildAnchor = "anchor.ch01.m02.build_lot";
        private static readonly FixedString64Bytes BuildButtonTarget = "ui.match.build";
        private static readonly FixedString64Bytes BarracksCatalogTarget = "ui.build_drawer.barracks";
        private static readonly FixedString64Bytes ResourceStripTarget = "ui.match.resources";

        [BurstCompile] public void OnCreate(ref SystemState state) => state.RequireForUpdate<CampaignMissionRootComponent>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionAttemptFactsComponent facts)) return;
            AssistantSettingsComponent settings = default;
            SystemAPI.TryGetSingleton(out settings);
            EntityManager em = state.EntityManager;
            CampaignMissionGuidanceProjectionComponent current = em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            DynamicBuffer<CampaignMissionGuidanceAcknowledgementRequestElement> acknowledgements = em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            bool acknowledged = ConsumeAcknowledgements(ref current, acknowledgements, in runtime);
            Entity friendly = Entity.Null, hostile = Entity.Null; ResolveMissionEntities(ref state, in runtime, ref friendly, ref hostile);
            float3 move = default, patrol = default, build = default;
            ResolveAnchors(ref state, ref move, ref patrol, ref build);
            if (!TryBuildProjection(in current, in runtime, in facts, in settings, friendly, hostile, move, patrol, build, out var next))
            { if (acknowledged) em.SetComponentData(root, current); return; }
            em.SetComponentData(root, next);
        }

        internal static bool TryBuildProjection(in CampaignMissionGuidanceProjectionComponent current,
            in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            in AssistantSettingsComponent settings, Entity friendly, Entity hostile, float3 move, float3 patrol,
            out CampaignMissionGuidanceProjectionComponent next) =>
            TryBuildProjection(in current, in runtime, in facts, in settings, friendly, hostile, move, patrol,
                default, out next);

        internal static bool TryBuildProjection(in CampaignMissionGuidanceProjectionComponent current,
            in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            in AssistantSettingsComponent settings, Entity friendly, Entity hostile, float3 move, float3 patrol,
            float3 build,
            out CampaignMissionGuidanceProjectionComponent next)
        {
            next = current;
            bool suppressed = runtime.RunKind != MissionRunKind.FirstClear && runtime.ReplayTutorialEnabled == 0;
            if (runtime.Version == 0 || runtime.Outcome != MissionOutcomeKind.None || suppressed)
            { if (current.Active == 0) return false; next = default; next.Version = Next(current.Version); return true; }
            bool establishBase = runtime.MissionId.Equals(EstablishBaseMissionId);
            CampaignMissionGuidancePromptKind prompt = establishBase
                ? PromptForEstablishBase(in current, in runtime, in facts)
                : PromptFor(runtime.Phase);
            if (prompt == CampaignMissionGuidancePromptKind.None || !Permits(runtime.Guidance, prompt))
            { if (current.Active == 0) return false; next = default; next.Version = Next(current.Version); return true; }
            byte strength = ResolveStrength(in current, in runtime, in facts, prompt);
            int id = (establishBase ? 35000 : 25000) +
                     (int)runtime.Guidance * 100 + (int)prompt * 10 + strength;
            bool same = current.Active != 0 && current.GuidanceId == id && current.MissionSourceVersion == runtime.Version &&
                        current.GuidanceMode == runtime.Guidance;
            if (same && current.AcknowledgedGuidanceId == id) return false;
            next = Build(prompt, id, in current, in runtime, in facts, in settings, friendly, hostile, move, patrol, build);
            return !same || !ProjectionEquals(in current, in next);
        }

        private static CampaignMissionGuidanceProjectionComponent Build(CampaignMissionGuidancePromptKind prompt, int id,
            in CampaignMissionGuidanceProjectionComponent current, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts, in AssistantSettingsComponent settings,
            Entity friendly, Entity hostile, float3 move, float3 patrol)
            => Build(prompt, id, in current, in runtime, in facts, in settings, friendly, hostile, move, patrol, default);

        private static CampaignMissionGuidanceProjectionComponent Build(CampaignMissionGuidancePromptKind prompt, int id,
            in CampaignMissionGuidanceProjectionComponent current, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts, in AssistantSettingsComponent settings,
            Entity friendly, Entity hostile, float3 move, float3 patrol, float3 build)
        {
            CampaignMissionGuidanceProjectionComponent next = new()
            {
                GuidanceId = id, Version = Next(current.Version), MissionSourceVersion = runtime.Version, Prompt = prompt,
                GuidanceMode = runtime.Guidance, HintStrength = ResolveStrength(in current, in runtime, in facts, prompt),
                Priority = AssistantMessagePriority.High, Active = 1, AcknowledgedGuidanceId = current.AcknowledgedGuidanceId,
                CooldownUntilMilliseconds = math.max(facts.ElapsedMilliseconds, current.CooldownUntilMilliseconds) + 3000,
                SubtitlesEnabled = settings.SubtitlesEnabled, LargeTextEnabled = settings.LargeTextEnabled,
                HighContrastEnabled = settings.HighContrastEnabled, CanShow = 1
            };
            switch (prompt)
            {
                case CampaignMissionGuidancePromptKind.FindSquad:
                    Set(ref next, AssistantRecommendationKind.Select, AssistantTargetKind.Squad, FindSquadTitle, FindSquadBody, DoItAction);
                    next.TargetEntity = friendly; next.CanExecute = friendly != Entity.Null ? (byte)1 : (byte)0; break;
                case CampaignMissionGuidancePromptKind.MoveToCover:
                    Set(ref next, AssistantRecommendationKind.Move, AssistantTargetKind.WorldPosition, MoveToCoverTitle, MoveToCoverBody, DoItAction);
                    next.SourceEntity = friendly; next.WorldPosition = move; next.HasWorldPosition = 1; next.CanExecute = friendly != Entity.Null ? (byte)1 : (byte)0; break;
                case CampaignMissionGuidancePromptKind.ConfirmThreat:
                    Set(ref next, AssistantRecommendationKind.Attack, AssistantTargetKind.Entity, ConfirmThreatTitle, ConfirmThreatBody, DoItAction);
                    next.SourceEntity = friendly; next.TargetEntity = hostile; next.TargetId = PatrolObjectiveAnchor;
                    next.WorldPosition = patrol; next.HasWorldPosition = 1;
                    next.CanExecute = friendly != Entity.Null && hostile != Entity.Null ? (byte)1 : (byte)0; break;
                case CampaignMissionGuidancePromptKind.Engage:
                    Set(ref next, AssistantRecommendationKind.Attack, AssistantTargetKind.Entity, EngagePatrolTitle, EngagePatrolBody, DoItAction);
                    next.SourceEntity = friendly; next.TargetEntity = hostile; next.CanExecute = friendly != Entity.Null && hostile != Entity.Null ? (byte)1 : (byte)0; break;
                case CampaignMissionGuidancePromptKind.SecureCorridor:
                    Set(ref next, AssistantRecommendationKind.CameraFocus, AssistantTargetKind.Objective, SecureCorridorTitle, SecureCorridorBody, DoItAction);
                    next.TargetId = CivilianSafeZoneAnchor; next.WorldPosition = patrol; next.HasWorldPosition = 1;
                    next.CanExecute = 1; break;
                case CampaignMissionGuidancePromptKind.EstablishBaseOpenBuild:
                    Set(ref next, AssistantRecommendationKind.Build, AssistantTargetKind.UiSurface, OpenBuildTitle, OpenBuildBody, DoItAction);
                    next.TargetId = BuildButtonTarget; next.CanExecute = 1; break;
                case CampaignMissionGuidancePromptKind.EstablishBaseSelectBarracks:
                    Set(ref next, AssistantRecommendationKind.Select, AssistantTargetKind.UiSurface, SelectBarracksTitle, SelectBarracksBody, DoItAction);
                    next.TargetId = BarracksCatalogTarget; next.CanExecute = 1; break;
                case CampaignMissionGuidancePromptKind.EstablishBasePlaceBarracks:
                    Set(ref next, AssistantRecommendationKind.Build, AssistantTargetKind.WorldPosition, PlaceBarracksTitle, PlaceBarracksBody, DoItAction);
                    next.TargetId = EstablishBaseBuildAnchor; next.WorldPosition = build; next.HasWorldPosition = 1;
                    next.CanExecute = 1; break;
                case CampaignMissionGuidancePromptKind.EstablishBaseObserveResourceSpend:
                    Set(ref next, AssistantRecommendationKind.Explain, AssistantTargetKind.UiSurface, ReviewResourceSpendTitle, ReviewResourceSpendBody, ContinueAction);
                    next.TargetId = ResourceStripTarget; next.CanExecute = 1; break;
            }
            ApplyModePolicy(ref next);
            return next;
        }

        private static void ApplyModePolicy(ref CampaignMissionGuidanceProjectionComponent next)
        {
            if (next.GuidanceMode == NarrativeGuidanceMode.Full) return;
            if (next.GuidanceMode == NarrativeGuidanceMode.Minimal)
            { next.CanExecute = 0; next.ActionLabel = ShowMeAction; return; }
            if (next.Prompt != CampaignMissionGuidancePromptKind.FindSquad &&
                next.Prompt != CampaignMissionGuidancePromptKind.MoveToCover &&
                next.Prompt != CampaignMissionGuidancePromptKind.EstablishBaseOpenBuild &&
                next.Prompt != CampaignMissionGuidancePromptKind.EstablishBaseSelectBarracks &&
                next.Prompt != CampaignMissionGuidancePromptKind.EstablishBasePlaceBarracks &&
                next.Prompt != CampaignMissionGuidancePromptKind.EstablishBaseObserveResourceSpend)
            { next.CanExecute = 0; next.ActionLabel = ShowMeAction; }
            if (next.HintStrength >= 2)
            {
                FixedString128Bytes body = next.Body; body.Append(ContextualTargetHint); next.Body = body;
            }
        }

        private static bool Permits(NarrativeGuidanceMode mode, CampaignMissionGuidancePromptKind prompt) =>
            mode != NarrativeGuidanceMode.Minimal || prompt == CampaignMissionGuidancePromptKind.ConfirmThreat ||
            prompt == CampaignMissionGuidancePromptKind.SecureCorridor;

        private static byte ResolveStrength(in CampaignMissionGuidanceProjectionComponent current,
            in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            CampaignMissionGuidancePromptKind prompt)
        {
            if (runtime.Guidance != NarrativeGuidanceMode.Contextual) return 0;
            bool same = current.Active != 0 && current.GuidanceMode == runtime.Guidance && current.Prompt == prompt &&
                        current.MissionSourceVersion == runtime.Version;
            if (!same) return 1;
            return facts.ElapsedMilliseconds >= current.CooldownUntilMilliseconds ? (byte)2 : current.HintStrength;
        }

        private static void Set(ref CampaignMissionGuidanceProjectionComponent p, AssistantRecommendationKind kind,
            AssistantTargetKind target, FixedString64Bytes title, FixedString128Bytes body, FixedString64Bytes action)
        { p.RecommendationKind = kind; p.TargetKind = target; p.Title = title; p.Body = body; p.ActionLabel = action; }

        private static CampaignMissionGuidancePromptKind PromptFor(MissionPhaseKind phase) => phase switch
        {
            MissionPhaseKind.FindSquad => CampaignMissionGuidancePromptKind.FindSquad,
            MissionPhaseKind.MoveToCover => CampaignMissionGuidancePromptKind.MoveToCover,
            MissionPhaseKind.ConfirmThreat => CampaignMissionGuidancePromptKind.ConfirmThreat,
            MissionPhaseKind.Engage => CampaignMissionGuidancePromptKind.Engage,
            MissionPhaseKind.SecureCorridor => CampaignMissionGuidancePromptKind.SecureCorridor, _ => CampaignMissionGuidancePromptKind.None
        };

        internal static CampaignMissionGuidancePromptKind PromptForEstablishBase(
            in CampaignMissionGuidanceProjectionComponent current,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if (!runtime.MissionId.Equals(EstablishBaseMissionId) ||
                runtime.Phase is not (MissionPhaseKind.FindSquad or MissionPhaseKind.Engage))
            {
                return CampaignMissionGuidancePromptKind.None;
            }

            if (facts.RequiredBuildingPlacedCount > 0)
            {
                if (current.Prompt == CampaignMissionGuidancePromptKind.EstablishBaseObserveResourceSpend &&
                    current.GuidanceId != 0 && current.AcknowledgedGuidanceId == current.GuidanceId)
                    return CampaignMissionGuidancePromptKind.None;
                return CampaignMissionGuidancePromptKind.EstablishBaseObserveResourceSpend;
            }

            if (current.Prompt == CampaignMissionGuidancePromptKind.EstablishBasePlaceBarracks)
                return CampaignMissionGuidancePromptKind.EstablishBasePlaceBarracks;
            if (current.Prompt == CampaignMissionGuidancePromptKind.EstablishBaseSelectBarracks)
                return current.GuidanceId != 0 && current.AcknowledgedGuidanceId == current.GuidanceId
                    ? CampaignMissionGuidancePromptKind.EstablishBasePlaceBarracks
                    : CampaignMissionGuidancePromptKind.EstablishBaseSelectBarracks;
            if (current.Prompt == CampaignMissionGuidancePromptKind.EstablishBaseOpenBuild &&
                current.GuidanceId != 0 && current.AcknowledgedGuidanceId == current.GuidanceId)
            {
                return CampaignMissionGuidancePromptKind.EstablishBaseSelectBarracks;
            }
            return CampaignMissionGuidancePromptKind.EstablishBaseOpenBuild;
        }

        private static bool ConsumeAcknowledgements(ref CampaignMissionGuidanceProjectionComponent current,
            DynamicBuffer<CampaignMissionGuidanceAcknowledgementRequestElement> requests, in CampaignMissionRuntimeComponent runtime)
        {
            int before = current.AcknowledgedGuidanceId;
            for (int i = 0; i < requests.Length; i++) if (requests[i].GuidanceId == current.GuidanceId &&
                requests[i].SessionToken.Equals(runtime.SessionToken) && requests[i].AttemptOrdinal == runtime.AttemptOrdinal)
                current.AcknowledgedGuidanceId = requests[i].GuidanceId;
            requests.Clear(); return before != current.AcknowledgedGuidanceId;
        }

        private void ResolveMissionEntities(ref SystemState state, in CampaignMissionRuntimeComponent runtime,
            ref Entity friendly, ref Entity hostile)
        {
            foreach ((RefRO<CampaignMissionUnitRoleComponent> role, RefRO<Faction> faction, Entity entity) in
                     SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>, RefRO<Faction>>().WithEntityAccess())
            { if (!role.ValueRO.SessionToken.Equals(runtime.SessionToken)) continue;
              if (FactionIdentity.IsPlayerControlled(faction.ValueRO.Id) && friendly == Entity.Null) friendly = entity;
              else if (!FactionIdentity.IsPlayerControlled(faction.ValueRO.Id) && hostile == Entity.Null) hostile = entity; }
        }

        private void ResolveAnchors(ref SystemState state, ref float3 move, ref float3 patrol, ref float3 build)
        {
            if (!SystemAPI.TryGetSingleton(out ActiveOperationMapComponent active) || !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) ||
                !metadata.Blob.IsCreated || metadata.Generation != active.Generation) return;
            ref OperationMapBlob map = ref metadata.Blob.Value;
            if (CampaignMissionSpawnSystem.TryFindAnchor(ref map, MoveTargetAnchor, out var a)) move = a.Position;
            if (CampaignMissionSpawnSystem.TryFindAnchor(ref map, PatrolObjectiveAnchor, out var b)) patrol = b.Position;
            if (CampaignMissionSpawnSystem.TryFindAnchor(ref map, EstablishBaseBuildAnchor, out var c)) build = c.Position;
        }

        private static bool ProjectionEquals(in CampaignMissionGuidanceProjectionComponent a, in CampaignMissionGuidanceProjectionComponent b) =>
            a.GuidanceId == b.GuidanceId && a.Prompt == b.Prompt && a.GuidanceMode == b.GuidanceMode &&
            a.HintStrength == b.HintStrength && a.TargetEntity == b.TargetEntity && a.SourceEntity == b.SourceEntity &&
            a.RecommendationKind == b.RecommendationKind && a.TargetKind == b.TargetKind &&
            a.TargetCell.Equals(b.TargetCell) && a.WorldPosition.Equals(b.WorldPosition) &&
            a.HasWorldPosition == b.HasWorldPosition && a.TargetId.Equals(b.TargetId) &&
            a.CanShow == b.CanShow && a.CanExecute == b.CanExecute &&
            a.AcknowledgedGuidanceId == b.AcknowledgedGuidanceId && a.SubtitlesEnabled == b.SubtitlesEnabled &&
            a.LargeTextEnabled == b.LargeTextEnabled && a.HighContrastEnabled == b.HighContrastEnabled;
        private static uint Next(uint value) => value == uint.MaxValue ? uint.MaxValue : value + 1u;
    }
}
