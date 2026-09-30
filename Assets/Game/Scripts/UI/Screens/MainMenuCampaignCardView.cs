using System;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    // Art, copy and navigation all consume the same Campaign projection.
    [DisallowMultipleComponent]
    public sealed class MainMenuCampaignCardView : MonoBehaviour
    {
        [Serializable] public struct MissionPlate { public string missionId; public Sprite plate; }
        [Serializable] public struct AftermathScene { public Sprite plate; public string captionKey; public string captionFallback; }
        [SerializeField] private Image art;
        [SerializeField] private MissionPlate[] plates = Array.Empty<MissionPlate>();
        [SerializeField] private TMP_Text title, chapter, purpose, actionLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private AftermathScene[] aftermathScenes = Array.Empty<AftermathScene>();
        [SerializeField] private Sprite epilogue;
        [SerializeField] private Button archiveButton;
        private static int previousAftermath = -1;
        private int visitAftermath = -1;
        private bool visitActive;
        private bool routeRequested;
        private bool directionalLabelsBound;
        public string PresentedMissionId { get; private set; }
        public bool CompletionVisible { get; private set; }
        public int AftermathIndex => visitAftermath;
        public void Configure(TMP_Text name, TMP_Text location, TMP_Text summary, TMP_Text action, Button button)
        { title=name; chapter=location; purpose=summary; actionLabel=action; continueButton=button; }
        private void OnEnable()
        {
            BindDirectionalLabels();
            routeRequested=false;
            if (!visitActive) BeginHomeVisit();
            continueButton?.onClick.AddListener(OpenCampaign);
            UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Refresh, CampaignMissionSequence.IdAt(0));
            Refresh();
        }
        private void BindDirectionalLabels()
        {
            if (directionalLabelsBound) return;
            foreach (TMP_Text label in transform.root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.GetComponent<V3LocalizedTextBindingView>() != null ||
                    !label.text.Contains('›'))
                    continue;
                UiLocalizedText.Set(label, label.text);
            }
            directionalLabelsBound = true;
        }
        private void OnDisable()
        {
            continueButton?.onClick.RemoveListener(OpenCampaign);
            if (UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.ActiveRoute != UIRoute.MainMenu) visitActive=false;
        }
        public void BeginHomeVisit() { visitAftermath = -1; visitActive = true; }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            BindDirectionalLabels();
            // An overlay can disable content without starting a new home visit.
            // Only a real route departure permits the next rotation.
            if (UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.ActiveRoute != UIRoute.MainMenu)
            { visitActive=false; return; }
            if (!visitActive) BeginHomeVisit();
            if (!UiShellRuntimeGateway.TryReadCampaignOperations(out var model) || !model.IsValid)
            {
                PresentedMissionId = string.Empty;
                CompletionVisible=false;
                if (archiveButton != null) archiveButton.gameObject.SetActive(false);
                if (art != null) { art.sprite=null; art.enabled=false; }
                Set(title, "CAMPAIGN"); Set(chapter, string.Empty);
                Set(purpose, "Review your Campaign missions."); Set(actionLabel, "CHOOSE MISSION   ›");
                return;
            }
            var mission = model.SelectedMission;
            PresentedMissionId = mission.MissionId;
            CompletionVisible = model.AllRequiredMissionsCompleted;
            if (archiveButton != null) archiveButton.gameObject.SetActive(CompletionVisible);
            if (CompletionVisible)
            {
                if (visitAftermath < 0 && aftermathScenes.Length > 0)
                {
                    visitAftermath = (previousAftermath + 1) % aftermathScenes.Length;
                    previousAftermath = visitAftermath;
                }
                bool full = model.FullCampaignRegistered;
                if (art != null)
                {
                    art.sprite = full ? epilogue : visitAftermath >= 0 ? aftermathScenes[visitAftermath].plate : null;
                    art.enabled = art.sprite != null;
                }
                Set(chapter, string.Empty);
                if(chapter!=null) chapter.gameObject.SetActive(false);
                Set(title, full ? "CAMPAIGN COMPLETE" : "ALL AVAILABLE MISSIONS COMPLETED");
                Set(purpose, full ? "The city is rebuilding. Your command made the difference." :
                    visitAftermath >= 0 ? UiShellRuntimeGateway.Localization.Get(aftermathScenes[visitAftermath].captionKey, aftermathScenes[visitAftermath].captionFallback) : "Review your Campaign missions.");
                Set(actionLabel, "CHOOSE MISSION   ›");
                return;
            }
            Sprite plate = FindPlate(PresentedMissionId);
            if(chapter!=null) chapter.gameObject.SetActive(true);
            if (art != null) { art.sprite=plate; art.enabled=plate != null; }
            int index = CampaignMissionSequence.IndexOf(PresentedMissionId);
            Set(chapter, UiShellRuntimeGateway.Localization.Format("ui.home.chapter_mission", "CHAPTER {0} • MISSION {1}", index / 5 + 1, index % 5 + 1));
            if (UiShellRuntimeGateway.TryReadMissionBriefing(out var briefing) && briefing.MissionId == PresentedMissionId)
            {
                Set(title, UiShellRuntimeGateway.Localization.Get(briefing.DisplayNameKey, mission.DisplayName));
                Set(purpose, FirstSentence(UiShellRuntimeGateway.Localization.Get(briefing.DisplaySummaryKey, "Review the mission briefing.")));
            }
            else { Set(title, mission.DisplayName); Set(purpose, "Review the mission briefing."); }
            var access = mission.AccessState;
            if (access != UiContentAccessState.Allowed)
                Set(purpose, access switch
                {
                    UiContentAccessState.NotOwned => "Campaign Edition required.",
                    UiContentAccessState.ProgressionLocked => "Complete the previous mission first.",
                    UiContentAccessState.InstalledContentMissing => "Content download required.",
                    _ => "This mission is currently unavailable."
                });
            Set(actionLabel, !mission.PendingResume && mission.FirstClearCompleted || access != UiContentAccessState.Allowed
                ? "CHOOSE MISSION   ›" : "CONTINUE CAMPAIGN   ›");
        }
        private void OpenCampaign()
        {
            if(routeRequested) return;
            Refresh(); // Re-read progression/access at the activation boundary.
            if (!string.IsNullOrEmpty(PresentedMissionId) &&
                !UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select, PresentedMissionId)) return;
            routeRequested=UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.Campaign, true);
        }
        private static void Set(TMP_Text target, string value)
        {
            if (target == null) return;
            UiLocalizedText.Set(target, value);
        }
        private static string FirstSentence(string summary)
        {
            int end = summary.IndexOf('.');
            return end >= 0 ? summary.Substring(0, end + 1) : summary;
        }
        private Sprite FindPlate(string missionId)
        {
            if (string.IsNullOrEmpty(missionId) || plates == null) return null;
            foreach (var item in plates) if (item.missionId == missionId) return item.plate;
            return null; // A missing target plate never borrows another mission's story.
        }
    }
}
