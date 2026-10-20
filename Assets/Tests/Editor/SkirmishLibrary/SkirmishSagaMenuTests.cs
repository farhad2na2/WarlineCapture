using System;
using System.Linq;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSagaMenuTests
    {
        private GameObject _host;
        private QuickCustomScreenView _view;
        private bool _priorSeating;
        private IUiLocalization _priorLocalization;
        private readonly System.Collections.Generic.Dictionary<string, (bool exists, string value)> _prefs = new();
        [SetUp]
        public void SetUp()
        {
            _priorLocalization = UiShellRuntimeGateway.Localization;
            foreach (string key in new[]
            {
                SkirmishLibraryViewModel.ScenarioPrefsKey,
                SkirmishLibraryViewModel.LevelPrefsKey,
                SkirmishLibraryViewModel.MapPrefsKey,
                SkirmishLibraryViewModel.ObjectivePrefsKey,
                SkirmishLibraryViewModel.LayoutPrefsKey,
                SkirmishLibraryViewModel.DifficultyPrefsKey
            }

            )
                _prefs[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetString(key));
            LocalizationTestState.UseShippedCatalog();
            UiShellRuntimeGateway.BindLocalization(new TestLocalization());
            _priorSeating = TheatreSeatGeometry.SeatingReady;
            TheatreSeatGeometry.SeatingReady = false;
            _host = new GameObject("SagaMenuTestCanvas", typeof(RectTransform), typeof(Canvas));
            _host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab, _host.transform);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _view = instance.GetComponent<QuickCustomScreenView>();
            _view.Bind(default);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_host);
            TheatreSeatGeometry.SeatingReady = _priorSeating;
            LocalizationTestState.UseShippedCatalog();
            UiShellRuntimeGateway.BindLocalization(_priorLocalization);
            foreach (var pair in _prefs)
            {
                if (pair.Value.exists)
                    PlayerPrefs.SetString(pair.Key, pair.Value.value);
                else
                    PlayerPrefs.DeleteKey(pair.Key);
            }

            PlayerPrefs.Save();
        }

        private SkirmishLibraryViewModel Bind(bool allOwned = true)
        {
            var model = new SkirmishLibraryViewModel(SkirmishBattleCatalogConfig.Load().Entries, null, key => allOwned || key == "skirmish.s001" ? ContentAccessState.Allowed : ContentAccessState.NotOwned, showPlanned: true, prefs: new MemoryPrefs());
            _view.UseLibraryViewModel(model);
            _view.SelectScenarioId("S001");
            Canvas.ForceUpdateCanvases();
            return model;
        }

        [TestCase(4800f, 2160f, .5f)]
        [TestCase(3840f, 2160f, .5f)]
        [TestCase(2880f, 2160f, .5f)]
        [TestCase(1920f, 1080f, 1f)]
        public void LiveCanvasDensityKeepsControlsAtTheirAuthoredScreenSize(float width, float height, float density)
        {
            var parent = new GameObject("LiveCanvasCoordinateSpace", typeof(RectTransform));
            try
            {
                var canvas = (RectTransform)parent.transform;
                canvas.sizeDelta = new Vector2(width, height);
                canvas.localScale = Vector3.one * density;
                var composition = new GameObject("Composition", typeof(RectTransform)).GetComponent<RectTransform>();
                composition.SetParent(canvas, false);
                SkirmishLibraryFrame.FitComposition(composition);
                Assert.AreEqual(1080f, composition.rect.height, .01f);
                var corners = new Vector3[4];
                composition.GetWorldCorners(corners);
                Assert.AreEqual(width * density, Vector3.Distance(corners[1], corners[2]), .1f);
                Assert.AreEqual(height * density, Vector3.Distance(corners[0], corners[1]), .1f);
                Assert.AreEqual(1f, composition.lossyScale.y, .001f, "Fixed-size controls must not shrink with the shell CanvasScaler.");
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }

        [Test]
        public void SetupRefreshKeepsTheMapAndNativeStopHitTargets()
        {
            Bind();
            var catalog = SkirmishBattleCatalogConfig.Load();
            Assert.IsTrue(catalog.TryGet("S002", out var selectedEntry));
            _view.PrepareScenarioAdjustment(selectedEntry.PlayableScenarioIndex, 104731);
            Canvas.ForceUpdateCanvases();
            var clip = _view.transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip") as RectTransform;
            Assert.Greater(clip.rect.width, 480f, "A setup refresh must preserve the visible map clip.");
            Assert.Greater(clip.rect.height, 480f);
            Assert.AreEqual(Vector2.zero, clip.anchorMin);
            Assert.AreEqual(Vector2.one, clip.anchorMax);
            var scroll = _view.GetComponentsInChildren<ScrollRect>(false).Single(x => x.name == "SagaViewport");
            var selected = (RectTransform)Stop("S002").transform;
            Assert.IsTrue(scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(selected.TransformPoint(selected.rect.center))), "The selected starter stop must remain inside the visible viewport.");
            foreach (var stop in _view.GetComponentsInChildren<Button>(false).Where(x => x.name.StartsWith("BattleCard_", StringComparison.Ordinal)))
            {
                Assert.IsTrue(stop.IsActive());
                Assert.IsTrue(stop.targetGraphic.raycastTarget, "Missing native medallion hit target: " + stop.name);
                Assert.AreEqual(stop.transform, stop.targetGraphic.transform.parent);
            }
            // Full EventSystem raycasts are proved by the normal-input Play-mode
            // harness, whose camera and Screen dimensions are stable while sampling.
        }

        [Test]
        public void ReturningFromTheatreHidesItsDetailPanelAndShowsFullBattleBriefing()
        {
            var model = Bind();
            model.BackToTheater();
            Canvas.ForceUpdateCanvases();
            _view.SelectScenarioId("S002");
            Canvas.ForceUpdateCanvases();
            Assert.IsFalse(_view.GetComponentsInChildren<Transform>(false).Any(x => x.name == "TheatreDetailPanel"), "Reparented theatre labels must not leak around the battlefield map.");
            var description = _view.GetComponentsInChildren<TMP_Text>(false).Single(x => x.name == "SeedHelp");
            Assert.AreEqual(TextOverflowModes.Overflow, description.overflowMode);
            Assert.AreEqual(int.MaxValue, description.maxVisibleLines);
            Assert.GreaterOrEqual(description.rectTransform.rect.height, 112f);
            description.GetComponent<V3LocalizedTextBindingView>().ApplyLocalization();
            Canvas.ForceUpdateCanvases();
            description.ForceMeshUpdate();
            Assert.AreEqual(20f, description.fontSizeMax, "A later localization pass must preserve the measured font.");
            Assert.LessOrEqual(description.renderedHeight, description.rectTransform.rect.height - 16f + 1f, "The complete briefing must stay above the stat tiles.");
        }

        [Test]
        public void LongPersianBriefingScrollsAtFourByThreeWithoutShrinkingOrCoveringItsText()
        {
            _host.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            ((RectTransform)_host.transform).sizeDelta=new Vector2(1440,1080);
            LocalizationTestState.UseShippedCatalog("fa-IR");
            Bind();
            _view.SelectScenarioId("S004");
            Canvas.ForceUpdateCanvases();
            var rules=(RectTransform)_view.transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            var scroll=rules.GetComponent<ScrollRect>();
            Assert.IsNotNull(scroll);
            Assert.IsTrue(scroll.enabled && scroll.vertical && !scroll.horizontal);
            Assert.IsTrue(rules.GetComponent<RectMask2D>().enabled);
            Assert.IsTrue(rules.GetComponent<Graphic>().raycastTarget);
            var handle=(RectTransform)rules.Find("BriefingScrollBar/Handle");
            Assert.LessOrEqual(handle.rect.width,6.1f,"The native scroll handle must stay inside its thin rail.");
            var description=rules.Find("BriefingColumn/SeedHelp").GetComponent<TMP_Text>();
            description.GetComponent<V3LocalizedTextBindingView>().ApplyLocalization();
            description.ForceMeshUpdate();
            Assert.GreaterOrEqual(description.fontSize,21.9f);
            var pic=(RectTransform)rules.Find("BriefingColumn/BriefingPreview/Picture");
            var a=new Vector3[4];var b=new Vector3[4];
            pic.GetWorldCorners(a);description.rectTransform.GetWorldCorners(b);
            Assert.GreaterOrEqual(a[0].y,b[1].y-.5f,"The picture must fit its allocated row above the text.");
            scroll.verticalNormalizedPosition=0;
            Canvas.ForceUpdateCanvases();
            var tiers=(RectTransform)rules.Find("BriefingColumn/MedalDifficultySelector");
            tiers.GetWorldCorners(a);
            foreach(var corner in a)
                Assert.IsTrue(rules.rect.Contains(rules.InverseTransformPoint(corner)),"The medal selector must be reachable at the bottom of the native scroller.");
        }

        [Test]
        public void NativeStopsDoNotGateOwnedBattlesByOrderAndRebuildWithoutDuplicates()
        {
            var model = Bind();
            var later = Stop("S004");
            Assert.IsNotNull(later);
            later.onClick.Invoke();
            Assert.AreEqual("S004", model.Briefing.ScenarioId);
            Assert.IsTrue(model.Briefing.CanLaunch);
            Stop("S001").onClick.Invoke();
            Stop("S004").onClick.Invoke();
            Assert.AreEqual(24, _view.GetComponentsInChildren<Button>(false).Count(x => x.name.StartsWith("BattleCard_", StringComparison.Ordinal)));
            Assert.IsTrue(_view.GetComponentsInChildren<ScrollRect>(false).Any(x => x.name == "SagaViewport"));
            Assert.IsFalse(_view.GetComponentsInChildren<Button>(false).Any(x => x.name == SkirmishLibraryControlNames.LayoutGrid));
        }

        [Test]
        public void NativeZoomControlOpensTheSelectedBattlefield()
        {
            var model = Bind();
            model.BackToTheater();
            Canvas.ForceUpdateCanvases();
            var alias = _view.GetComponentsInChildren<Transform>(false).Single(x => x.name == SkirmishLibraryControlNames.ZoomIn);
            var zoom = alias.GetComponentInParent<Button>();
            Assert.IsNotNull(zoom);
            Assert.IsTrue(zoom.interactable);
            zoom.onClick.Invoke();
            Assert.AreEqual(SkirmishLibraryLevel.Battlefield, model.Level);
            Assert.IsNotNull(Stop("S001"));
        }

        [Test]
        public void FreshProfileNativeLocksCoverEveryPublishedBattleOutsideS001()
        {
            var model = Bind(false);
            int checkedLocks = 0;
            foreach (var entry in SkirmishBattleCatalogConfig.Load().Entries)
            {
                if (!entry.IsPlayable || entry.ScenarioId == "S001")
                    continue;
                _view.SelectScenarioId(entry.ScenarioId);
                var stop = Stop(entry.ScenarioId);
                Assert.IsNotNull(stop, entry.ScenarioId);
                Assert.IsNotNull(stop.transform.Find("StoreTag"), entry.ScenarioId);
                stop.onClick.Invoke();
                Assert.AreEqual(SkirmishCardAccess.NotOwned, model.Briefing.Access, entry.ScenarioId);
                Assert.IsFalse(model.Launch(), entry.ScenarioId);
                checkedLocks++;
            }

            Assert.Greater(checkedLocks, 0);
        }

        [Test]
        public void NativeNotOwnedStopHasStoreTagAndCannotLaunch()
        {
            var model = Bind(false);
            var locked = Stop("S002");
            Assert.IsNotNull(locked.transform.Find("StoreTag"));
            locked.onClick.Invoke();
            Assert.AreEqual(SkirmishCardAccess.NotOwned, model.Briefing.Access);
            Assert.IsFalse(model.Briefing.CanLaunch);
            Assert.IsFalse(model.Launch());
        }

        [Test]
        public void UnwonTiersStayEmptyAndRepeatedPresentationDoesNotChangeMedalColors()
        {
            Bind();
            var row = _view.transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn/MedalRow");
            var icon = row.Find("Slot0/Icon").GetComponent<Image>();
            var sprite = icon.sprite;
            var tint = icon.color;
            for (int i = 0; i < 3; i++)
            {
                _view.SelectScenarioId("S002");
                _view.SelectScenarioId("S001");
            }

            Assert.AreSame(sprite, icon.sprite);
            Assert.AreEqual(tint, icon.color);
            StringAssert.Contains("empty", icon.sprite.name.ToLowerInvariant());
        }

        [Test]
        public void PersianStopNumbersKeepTheirLogicalDigitOrder()
        {
            LocalizationTestState.UseShippedCatalog("fa-IR");
            Bind();
            var number = Stop("S012").transform.Find("StopNumber").GetComponent<TMP_Text>();
            Assert.AreEqual("۱۲", number.text);
            Assert.IsFalse(number.isRightToLeftText);
            Assert.IsNull(number.GetComponent<V3LocalizedTextBindingView>(), "Pure numeric strings must not pass through Arabic word shaping.");
        }

        [Test]
        public void ChapterJumpFocusesItsFirstStopInsideThePannableViewport()
        {
            Bind();
            var chapter = _view.GetComponentsInChildren<Button>(false).Single(x => x.name == SkirmishLibraryControlNames.ObjectiveTab("CE"));
            chapter.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            var scroll = _view.GetComponentsInChildren<ScrollRect>(false).Single(x => x.name == "SagaViewport");
            var stop = (RectTransform)Stop("S019").transform;
            Vector3 center = scroll.viewport.InverseTransformPoint(stop.TransformPoint(stop.rect.center));
            Assert.IsTrue(scroll.viewport.rect.Contains(center), "Chapter navigation must pan to its stop.");
        }

        [Test]
        public void RectangularFramesStaySteelDuringPointerFeedback()
        {
            Bind();
            var catalog = SkirmishBattleCatalogConfig.Load();
            Assert.IsTrue(catalog.TryGet("S002", out var expanded));
            _view.PrepareScenarioAdjustment(expanded.PlayableScenarioIndex, 104731);
            var lines = new System.Collections.Generic.List<string>();
            foreach (var graphic in _view.GetComponentsInChildren<Graphic>(false))
            {
                if (graphic is TMP_Text)
                    continue;
                var data = new SerializedObject(graphic);
                string path = AnimationUtility.CalculateTransformPath(graphic.transform, _view.transform);
                string art = graphic is Image image && image.sprite != null ? image.sprite.name : "";
                lines.Add(path + " type=" + graphic.GetType().Name + " size=" + graphic.rectTransform.rect.size + " color=" + graphic.color + " art=" + art);
                if (graphic is V3GradientGraphic)
                {
                    var border = data.FindProperty("borderColor").colorValue;
                    Assert.IsFalse(IsYellow(border), "Yellow rectangular frame: " + path);
                }
            }

            string report = "Design/AgentReports/SkirmishBattleLibrary/2026-10-20/tests/native-chrome.txt";
            System.IO.File.WriteAllLines(report, lines);
            foreach (var state in _view.GetComponentsInChildren<SkirmishLibraryButtonStateView>(false))
            {
                state.OnPointerEnter(null);
                state.OnPointerDown(null);
                state.OnPointerUp(null);
                state.OnPointerExit(null);
                var border = new SerializedObject(state.GetComponent<V3GradientGraphic>()).FindProperty("borderColor").colorValue;
                Assert.IsFalse(IsYellow(border), "Pointer feedback restored a yellow frame on " + state.name);
            }
        }

        private static bool IsYellow(Color c) => c.a > .05f && c.r > .4f && c.g > .3f && c.b < c.g * .8f;
        private Button Stop(string id) => _view.GetComponentsInChildren<Button>(false).SingleOrDefault(x => x.name == SkirmishLibraryControlNames.BattleCard(id));
        private sealed class TestLocalization : IUiLocalization
        {
            public event Action LocaleChanged { add => GameLocalization.LocaleChanged += value; remove => GameLocalization.LocaleChanged -= value; }

            public string CurrentLocaleCode => GameLocalization.CurrentLocaleCode;
            public bool IsRightToLeft => GameLocalization.IsRightToLeft;
            public UnityEngine.Object CurrentFontAsset => GameLocalization.CurrentFontAsset;
            public int AvailableLocaleCount => 2;

            public string[] GetLocaleShortLabels() => GameLocalization.GetLocaleShortLabels();
            public string[] GetLocaleSwitchLabels() => GameLocalization.GetLocaleSwitchLabels();
            public int GetLocaleIndex(string code) => GameLocalization.GetLocaleIndex(code);
            public string GetLocaleCode(int index) => GameLocalization.GetLocaleCode(index);
            public bool SetLocale(string code, bool persist = true) => GameLocalization.SetLocale(code, persist);
            public string Get(string key, string fallback = "") => GameLocalization.Get(key, fallback);
            public bool TryGet(string key, out string value) => GameLocalization.TryGet(key, out value);
            public string Format(string key, string fallback, params object[] args) => string.Format(Get(key, fallback), args);
            public string GetBySource(string source) => GameLocalization.GetBySource(source);
            public bool TryGetBySource(string source, out string key, out string value) => GameLocalization.TryGetBySource(source, out key, out value);
            public bool TryGetSourceByLocalized(string value, out string key, out string source) => GameLocalization.TryGetSourceByLocalized(value, out key, out source);
        }

        private sealed class MemoryPrefs : ISkirmishLibraryPrefs
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _values = new();
            public string Get(string key, string fallback) => _values.TryGetValue(key, out var value) ? value : fallback;
            public void Set(string key, string value) => _values[key] = value;
            public void Save()
            {
            }
        }
    }
}
