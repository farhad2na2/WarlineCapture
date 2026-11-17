#if UNITY_EDITOR
// =============================================================================
// RunCatalogSaveResumeAndExit - generic gate-4 (checkpoint and recovery) journey for any catalog
// that the product itself lets the player save, driven through the real native UI by Touch:
//   generic dispatch (RunFocusedAriaAndExit) -> ARIA plays -> after WARLINE_SAVE_RESUME_AFTER_SECONDS
//   (default 60) of match time: stop ARIA through its visible control (AriaInstantStop), then Pause -> Save & Exit -> setup screen -> Continue -> verify -> ARIA
//   continues to the result -> terminal probe (return to menu).
//
//   WARLINE_SKIRMISH_CATALOG=S026 WARLINE_S002_SEED=<seed> WARLINE_S002_LOCALE=en \
//   Tools/CI/invoke_unity_macos.sh --timeout 2400 --log <log> -- \
//     -executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunCatalogSaveResumeAndExit
// (no -quit; the harness quits at the end). Marker, written before the terminal probe:
//   [SkirmishSaveResume] catalog=<id> result=Passed|Failed reason=...
//
// Verified after Continue: same attempt (session id) as saved on disk and live, clock continuity,
// own/enemy living units, buildings and designated base health within tolerance, old battlefield
// entities gone, recovery record complete. The touches use a separate Touch helper that the ARIA
// command-receipt audit does not count; the S005 helpers PlayerResumeTap / TryPresentedButtonPoint
// are reused. Nothing here changes gameplay, ARIA decisions or stats. This is checkpoint evidence,
// not an ARIA win row: the run's ledger row must not be counted as a plain ARIA victory.
// The runtime offers Save only for S005 and generic Base Assault catalogs
// (SkirmishNativePlayerSaveService.SupportsGenericSave); elsewhere the probe fails with
// reason=saveUnavailable, which is the honest answer, not a harness fault.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private const float CatalogSaveResumeDefaultAfterSeconds = 60f;
        private const int CatalogSaveResumeUnitTolerance = 2;      // plus 5 percent of the saved count
        private const int CatalogSaveResumeBuildingTolerance = 1;
        private const float CatalogSaveResumeBaseHpFraction = .03f; // plus 1 hp
        private const float CatalogSaveResumeClockSlackSeconds = 10f;
        // Bounded retries. ARIA's own touchscreen presses while it plays; a press that lands while this probe's touchscreen
        // is listening is seen as a physical takeover and stops it (sr26/sr26b: physicalTakeover, no gesture ever started).
        private const int CatalogSaveResumeMaxInterruptions = 8;
        private const int CatalogSaveResumeMaxPauseTaps = 10;
        private const int CatalogSaveResumeMaxStopTaps = 6;
        private const double CatalogSaveResumeStopVisibleSeconds = 25d;
        private const double CatalogSaveResumePauseVisibleSeconds = 25d;
        private const double CatalogSaveResumePopupVisibleSeconds = 30d;

        private static bool catalogSaveResumeActive, catalogSaveResumeDone;
        private static int catalogSaveResumeStep, catalogSaveResumeLoggedStep, catalogSaveResumeStopTaps;
        private static double catalogSaveResumeDeadline, catalogSaveResumeStartedAt, catalogSaveResumeStepSince;
        private static int catalogSaveResumeInterruptions, catalogSaveResumePauseTaps, catalogSaveResumeTapStep;
        private static string catalogSaveResumeSessionId;
        private static float catalogSaveResumeSavedElapsed;
        private static CatalogSaveResumeCounts catalogSaveResumeSaved;
        private static readonly List<Entity> catalogSaveResumeOldEntities = new();

        private struct CatalogSaveResumeCounts
        {
            public int OwnUnits, EnemyUnits, OwnBuildings, EnemyBuildings, OwnBaseHp, EnemyBaseHp;
            public override string ToString() => "units=" + OwnUnits + "/" + EnemyUnits + " buildings=" + OwnBuildings + "/" + EnemyBuildings +
                " baseHp=" + OwnBaseHp + "/" + EnemyBaseHp;
        }

        public static void RunCatalogSaveResumeAndExit()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG")))
                throw new InvalidOperationException("RunCatalogSaveResumeAndExit requires WARLINE_SKIRMISH_CATALOG.");
            catalogSaveResumeActive = catalogSaveResumeDone = false;
            Environment.SetEnvironmentVariable("WARLINE_CATALOG_SAVE_RESUME", "1");
            // Same play-mode contract as the S005 journey entries: statics survive entering Play.
            SessionState.SetBool("S004EditorJourney.OptionsSaved", true);
            SessionState.SetBool("S004EditorJourney.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("S004EditorJourney.Options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", true);
            Environment.SetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY", null);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_TERMINAL_ACTION")))
                Environment.SetEnvironmentVariable("WARLINE_TERMINAL_ACTION", "MAIN MENU");
            try { RunFocusedAriaAndExit(); }
            catch
            {
                Environment.SetEnvironmentVariable("WARLINE_CATALOG_SAVE_RESUME", null);
                AssetDatabase.AllowAutoRefresh();
                SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", false);
                EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("S004EditorJourney.OptionsEnabled", false);
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("S004EditorJourney.Options", 0);
                SessionState.SetBool("S004EditorJourney.OptionsSaved", false);
                throw;
            }
        }

        // Called from the main Tick after the per-tick trace, once ARIA is playing. True = the probe owns the frame.
        private static bool TryStartCatalogSaveResume(EntityManager em, Entity session, in SkirmishMatchState match,
            bool simulationActive, float elapsed, double now)
        {
            if (Environment.GetEnvironmentVariable("WARLINE_CATALOG_SAVE_RESUME") != "1" || catalogSaveResumeDone || catalogSaveResumeActive) return false;
            if (stage < 3 || match.Phase != SkirmishPhase.Playing || !simulationActive) return false;
            float after = float.TryParse(Environment.GetEnvironmentVariable("WARLINE_SAVE_RESUME_AFTER_SECONDS"), NumberStyles.Float,
                CultureInfo.InvariantCulture, out float parsed) && parsed >= 0f ? parsed : CatalogSaveResumeDefaultAfterSeconds;
            if (elapsed < after || !UiShellRuntimeGateway.ReadAriaPlay().Active) return false;

            catalogSaveResumeActive = true;
            catalogSaveResumeStep = 0; catalogSaveResumeLoggedStep = -1; catalogSaveResumeStopTaps = 0; // step 0: stop ARIA first
            catalogSaveResumeSessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString();
            catalogSaveResumeDeadline = now + 420d;
            catalogSaveResumeStartedAt = now;
            playerResumeDue = now + 1d;
            playerResumeDiagnosticDue = 0d;
            playerResumeContinueStableAt = playerResumeContinueAttemptAt = 0d;
            // Created idle: the touchscreen is only added around one gesture (CatalogSaveResumeTap), so ARIA's own
            // presses are never mistaken for a player takeover of this probe.
            playerResumeTouch = new AriaTouchInputUiSystemHelper();
            catalogSaveResumeInterruptions = catalogSaveResumePauseTaps = catalogSaveResumeTapStep = 0;
            catalogSaveResumeStepSince = now;
            var runner = new GameObject("CatalogSaveResumeInputProbe");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<SkirmishCatalogSaveResumeFrame>();
            Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=Begin afterSeconds=" + after.ToString(CultureInfo.InvariantCulture) +
                " matchSeconds=" + elapsed.ToString("0.#", CultureInfo.InvariantCulture) + " aria=Active session=" + catalogSaveResumeSessionId);
            return true;
        }

        private static CatalogSaveResumeCounts ReadCatalogSaveResumeCounts(EntityManager em, Entity session)
        {
            var counts = new CatalogSaveResumeCounts();
            using (var units = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(UnitHealth)))
            using (var entities = units.ToEntityArray(Allocator.Temp))
                foreach (var unit in entities)
                {
                    if (em.GetComponentData<UnitHealth>(unit).Current <= 0) continue;
                    byte faction = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).FactionId;
                    if (faction == 1) counts.OwnUnits++; else if (faction == 2) counts.EnemyUnits++;
                }
            using (var buildings = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(RuntimeBuildingCombatInfo)))
            using (var entities = buildings.ToEntityArray(Allocator.Temp))
                foreach (var building in entities)
                {
                    if (em.HasComponent<UnitHealth>(building) && em.GetComponentData<UnitHealth>(building).Current <= 0) continue;
                    byte faction = em.GetComponentData<SkirmishAttemptOwnedComponent>(building).FactionId;
                    if (faction == 1) counts.OwnBuildings++; else if (faction == 2) counts.EnemyBuildings++;
                }
            counts.OwnBaseHp = DesignatedBaseHp(em, session, 1);
            counts.EnemyBaseHp = DesignatedBaseHp(em, session, 2);
            return counts;
        }

        private static string CatalogSaveResumeDifference(in CatalogSaveResumeCounts before, in CatalogSaveResumeCounts after)
        {
            int Allow(int saved) => CatalogSaveResumeUnitTolerance + saved / 20;
            if (Math.Abs(after.OwnUnits - before.OwnUnits) > Allow(before.OwnUnits)) return "ownUnits " + before.OwnUnits + "->" + after.OwnUnits;
            if (Math.Abs(after.EnemyUnits - before.EnemyUnits) > Allow(before.EnemyUnits)) return "enemyUnits " + before.EnemyUnits + "->" + after.EnemyUnits;
            if (Math.Abs(after.OwnBuildings - before.OwnBuildings) > CatalogSaveResumeBuildingTolerance) return "ownBuildings " + before.OwnBuildings + "->" + after.OwnBuildings;
            if (Math.Abs(after.EnemyBuildings - before.EnemyBuildings) > CatalogSaveResumeBuildingTolerance) return "enemyBuildings " + before.EnemyBuildings + "->" + after.EnemyBuildings;
            if (Math.Abs(after.OwnBaseHp - before.OwnBaseHp) > before.OwnBaseHp * CatalogSaveResumeBaseHpFraction + 1f) return "ownBaseHp " + before.OwnBaseHp + "->" + after.OwnBaseHp;
            if (Math.Abs(after.EnemyBaseHp - before.EnemyBaseHp) > before.EnemyBaseHp * CatalogSaveResumeBaseHpFraction + 1f) return "enemyBaseHp " + before.EnemyBaseHp + "->" + after.EnemyBaseHp;
            return null;
        }

        // One gesture: add the touchscreen, press, and let the frame tick remove it once the gesture is done.
        // A false return means nothing was pressed (control not presented, or a foreign contact is down): try again later.
        private static bool CatalogSaveResumeTap(Button button)
        {
            if (!playerResumeTouch.IsRunning && !playerResumeTouch.Start()) return false;
            bool started = PlayerResumeTap(button);
            if (!started && !playerResumeTouch.IsBusy) playerResumeTouch.Stop();
            if (started) catalogSaveResumeTapStep = catalogSaveResumeStep;
            return started;
        }

        private static string CatalogSaveResumeButtonsLike(string fragment)
        {
            var names = new List<string>();
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (button.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    names.Add(button.name + "(active=" + button.IsActive() + ",interactable=" + button.IsInteractable() + ")");
            return names.Count == 0 ? "none" : string.Join(",", names);
        }

        internal static void TickCatalogSaveResumeFrame()
        {
            if (!catalogSaveResumeActive) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                playerResumeStep = catalogSaveResumeStep; // context for the shared PlayerResumeTap diagnostics
                if (catalogSaveResumeLoggedStep != catalogSaveResumeStep)
                {
                    catalogSaveResumeLoggedStep = catalogSaveResumeStep;
                    catalogSaveResumeStepSince = now;
                    Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=Input step=" + catalogSaveResumeStep);
                    ScreenCapture.CaptureScreenshot(LiveTracePath() + ".catalog-save-step-" + catalogSaveResumeStep + ".png");
                }
                if (now > catalogSaveResumeDeadline) throw new InvalidOperationException("timeout at step=" + catalogSaveResumeStep);
                playerResumeTouch.Tick(Time.unscaledTime);
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                bool hasSession = SkirmishLaunchProjection.TryGet(em, out Entity session, out var match);
                if (playerResumeTouch.PlayerInterrupted || playerResumeTouch.UnexpectedSamples != 0)
                {
                    // A foreign contact (ARIA's driver) hit while our touchscreen was listening. Retry with a fresh helper.
                    catalogSaveResumeInterruptions++;
                    string detail = "interrupted by a foreign touch contact at step=" + catalogSaveResumeStep + " lastInterruption=" +
                        playerResumeTouch.LastInterruption + " interventions=" + playerResumeTouch.PhysicalInterventions +
                        " unexpectedSamples=" + playerResumeTouch.UnexpectedSamples + " aria=" + UiShellRuntimeGateway.ReadAriaPlay().Phase;
                    if (catalogSaveResumeInterruptions > CatalogSaveResumeMaxInterruptions)
                        throw new InvalidOperationException("touchInterrupted: " + detail + "; gave up after " + CatalogSaveResumeMaxInterruptions + " retries");
                    Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=InputRetry " + detail + " retry=" + catalogSaveResumeInterruptions);
                    playerResumeTouch.Dispose();
                    playerResumeTouch = new AriaTouchInputUiSystemHelper();
                    // A canceled Save & Exit tap leaves the match running: go back and press it again.
                    if (catalogSaveResumeStep == 3 && catalogSaveResumeTapStep == 2 && hasSession) catalogSaveResumeStep = 2;
                    playerResumeDue = now + .5d;
                    return;
                }
                if (playerResumeTouch.IsRunning && !playerResumeTouch.IsBusy) playerResumeTouch.Stop(); // never idle as a listening touchscreen
                if (playerResumeTouch.IsBusy || now < playerResumeDue) return;

                if (catalogSaveResumeStep == 0) // stop ARIA through its visible control so its touches cannot collide with ours
                {
                    if (!UiShellRuntimeGateway.ReadAriaPlay().Active)
                    {
                        Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=AriaStopped taps=" + catalogSaveResumeStopTaps +
                            " aria=" + UiShellRuntimeGateway.ReadAriaPlay().Phase + " interruptions=" + catalogSaveResumeInterruptions);
                        catalogSaveResumeStep = 1;
                        playerResumeDue = now + .5d;
                        return;
                    }
                    if (catalogSaveResumeStopTaps >= CatalogSaveResumeMaxStopTaps)
                        throw new InvalidOperationException("ariaStopFailed: " + catalogSaveResumeStopTaps + " taps on the ARIA stop control left ARIA " +
                            UiShellRuntimeGateway.ReadAriaPlay().Phase + " (interruptions=" + catalogSaveResumeInterruptions + ")");
                    Button stop = null;
                    foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (button.name == "AriaInstantStop") { stop = button; break; }
                        if (button.name == "WatchAriaPlay") stop = button; // the Play control toggles to Stop while ARIA plays
                    }
                    if (stop != null)
                    {
                        if (CatalogSaveResumeTap(stop)) { catalogSaveResumeStopTaps++; Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=AriaStopTap n=" + catalogSaveResumeStopTaps + " button=" + stop.name); }
                        return;
                    }
                    if (now - catalogSaveResumeStepSince > CatalogSaveResumeStopVisibleSeconds)
                        throw new InvalidOperationException("ariaStopNotPresented: no AriaInstantStop/WatchAriaPlay control after " + CatalogSaveResumeStopVisibleSeconds +
                            " s; Aria-like buttons: " + CatalogSaveResumeButtonsLike("aria"));
                    return;
                }
                if (catalogSaveResumeStep == 1) // Pause
                {
                    if (!hasSession) throw new InvalidOperationException("session lost before Pause");
                    var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                    if (drawer != null && drawer.IsOpen) { CatalogSaveResumeTap(drawer.CloseButton); return; }
                    if (!UiShellRuntimeGateway.TryReadSkirmish(out var model)) return;
                    if (model.Finished || match.Phase == SkirmishPhase.Finished) throw new InvalidOperationException("mission ended before Pause (outcome " + match.Outcome + ")");
                    var fullMap = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
                    if (fullMap != null && fullMap.IsOpen) { CatalogSaveResumeTap(fullMap.CloseAction); return; }
                    if (model.Paused)
                    {
                        // Freeze the baseline at the same paused state that Save & Exit captures.
                        catalogSaveResumeSavedElapsed = SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match);
                        catalogSaveResumeSaved = ReadCatalogSaveResumeCounts(em, session);
                        catalogSaveResumeOldEntities.Clear();
                        using (var query = em.CreateEntityQuery(new EntityQueryDesc { Any = new[] {
                            ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(), ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                            ComponentType.ReadOnly<OperationMapBuildingComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities }))
                        using (var entities = query.ToEntityArray(Allocator.Temp))
                            foreach (Entity entity in entities) catalogSaveResumeOldEntities.Add(entity);
                        Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=Paused savedElapsed=" +
                            catalogSaveResumeSavedElapsed.ToString("0.###", CultureInfo.InvariantCulture) + " " + catalogSaveResumeSaved +
                            " aria=" + (UiShellRuntimeGateway.ReadAriaPlay().Phase));
                        catalogSaveResumeStep = 2;
                        return;
                    }
                    foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (button.name == "PauseButton")
                        {
                            if (catalogSaveResumePauseTaps >= CatalogSaveResumeMaxPauseTaps)
                                throw new InvalidOperationException("pauseNotReached: " + catalogSaveResumePauseTaps + " Pause taps did not pause the match (aria=" +
                                    UiShellRuntimeGateway.ReadAriaPlay().Phase + ", interruptions=" + catalogSaveResumeInterruptions + ")");
                            if (CatalogSaveResumeTap(button)) { catalogSaveResumePauseTaps++; Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=PauseTap n=" + catalogSaveResumePauseTaps + " aria=" + UiShellRuntimeGateway.ReadAriaPlay().Phase); }
                            return;
                        }
                    if (now - catalogSaveResumeStepSince > CatalogSaveResumePauseVisibleSeconds)
                        throw new InvalidOperationException("pauseNotPresented: no active PauseButton in the HUD after " + CatalogSaveResumePauseVisibleSeconds +
                            " s; Pause-like buttons: " + CatalogSaveResumeButtonsLike("pause"));
                    return;
                }
                if (catalogSaveResumeStep == 2) // Save & Exit
                {
                    if (!hasSession || !UiShellRuntimeGateway.TryReadSkirmish(out var model) || !model.Paused) throw new InvalidOperationException("saveUnavailable: not paused");
                    if (!model.CanSaveMatch)
                        throw new InvalidOperationException("saveUnavailable: the product offers no Save for " + catalogId +
                            " (SkirmishNativePlayerSaveService supports S005 and generic Base Assault catalogs only)");
                    if (Mathf.Abs(SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match) - catalogSaveResumeSavedElapsed) > .05f)
                        throw new InvalidOperationException("paused clock advanced");
                    if (model.SaveFailed) throw new InvalidOperationException("native capture rejected");
                    var pause = UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
                    if (pause == null)
                    {
                        if (now - catalogSaveResumeStepSince > CatalogSaveResumePopupVisibleSeconds)
                            throw new InvalidOperationException("pausePopupMissing: match is paused but PauseOptionsV3PopupView never appeared");
                        return;
                    }
                    ScreenCapture.CaptureScreenshot(LiveTracePath() + ".catalog-save-pause.png");
                    if (CatalogSaveResumeTap(pause.RestartButton)) catalogSaveResumeStep = 3;
                    return;
                }
                if (catalogSaveResumeStep == 3) // setup screen: Continue
                {
                    if (hasSession && playerResumeContinueAttemptAt > 0)
                    {
                        catalogSaveResumeStep = 4;
                        Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=ContinueObservedAfterRetry input=Touch");
                        return;
                    }
                    if (hasSession)
                    {
                        if (UiShellRuntimeGateway.TryReadSkirmish(out var model) && model.SaveFailed) throw new InvalidOperationException("Save & Exit failed");
                        return;
                    }
                    if (!SkirmishNativePlayerSaveService.TryRead(catalogId, out var saved, out _) || saved.SessionId != catalogSaveResumeSessionId)
                        throw new InvalidOperationException("saved attempt identity was not preserved on disk");
                    foreach (Entity old in catalogSaveResumeOldEntities)
                        if (em.Exists(old) && (em.HasComponent<SkirmishAttemptOwnedComponent>(old) || em.HasComponent<RuntimeBuildingCombatInfo>(old) ||
                            em.HasComponent<OperationMapBuildingComponent>(old) || em.HasComponent<Unity.Transforms.LocalTransform>(old) ||
                            em.HasComponent<UnitHealth>(old))) return;
                    foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (setup.SelectedScenarioId != catalogId) continue;
                        foreach (var button in setup.GetComponentsInChildren<Button>())
                        {
                            var label = button.GetComponentInChildren<TMP_Text>();
                            if (button.name != "LaunchMissionButton" || label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                            // The setup animates in after Save & Exit; touch only a control that stayed put for half a second.
                            if (!TryPresentedButtonPoint(button, out var point)) { playerResumeContinueStableAt = 0; return; }
                            if (playerResumeContinueStableAt == 0 || Vector2.Distance(point, playerResumeContinuePoint) > 1f)
                            { playerResumeContinuePoint = point; playerResumeContinueStableAt = now; return; }
                            if (now - playerResumeContinueStableAt < .5) return;
                            ScreenCapture.CaptureScreenshot(LiveTracePath() + ".catalog-continue-setup.png");
                            if (CatalogSaveResumeTap(button)) { playerResumeContinueAttemptAt = now; catalogSaveResumeStep = 4; }
                            return;
                        }
                    }
                    return;
                }
                // Step 4: observe recovery.
                if (!hasSession)
                {
                    if (now - playerResumeContinueAttemptAt >= 2)
                        foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                            if (setup.SelectedScenarioId == catalogId)
                            {
                                playerResumeContinueStableAt = 0;
                                catalogSaveResumeStep = 3;
                                Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " phase=ContinueRetry input=Touch reason=SavedSetupStillVisible");
                                break;
                            }
                    return;
                }
                if (match.Phase != SkirmishPhase.Playing || !SkirmishLaunchProjection.IsSimulationActive(em)) return;
                if (!em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(session)) throw new InvalidOperationException("Continue launched a fresh match, not a recovery");
                if (!em.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(session).Complete) return;
                string resumedId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString();
                float resumedElapsed = SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match);
                if (resumedId != catalogSaveResumeSessionId) throw new InvalidOperationException("different attempt " + catalogSaveResumeSessionId + " -> " + resumedId);
                if (resumedElapsed < catalogSaveResumeSavedElapsed - .5f || resumedElapsed > catalogSaveResumeSavedElapsed + CatalogSaveResumeClockSlackSeconds)
                    throw new InvalidOperationException("clock discontinuity " + catalogSaveResumeSavedElapsed + " -> " + resumedElapsed);
                if (!Mathf.Approximately(Time.timeScale, 1)) throw new InvalidOperationException("restored match is not at normal speed");
                foreach (Entity old in catalogSaveResumeOldEntities)
                    if (em.Exists(old)) throw new InvalidOperationException("old battlefield entity survived recovery");
                var resumed = ReadCatalogSaveResumeCounts(em, session);
                string difference = CatalogSaveResumeDifference(in catalogSaveResumeSaved, in resumed);
                if (difference != null) throw new InvalidOperationException("state differs after recovery: " + difference);

                bool ariaAfter = UiShellRuntimeGateway.ReadAriaPlay().Active;
                Debug.Log("[SkirmishSaveResume] catalog=" + catalogId + " result=Passed input=Touch sameAttempt=1 session=" + resumedId +
                    " savedElapsed=" + catalogSaveResumeSavedElapsed.ToString("0.###", CultureInfo.InvariantCulture) +
                    " resumedElapsed=" + resumedElapsed.ToString("0.###", CultureInfo.InvariantCulture) +
                    " saved{" + catalogSaveResumeSaved + "} resumed{" + resumed + "} oldPhysicalGone=" + catalogSaveResumeOldEntities.Count +
                    " ariaStoppedBeforePause=1 ariaAfterResume=" + (ariaAfter ? "Active" : "Stopped") + " ariaMatchContinues=Pending ariaLedgerCountable=0");
                ScreenCapture.CaptureScreenshot(LiveTracePath() + ".catalog-resumed.png");
                catalogSaveResumeActive = false; catalogSaveResumeDone = true;
                playerResumeTouch.Dispose(); playerResumeTouch = null;
                // Hand the match back to the main tick. A restored-and-still-active ARIA keeps playing; otherwise the
                // player re-consents through the same visible controls as at the start (stage 2).
                stage = ariaAfter ? 3 : 2;
                deadline += now - catalogSaveResumeStartedAt; // the save/continue detour does not consume the match wall budget
                next = now + 1d;
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishSaveResume] catalog=" + catalogId + " result=Failed step=" + catalogSaveResumeStep + " reason=" + exception.Message);
                catalogSaveResumeActive = false; catalogSaveResumeDone = true;
                playerResumeTouch?.Dispose(); playerResumeTouch = null;
                Finish(true, "catalogSaveResume=" + exception.Message);
            }
        }
    }

    public sealed class SkirmishCatalogSaveResumeFrame : MonoBehaviour
    { private void Update() => SkirmishS002AriaRunHarness.TickCatalogSaveResumeFrame(); }
}
#endif
