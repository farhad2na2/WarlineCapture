using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudCommandFeedbackPanelTests
{
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void RuntimeFeedbackSystem_AppliesCommandFeedbackSeverityIcons()
    {
        _root = new GameObject("FeedbackView");
        var panel = new GameObject("FeedbackPanel");
        var textNode = new GameObject("FeedbackText");
        var iconNode = new GameObject("FeedbackIcon");

        panel.transform.SetParent(_root.transform);
        textNode.transform.SetParent(panel.transform);
        iconNode.transform.SetParent(panel.transform);

        var view = _root.AddComponent<BattleHudRuntimeFeedbackView>();
        TMP_Text text = textNode.AddComponent<TextMeshProUGUI>();
        Image icon = iconNode.AddComponent<Image>();
        Sprite neutral = CreateTestSprite("FeedbackNeutral");
        Sprite ready = CreateTestSprite("FeedbackReady");
        Sprite warning = CreateTestSprite("FeedbackWarning");
        Sprite error = CreateTestSprite("FeedbackError");
        SetPrivateField(view, "feedbackPanel", panel);
        SetPrivateField(view, "feedbackText", text);
        SetPrivateField(view, "feedbackIcon", icon);
        SetPrivateField(view, "neutralIcon", neutral);
        SetPrivateField(view, "readyIcon", ready);
        SetPrivateField(view, "warningIcon", warning);
        SetPrivateField(view, "errorIcon", error);

        BattleHudRuntimeFeedbackSystem.SetActiveView(view);

        BattleHudRuntimeFeedbackSystem.ApplyCommandMode(view, TacticalCommandMode.Attack);
        Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Tap hostile target.", text.text);
        Assert.AreSame(ready, icon.sprite);

        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(
            view,
            TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotEnemy));
        Assert.AreEqual("Target is not hostile.", text.text);
        Assert.AreSame(error, icon.sprite);

        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(
            view,
            TacticalCommandResult.Success("Destroyed selected unit."));
        Assert.AreEqual("Destroyed selected unit.", text.text);
        Assert.AreSame(warning, icon.sprite);
    }

    [Test]
    public void MatchHudContentPrefab_HasCommandFeedbackReferencesAssigned()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab, $"Missing Match HUD content prefab at {MatchHudContentPrefabPath}.");

        var view = prefab.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
        Assert.NotNull(view, "SCN08 Match HUD content must expose BattleHudRuntimeFeedbackView.");

        var serializedView = new SerializedObject(view);
        AssertObjectReference(serializedView, "feedbackPanel");
        AssertObjectReference(serializedView, "feedbackText");
        AssertObjectReference(serializedView, "feedbackIcon");
        AssertObjectReference(serializedView, "neutralIcon");
        AssertObjectReference(serializedView, "readyIcon");
        AssertObjectReference(serializedView, "warningIcon");
        AssertObjectReference(serializedView, "errorIcon");

        Image icon = view.FeedbackIcon;
        Assert.NotNull(icon, "SCN08 command feedback must serialize the actual FeedbackPanel Icon image.");
        Assert.AreSame(
            FindRequiredImage(prefab.transform, "FooterContent/FeedbackPanel/Frame/Icon"),
            icon,
            "SCN08 command feedback icon must point at FooterContent/FeedbackPanel/Frame/Icon.");
        Assert.IsTrue(icon.enabled, "SCN08 command feedback icon Image must be enabled like the Build Drawer instruction icon.");
        Assert.AreSame(serializedView.FindProperty("neutralIcon").objectReferenceValue, icon.sprite);
    }

    [Test]
    public void MatchHudContentPrefab_UpdatesActualFeedbackIconForMessageSeverity()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab, $"Missing Match HUD content prefab at {MatchHudContentPrefabPath}.");
        _root = Object.Instantiate(prefab);

        var view = _root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
        Assert.NotNull(view, "SCN08 Match HUD content must expose BattleHudRuntimeFeedbackView.");
        Image icon = view.FeedbackIcon;
        Assert.NotNull(icon, "SCN08 command feedback must serialize the actual FeedbackPanel Icon image.");
        Assert.AreSame(
            FindRequiredImage(_root.transform, "FooterContent/FeedbackPanel/Frame/Icon"),
            icon,
            "Runtime command feedback must update the FooterContent feedback icon.");

        var serializedView = new SerializedObject(view);
        Sprite ready = (Sprite)serializedView.FindProperty("readyIcon").objectReferenceValue;
        Sprite error = (Sprite)serializedView.FindProperty("errorIcon").objectReferenceValue;
        Sprite warning = (Sprite)serializedView.FindProperty("warningIcon").objectReferenceValue;
        Assert.NotNull(ready);
        Assert.NotNull(error);
        Assert.NotNull(warning);

        BattleHudRuntimeFeedbackSystem.ApplyCommandMode(view, TacticalCommandMode.Move);
        Assert.AreSame(ready, icon.sprite);

        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(
            view,
            TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
        Assert.AreSame(error, icon.sprite);

        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(
            view,
            TacticalCommandResult.Success("PLACEMENT CANCELLED"));
        Assert.AreSame(warning, icon.sprite);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing private field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static Sprite CreateTestSprite(string name)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        sprite.name = name;
        return sprite;
    }

    private static Image FindRequiredImage(Transform root, string path)
    {
        Transform child = root.Find(path);
        Assert.NotNull(child, $"Missing prefab path {path}.");

        Image image = child.GetComponent<Image>();
        Assert.NotNull(image, $"Missing Image component at {path}.");
        return image;
    }

    private static void AssertObjectReference(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property {propertyName}.");
        Assert.NotNull(property.objectReferenceValue, $"Missing serialized reference {propertyName}.");
    }
}
