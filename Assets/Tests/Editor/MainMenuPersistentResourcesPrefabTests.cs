using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuPersistentResourcesPrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
    private const string CreditsIconPath = "Assets/Game/Art/UI/Resources/resource_credits.png";
    private const string CommandIconPath = "Assets/Game/Art/UI/Resources/resource_command.png";

    [Test]
    public void HeaderShowsOnlyCreditsAndCommandWithCanonicalIcons()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);

        Transform resourceArea = prefab.transform.Find("HeaderContent/HeaderResourceArea");
        Assert.That(resourceArea, Is.Not.Null);
        Assert.That(resourceArea.Find("SuppliesPanel"), Is.Null);

        AssertPanel(resourceArea, "CreditsPanel", "CREDITS", CreditsIconPath, -350f);
        AssertPanel(resourceArea, "CommandPanel", "COMMAND", CommandIconPath, 350f);
    }

    private static void AssertPanel(
        Transform resourceArea,
        string panelName,
        string expectedLabel,
        string expectedIconPath,
        float expectedX)
    {
        Transform panel = resourceArea.Find(panelName);
        Assert.That(panel, Is.Not.Null, panelName);
        Assert.That(panel.GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(expectedX));

        Transform frame = panel.Find("Frame");
        TMP_Text label = frame.Find("Label").GetComponent<TMP_Text>();
        Image icon = frame.Find("Icon").GetComponent<Image>();
        Sprite expectedIcon = AssetDatabase.LoadAssetAtPath<Sprite>(expectedIconPath);

        Assert.That(label.gameObject.activeSelf, Is.True);
        Assert.That(label.text, Is.EqualTo(expectedLabel));
        Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(150f, -20f)));
        Assert.That(frame.Find("Value").GetComponent<RectTransform>().anchoredPosition,
            Is.EqualTo(new Vector2(150f, -58f)));
        Assert.That(icon.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(-235f, 0f)));
        Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(112f, 112f)));
        Assert.That(icon.sprite, Is.EqualTo(expectedIcon));
        Assert.That(icon.preserveAspect, Is.True);
    }
}
