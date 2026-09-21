using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static string cachedAssistantTextLocale;
        private static string cachedObjectiveTextLocale;
        private static partial class UiShellReadModelAdapter
        {
        public static bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
        {
            statusSurfaces = UiMatchHudStatusSurfacesModel.Default;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureMatchHudStatusSurfacesState(entityManager, boundary);
            UiMatchHudStatusSurfacesComponent component =
                entityManager.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
            if (hasCachedMatchHudStatus && cachedMatchHudStatusWorld == entityManager.World &&
                cachedObjectiveTextLocale==GameLocalization.CurrentLocaleCode &&
                cachedMatchHudStatusBoundary == boundary &&
                SameMatchHudStatus(in component, in cachedMatchHudStatusComponent))
            {
                statusSurfaces = cachedMatchHudStatus;
                return true;
            }
            statusSurfaces = new UiMatchHudStatusSurfacesModel(
                component.ObjectivesTitle.ToString(),
                new UiMatchHudObjectiveRowModel(GameText.Get(component.Objective0Text.ToString(),component.Objective0Text.ToString()), component.Objective0IconKind),
                new UiMatchHudObjectiveRowModel(GameText.Get(component.Objective1Text.ToString(),component.Objective1Text.ToString()), component.Objective1IconKind),
                new UiMatchHudObjectiveRowModel(GameText.Get(component.Objective2Text.ToString(),component.Objective2Text.ToString()), component.Objective2IconKind),
                component.ElapsedText.ToString(),
                component.ThreatVisible != 0,
                component.ThreatTitle.ToString(),
                component.ThreatSubtitle.ToString(),
                component.JumpEnabled != 0,
                component.FeedbackVisible != 0,
                component.FeedbackText.ToString(),
                component.BoardAllVisible != 0,
                component.BoardAllEnabled != 0,
                component.CancelVisible != 0,
                component.CancelEnabled != 0);
            hasCachedMatchHudStatus = true;
            cachedMatchHudStatusWorld = entityManager.World;
            cachedMatchHudStatusBoundary = boundary;
            cachedMatchHudStatusComponent = component;
            cachedMatchHudStatus = statusSurfaces;
            cachedObjectiveTextLocale=GameLocalization.CurrentLocaleCode;
            return true;
        }

        private static bool SameMatchHudStatus(
            in UiMatchHudStatusSurfacesComponent left,
            in UiMatchHudStatusSurfacesComponent right) =>
            left.ObjectivesTitle.Equals(right.ObjectivesTitle) &&
            left.Objective0Text.Equals(right.Objective0Text) &&
            left.Objective1Text.Equals(right.Objective1Text) &&
            left.Objective2Text.Equals(right.Objective2Text) &&
            left.Objective0IconKind == right.Objective0IconKind &&
            left.Objective1IconKind == right.Objective1IconKind &&
            left.Objective2IconKind == right.Objective2IconKind &&
            left.ElapsedText.Equals(right.ElapsedText) &&
            left.ThreatVisible == right.ThreatVisible &&
            left.ThreatTitle.Equals(right.ThreatTitle) &&
            left.ThreatSubtitle.Equals(right.ThreatSubtitle) &&
            left.JumpEnabled == right.JumpEnabled &&
            left.FeedbackVisible == right.FeedbackVisible &&
            left.FeedbackText.Equals(right.FeedbackText) &&
            left.BoardAllVisible == right.BoardAllVisible &&
            left.BoardAllEnabled == right.BoardAllEnabled &&
            left.CancelVisible == right.CancelVisible && left.CancelEnabled == right.CancelEnabled;

        public static bool TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel)
        {
            assistantPanel = UiAssistantPanelModel.Empty;
            if(TrySkirmish(out _,out _,out var skirmish))
            {
                if(skirmish.Phase!=SkirmishPhase.Playing)return true;
                assistantPanel=new UiAssistantPanelModel(1,"","","",false,false,true,
                    GameText.Get("ui.skirmish.base_assault","BASE ASSAULT"),
                    GameText.Get("ui.skirmish.match_help","Destroy the enemy base. Defend yours with Watchtowers and rifle squads. Tap a squad card, then Move or Attack."),
                    "","",false,false,false,false,"","");
                return true;
            }
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!UiShellActionAdapter.IsAssistantRuntimeActive(entityManager, boundary))
            {
                hasCachedAssistantPanel = false;
                return false;
            }

            if (!entityManager.HasComponent<AssistantStateComponent>(boundary) ||
                !entityManager.HasComponent<AssistantRecommendationReadModelComponent>(boundary) ||
                !entityManager.HasComponent<AssistantMessageReadModelComponent>(boundary) ||
                !entityManager.HasComponent<AssistantThreatReadModelStateComponent>(boundary) ||
                !entityManager.HasComponent<AssistantTargetLockReadModelComponent>(boundary) ||
                !entityManager.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary) ||
                !entityManager.HasBuffer<AssistantGoalReadModelElement>(boundary) ||
                !entityManager.HasBuffer<AssistantRecommendationElement>(boundary) ||
                !entityManager.HasBuffer<AssistantMessageElement>(boundary))
            {
                return false;
            }

            AssistantStateComponent assistantState =
                entityManager.GetComponentData<AssistantStateComponent>(boundary);
            AssistantRecommendationReadModelComponent recommendationReadModel =
                entityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);
            AssistantMessageReadModelComponent messageReadModel =
                entityManager.GetComponentData<AssistantMessageReadModelComponent>(boundary);
            AssistantThreatReadModelStateComponent threatReadModel =
                entityManager.GetComponentData<AssistantThreatReadModelStateComponent>(boundary);
            AssistantTargetLockReadModelComponent targetLockReadModel =
                entityManager.GetComponentData<AssistantTargetLockReadModelComponent>(boundary);
            MatchObjectiveRuntimeStateComponent objectiveState =
                entityManager.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
            DynamicBuffer<AssistantGoalReadModelElement> goals =
                entityManager.GetBuffer<AssistantGoalReadModelElement>(boundary, true);
            DynamicBuffer<AssistantRecommendationElement> recommendations =
                entityManager.GetBuffer<AssistantRecommendationElement>(boundary, true);
            DynamicBuffer<AssistantMessageElement> messages =
                entityManager.GetBuffer<AssistantMessageElement>(boundary, true);
            AssistantSettingsComponent settings = entityManager.HasComponent<AssistantSettingsComponent>(boundary)
                ? entityManager.GetComponentData<AssistantSettingsComponent>(boundary)
                : default;
            uint settingsVersion = AssistantSettingsVersion(settings);
            bool hasNarrationRequests = entityManager.HasBuffer<AssistantNarrationRequestElement>(boundary);
            DynamicBuffer<AssistantNarrationRequestElement> narrationRequests = hasNarrationRequests
                ? entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary, true)
                : default;
            AssistantNarrationStateComponent narrationState =
                entityManager.HasComponent<AssistantNarrationStateComponent>(boundary)
                    ? entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary)
                    : default;
            bool narrationPulse = narrationState.LastPresentedAt > 0f &&
                                  Time.time - narrationState.LastPresentedAt <= 0.8f;

            var breachStatusStamp=ReadBreachStatusStamp();
            var gridlockStamp=ReadGridlockStamp();
            int extractionSelectionCount=ReadExtractionSelectionCount(recommendations.Length>0?recommendations[0].TutorialStep:(byte)0);
            int extractionHoldStatus=ReadExtractionHoldStatus(recommendations.Length>0?recommendations[0].TutorialStep:(byte)0);
            if (hasCachedAssistantPanel && cachedAssistantTextLocale==GameLocalization.CurrentLocaleCode &&
                cachedBreachStatusStamp==breachStatusStamp && cachedGridlockStamp==gridlockStamp &&
                cachedExtractionSelectionCount==extractionSelectionCount && cachedExtractionHoldStatus==extractionHoldStatus &&
                cachedAssistantPanelWorld == entityManager.World &&
                cachedAssistantPanelBoundary == boundary &&
                cachedAssistantPanelSourceVersion == assistantState.SourceVersion &&
                cachedAssistantPanelRecommendationVersion == recommendationReadModel.Version &&
                cachedAssistantPanelObjectiveVersion == objectiveState.Version &&
                cachedAssistantPanelMessageReadModelVersion == messageReadModel.Version &&
                cachedAssistantPanelThreatVersion == threatReadModel.Version &&
                cachedAssistantPanelTargetLockVersion == targetLockReadModel.Version &&
                cachedAssistantPanelNarrationStateVersion == narrationState.Version &&
                cachedAssistantPanelNarrationPulse == narrationPulse &&
                cachedAssistantPanelSettingsVersion == settingsVersion &&
                cachedAssistantPanelGoalCount == goals.Length &&
                cachedAssistantPanelMessageCount == messages.Length &&
                cachedAssistantPanelRecommendationCount == recommendations.Length &&
                cachedAssistantPanelControlState == assistantState.ControlState)
            {
                assistantPanel = cachedAssistantPanel;
                return true;
            }

            AssistantRecommendationElement topRecommendation =
                recommendations.Length > 0 ? recommendations[0] : default;
            string recommendationTitle = topRecommendation.RecommendationId != 0
                ? topRecommendation.Title.ToString()
                : string.Empty;
            string recommendationBody = topRecommendation.RecommendationId != 0
                ? topRecommendation.Reason.ToString()
                : string.Empty;
            bool tutorialRightToLeft = false;
            if (topRecommendation.TutorialStepCount is 8 or 10 or 12)
            {
                recommendationTitle=GameText.Get(recommendationTitle,recommendationTitle);
                recommendationBody=GameText.Get(recommendationBody,recommendationBody);
                if(topRecommendation.TutorialStepCount==8) recommendationBody=AppendBreachStatus(recommendationBody);
                if(topRecommendation.TutorialStepCount==10) recommendationBody=AppendGridlockStatus(recommendationBody);
                if(extractionSelectionCount>=0 && extractionSelectionCount<4)
                    recommendationBody=GameText.Format("mission.m04.tutorial.selection_progress", "Specialists selected: {0}/4", extractionSelectionCount);
                if(extractionHoldStatus!=int.MinValue) recommendationBody=ExtractionHoldCopy(extractionHoldStatus);
                tutorialRightToLeft=GameLocalization.CurrentLocaleCode=="fa-IR";
            }
            if (topRecommendation.RecommendationId != 0 &&
                topRecommendation.TutorialStepCount == 9)
            {
                if (TryResolveM02GuidancePresentationText(
                    in topRecommendation,
                    ResolveTutorialNarrationLanguage(),
                    out string localizedTitle,
                    out string localizedBody,
                    out bool localizedRightToLeft))
                {
                    recommendationTitle = localizedTitle;
                    recommendationBody = localizedBody;
                    tutorialRightToLeft = localizedRightToLeft;
                }
            }
            if (topRecommendation.RecommendationId != 0 &&
                topRecommendation.TutorialStep > 0 &&
                topRecommendation.TutorialStepCount == 5 &&
                topRecommendation.TargetKind != AssistantTargetKind.UiSurface)
            {
                TryResolveTutorialPresentationText(
                    topRecommendation.TutorialStep,
                    ResolveTutorialNarrationLanguage(),
                    out recommendationTitle,
                    out recommendationBody,
                    out tutorialRightToLeft);
            }
            bool gridlockDiagnostic=HasGridlockIntegrityDiagnostic();
            if(gridlockDiagnostic)
            {
                recommendationTitle=GameText.Get("mission.gridlock.name");
                recommendationBody=GameText.Get("mission.gridlock.failure.integrity");
                tutorialRightToLeft=GameLocalization.CurrentLocaleCode=="fa-IR";
                // Diagnostic stays visible even though unsafe guidance was cleared.
                topRecommendation=default;
            }
            BuildAssistantGoalRows(
                goals,
                out UiAssistantGoalRowModel goal0,
                out UiAssistantGoalRowModel goal1,
                out UiAssistantGoalRowModel goal2);
            BuildAssistantMessageRows(
                messages,
                out UiAssistantMessageRowModel alert0,
                out UiAssistantMessageRowModel alert1,
                out UiAssistantMessageRowModel alert2,
                out UiAssistantMessageRowModel report0,
                out UiAssistantMessageRowModel report1);
            UiAssistantTargetLockModel targetLock = BuildAssistantTargetLockModel(targetLockReadModel);
            UiAssistantNarrationModel narration = BuildAssistantNarrationModel(
                entityManager,
                settings,
                narrationState,
                hasNarrationRequests ? narrationRequests : default,
                narrationPulse);
            cachedAssistantTextLocale=GameLocalization.CurrentLocaleCode;
            cachedAssistantPanelVersion = NextManagedAssistantPanelVersion(cachedAssistantPanelVersion);
            assistantPanel = new UiAssistantPanelModel(
                cachedAssistantPanelVersion,
                objectiveState.MatchActive != 0,
                objectiveState.ElapsedWholeSeconds,
                goal0,
                goal1,
                goal2,
                alert0,
                alert1,
                alert2,
                report0,
                report1,
                targetLock,
                narration,
                topRecommendation.RecommendationId != 0 || gridlockDiagnostic,
                recommendationTitle,
                recommendationBody,
                topRecommendation.RecommendationId != 0 ? PriorityText(topRecommendation.Priority) : string.Empty,
                topRecommendation.RecommendationId != 0 ? GameText.Get(topRecommendation.ActionLabel.ToString(),topRecommendation.ActionLabel.ToString()) : string.Empty,
                topRecommendation.CanShow != 0,
                topRecommendation.CanExecute != 0,
                CanStopAssistantControl(assistantState.ControlState),
                topRecommendation.CanTakeControl != 0,
                ControlStateText(assistantState.ControlState),
                ControlStateDetailText(assistantState.ControlState),
                settings.LargeTextEnabled != 0,
                settings.HighContrastEnabled != 0,
                (byte)topRecommendation.Kind,
                (byte)topRecommendation.TargetKind,
                topRecommendation.TutorialStep,
                topRecommendation.TutorialStepCount,
                tutorialRightToLeft);

            hasCachedAssistantPanel = true;
            cachedAssistantPanelWorld = entityManager.World;
            cachedAssistantPanelBoundary = boundary;
            cachedAssistantPanelSourceVersion = assistantState.SourceVersion;
            cachedAssistantPanelRecommendationVersion = recommendationReadModel.Version;
            cachedAssistantPanelObjectiveVersion = objectiveState.Version;
            cachedAssistantPanelMessageReadModelVersion = messageReadModel.Version;
            cachedAssistantPanelThreatVersion = threatReadModel.Version;
            cachedAssistantPanelTargetLockVersion = targetLockReadModel.Version;
            cachedAssistantPanelNarrationStateVersion = narrationState.Version;
            cachedAssistantPanelNarrationPulse = narrationPulse;
            cachedAssistantPanelSettingsVersion = settingsVersion;
            cachedAssistantPanelGoalCount = goals.Length;
            cachedAssistantPanelMessageCount = messages.Length;
            cachedAssistantPanelRecommendationCount = recommendations.Length;
            cachedAssistantPanelControlState = assistantState.ControlState;
            cachedBreachStatusStamp=breachStatusStamp;cachedGridlockStamp=gridlockStamp;
            cachedExtractionSelectionCount=extractionSelectionCount;
            cachedExtractionHoldStatus=extractionHoldStatus;
            cachedAssistantPanel = assistantPanel;
            return true;
        }

        public static bool TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight)
        {
            assistantHighlight = UiAssistantHighlightModel.Empty;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!entityManager.HasBuffer<AssistantPreviewHighlightElement>(boundary))
                return false;

            DynamicBuffer<AssistantPreviewHighlightElement> highlights =
                entityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary, true);
            if (highlights.Length == 0 || highlights[0].Active == 0)
                return false;

            AssistantPreviewHighlightElement highlight = highlights[0];
            uint version = AssistantHighlightVersion(highlight);
            if (hasCachedAssistantHighlight &&
                cachedAssistantHighlightWorld == entityManager.World &&
                cachedAssistantHighlightBoundary == boundary &&
                cachedAssistantHighlightVersion == version &&
                cachedAssistantHighlightRequestId == highlight.RequestId)
            {
                assistantHighlight = cachedAssistantHighlight;
                return true;
            }

            assistantHighlight = new UiAssistantHighlightModel(
                version,
                true,
                highlight.RequestId,
                highlight.RecommendationId,
                (byte)highlight.RecommendationKind,
                (byte)highlight.TargetKind,
                highlight.WorldPosition.x,
                highlight.WorldPosition.y,
                highlight.WorldPosition.z,
                highlight.Strength);

            hasCachedAssistantHighlight = true;
            cachedAssistantHighlightWorld = entityManager.World;
            cachedAssistantHighlightBoundary = boundary;
            cachedAssistantHighlightVersion = version;
            cachedAssistantHighlightRequestId = highlight.RequestId;
            cachedAssistantHighlight = assistantHighlight;
            return true;
        }


        }
    }
}
