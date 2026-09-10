using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M02BuildingPlacementEditorProbe
    {
        private static void AdvanceCampaignReturn(EntityManager em, Entity root)
        {
            if (EditorApplication.timeSinceStartup < nextAction) return;
            if (step == 12)
            {
                var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if (runtime.MissionId.ToString() != "saga.ch01.m03.radar_warning" || runtime.Phase != MissionPhaseKind.Engage) return;
                ScreenCapture.CaptureScreenshot(Output + "/m03-deployed.png");
                Complete(true, "M2 build/recruit/Victory/debrief/Continue; Campaign return; M3 unlocked and selected; M2/M3 node clicks refresh; M3 deployed into Engage"); return;
            }
            if (step == 11)
            {
                var briefing = UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();
                if (Click(briefing?.DeployOperationButton)) Next();
                return;
            }
            var view = UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
            if (view == null || !UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign)) return;
            if (step == 7)
            {
                if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.ActiveRoute != UIRoute.Campaign || shell.CurrentMode != UiShellMode.MainMenu || shell.IsTransitionRunning || shell.Phase != UiShellTransitionPhase.MenuReady) return;
                if (campaign.SelectedMission.MissionId != "saga.ch01.m03.radar_warning" || view.MissionNumber.text != "M03") throw new InvalidOperationException($"Return must focus unlocked M3: model={campaign.SelectedMission.MissionId}, view={view.MissionNumber.text}, mask={campaign.AvailableMissionMask}.");
                if (!view.MissionNodeButtons[2].interactable || view.MissionNodes[2].Find("StateIcon").gameObject.activeSelf) throw new InvalidOperationException("M3 still looks locked.");
                if ((campaign.CompletedMissionMask & 3) != 3) throw new InvalidOperationException("M1/M2 completion receipts missing.");
                var panel = new Vector3[4]; var header = new Vector3[4];
                view.MissionBriefing.GetWorldCorners(panel);
                ((RectTransform)view.transform.Find("CommandChip")).GetWorldCorners(header);
                if (panel[1].y >= header[0].y) throw new InvalidOperationException("Campaign briefing overlaps its header.");
                ScreenCapture.CaptureScreenshot(Output + "/campaign-m03-unlocked.png");
                Log("Returned directly to Campaign; M3 unlocked and focused; header clear"); Next(); return;
            }
            if (step == 8) { if (Click(view.MissionNodeButtons[1])) Next(); return; }
            if (step == 9)
            {
                if (view.MissionNumber.text != "M02") throw new InvalidOperationException("Campaign node selection did not refresh M2.");
                if (Click(view.MissionNodeButtons[2])) Next(); return;
            }
            if (step == 10)
            {
                if (view.MissionNumber.text != "M03") throw new InvalidOperationException("Campaign node selection did not refresh M3.");
                if (Click(view.LaunchMissionButton)) Next();
            }
        }
    }
}
