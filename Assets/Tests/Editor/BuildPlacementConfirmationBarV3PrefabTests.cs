#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuildPlacementConfirmationBarV3PrefabTests
{
    [Test]
    public void Prefab_UsesProceduralV3ChromeAndExistingBuildingPortrait()
    {
        GameObject prefab = RequirePrefab();
        BuildPlacementConfirmationBarView view = prefab.GetComponent<BuildPlacementConfirmationBarView>();
        Assert.NotNull(view);
        Assert.That(prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length, Is.GreaterThanOrEqualTo(7));
        Assert.IsNull(prefab.GetComponent<Image>(), "The old raster root frame must not render over procedural V3 chrome.");
        Assert.AreEqual(
            BuildPlacementConfirmationBarPrefabSetupEditor.BuildingPortraitPath,
            AssetDatabase.GetAssetPath(view.BuildingPortrait.sprite));
        Assert.AreEqual(
            BuildPlacementConfirmationBarPrefabSetupEditor.MaterialsIconSpritePath,
            AssetDatabase.GetAssetPath(view.MaterialsIconSprite));
    }

    [Test]
    public void RuntimeMount_UsesV3FullWidthTargetFootprint()
    {
        GameObject canvasObject = new("PlacementV3Canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            canvas.sizeDelta = new Vector2(1672f, 941f);
            BuildPlacementConfirmationBarView view =
                BuildPlacementConfirmationBarView.Ensure(RequirePrefab(), canvas);
            Assert.NotNull(view);
            RectTransform mountedSection = view.transform as RectTransform;
            Assert.AreEqual(Vector2.zero, mountedSection.anchorMin);
            Assert.AreEqual(Vector2.one, mountedSection.anchorMax);
            Assert.AreEqual("PlacementBarPanel", view.Root.name);
            Assert.That(Vector2.Distance(
                new Vector2(4f / 1672f, 14f / 941f), view.Root.anchorMin), Is.LessThan(0.0001f));
            Assert.That(Vector2.Distance(
                new Vector2(1668f / 1672f, 324f / 941f), view.Root.anchorMax), Is.LessThan(0.0001f));
            Assert.AreEqual(Vector2.zero, view.Root.sizeDelta);
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(1664f, view.Root.rect.width);
            Assert.AreEqual(310f, view.Root.rect.height);
            BuildPlacementConfirmationBarDesignLayoutView designLayout =
                view.Root.GetComponent<BuildPlacementConfirmationBarDesignLayoutView>();
            Assert.NotNull(designLayout);
            Assert.NotNull(designLayout.DesignContent);
            Assert.AreEqual(new Vector2(1664f, 310f), designLayout.ReferenceSize);
            Assert.NotNull(view.ValidityPanel);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void Prefab_PreservesAllThreePlacementActionsAndUltrawideLayout()
    {
        GameObject prefab = RequirePrefab();
        BuildPlacementConfirmationBarView view = prefab.GetComponent<BuildPlacementConfirmationBarView>();
        Assert.NotNull(view.CancelButton);
        Assert.NotNull(view.RotateButton);
        Assert.NotNull(view.ConfirmButton);
        Assert.NotNull(view.MaterialsCostText);
        Assert.NotNull(view.OilCostText);
        Assert.NotNull(view.FuelCostText);
        Assert.NotNull(view.FootprintText);
        BuildPlacementConfirmationResponsiveLayoutView responsive =
            prefab.GetComponentInChildren<BuildPlacementConfirmationResponsiveLayoutView>(true);
        Assert.NotNull(responsive);
        Assert.AreEqual(1664f, responsive.ReferenceWidth);
        Assert.AreEqual(3, responsive.RightAnchoredTargets.Length);
        Assert.NotNull(view.ValidityPanelPrefab);
        BuildPlacementValidityPanelView validityPrefab =
            view.ValidityPanelPrefab.GetComponent<BuildPlacementValidityPanelView>();
        Assert.NotNull(validityPrefab);
        Assert.NotNull(validityPrefab.ValiditySurface);
        Assert.NotNull(validityPrefab.MinimapSurface);
    }

    [Test]
    public void RuntimeState_ValidInvalidAndClosedSwapTargetPanelsAndConfirmState()
    {
        GameObject canvasObject = new("PlacementStateCanvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            canvas.sizeDelta = new Vector2(1672f, 941f);
            GameObject aria = new("AriaAssistantButton", typeof(RectTransform));
            aria.transform.SetParent(canvas, false);
            GameObject threat = new("ThreatJumpPanel", typeof(RectTransform));
            threat.transform.SetParent(canvas, false);

            BuildPlacementConfirmationBarView view =
                BuildPlacementConfirmationBarView.Ensure(RequirePrefab(), canvas);
            Assert.NotNull(view);

            view.BindRuntimeCommands(new PreviewBuildingCommand(true));
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(view.ConfirmButton.interactable);
            Assert.IsFalse(view.ValidityPanel.IsVisible);
            Assert.IsTrue(aria.activeSelf);
            Assert.IsFalse(threat.activeSelf);
            Assert.AreEqual(231f, view.Root.rect.height, 0.01f);

            view.BindRuntimeCommands(new PreviewBuildingCommand(false));
            Canvas.ForceUpdateCanvases();
            Assert.IsFalse(view.ConfirmButton.interactable);
            Assert.IsTrue(view.ValidityPanel.IsVisible);
            Assert.IsFalse(aria.activeSelf);
            StringAssert.Contains("INVALID", view.StatusText.text);
            Assert.AreEqual(310f, view.Root.rect.height, 0.01f);

            view.BindRuntimeCommands(null);
            Assert.IsFalse(view.ValidityPanel.IsVisible);
            Assert.IsTrue(aria.activeSelf);
            Assert.IsTrue(threat.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    public static void RunFocusedValidation()
    {
        var tests = new BuildPlacementConfirmationBarV3PrefabTests();
        int passed = 0;
        try
        {
            tests.Prefab_UsesProceduralV3ChromeAndExistingBuildingPortrait(); passed++;
            tests.RuntimeMount_UsesV3FullWidthTargetFootprint(); passed++;
            tests.Prefab_PreservesAllThreePlacementActionsAndUltrawideLayout(); passed++;
            tests.RuntimeState_ValidInvalidAndClosedSwapTargetPanelsAndConfirmState(); passed++;
            Debug.Log($"[BuildPlacementConfirmationBarV3Validation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[BuildPlacementConfirmationBarV3Validation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            BuildPlacementConfirmationBarPrefabSetupEditor.PrefabPath);
        Assert.NotNull(prefab);
        return prefab;
    }

    private sealed class PreviewBuildingCommand : IBuildingUiCommand
    {
        private readonly bool canConfirm;

        public PreviewBuildingCommand(bool canConfirmPlacement)
        {
            canConfirm = canConfirmPlacement;
        }

        public int CurrentDollars => 12500;
        public bool HasPendingBuildingPlacement => true;
        public bool CanConfirmBuildingPlacement => canConfirm;
        public string PlacementStatusText => canConfirm
            ? "Power Plant: Valid placement"
            : "Power Plant: Invalid placement";
        public int ActivePlacementCost => 1500;
        public int ActivePlacementCreditsCost => 250;
        public float ActivePlacementDurationSeconds => 45f;
        public int MaxQueuedUnitProductions => 25;

        public BuildingUiCommandFailure GetCampRequestFailure(
            GameObject prefab, int price, out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return default;
        }

        public BuildingUiCommandFailure TryRequestCampItem(
            GameObject prefab,
            int price,
            out string requiredBuildingDisplayName,
            bool focusProducerOnSuccess)
        {
            requiredBuildingDisplayName = string.Empty;
            return default;
        }

        public bool CancelProduction(int buildingId, int pendingProductionIndex) => false;
        public bool ConfirmBuildingPlacement() => canConfirm;
        public void CancelBuildingPlacement() { }
        public bool RotateBuildingPlacement() => true;
    }
}
#endif
