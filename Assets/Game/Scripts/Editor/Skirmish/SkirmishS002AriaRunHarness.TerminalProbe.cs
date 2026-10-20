#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Unity.Collections;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool terminalProbeActive, terminalSnapshotValid;
        private static SkirmishMatchState terminalMatch;
        private static Entity terminalSession;
        private static FixedString64Bytes terminalAttempt;
        private static string terminalAction;
        private static float terminalElapsed;
        private static double terminalWall;
        private static float terminalTimeScale;
        private static int terminalPlayerHp, terminalEnemyHp, terminalStep;
        private static double terminalDue, terminalDeadline;
        private static double terminalDestinationVisibleSince;
        private static SkirmishTerminalPresentationObservation.Proof terminalResultPresentation, terminalMenuPresentation;
        private static AriaTouchInputUiSystemHelper terminalTouch;
        private static int terminalInterventions, terminalUnexpectedSamples;
        private static readonly List<Entity> terminalOldBattlefield = new();

        private static void StartTerminalProbe(EntityManager em, Entity session, SkirmishMatchState match, float elapsed)
        {
            RecordProductionResolvedSnapshot(em, session, in match, SkirmishLaunchProjection.IsSimulationActive(em), elapsed, "TerminalSnapshot");
            StopS004ManualProbe();
            terminalSnapshotValid = terminalProbeActive = true;
            terminalInterventions = terminalUnexpectedSamples = 0;
            terminalMatch = match;
            terminalSession = session;
            terminalAttempt = ReadProductionTerminalAttempt(em,session,in match);
            terminalOldBattlefield.Clear();
            using (var oldState = em.CreateEntityQuery(new EntityQueryDesc
            {
                Any = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                    ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                    ComponentType.ReadOnly<BuildingResourceStorageComponent>(),
                    ComponentType.ReadOnly<OperationMapBuildingComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities
            }))
            using (var entities = oldState.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities) terminalOldBattlefield.Add(entity);
            terminalAction = Environment.GetEnvironmentVariable("WARLINE_TERMINAL_ACTION") ?? "MAIN MENU";
            if (terminalAction != "MAIN MENU" && terminalAction != "REPLAY" && terminalAction != "ADJUST SETUP")
                throw new ArgumentException("Unsupported terminal action: " + terminalAction);
            if (productionCanary?.Request.IsLegacyPreset == true && terminalAction != "MAIN MENU") throw new InvalidOperationException("Legacy production proof uses normal MainMenu return only");
            terminalElapsed = elapsed;
            terminalWall = EditorApplication.timeSinceStartup - playingSince;
            terminalTimeScale = Time.timeScale;
            terminalPlayerHp = DesignatedBaseHp(em, session, 1);
            terminalEnemyHp = DesignatedBaseHp(em, session, 2);
            terminalStep = 0;
            terminalDestinationVisibleSince = 0d;
            terminalResultPresentation = terminalMenuPresentation = null;
            terminalDue = EditorApplication.timeSinceStartup + 3d;
            terminalDeadline = terminalDue + 45d;
            var runner = new GameObject("SkirmishTerminalInputProbe");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<SkirmishTerminalInputProbeFrame>();
        }

        internal static void TickTerminalPlayerFrame()
        {
            if (!terminalProbeActive) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (now > terminalDeadline) { EndTerminalProbe(false, "returnTimeout"); return; }
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) { EndTerminalProbe(false, "worldLost"); return; }
                var em = world.EntityManager;
                terminalTouch?.Tick(Time.unscaledTime);
                if (terminalTouch != null && (terminalTouch.PlayerInterrupted || !terminalTouch.IsRunning))
                { EndTerminalProbe(false, "inputInterrupted"); return; }
                if (now < terminalDue) return;
                string prefix = LiveTracePath() + ".terminal";
                if (terminalStep == 0)
                {
                    if (!SkirmishLaunchProjection.TryGet(em, out var session, out var match) || session != terminalSession ||
                        match.Phase != SkirmishPhase.Finished || match.Outcome != terminalMatch.Outcome ||
                        !ReadProductionTerminalAttempt(em,session,in match).Equals(terminalAttempt) ||
                        Mathf.Abs(SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match) - terminalElapsed) > .05f ||
                        DesignatedBaseHp(em, session, 1) != terminalPlayerHp || DesignatedBaseHp(em, session, 2) != terminalEnemyHp ||
                        SkirmishLaunchProjection.IsSimulationActive(em))
                    { EndTerminalProbe(false, "terminalNotFrozen"); return; }
                    if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
                    {
                        var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                        if (clock.Playing != 0 || clock.Paused == 0)
                        { EndTerminalProbe(false, "terminalClockNotFrozen"); return; }
                    }
                    if (productionCanary?.Request.IsLegacyPreset == true)
                    {
                        if (!SkirmishLegacyProductionIdentity.ReadSavedResultMatches(in match,out _)) return;
                        RecordProductionResolvedSnapshot(em,session,in match,false,terminalElapsed,"TerminalSnapshot");
                    }
                    var view = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                    terminalResultPresentation = SkirmishTerminalPresentationObservation.ReadResult(view, terminalAttempt.ToString(), match.Outcome);
                    if (!SkirmishTerminalPresentationObservation.AcceptResult(terminalResultPresentation, terminalAttempt.ToString(), match.Outcome)) return;
                    if (sagaMenuProof && !ObserveSagaResultMedal(em,session,in match)) return;
                    if (UiShellRuntimeGateway.Localization.IsRightToLeft)
                    {
                        var card = UnityEngine.Object.FindAnyObjectByType<CampaignStyleResultCard>();
                        if (card == null) { EndTerminalProbe(false, "localizedResultMissing"); return; }
                        foreach (var label in card.GetComponentsInChildren<TMPro.TMP_Text>())
                            if (System.Text.RegularExpressions.Regex.IsMatch(label.text ?? string.Empty, "[A-Za-z]{2,}"))
                            {
                                Debug.LogError("[SkirmishNativeResultLocalization] untranslated=" + label.text);
                                EndTerminalProbe(false, "resultNotLocalized"); return;
                            }
                        Debug.Log("[SkirmishNativeResultLocalization] result=Passed locale=" +
                            UiShellRuntimeGateway.Localization.CurrentLocaleCode);
                    }
                    string actionObject = terminalAction == "REPLAY" ? "Replay"
                        : terminalAction == "ADJUST SETUP" ? "Adjust"
                        : terminalAction == "RETRY" ? "Retry" : "Leave";
                    var button = view == null ? null : view.transform.Find("SkirmishResult/CampaignStyleResult/" + actionObject)?.GetComponent<Button>();
                    if (!TryPresentedButtonPoint(button, out var point)) return;
                    // Retain the delayed, verified terminal state as well as the
                    // first finish observation before leaving the match.
                    AppendTrace(em, session, in match, false, terminalElapsed);
                    ScreenCapture.CaptureScreenshot(prefix + "-result.png");
                    terminalTouch = new AriaTouchInputUiSystemHelper();
                    if (!terminalTouch.Start() || !terminalTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                    { EndTerminalProbe(false, "returnTouchRejected"); return; }
                    terminalStep = 1;
                    terminalDue = now + 1d;
                    Debug.Log("[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=" + terminalAction + " point=" + point);
                    return;
                }
                if (terminalStep == 1)
                {
                    if (terminalTouch.IsBusy || !UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning) return;
                    if (terminalAction == "REPLAY")
                    {
                        if (!SkirmishLaunchProjection.TryGet(em, out var replay, out var replayMatch) ||
                            replayMatch.Phase != SkirmishPhase.Playing || !SkirmishLaunchProjection.IsSimulationActive(em)) return;
                        if (em.GetComponentData<SkirmishExpandedSessionComponent>(replay).SessionId.Equals(terminalAttempt) ||
                            em.GetComponentData<SkirmishExpandedSessionComponent>(replay).CatalogId.ToString() != catalogId ||
                            replayMatch.ScenarioIndex != terminalMatch.ScenarioIndex || replayMatch.Seed != terminalMatch.Seed ||
                            SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, replay, in replayMatch) > 10f ||
                            DesignatedBaseHp(em, replay, 1) <= 0 || DesignatedBaseHp(em, replay, 2) <= 0)
                        { EndTerminalProbe(false, "replayNotFresh"); return; }
                        foreach (var oldEntity in terminalOldBattlefield)
                            if (em.Exists(oldEntity))
                            {
                                using var types = em.GetComponentTypes(oldEntity);
                                Debug.LogError("[SkirmishNativeReplayLeak] entity=" + oldEntity + " name=" + em.GetName(oldEntity) +
                                    " types=" + string.Join(",", types.ToArray()));
                                EndTerminalProbe(false, "replayRetainedBattlefieldEntity:" + oldEntity); return;
                            }
                        var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(replay).Setup;
                        using var structures = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo), typeof(SkirmishAttemptOwnedComponent));
                        using var banks = em.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent));
                        if (structures.CalculateEntityCount() != setup.Structures.Length || banks.CalculateEntityCount() != 2 ||
                            !em.HasComponent<SkirmishSharedSupplyInitialized>(replay) ||
                            !NativeStartupRosterMatches(em, replay, setup, out _, out _))
                        { EndTerminalProbe(false, "replayRosterOrStoresNotFresh"); return; }
                        var playerBank = SkirmishMaterialsService.FindEconomy(em, 1);
                        var balance = em.GetComponentData<FactionTacticalMaterialsComponent>(playerBank);
                        if (balance.Current != setup.MaterialsEach || balance.Capacity != setup.MaterialsCapacityEach ||
                            balance.LifetimeSpent != 0 || balance.LifetimeFabricated != 0 || balance.LifetimeImported != 0 ||
                            balance.LifetimeRewarded != 0 || balance.LifetimeExported != 0)
                        { EndTerminalProbe(false, "replayPlayerBankNotReset"); return; }
                    }
                    else
                    {
                        UIRoute expected = terminalAction == "ADJUST SETUP" ? UIRoute.QuickCustomSetup : UIRoute.MainMenu;
                        if (shell.ActiveRoute != expected) return;
                        if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                        { EndTerminalProbe(false, "sessionNotCleaned"); return; }
                    }
                    // Route state can precede the loading fade and destination layout.
                    // Require the actual presented controls before accepting a return.
                    var shellView = UnityEngine.Object.FindAnyObjectByType<UIShellView>();
                    bool obscured = shellView != null &&
                        shellView.TryGetRegion(UIShellRegionId.LoadingLayer, out var loading) &&
                        loading.CanvasGroup != null && loading.CanvasGroup.alpha > .01f;
                    bool controlsVisible = terminalAction == "REPLAY";
                    if (terminalAction == "MAIN MENU")
                    {
                        terminalMenuPresentation = ReadTerminalMenuPresentation();
                        controlsVisible = terminalMenuPresentation != null;
                    }
                    if (terminalAction == "ADJUST SETUP")
                    {
                        foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude))
                        {
                        if (setup.IsBaseAssaultSetup && setup.SelectedScenarioId == catalogId &&
                            setup.ReadConfigFromControls().ScenarioIndex == terminalMatch.ScenarioIndex &&
                            setup.ReadConfigFromControls().MapSeed == terminalMatch.Seed)
                            foreach (var control in setup.GetComponentsInChildren<Button>())
                                if (TryPresentedButtonPoint(control, out _)) { controlsVisible = true; break; }
                        }
                    }
                    if (obscured || !controlsVisible)
                    { terminalDestinationVisibleSince = 0d; return; }
                    if (terminalDestinationVisibleSince == 0d) terminalDestinationVisibleSince = now;
                    if (now - terminalDestinationVisibleSince < 2d) return;
                    if (terminalAction == "REPLAY")
                    {
                        SkirmishLaunchProjection.TryGet(em, out var fresh, out _);
                        Debug.Log("[SkirmishNativeReplayConservation] result=Passed removedOldEntities=" + terminalOldBattlefield.Count +
                            " oil=" + SkirmishStartingSupplyService.ReadOil(em, fresh, 1) +
                            " fuel=" + SkirmishStartingSupplyService.ReadFuel(em, fresh, 1) +
                            " materials=" + SkirmishMaterialsService.Read(em, fresh, 1));
                    }
                    if(sagaMenuProof) VerifySagaReturnedMedal(prefix);
                    ScreenCapture.CaptureScreenshot(prefix + "-destination.png");
                    terminalStep = 2;
                    terminalDue = now + 1d;
                    return;
                }
                EndTerminalProbe(true, "frozenResultAnd:" + terminalAction);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EndTerminalProbe(false, "exception");
            }
        }

        private static SkirmishTerminalPresentationObservation.Proof ReadTerminalMenuPresentation()
        {
            if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.ActiveRoute != UIRoute.MainMenu || shell.IsTransitionRunning)
                return null;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || SkirmishTerminalPresentationObservation.NativeSessionCount(world.EntityManager) != 0) return null;
            var view = UnityEngine.Object.FindAnyObjectByType<UIShellView>();
            if (view != null && view.TryGetRegion(UIShellRegionId.LoadingLayer, out var loading) &&
                loading.CanvasGroup != null && loading.CanvasGroup.alpha > .01f) return null;
            var proof = SkirmishTerminalPresentationObservation.ReadMenu(terminalAttempt.ToString(),
                button => TryPresentedButtonPoint(button, out var point) ? point : (Vector2?)null);
            if (proof != null) terminalMenuPresentation = proof;
            return proof;
        }

        private static void EndTerminalProbe(bool passed, string reason)
        {
            terminalInterventions = (int)(terminalTouch?.PhysicalInterventions ?? 0);
            terminalUnexpectedSamples = (int)(terminalTouch?.UnexpectedSamples ?? 0);
            if (terminalUnexpectedSamples > 0) { passed = false; reason = "unexpectedTerminalInput"; }
            if (passed && (!SkirmishTerminalPresentationObservation.AcceptResult(terminalResultPresentation, terminalAttempt.ToString(), terminalMatch.Outcome) ||
                terminalAction == "MAIN MENU" && (ReadTerminalMenuPresentation() == null ||
                    terminalResultPresentation.locale != terminalMenuPresentation.locale)))
            { passed = false; reason = "terminalPresentationLost"; }
            Debug.Log("[SkirmishTerminalProbe] result=" + (passed ? "Passed" : "Failed") + " reason=" + reason +
                " completed=" + (terminalTouch?.CompletedGestures ?? 0) + " interventions=" + (terminalTouch?.PhysicalInterventions ?? 0));
            RecordProductionTerminalReturn(passed, reason);
            terminalProbeActive = false;
            terminalTouch?.Dispose(); terminalTouch = null;
            Finish(!passed, passed ? null : "terminal:" + reason);
        }
    }

    internal sealed class SkirmishTerminalInputProbeFrame : MonoBehaviour
    {
        private void Update() => SkirmishS002AriaRunHarness.TickTerminalPlayerFrame();
    }
}
#endif
