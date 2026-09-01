using Game.UI.Runtime;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudFullMapV3PrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN08_FullMapPopup.prefab";

    [Test]
    public void Prefab_UsesV3CompositionAndSharedMarkerArt()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null, PrefabPath);
        MatchHudFullMapPopupView popup = prefab.GetComponent<MatchHudFullMapPopupView>();
        Assert.That(popup, Is.Not.Null);
        Assert.That(popup.Minimap, Is.Not.Null);
        Assert.That(popup.Minimap.PlayerMarkerSprite, Is.Not.Null);
        Assert.That(popup.Minimap.EnemyMarkerSprite, Is.Not.Null);
        Assert.That(popup.Minimap.NeutralMarkerSprite, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length, Is.GreaterThanOrEqualTo(20));

        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.That(layout, Is.Not.Null);
        Assert.That(layout.ReferenceResolution, Is.EqualTo(new Vector2(1672f, 941f)));
        Assert.That(Find(prefab.transform, "LegendPanel"), Is.Not.Null);
        Assert.That(Find(prefab.transform, "MapInfoPanel"), Is.Not.Null);
        Assert.That(Find(prefab.transform, "QuickTogglePanel"), Is.Not.Null);
    }

    [Test]
    public void Prefab_MapArtPreservesAspectAndActionsAreBound()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            MatchHudFullMapPopupView popup = instance.GetComponent<MatchHudFullMapPopupView>();
            Assert.That(popup.CloseAction, Is.Not.Null);
            Assert.That(popup.CenterOnHqAction, Is.Not.Null);
            AspectRatioFitter fitter = popup.Minimap.MapImage.GetComponent<AspectRatioFitter>();
            Assert.That(fitter, Is.Not.Null);
            Assert.That(fitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));

            bool focusRequested = false;
            popup.Minimap.FocusRequested += _ => focusRequested = true;
            typeof(MatchHudFullMapPopupView)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(popup, null);
            popup.CenterOnHqAction.onClick.Invoke();
            Assert.That(focusRequested, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Prefab_QuickTogglesUseProceduralCheckVisuals()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Toggle[] toggles = prefab.GetComponentsInChildren<Toggle>(true);
        Assert.That(toggles.Length, Is.EqualTo(5));
        for (int i = 0; i < toggles.Length; i++)
        {
            Assert.That(toggles[i].GetComponent<V3ToggleCheckView>(), Is.Not.Null);
            Assert.That(toggles[i].transform.Find("CheckBox/Check/CheckShort"), Is.Not.Null);
            Assert.That(toggles[i].transform.Find("CheckBox/Check/CheckLong"), Is.Not.Null);
        }
    }

    public static void RunFocusedValidation()
    {
        MatchHudFullMapV3PrefabTests tests = new();
        tests.Prefab_UsesV3CompositionAndSharedMarkerArt();
        tests.Prefab_MapArtPreservesAspectAndActionsAreBound();
        tests.Prefab_QuickTogglesUseProceduralCheckVisuals();
        Debug.Log("[MatchHudFullMapV3Validation] result=Passed tests=3");
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
