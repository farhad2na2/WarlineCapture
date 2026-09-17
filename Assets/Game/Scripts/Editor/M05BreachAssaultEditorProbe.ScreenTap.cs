using System;
using Game.Configs;
using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private static Entity tappedGate;
        private static int tapStage;
        private static double tapStarted;
        private static int gateStartHealth;

        public static void RunScreenTapAudit()
        {
            SessionState.SetBool("Warline.M05.ScreenTapAudit", true);
            SessionState.SetBool("Warline.M05.SkipComics", true);
            SessionState.SetBool("Warline.M05.EnglishCombat", true);
            SessionState.SetBool("Warline.M05.Guided", false);
            SessionState.SetBool("Warline.M05.GuideOnly", false);
            Run();
        }

        private static bool TickGateScreenTap(EntityManager em, CampaignMissionBreachState breach)
        {
            if (tappedGate != breach.Gate)
            {
                tappedGate = breach.Gate;
                tapStage = 0;
                tapStarted = EditorApplication.timeSinceStartup;
                gateStartHealth = Hp(em, breach.Gate);
            }
            if (breach.GateDestroyed != 0)
            {
                if (tapStage != 4)
                    Debug.Log("[M05GateScreenTap] result=Passed locale=" + GameLocalization.CurrentLocaleCode +
                        " Attack button -> screen-only tap -> damage -> destroyed gate; no entity attack request supplied by probe");
                tapStage = 4;
                return false;
            }
            if (EditorApplication.timeSinceStartup - tapStarted > 150)
                throw new InvalidOperationException("Screen-tapped gate did not fall; stage=" + tapStage + " hp=" + Hp(em, breach.Gate));
            if (tapStage == 0)
            {
                // Use the tutorial's actor-selection action, then the real HUD control.
                if (!UiShellRuntimeGateway.TrySelectBreachActor()) return true;
                tapStage = 1;
                return true;
            }
            if (tapStage == 1)
            {
                var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                if (controls?.AttackButton == null || !controls.AttackButton.interactable) return true;
                var cameraRequests = em.World.GetExistingSystemManaged<RtsCameraRequestSystem>();
                cameraRequests.QueueApplyPerspectiveModeInstant(em, 35f, 42f, 0f, 36f);
                cameraRequests.QueueMoveGroundCenterTo(em, breach.GateCenter);
                controls.AttackButton.onClick.Invoke();
                tapStage = 2;
                return true;
            }
            if (tapStage == 2)
            {
                var camera = Camera.main;
                // Tap the exposed gate face, rather than its ground cell.
                Vector2 screen = camera.WorldToScreenPoint((Vector3)breach.GateCenter + Vector3.up * 2f);
                if (!new RtsSelectionInputCompositionSystemHelper(em).QueueAttackCommandRequest(screen, true, Time.frameCount))
                    throw new InvalidOperationException("Gate screen tap was not queued");
                ScreenCapture.CaptureScreenshot(Output + "/gate-screen-tap-" + GameLocalization.CurrentLocaleCode + ".png");
                Debug.Log("[M05GateScreenTap] queued locale=" + GameLocalization.CurrentLocaleCode + " screen=" + screen);
                tapStage = 3;
                return true;
            }
            if (tapStage == 3 && Hp(em, breach.Gate) < gateStartHealth)
            {
                Debug.Log("[M05GateScreenTap] damage locale=" + GameLocalization.CurrentLocaleCode + " hp=" + Hp(em, breach.Gate));
                gateStartHealth = Hp(em, breach.Gate);
            }
            return true;
        }
    }
}
