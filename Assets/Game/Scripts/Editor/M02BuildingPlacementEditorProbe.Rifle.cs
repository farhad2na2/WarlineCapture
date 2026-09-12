using System;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

namespace Game.Editor
{
    public static partial class M02BuildingPlacementEditorProbe
    {
        private const string RifleSingleClickMode = "Warline.M02.RifleSingleClickProbe";
        private static int rifleClicks;
        private static int[] rifleControlClicks;
        private static BuildDrawerCatalogRuntimeView rifleCatalog;
        private static BuildDrawerView rifleDrawer;
        private static RifleGameFrameDriver rifleDriver;
        private static bool insideRifleGameFrame;

        public static void RunRifleSingleClickValidation()
        {
            rifleClicks = 0;
            rifleControlClicks = new int[4];
            rifleCatalog = null;
            rifleDrawer = null;
            rifleDriver = null;
            insideRifleGameFrame = false;
            SessionState.SetBool(RifleSingleClickMode, true);
            Run();
        }

        private static void EnsureRifleGameFrame()
        {
            if (rifleDriver != null) return;
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType != null) EditorWindow.GetWindow(gameViewType).Focus();
            var owner = new GameObject("M2 ARIA pointer QA");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            rifleDriver = owner.AddComponent<RifleGameFrameDriver>();
        }

        public sealed class RifleGameFrameDriver : MonoBehaviour
        {
            private IEnumerator Start()
            {
                while (SessionState.GetBool(Active, false))
                {
                    yield return new WaitForEndOfFrame();
                    insideRifleGameFrame = true;
                    try { Tick(); }
                    finally { insideRifleGameFrame = false; }
                }
            }
        }

        private static void AdvanceRifleSingleClick(EntityManager em, Entity root,
            CampaignMissionAttemptFactsComponent facts, MatchOverlayCommandControlsView controls)
        {
            if (!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel)) return;
            var aria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if (panel.TutorialStep == 5)
            {
                if (Click(aria?.DoItButton)) nextAction = EditorApplication.timeSinceStartup + 2;
                return;
            }
            if (facts.RequiredBuildingCompletedCount == 0) return;
            if (rifleClicks == 1 && rifleCatalog == null)
            {
                rifleDrawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                rifleCatalog = rifleDrawer?.GetComponent<BuildDrawerCatalogRuntimeView>();
                if (rifleCatalog == null) return;
                foreach (var tab in rifleDrawer.Tabs)
                    if (tab.Category == BuildDrawerCategory.Soldiers) tab.Button.onClick.AddListener(() => rifleControlClicks[1]++);
                rifleDrawer.ItemTemplate.SelectionButton.onClick.AddListener(() => rifleControlClicks[2]++);
                rifleDrawer.PrimaryActionButton.onClick.AddListener(() => rifleControlClicks[3]++);
            }
            if (rifleClicks > 0)
            {
                // Check again after multiple rendered frames, before issuing another user click.
                for (int control = 0; control < 4; control++)
                    if (rifleControlClicks[control] != (control < rifleClicks ? 1 : 0))
                        throw new InvalidOperationException($"Do It {rifleClicks} automatically clicked control {control}: {string.Join(",", rifleControlClicks)}");
                if (rifleClicks < 4 && (panel.TutorialStep != 6 || facts.RequiredUnitProducedCount != 0))
                    throw new InvalidOperationException("Rifle lesson completed before the player confirmed production.");
                if (rifleClicks == 2 && (bool)ReadRifleField("_hasSelectedItem"))
                    throw new InvalidOperationException("Selecting Soldiers automatically selected its first item.");
                if (rifleClicks == 3 && !(bool)ReadRifleField("_hasSelectedItem"))
                    throw new InvalidOperationException("Third Do It did not select the rifle item.");
            }
            if (rifleClicks == 4)
            {
                if (facts.RequiredUnitProducedCount == 0) return;
                Complete(true, "M2 ARIA: four explicit Do It clicks = open Build / Soldiers / rifle / Produce; no automatic clicks between actions; actual rifle delivered.");
                return;
            }
            if (panel.TutorialStep != 6 || aria == null || !aria.DoItButton.isActiveAndEnabled) return;
            if (!IsTouchable(aria.DoItButton)) return;
            if (rifleClicks == 0)
            {
                if (controls?.BuildButton == null) return;
                controls.BuildButton.onClick.AddListener(() => rifleControlClicks[0]++);
            }
            if (!Click(aria.DoItButton)) return;
            rifleClicks++;
            Log($"Rifle explicit Do It {rifleClicks}: controls={string.Join(",", rifleControlClicks)}");
            ScreenCapture.CaptureScreenshot(Output + $"/rifle-click-{rifleClicks}.png");
            nextAction = EditorApplication.timeSinceStartup + 3;
        }

        private static bool IsTouchable(Button button)
        {
            if (!button.IsInteractable()) return false;
            var rect = (RectTransform)button.transform;
            var raycaster = button.GetComponentInParent<GraphicRaycaster>();
            if (raycaster == null) return false;
            var camera = raycaster.eventCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            bool touchable = hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button;
            if (!touchable && EditorApplication.timeSinceStartup >= nextAction)
            {
                Log($"ARIA pointer waiting: position={point} screen={Screen.width}x{Screen.height} raycaster={raycaster.name} camera={camera?.name} blocker={(hits.Count > 0 ? hits[0].gameObject.name : "none")}");
                nextAction = EditorApplication.timeSinceStartup + 1;
            }
            return touchable;
        }

        private static object ReadRifleField(string name) => rifleCatalog.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rifleCatalog);
    }
}
