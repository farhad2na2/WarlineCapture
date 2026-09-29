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
        [SerializeField] private Image art;
        [SerializeField] private MissionPlate[] plates = Array.Empty<MissionPlate>();
        [SerializeField] private TMP_Text title, chapter, purpose, actionLabel;
        [SerializeField] private Button continueButton;
        private bool routeRequested;
        public string PresentedMissionId { get; private set; }
        public void Configure(TMP_Text name, TMP_Text location, TMP_Text summary, TMP_Text action, Button button)
        { title=name; chapter=location; purpose=summary; actionLabel=action; continueButton=button; }
        private void OnEnable()
        {
            routeRequested=false;
            continueButton?.onClick.AddListener(OpenCampaign);
            UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Refresh, CampaignMissionSequence.IdAt(0));
            Refresh();
        }
        private void OnDisable() => continueButton?.onClick.RemoveListener(OpenCampaign);
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (!UiShellRuntimeGateway.TryReadCampaignOperations(out var model) || !model.IsValid)
            {
                PresentedMissionId = string.Empty;
                if (art != null) { art.sprite=null; art.enabled=false; }
                Set(title, "CAMPAIGN"); Set(chapter, string.Empty);
                Set(purpose, "Review your Campaign missions."); Set(actionLabel, "CHOOSE MISSION   ›");
                return;
            }
            var mission = model.SelectedMission;
            PresentedMissionId = mission.MissionId;
            Sprite plate = FindPlate(PresentedMissionId);
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
            Set(actionLabel, mission.FirstClearCompleted || access != UiContentAccessState.Allowed
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
