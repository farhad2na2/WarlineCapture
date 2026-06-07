using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudSquadTrayQuickSelectTests
{
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";

    [Test]
    public void MatchHudSquadTrayCardsAreButtons()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab);

        Transform frame = prefab.transform.Find("FooterContent/SquadTray/Frame");
        Assert.NotNull(frame);

        for (int i = 1; i <= 5; i++)
        {
            Transform card = frame.Find($"SquadCard{i}");
            Assert.NotNull(card, $"SquadCard{i} must exist under the squad tray frame.");

            Button button = card.GetComponent<Button>();
            Assert.NotNull(button, $"SquadCard{i} must be clickable.");
            Assert.IsTrue(button.interactable, $"SquadCard{i} button should be interactable.");

            Image frameImage = card.Find("Frame")?.GetComponent<Image>();
            Assert.NotNull(frameImage, $"SquadCard{i}/Frame must keep its frame image.");
            Assert.AreSame(frameImage, button.targetGraphic, $"SquadCard{i} button should target its existing frame image.");
            Assert.IsTrue(frameImage.raycastTarget, $"SquadCard{i}/Frame image must receive pointer raycasts.");
        }
    }

    [Test]
    public void MatchHudSquadTrayViewHasSerializedReferences()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab);

        Transform frame = prefab.transform.Find("FooterContent/SquadTray/Frame");
        Assert.NotNull(frame);

        MatchHudSquadTrayView view = frame.GetComponent<MatchHudSquadTrayView>();
        Assert.NotNull(view, "The squad tray frame must own MatchHudSquadTrayView so runtime code uses serialized references.");

        SerializedObject serialized = new(view);
        Assert.NotNull(serialized.FindProperty("normalFrameSprite").objectReferenceValue);
        Assert.NotNull(serialized.FindProperty("selectedFrameSprite").objectReferenceValue);

        SerializedProperty cards = serialized.FindProperty("cards");
        Assert.NotNull(cards);
        Assert.AreEqual(5, cards.arraySize);
        for (int i = 0; i < cards.arraySize; i++)
        {
            SerializedProperty card = cards.GetArrayElementAtIndex(i);
            Assert.NotNull(card.FindPropertyRelative("Button").objectReferenceValue, $"Card {i + 1} button reference is required.");
            Assert.NotNull(card.FindPropertyRelative("FrameImage").objectReferenceValue, $"Card {i + 1} frame reference is required.");
            Assert.NotNull(card.FindPropertyRelative("PortraitImage").objectReferenceValue, $"Card {i + 1} portrait reference is required.");
        }
    }
}
