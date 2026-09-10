using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Observes real mission fire/audio with an isolated save; never writes combat state.</summary>
    [InitializeOnLoad]
    public static class MissionTowerFireEditorProbe
    {
        private const string Active = "Warline.TowerFire.Active";
        private static readonly HashSet<int> AudioIds = new();
        private static readonly Dictionary<Entity, int> TowerShots = new();
        private static readonly List<string> Rows = new();
        private static readonly HashSet<string> SourceClips = new();
        private static readonly HashSet<Entity> EngineSources = new();
        private static string mission, output, error;
        private static bool prepared, deployed, strict;
        private static double started, lastClick, lastInventory;
        private static float observeAt = -1;
        private static int weaponAudio, authoredAudio, authoredShots, maxTowers, maxDormant;
        private static int authoredEngineAudio;
        private static int infantryEngineAudio;

        static MissionTowerFireEditorProbe()
        {
            if (SessionState.GetBool(Active, false)) EditorApplication.update += Tick;
        }

        public static void RunM02Baseline() => Start(M02EstablishBaseConfigBuilder.MissionId, false);
        public static void RunM02() => Start(M02EstablishBaseConfigBuilder.MissionId, true);
        public static void RunM03() => Start(M03RadarWarningConfigBuilder.MissionId, true);
        public static void RunM04() => Start(M04AirliftConfigBuilder.MissionId, true);

        private static void Start(string id, bool validate)
        {
            mission = id; strict = validate;
            output = Path.Combine(Path.GetTempPath(), "warline-tower-fire", id + (strict ? "-fixed" : "-baseline"));
            Directory.CreateDirectory(output);
            AudioIds.Clear(); TowerShots.Clear(); Rows.Clear(); SourceClips.Clear(); EngineSources.Clear();
            prepared = deployed = false; error = null; observeAt = -1;
            weaponAudio = authoredAudio = authoredShots = maxTowers = maxDormant = 0;
            authoredEngineAudio = infantryEngineAudio = 0;
            started = EditorApplication.timeSinceStartup; lastClick = lastInventory = 0;
            SessionState.SetBool(Active, true);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Application.logMessageReceived += ObserveError;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                if (error != null) throw new InvalidOperationException(error);
                if (EditorApplication.timeSinceStartup - started > 240) throw new TimeoutException("Tower fire mission observation timed out.");
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                using var roots = em.CreateEntityQuery(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent));
                if (roots.CalculateEntityCount() != 1) return;
                Entity root = roots.GetSingletonEntity();
                if (!prepared)
                {
                    if (!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root)) return;
                    var store = new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(
                        Path.Combine(output, "save-" + Guid.NewGuid().ToString("N")))));
                    store.EnsureAvailable(mission);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store = store;
                    prepared = true; return;
                }
                if (!deployed)
                {
                    if (!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign) || !campaign.IsValid) return;
                    if (campaign.SelectedMission.MissionId != mission)
                    { UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select, mission); return; }
                    deployed = UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy, mission);
                    return;
                }
                var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if (runtime.MissionId.ToString() != mission) return;
                ObserveCombat(em);
                SkipNarrative();
                var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                if (!em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)) return;
                var opening = em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root);
                if (facts.InteractiveBriefCompleted == 0 || opening.Stage < 7) return;
                if (observeAt < 0)
                {
                    observeAt = Time.time;
                    if (mission == M02EstablishBaseConfigBuilder.MissionId) MoveSquadToBase(em);
                }
                if (Time.time - observeAt < 40) return;
                bool quiet = authoredAudio == 0 && authoredShots == 0 && authoredEngineAudio == 0 && infantryEngineAudio == 0 && maxDormant == maxTowers &&
                    (mission != M02EstablishBaseConfigBuilder.MissionId || weaponAudio == 0);
                if (maxTowers == 0) throw new InvalidOperationException("No authored map towers were observed; this is not a valid tower audit.");
                Complete(!strict || quiet,
                    $"mission={mission} observationSeconds=40 towers={maxTowers} dormant={maxDormant} authoredShots={authoredShots} authoredWeaponAudio={authoredAudio} authoredEngineAudio={authoredEngineAudio} infantryEngineAudio={infantryEngineAudio} totalWeaponAudio={weaponAudio} quiet={quiet} baseline={!strict}");
            }
            catch (Exception exception) { Debug.LogException(exception); Complete(false, exception.Message); }
        }

        private static void ObserveCombat(EntityManager em)
        {
            using var towers = em.CreateEntityQuery(typeof(BuildingDefenseWeapon), typeof(BuildingDefenseAttackSlot));
            using var entities = towers.ToEntityArray(Allocator.Temp);
            int authored = 0, dormant = 0;
            foreach (var entity in entities)
            {
                bool map = em.HasComponent<OperationMapBuildingComponent>(entity);
                bool asleep = em.HasComponent<CampaignMissionDormantMapDefenseTag>(entity);
                if (map) { authored++; if (asleep) dormant++; }
                int total = 0;
                foreach (var slot in em.GetBuffer<BuildingDefenseAttackSlot>(entity, true)) total += slot.ShotCounter;
                TowerShots.TryGetValue(entity, out int previous);
                if (!TowerShots.ContainsKey(entity)) Rows.Add($"tower={Describe(em, entity)} authored={map} dormant={asleep}");
                if (total > previous)
                {
                    if (map) authoredShots += total - previous;
                    Rows.Add($"shot source={entity} authored={map} dormant={asleep} count={total - previous}");
                    foreach (var slot in em.GetBuffer<BuildingDefenseAttackSlot>(entity, true))
                        if (slot.Target != Entity.Null) Rows.Add("  target=" + Describe(em, slot.Target));
                }
                TowerShots[entity] = total;
            }
            maxTowers = Math.Max(maxTowers, authored); maxDormant = Math.Max(maxDormant, dormant);
            using var audioQuery = em.CreateEntityQuery(typeof(AudioPlaybackRequestElement));
            if (audioQuery.CalculateEntityCount() == 1)
            {
                foreach (var request in em.GetBuffer<AudioPlaybackRequestElement>(audioQuery.GetSingletonEntity(), true))
                {
                    if (!AudioIds.Add(request.RequestId)) continue;
                    bool map = em.Exists(request.SourceEntity) && (em.HasComponent<OperationMapBuildingComponent>(request.SourceEntity) ||
                        em.HasComponent<OperationMapAuthoredVehiclePresentation>(request.SourceEntity));
                    if (request.EventId.ToString().StartsWith("Gameplay.Unit.Engine.", StringComparison.Ordinal))
                    {
                        if (map) authoredEngineAudio++;
                        if (em.Exists(request.SourceEntity) && em.HasComponent<UnitMovementBehavior>(request.SourceEntity) &&
                            em.GetComponentData<UnitMovementBehavior>(request.SourceEntity).UsesVehicleMotion == 0 &&
                            !em.HasComponent<UnitAirMovement>(request.SourceEntity)) infantryEngineAudio++;
                        if (EngineSources.Add(request.SourceEntity))
                            Rows.Add($"engine event={request.EventId} source={Describe(em, request.SourceEntity)} authored={map}");
                    }
                    if (!request.EventId.ToString().StartsWith("Gameplay.Weapon.", StringComparison.Ordinal)) continue;
                    weaponAudio++;
                    if (map) authoredAudio++;
                    Rows.Add($"audio id={request.RequestId} event={request.EventId} status={request.Status} source={Describe(em, request.SourceEntity)} authored={map}");
                }
            }
            if (EditorApplication.timeSinceStartup - lastInventory < 5) return;
            lastInventory = EditorApplication.timeSinceStartup;
            foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (!source.isPlaying || source.clip == null) continue;
                string description = $"playing source={source.name} clip={source.clip.name} loop={source.loop} spatial={source.spatialBlend}";
                if (SourceClips.Add(description)) Rows.Add(description);
            }
            Debug.Log($"[MissionTowerFire] mission={mission} towers={authored} dormant={dormant} authoredShots={authoredShots} weaponAudio={weaponAudio}");
        }

        private static void MoveSquadToBase(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(Faction), typeof(UnitGrid));
            using var units = query.ToEntityArray(Allocator.Temp);
            int index = 0;
            foreach (var unit in units)
            {
                if (em.GetComponentData<Faction>(unit).Id != FactionIdentity.PlayerFactionId) continue;
                var destination = new int2(1012 + index * 2, 348);
                UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(em, unit, destination);
                Rows.Add($"ordered base approach unit={Describe(em, unit)} destination={destination}"); index++;
            }
            if (index != 4) throw new InvalidOperationException("M2 approach must command all four real squad members.");
        }

        private static string Describe(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity)) return entity + " removed";
            string key = em.HasComponent<UnitSourcePrefabKey>(entity) ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() : em.GetName(entity);
            return $"{entity} {key} faction={(em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : -1)} building={em.HasComponent<RuntimeBuildingCombatTag>(entity)}";
        }

        private static void SkipNarrative()
        {
            if (EditorApplication.timeSinceStartup - lastClick < .75) return;
            var narrative = UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            if (narrative == null || !Visible(narrative, "rootGroup")) return;
            var confirmation = narrative.SkipConfirmationView;
            bool confirming = confirmation != null && Visible(confirmation, "group");
            object owner = confirming ? (object)confirmation : narrative.PlaybackControlsView;
            var button = owner?.GetType().GetField(confirming ? "confirmButton" : "skipButton", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner) as Button;
            if (button == null || !button.isActiveAndEnabled || !button.interactable) return;
            button.onClick.Invoke(); lastClick = EditorApplication.timeSinceStartup;
        }
        private static bool Visible(object owner, string field)
        {
            var group = owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;
            return group != null && group.alpha > .9f && group.gameObject.activeInHierarchy;
        }
        private static void ObserveError(string message, string stack, LogType type)
        {
            if (MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message, stack, type)) return;
            if (type is LogType.Exception or LogType.Assert) error = message;
        }
        private static void Complete(bool passed, string detail)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= ObserveError;
            SessionState.SetBool(Active, false);
            Rows.Add($"result={(passed ? "Passed" : "Failed")} {detail}");
            File.WriteAllLines(Path.Combine(output, "audit.txt"), Rows);
            Debug.Log("[MissionTowerFire] " + Rows[Rows.Count - 1]);
            MissionEditorValidationExit.Complete(passed);
        }
    }
}
