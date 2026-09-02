using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class DistrictDetailActionsV3PrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN12_DistrictDetailActionsContent.prefab";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(PrefabUsesResponsiveV3Composition), test => test.PrefabUsesResponsiveV3Composition(), ref passed);
            Run(nameof(ActiveActionsQueueAndLockedActionsStayDisabled), test => test.ActiveActionsQueueAndLockedActionsStayDisabled(), ref passed);
            Run(nameof(RaidRequiresSharedV3Confirmation), test => test.RaidRequiresSharedV3Confirmation(), ref passed);
            Debug.Log($"[DistrictDetailActionsV3Validation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[DistrictDetailActionsV3Validation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void PrefabUsesResponsiveV3Composition()
    {
        GameObject prefab = RequirePrefab();
        DistrictDetailActionsScreenView view = prefab.GetComponentInChildren<DistrictDetailActionsScreenView>(true);
        Assert.NotNull(view);
        Assert.NotNull(view.BackRouteButton);
        Assert.NotNull(view.DistrictImage);
        Assert.NotNull(view.AriaPortrait);
        Assert.NotNull(view.AriaPortrait.GetComponent<AspectRatioFitter>());
        Assert.AreEqual(7, view.ActionButtons.Length);
        Assert.NotNull(view.ConfirmRaidPopupPrefab);
        Assert.NotNull(view.ConfirmRaidPopupPrefab.GetComponent<ConfirmRaidV3PopupView>());
        Assert.GreaterOrEqual(prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length, 20);

        MainMenuV3SectionLayoutView responsive = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(responsive);
        Assert.IsTrue(responsive.ExpandToCanvasWidth);
        Assert.AreEqual(new Vector2(1672f, 941f), responsive.ReferenceResolution);
        Assert.AreEqual(8, responsive.RightAnchoredTargets.Length);
    }

    [Test]
    public void ActiveActionsQueueAndLockedActionsStayDisabled()
    {
        GameObject canvas = CreateCanvas();
        try
        {
            DistrictDetailActionsScreenView view = InstantiateView(canvas.transform);
            var requested = new List<DistrictOperationActionKind>();
            view.ActionRequested += requested.Add;
            view.RefreshBindings();

            int[] activeIndices = { 0, 1, 2, 4 };
            DistrictOperationActionKind[] expected =
            {
                DistrictOperationActionKind.Patrol,
                DistrictOperationActionKind.DroneScan,
                DistrictOperationActionKind.Aid,
                DistrictOperationActionKind.Repair
            };
            for (int i = 0; i < activeIndices.Length; i++)
            {
                Button button = view.ActionButtons[activeIndices[i]];
                Assert.IsTrue(button.interactable);
                button.onClick.Invoke();
                Assert.IsFalse(button.interactable);
                TMP_Text time = button.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(text => text != null && text.name == "Time");
                Assert.NotNull(time);
                Assert.AreEqual("QUEUED", time.text);
            }

            CollectionAssert.AreEqual(expected, requested);
            Assert.IsFalse(view.ActionButtons[5].interactable);
            Assert.IsFalse(view.ActionButtons[6].interactable);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvas);
        }
    }

    [Test]
    public void RaidRequiresSharedV3Confirmation()
    {
        GameObject canvas = CreateCanvas();
        try
        {
            DistrictDetailActionsScreenView view = InstantiateView(canvas.transform);
            DistrictOperationActionKind? requested = null;
            view.ActionRequested += action => requested = action;
            view.RefreshBindings();

            view.ActionButtons[3].onClick.Invoke();
            Assert.IsNull(requested, "Raid must not queue before confirmation.");
            ConfirmRaidV3PopupView popup = canvas.GetComponentInChildren<ConfirmRaidV3PopupView>(true);
            Assert.NotNull(popup);
            Assert.IsTrue(popup.gameObject.activeSelf);

            popup.Configure(popup.CancelButton, popup.ConfirmButton);
            popup.ConfirmButton.onClick.Invoke();
            Assert.AreEqual(DistrictOperationActionKind.Raid, requested);
            Assert.IsFalse(view.ActionButtons[3].interactable);
            Assert.IsFalse(popup.gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvas);
        }
    }

    private static DistrictDetailActionsScreenView InstantiateView(Transform parent)
    {
        GameObject instance = UnityEngine.Object.Instantiate(RequirePrefab(), parent, false);
        DistrictDetailActionsScreenView view = instance.GetComponentInChildren<DistrictDetailActionsScreenView>(true);
        Assert.NotNull(view);
        return view;
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvas = new("DistrictDetailActionsTestCanvas", typeof(RectTransform), typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        return canvas;
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, $"Missing V3 District Detail prefab at {PrefabPath}.");
        return prefab;
    }

    private static void Run(string name, Action<DistrictDetailActionsV3PrefabTests> action, ref int passed)
    {
        var test = new DistrictDetailActionsV3PrefabTests();
        action(test);
        passed++;
        Debug.Log($"[DistrictDetailActionsV3Validation] passed={name}");
    }
}
