#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class M02EstablishBaseGuidanceTests
{
    [TestCase(RenderMode.ScreenSpaceOverlay)]
    [TestCase(RenderMode.ScreenSpaceCamera)]
    public void ScreenSpaceCanvasKeepsItsScaleWhenRuntimeUiModeIsReasserted(RenderMode mode)
    {
        var root = new GameObject("Bootstrap");
        root.SetActive(false);
        var canvasObject = new GameObject("Scaled UI", typeof(RectTransform), typeof(Canvas));
        try
        {
            var bootstrap = root.AddComponent<Game.Composition.MenuBootstrapView>();
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = mode;
            SetPrivateField(bootstrap, "uiCanvas", canvas);
            canvas.transform.localScale = Vector3.one * .5f;
            bootstrap.ApplyRuntimeUiMode();
            Assert.That(canvas.transform.localScale, Is.EqualTo(Vector3.one * .5f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
    }

    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    public void RifleDoItClicksExactlyOneControlAndNeverRetriesPastIt(bool synchronousOpen, bool rejectProduction)
    {
        UiShellRuntimeGateway.Register(null);
        var buildObject = new GameObject("Build", typeof(RectTransform), typeof(Image), typeof(Button));
        var drawerObject = new GameObject("Drawer", typeof(RectTransform));
        drawerObject.SetActive(false);
        var content = new GameObject("Items", typeof(RectTransform));
        content.transform.SetParent(drawerObject.transform, false);
        var itemObject = new GameObject("Rifle", typeof(RectTransform), typeof(Image), typeof(Button));
        itemObject.transform.SetParent(content.transform, false);
        var primaryObject = new GameObject("Produce", typeof(RectTransform), typeof(Image), typeof(Button));
        primaryObject.transform.SetParent(drawerObject.transform, false);
        var rifle = new GameObject("Unit_Chr_Soldier_Male_02_Alt_04");
        var assistant = new MatchHudAssistantUiSystemHelper();
        var command = new TestBuildingUiCommand();
        try
        {
            var drawer = drawerObject.AddComponent<BuildDrawerView>();
            var item = itemObject.AddComponent<BuildDrawerItemView>();
            SetPrivateField(item, "selectionButton", itemObject.GetComponent<Button>());
            SetPrivateField(drawer, "drawerRoot", drawerObject);
            SetPrivateField(drawer, "itemContentRoot", content.transform as RectTransform);
            SetPrivateField(drawer, "itemTemplate", item);
            SetPrivateField(drawer, "buildButton", primaryObject.GetComponent<Button>());
            var tabs = CreateCategoryTabs(drawerObject.transform);
            SetPrivateField(drawer, "tabs", tabs);
            var catalog = drawerObject.AddComponent<BuildDrawerCatalogRuntimeView>();
            catalog.ConfigureForTests(drawer, new TestPrefabSource(new[] { rifle }, Array.Empty<GameObject>()),
                new TestPrefabSource(Array.Empty<GameObject>(), Array.Empty<GameObject>()));
            catalog.ConfigureCatalogMetadataResolvers(null, ResolveRequestableRifleMetadata);
            int closes = 0, acknowledgements = 0;
            catalog.BindRuntimeCommands(command, () => closes++);
            SetPrivateField(assistant, "_lastPanelModel", new UiAssistantPanelModel(
                1, false, 0, UiAssistantGoalRowModel.Empty, UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty, UiAssistantMessageRowModel.Empty, UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty, UiAssistantMessageRowModel.Empty, UiAssistantMessageRowModel.Empty,
                UiAssistantTargetLockModel.Empty, UiAssistantNarrationModel.Empty, true, "Rifle", "Recruit", "HIGH",
                "DO IT", true, true, false, false, string.Empty, string.Empty,
                recommendationKind: 5, recommendationTargetKind: 4, tutorialStep: 6, tutorialStepCount: 9));
            var highlight = GetPrivateField<AssistantHighlightPresentationSystemHelper>(assistant, "_highlightPresentationSystem");
            highlight.Bind(null, uiSurfaceAcknowledged: _ => acknowledgements++);
            highlight.BindBuildButton(buildObject.GetComponent<Button>());
            highlight.BindBuildDrawer(drawer);
            int[] clicks = new int[4];
            buildObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                clicks[0]++;
                if (synchronousOpen) drawerObject.SetActive(true);
            });
            foreach (var tab in tabs)
                if (tab.Category == BuildDrawerCategory.Soldiers) tab.Button.onClick.AddListener(() => clicks[1]++);
            item.SelectionButton.onClick.AddListener(() => clicks[2]++);
            primaryObject.GetComponent<Button>().onClick.AddListener(() => clicks[3]++);

            for (int action = 0; action < 4; action++)
            {
                if (action == 3 && rejectProduction) command.ProductionFailure = BuildingUiCommandFailure.InvalidSelection;
                typeof(MatchHudAssistantUiSystemHelper).GetMethod("ExecuteRecommendation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(assistant, null);
                if (action == 0 && !synchronousOpen) drawerObject.SetActive(true);
                for (int frame = 0; frame < 120; frame++) assistant.TickHighlight(Time.unscaledTime + frame / 60f);
                for (int control = 0; control < clicks.Length; control++)
                    Assert.That(clicks[control], Is.EqualTo(control <= action ? 1 : 0), $"Action {action}, control {control}");
                Assert.That(GetPrivateField<byte>(assistant, "_pendingM02DoItStep"), Is.Zero);
                Assert.That(command.ProductionRequests, Is.EqualTo(action == 3 ? 1 : 0));
                Assert.That(acknowledgements, Is.EqualTo(action == 3 && !rejectProduction ? 1 : 0));
                Assert.That(closes, Is.EqualTo(action == 3 && !rejectProduction ? 1 : 0));
            }
        }
        finally
        {
            assistant.Unbind();
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(drawerObject);
            UnityEngine.Object.DestroyImmediate(buildObject);
            UnityEngine.Object.DestroyImmediate(rifle);
        }
    }
}
#endif
