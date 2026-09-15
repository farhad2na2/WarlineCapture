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
        private static bool radarToolbarAudit, radarToolbarClicked;
        private static double radarToolbarClickedAt, radarToolbarSeenAt;
        private static TacticalCommandMode radarToolbarPreviousMode;
        private static string radarToolbarLocale;
        public static void RunRadarToolbarEnglish() => BeginRadarToolbar("en");
        public static void RunRadarToolbarPersian() => BeginRadarToolbar("fa-IR");
        private static void BeginRadarToolbar(string locale)
        {
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
            if (guidance.GuidanceId == 45004)
            {
                var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                if (drawer != null && drawer.IsOpen) { ClickCommand(drawer.CloseButton); return true; }
            }
            if (!radarToolbarClicked && guidance.GuidanceId != 45008) return false;
            if (radarToolbarSeenAt == 0) radarToolbarSeenAt = EditorApplication.timeSinceStartup;
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (!radarToolbarClicked)
            {
                if (controls == null || controls.SupportButton == null || !controls.SupportButton.IsInteractable()) return true;
                if (!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep != 8) return true;
                if (!UiShellRuntimeGateway.TryReadMissionDefense(out var defense) || !defense.CanPing || defense.Charges != 2)
                    throw new InvalidOperationException("Radar lesson did not start with two available loaned scans.");
                var frame = GameObject.Find("AriaAssistantTargetIndicatorRuntime");
                if (frame == null || !frame.activeInHierarchy) return true;
                var buttonBounds = IndicatorScreenBounds((RectTransform)controls.SupportButton.transform);
                if (Vector2.Distance(IndicatorScreenBounds((RectTransform)frame.transform).center, buttonBounds.center) > 2)
                {
                    // ECS projection can advance before the next HUD presentation frame.
                    if (EditorApplication.timeSinceStartup - radarToolbarSeenAt < 2) return true;
                    throw new InvalidOperationException("Radar lesson points at the wrong control.");
                }
                UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command);
                radarToolbarPreviousMode = command.ActiveCommandMode;
                ScreenCapture.CaptureScreenshot(Output + "/radar-toolbar-before-" + GameLocalization.CurrentLocaleCode + ".png");
                ClickTutorialButton(controls.SupportButton);
                radarToolbarClicked = true; radarToolbarClickedAt = EditorApplication.timeSinceStartup;
                return true;
            }
            var ping = em.GetComponentData<RadarPingState>(root);
            if (EditorApplication.timeSinceStartup - radarToolbarClickedAt > 10)
                throw new InvalidOperationException("Actual radar toolbar click did not finish the scan and advance lesson: " + ping.Result);
            if (ping.Result != RadarPingResultKind.Accepted || guidance.GuidanceId == 45008) return true;
            if (EditorApplication.timeSinceStartup - radarToolbarClickedAt < .5) return true;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if (ping.Charges != 1 || ping.PendingRequestId != 0 || ping.ReadyAtMilliseconds <= facts.ElapsedMilliseconds ||
                em.GetComponentData<CampaignMissionDefenseStateComponent>(root).PingUsed == 0 || guidance.GuidanceId != 45009)
                throw new InvalidOperationException("Radar did not consume exactly one charge, start cooldown and advance to reinforcement.");
            UiShellRuntimeGateway.TryReadMatchHudCommandState(out var after);
            if (after.ActiveCommandMode != radarToolbarPreviousMode || controls.SupportButton.IsInteractable())
                throw new InvalidOperationException("Mission radar entered generic Scan targeting or remained available during cooldown.");
            ScreenCapture.CaptureScreenshot(Output + "/radar-toolbar-after-" + GameLocalization.CurrentLocaleCode + ".png");
            radarToolbarAudit = false;
            Complete(true, "Radar toolbar " + GameLocalization.CurrentLocaleCode + ": actual click -> scan accepted -> one charge/cooldown -> reinforcement lesson; no generic Scan targeting.");
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
                    " camera=" + camera?.name + " button=" + button.name + " group=" + button.GetComponent<CanvasGroup>()?.blocksRaycasts);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
