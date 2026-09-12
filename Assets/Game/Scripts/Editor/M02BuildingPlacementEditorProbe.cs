using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Exercises M2's visible build buttons, construction and production with an isolated save.</summary>
    [InitializeOnLoad]
    public static partial class M02BuildingPlacementEditorProbe
    {
        private const string Active = "Warline.M02.PlacementProbe.Active";
        private const string Output = "/private/tmp/warline-m02-placement";
        private static bool prepared, deployed, capturedPreview;
        private static double started, nextAction, nextLog;
        private static int step;
        private static string error;

        static M02BuildingPlacementEditorProbe()
        {
            if (SessionState.GetBool(Active, false)) EditorApplication.update += Tick;
        }

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/audit.txt", "M2 placement run " + DateTime.UtcNow.ToString("O") + "\n");
            prepared = deployed = capturedPreview = false; step = 0; error = null; nextAction = nextLog = 0;
            started = EditorApplication.timeSinceStartup;
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
            if (SessionState.GetBool(RifleSingleClickMode, false) && !insideRifleGameFrame)
            {
                EnsureRifleGameFrame();
                return;
            }
            try
            {
                if (error != null) throw new InvalidOperationException(error);
                double deadline = SessionState.GetBool(RifleSingleClickMode, false) ? 200 : 480;
                if (EditorApplication.timeSinceStartup - started > deadline) throw new TimeoutException("M2 placement/production deadline, step=" + step);
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                using var roots = em.CreateEntityQuery(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent));
                if (roots.CalculateEntityCount() != 1) return;
                Entity root = roots.GetSingletonEntity();
                if (!prepared)
                {
                    if (!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root)) return;
                    var store = new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Output + "/save-" + Guid.NewGuid().ToString("N"))));
                    store.Settle("saga.ch01.m01.first_contact", "qa-prerequisite", 0, true, 3, 30000, M02EstablishBaseConfigBuilder.MissionId);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store = store;
                    prepared = true; return;
                }
                if (!deployed)
                {
                    if (!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign) || !campaign.IsValid) return;
                    if (campaign.SelectedMission.MissionId != M02EstablishBaseConfigBuilder.MissionId)
                    { UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select, M02EstablishBaseConfigBuilder.MissionId); return; }
                    deployed = UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy, M02EstablishBaseConfigBuilder.MissionId);
                    return;
                }
                SkipNarrative();
                if (step >= 7) { AdvanceCampaignReturn(em, root); return; }
                if (!em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)) return;
                var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                if (facts.InteractiveBriefCompleted == 0 || em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage < 7) return;
                var match = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
                var command = match?.MatchBootstrap?.BuildingUiCommandContract;
                if (command == null) return;
                if (EditorApplication.timeSinceStartup >= nextLog)
                {
                    nextLog = EditorApplication.timeSinceStartup + 5;
                    Log($"step={step} placed={facts.RequiredBuildingPlacedCount} completed={facts.RequiredBuildingCompletedCount} produced={facts.RequiredUnitProducedCount} pending={command.HasPendingBuildingPlacement} valid={command.CanConfirmBuildingPlacement} status={command.PlacementStatusText}");
                }
                if (EditorApplication.timeSinceStartup < nextAction) return;
                var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>();
                switch (step)
                {
                    case 0:
                        if (!Click(controls?.BuildButton)) return;
                        Next(); break;
                    case 1:
                        var item = catalog?.GetType().GetMethod("ResolveBarracksGuidanceButton", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(catalog, null) as Button;
                        if (!Click(item)) return;
                        Next(); break;
                    case 2:
                        InspectLot(em, match);
                        if (!Click(drawer?.BuildButton)) return;
                        Log($"after place pending={command.HasPendingBuildingPlacement} valid={command.CanConfirmBuildingPlacement} status={command.PlacementStatusText}");
                        if (!command.HasPendingBuildingPlacement) throw new InvalidOperationException("Place button did not start a preview.");
                        Next(); break;
                    case 3:
                        if (!command.CanConfirmBuildingPlacement) throw new InvalidOperationException("Canonical barracks preview has no valid placement: " + command.PlacementStatusText);
                        if (!capturedPreview)
                        {
                            capturedPreview = true; ScreenCapture.CaptureScreenshot(Output + "/preview.png");
                            nextAction = EditorApplication.timeSinceStartup + 1; return;
                        }
                        var confirmation = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>();
                        if (!Click(confirmation?.ConfirmButton)) return;
                        if (command.HasPendingBuildingPlacement) throw new InvalidOperationException("Confirm did not commit the building.");
                        Next(); break;
                    case 4:
                        if (SessionState.GetBool(RifleSingleClickMode, false))
                        {
                            AdvanceRifleSingleClick(em, root, facts, controls);
                            break;
                        }
                        if (facts.RequiredBuildingCompletedCount == 0) return;
                        if (!Click(controls?.BuildButton)) return;
                        Next(); break;
                    case 5:
                        if (catalog == null) return;
                        object[] productionArguments = { false };
                        catalog.GetType().GetMethod("TryInvokeRifleProductionFromGuidance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(catalog, productionArguments);
                        bool accepted = (bool)productionArguments[0];
                        nextAction = EditorApplication.timeSinceStartup + 1;
                        if (accepted) Next();
                        break;
                    case 6:
                        if (facts.RequiredUnitProducedCount == 0) return;
                        var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                        if (runtime.Outcome == MissionOutcomeKind.None) return;
                        if (runtime.Outcome != MissionOutcomeKind.Victory) throw new InvalidOperationException("M2 settled without Victory.");
                        if (!UiShellRuntimeGateway.TryReadMissionResult(out var result) || result.DebriefRequired || !result.PrimaryActionEnabled) return;
                        var popup = UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                        var primary = popup?.GetType().GetField("primaryButton", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(popup) as Button;
                        if (!Click(primary)) return;
                        ScreenCapture.CaptureScreenshot(Output + "/produced.png");
                        Log("M2 settled and final result Continue accepted"); Next();
                        break;
                }
            }
            catch (Exception exception) { Debug.LogException(exception); Complete(false, exception.Message); }
        }

        private static bool Click(Button button)
        {
            if (button == null || !button.isActiveAndEnabled || !button.interactable) return false;
            Log("click begin " + button.name);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            button.onClick.Invoke();
            Log($"click end {button.name} milliseconds={watch.Elapsed.TotalMilliseconds:F1}");
            if (watch.Elapsed.TotalSeconds > 1) throw new InvalidOperationException($"UI action froze for {watch.Elapsed.TotalSeconds:F2}s: {button.name}");
            return true;
        }

        private static void InspectLot(EntityManager em, MatchSceneView match)
        {
            RectInt lot = new(1006, 330, 40, 20);
            using var query = em.CreateEntityQuery(typeof(UnitGrid), typeof(UnitFootprint));
            using var units = query.ToEntityArray(Allocator.Temp);
            foreach (Entity unit in units)
            {
                bool map = em.HasComponent<OperationMapBuildingComponent>(unit);
                if (!map && (em.HasComponent<StaticGridBlocker>(unit) || em.HasComponent<RuntimeBuildingCombatTag>(unit))) continue;
                var size = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(unit).Size);
                var min = UnitFootprintUtility.GetMinCell(em.GetComponentData<UnitGrid>(unit).Cell, size);
                if (map && em.HasComponent<RuntimeBuildingCombatInfo>(unit))
                { var info = em.GetComponentData<RuntimeBuildingCombatInfo>(unit); min = info.OriginCell; size = info.FootprintCells; }
                if (lot.Overlaps(new RectInt(min.x, min.y, size.x, size.y)))
                    Log($"lot occupant={unit} {em.GetName(unit)} map={map} origin={min} size={size}");
            }
            foreach (var building in match.MatchBootstrap.BuildingUiQueryContext.RuntimeBuildings.Values)
                if (building?.Definition != null && !building.IsDestroyed && lot.Overlaps(new RectInt(building.OriginCell, building.Definition.FootprintCells)))
                    Log($"lot building={building.Definition.Prefab.name} origin={building.OriginCell} footprint={building.Definition.FootprintCells}");
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridRoad), typeof(DynamicBlockerComponent));
            if (grids.CalculateEntityCount() == 1)
            {
                var gridEntity = grids.GetSingletonEntity(); var grid = em.GetComponentData<GridConfig>(gridEntity);
                var roads = em.GetBuffer<GridRoad>(gridEntity); var blocker = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
                int road = 0, blocked = 0;
                for (int y = lot.yMin; y < lot.yMax; y++) for (int x = lot.xMin; x < lot.xMax; x++)
                { int i = y * grid.Width + x; if (roads[i].Value != 0) road++; if (blocker.Blocked.IsCreated && blocker.Blocked.IsSet(i)) blocked++; }
                Log($"lot roads={road} blockedCells={blocked} grid={grid.Width}x{grid.Height}");
            }
        }
        private static void Next() { step++; nextAction = EditorApplication.timeSinceStartup + 2; }
        private static void SkipNarrative()
        {
            if (EditorApplication.timeSinceStartup < nextAction) return;
            var narrative = UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            if (narrative == null || !Visible(narrative, "rootGroup")) return;
            var confirmation = narrative.SkipConfirmationView;
            bool confirming = confirmation != null && Visible(confirmation, "group");
            object owner = confirming ? (object)confirmation : narrative.PlaybackControlsView;
            var button = owner?.GetType().GetField(confirming ? "confirmButton" : "skipButton", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner) as Button;
            if (button != null && button.isActiveAndEnabled && button.interactable) { button.onClick.Invoke(); nextAction = EditorApplication.timeSinceStartup + .75; }
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
        private static void Log(string detail) { File.AppendAllText(Output + "/audit.txt", detail + "\n"); Debug.Log("[M02PlacementProbe] " + detail); }
        private static void Complete(bool passed, string detail)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= ObserveError;
            SessionState.SetBool(Active, false);
            SessionState.SetBool(RifleSingleClickMode, false);
            Log("result=" + (passed ? "Passed " : "Failed ") + detail);
            MissionEditorValidationExit.Complete(passed);
        }
    }
}
