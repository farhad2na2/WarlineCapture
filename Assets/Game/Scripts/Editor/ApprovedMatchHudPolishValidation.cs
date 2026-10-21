#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    // Native view regression fixtures; never claim these synthetic models as mission evidence.
    public static class ApprovedMatchHudPolishValidation
    {
        static int checks;
        public static void Run() => MissionEditorValidationExit.Complete(ValidateNativeViews());
        public static void ValidateAndRunJourney()
        {
            if (!ValidateNativeViews()) { MissionEditorValidationExit.Complete(false); return; }
            CampaignTutorialEntryEditorProbe.RunApprovedAtlasM01Journey();
        }
        public static bool ValidateNativeViews()
        {
            bool passed = false;
            string locale = GameLocalization.CurrentLocaleCode;
            try
            {
                checks = 0;
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                foreach (string language in new[] { "en", "fa-IR" })
                {
                    GameLocalization.Initialize(catalog, language, false);
                    CheckHud(language); CheckExchange(); CheckPlacement(); CheckSpacing();
                    Check(!GameLocalization.Get("hud.build.place", "").Contains("{0}"), "No unresolved empty Build label");
                    Check(!string.IsNullOrWhiteSpace(GameLocalization.Get("hud.passenger.unload_all", "")), "Unload label localized");
                }
                passed = true;
                Debug.Log("[ApprovedMatchHudPolishValidation] result=Passed checks=" + checks + " locales=en,fa-IR nativeViews=true fixtures=true missionReadiness=NotClaimed");
            }
            catch (Exception e) { Debug.LogException(e); Debug.LogError("[ApprovedMatchHudPolishValidation] result=Failed checks=" + checks); }
            finally { GameLocalization.SetLocale(locale, false); }
            return passed;
        }
        static GameObject Instantiate(string path) => UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        static void CheckHud(string locale)
        {
            var root = Instantiate(ApprovedMatchHudPolishBuilder.Hud);
            try
            {
                Check(!root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "SquadPagination"), "No extra squad paging strip");
                var feedback = root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
                feedback.ShowFeedbackMessage("Move ready — tap clear ground to set a destination.", CommandFeedbackSeverity.Ready);
                Check(feedback.FeedbackText.fontSize >= 26, "Readable feedback type");
                Check(feedback.FeedbackText.rectTransform.rect.width >= 320, "Readable feedback clears ARIA controls");
                foreach (var map in root.GetComponentsInChildren<MatchHudMinimapView>(true))
                foreach (var button in new[] { map.ZoomInButton, map.ZoomOutButton })
                {
                    if (button == null) continue;
                    Check(button.targetGraphic is V3GradientGraphic, "Native square zoom surface");
                    Check(button.GetComponentsInChildren<Image>(true).All(i => i.sprite == null), "No legacy zoom sprite");
                }
                var wheel = root.GetComponentInChildren<CommandWheelPanelView>(true);
                var radial = Field<RectTransform>(wheel, "wheelTransform");
                Check(Field<Button>(wheel, "closeButton").transform.parent == radial, "Close control follows radial wheel layout");
                foreach (var layout in root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                    if (layout.TryGetAuthoredBasePosition(radial, out var position))
                        Check(Mathf.Approximately(position.y, -265), "Responsive cache preserves radial clearance below status");
                var drawer = root.GetComponentInChildren<MatchHudTransportPassengerDrawerView>(true);
                foreach (int rows in new[] { 0, 4, 10, 0 })
                {
                    var items = Enumerable.Range(0, rows).Select(i => new MatchHudSelectionPanelPassengerItemModel(
                        new UiEntityHandle(i + 2, 1), "Specialist " + (i + 1), "PASSENGER", "100/100", 1, null, i != 1)).ToArray();
                    drawer.Apply(new MatchHudTransportPassengersModel(true, true, new UiEntityHandle(1, 1), rows, 10, rows > 0, items, rows, 10));
                    var rect = drawer.transform as RectTransform;
                    Check(Mathf.Approximately(rect.anchoredPosition.y, -214), "Drawer below status strip");
                    Check(rect.sizeDelta.y <= 494, "Drawer clears squad tray");
                    var panel = Field<RectTransform>(drawer, "_selectionPanel");
                    var wide = Field<Vector2>(drawer, "_widePanelSize");
                    Check(panel != null && Mathf.Approximately(panel.sizeDelta.x, wide.x), "Selection panel never squeezed");
                    var exit = Field<Button>(drawer, "exitAllButton");
                    Check(exit.interactable == (rows > 0), "Actual unload gate retained");
                    CheckSurface(exit, rows > 0);
                    if (rows == 10)
                    {
                        var content = Field<RectTransform>(drawer, "contentRoot");
                        Check(content.GetComponentInParent<ScrollRect>()?.vertical == true, "Long passenger list scrolls");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void CheckExchange()
        {
            var root = Instantiate(ApprovedMatchHudPolishBuilder.Exchange);
            try
            {
                var view = root.GetComponent<ResourceExchangePopupView>();
                Check(view.ResolveInputThumbnail("100 OIL", 0) == view.ResolveRecipeThumbnail(1), "Oil icon follows input rather than list position");
                Check(view.ResolveInputThumbnail("100 MATERIALS", 0) == view.ResolveRecipeThumbnail(3), "Materials icon follows input rather than list position");
                Check(view.ResolveInputThumbnail("100 FUEL", 4) == view.ResolveRecipeThumbnail(0), "Fuel icon follows input rather than queue position");
                foreach (bool enabled in new[] { false, true, false, true })
                {
                    view.ApplyDetail("Oil to Materials", "", "", "100", "100 OIL", "300 MATERIALS", "00:45", "", enabled ? "" : "Insufficient Oil", enabled, !enabled, null);
                    var amount = Field<TMP_Text>(view, "detailAmountText");
                    Check(amount.gameObject.activeSelf && amount.enabled && !string.IsNullOrEmpty(amount.text), "Amount stays visible inside native stepper");
                    var background = amount.transform.parent.Find(amount.name + "ApprovedSurface");
                    Check(background != null && background.GetSiblingIndex() < amount.transform.GetSiblingIndex(), "Repeated authoring cannot cover amount text");
                    Check(view.FooterConfirmButton.interactable == enabled, "Exchange model owns availability");
                    CheckSurface(view.FooterConfirmButton, enabled);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void CheckPlacement()
        {
            var canvas = new GameObject("PlacementFixtureCanvas", typeof(RectTransform), typeof(Canvas));
            var root = Instantiate(ApprovedMatchHudPolishBuilder.Placement); root.transform.SetParent(canvas.transform, false);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_Alt_02_AdvancedRifle_Card_512.png");
            var model = new PlacementModel { Portrait = sprite };
            try
            {
                var view = root.GetComponent<BuildPlacementConfirmationBarView>();
                foreach (bool valid in new[] { false, true, false, true })
                {
                    model.Valid = valid; view.BindRuntimeCommands(model);
                    Check(view.ConfirmButton.interactable == valid, "Placement validity owns availability");
                    CheckSurface(view.ConfirmButton, valid);
                    Check(Field<Image>(view, "buildingPortrait").sprite == sprite, "Selected definition owns placement artwork");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas); }
        }
        static void CheckSpacing()
        {
            var settings = Instantiate(ApprovedMatchHudPolishBuilder.Settings);
            var build = Instantiate(ApprovedMatchHudPolishBuilder.BuildDrawer);
            try
            {
                var title = settings.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "TitleText");
                Check(title.rectTransform.offsetMax.x <= -136, "Settings title reserves close-button column");
                var rail = settings.GetComponentsInChildren<Transform>(true).First(t => t.name == "SettingsTabRail");
                Check(rail.GetComponentsInChildren<TMP_Text>(true).All(t => t.margin.z >= 24), "RTL settings tabs have right frame inset");
                foreach (var value in build.GetComponentsInChildren<TMP_Text>(true).Where(t => t.name == "Value" && t.transform.parent.name is "Footprint" or "BuildTime"))
                {
                    var label = value.transform.parent.Find("Label") as RectTransform;
                    Check(value.rectTransform.anchoredPosition.x - (label.anchoredPosition.x + label.sizeDelta.x) >= 18, "Stat columns keep a clear gutter");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); UnityEngine.Object.DestroyImmediate(build); }
        }
        static void CheckSurface(Button button, bool available)
        {
            HudActionVisuals.Refresh(button);
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic is TMP_SubMeshUI) continue;
                if (graphic is TMP_Text text)
                {
                    Check(text.fontSharedMaterial != null && text.fontSharedMaterial.shader.name.Contains("TextMeshPro"), "Disabled labels preserve SDF font");
                    continue;
                }
                bool grey = graphic.material != null && graphic.material.shader != null && graphic.material.shader.name == "Warline/UI/Disabled Grayscale";
                Check(grey != available, "Every disabled graphic greys and restores");
            }
        }
        static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        static void Check(bool condition, string reason) { checks++; if (!condition) throw new InvalidOperationException(reason); }
        sealed class PlacementModel : IBuildingUiCommand, IBuildingPlacementArtworkQuery, IBuildingPlacementFootprintQuery
        {
            public bool Valid; public Sprite Portrait;
            public int CurrentDollars => 1000;
            public bool HasPendingBuildingPlacement => true;
            public bool CanConfirmBuildingPlacement => Valid;
            public string PlacementStatusText => "Barracks: " + (Valid ? "VALID GROUND" : "FOOTPRINT BLOCKED");
            public int ActivePlacementCost => 90;
            public int ActivePlacementCreditsCost => 0;
            public float ActivePlacementDurationSeconds => 30;
            public int MaxQueuedUnitProductions => 4;
            public Sprite ActivePlacementPortrait => Portrait;
            public Vector2Int ActivePlacementFootprint => new(28, 15);
            public BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int cost, out string required) { required = ""; return BuildingUiCommandFailure.None; }
            public BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int cost, out string required, bool focus) { required = ""; return BuildingUiCommandFailure.None; }
            public bool CancelProduction(int id, int index) => false;
            public bool ConfirmBuildingPlacement() => Valid;
            public void CancelBuildingPlacement() { }
            public bool RotateBuildingPlacement() => true;
        }
    }
}
#endif
