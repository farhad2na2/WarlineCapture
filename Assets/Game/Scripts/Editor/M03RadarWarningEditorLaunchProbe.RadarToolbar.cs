using System;
using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static bool radarToolbarAudit, radarToolbarClicked, unifiedScanJourney;
        private static double radarToolbarClickedAt, radarToolbarSeenAt;
        private static TacticalCommandMode radarToolbarPreviousMode;
        private static string radarToolbarLocale;
        public static void RunRadarToolbarEnglish() => BeginRadarToolbar("en");
        public static void RunRadarToolbarPersian() => BeginRadarToolbar("fa-IR");
        private static void BeginRadarToolbar(string locale)
        {
            unifiedScanJourney=false;
            M03RadarWarningLocalizationBuilder.Import();
            RunFullGuidanceJourney();
            GameLocalization.SetLocale(locale, false);
            radarToolbarLocale = locale;
            radarToolbarAudit = true; radarToolbarClicked = false; radarToolbarSeenAt = 0;
        }

        private static bool ObserveRadarToolbar(EntityManager em, Entity root)
        {
            if (!radarToolbarAudit) return false;
            if (GameLocalization.CurrentLocaleCode != radarToolbarLocale) GameLocalization.SetLocale(radarToolbarLocale, false);
            var guidance = em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if (!optionalJourney && guidance.GuidanceId == 45004)
            {
                var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                if (drawer != null && drawer.IsOpen) { ClickCommand(drawer.CloseButton); return true; }
            }
            if (!radarToolbarClicked && guidance.GuidanceId != 45010) return false;
            if (radarToolbarSeenAt == 0) radarToolbarSeenAt = EditorApplication.timeSinceStartup;
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (!radarToolbarClicked)
            {
                if (controls == null || controls.ScanButton == null || !controls.ScanButton.IsInteractable()) return true;
                if (!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep != 7) return true;
                if (!UiShellRuntimeGateway.TryReadMissionDefense(out var defense) || !defense.CanPing || defense.Charges != 2)
                    throw new InvalidOperationException("Optional radar did not start with two available loaned scans.");
                UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command);
                radarToolbarPreviousMode = command.ActiveCommandMode;
                ScreenCapture.CaptureScreenshot(Output + "/radar-toolbar-before-" + GameLocalization.CurrentLocaleCode + ".png");
                ClickTutorialButton(controls.ScanButton);
                radarToolbarClicked = true; radarToolbarClickedAt = EditorApplication.timeSinceStartup;
                return true;
            }
            var ping = em.GetComponentData<RadarPingState>(root);
            if (EditorApplication.timeSinceStartup - radarToolbarClickedAt > 10)
                throw new InvalidOperationException("Actual radar toolbar click did not finish the optional scan: " + ping.Result);
            if (ping.Result != RadarPingResultKind.Accepted) return true;
            if (EditorApplication.timeSinceStartup - radarToolbarClickedAt < .5) return true;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if (ping.Charges != 1 || ping.PendingRequestId != 0 || ping.ReadyAtMilliseconds <= facts.ElapsedMilliseconds ||
                em.GetComponentData<CampaignMissionDefenseStateComponent>(root).PingUsed == 0 || guidance.GuidanceId != 45010)
                throw new InvalidOperationException("Radar did not consume exactly one charge, start cooldown and keep defending.");
            UiShellRuntimeGateway.TryReadMatchHudCommandState(out var after);
            if (controls.SupportButton.IsInteractable()) throw new InvalidOperationException("M3 Support must retain its mission restriction, not become another scan button.");
            if (after.ActiveCommandMode != radarToolbarPreviousMode)
                throw new InvalidOperationException("Mission Scan changed the current command mode.");
            if (UiShellRuntimeGateway.TryReadMissionDefense(out var cooling) && cooling.CanPing) throw new InvalidOperationException("Scan cooldown did not block another sweep.");
            var feedback = UnityEngine.Object.FindAnyObjectByType<BattleHudRuntimeFeedbackView>();
            var label = (TMPro.TMP_Text)typeof(BattleHudRuntimeFeedbackView)
                .GetField("feedbackText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(feedback);
            if (string.IsNullOrWhiteSpace(label.text) || label.text.Contains("{0}"))
                throw new InvalidOperationException("Scan feedback lost its formatted contact count: " + label.text);
            ScreenCapture.CaptureScreenshot(Output + "/radar-toolbar-after-" + GameLocalization.CurrentLocaleCode + ".png");
            radarToolbarAudit = false;
            if (unifiedScanJourney) return false;
            Complete(true, "Radar toolbar " + GameLocalization.CurrentLocaleCode + ": actual click -> scan accepted -> one charge/cooldown -> same defense lesson; no generic Scan targeting.");
            return true;
        }

        internal static void ClickTutorialButton(Button button)
        {
            var rect = (RectTransform)button.transform;
            var camera = button.GetComponentInParent<GraphicRaycaster>().eventCamera;
            var pointer = new PointerEventData(EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button)
                throw new InvalidOperationException("Radar button is not the top pointer target: point=" + pointer.position +
                    " screen=" + Screen.width + "x" + Screen.height + " hit=" + (hits.Count > 0 ? hits[0].gameObject.name : "none") +
                    " camera=" + camera?.name + " button=" + button.name + " group=" + (button.GetComponent<CanvasGroup>()!=null ? button.GetComponent<CanvasGroup>().blocksRaycasts.ToString() : "no CanvasGroup"));
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
