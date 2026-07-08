using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Runtime;

public sealed class MatchHudSquadTrayQuickSelectTests
{
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private static readonly string[] DedicatedSquadPortraitPaths =
    {
        "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card1_RifleSquad.png",
        "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card2_CombatVehicles.png",
        "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card3_AttackHelicopter.png",
        "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card4_FighterJet.png",
        "Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card5_Transport.png",
    };

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
    public void MatchHudSquadTrayCardsUseDedicatedPortraitSprites()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab);

        Transform frame = prefab.transform.Find("FooterContent/SquadTray/Frame");
        Assert.NotNull(frame);

        MatchHudSquadTrayView view = frame.GetComponent<MatchHudSquadTrayView>();
        Assert.NotNull(view, "The squad tray frame must own MatchHudSquadTrayView so runtime code uses serialized references.");

        SerializedObject serialized = new(view);
        SerializedProperty cards = serialized.FindProperty("cards");
        Assert.NotNull(cards);
        Assert.AreEqual(DedicatedSquadPortraitPaths.Length, cards.arraySize);

        for (int i = 0; i < DedicatedSquadPortraitPaths.Length; i++)
        {
            Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DedicatedSquadPortraitPaths[i]);
            Assert.NotNull(expectedSprite, $"Dedicated squad portrait sprite is missing at {DedicatedSquadPortraitPaths[i]}.");

            SerializedProperty card = cards.GetArrayElementAtIndex(i);
            Image portraitImage = card.FindPropertyRelative("PortraitImage").objectReferenceValue as Image;
            Assert.NotNull(portraitImage, $"Card {i + 1} portrait reference is required.");
            Assert.AreSame(expectedSprite, portraitImage.sprite, $"Card {i + 1} must use its dedicated Match HUD squad tray sprite.");
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

    [Test]
    public void MatchHudSquadTrayCardClick_EmitsPrimaryClickAudio()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Assert.NotNull(prefab);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            Transform frame = instance.transform.Find("FooterContent/SquadTray/Frame");
            Assert.NotNull(frame);

            MatchHudSquadTrayView view = frame.GetComponent<MatchHudSquadTrayView>();
            Assert.NotNull(view, "The squad tray frame must own MatchHudSquadTrayView.");

            SerializedObject serialized = new(view);
            SerializedProperty cards = serialized.FindProperty("cards");
            Assert.NotNull(cards);
            Button button = cards.GetArrayElementAtIndex(0).FindPropertyRelative("Button").objectReferenceValue as Button;
            Assert.NotNull(button, "Squad card 1 button reference is required.");

            int eventCount = 0;
            UIAudioEventKind lastKind = UIAudioEventKind.None;
            void Capture(UIAudioEventRequest request)
            {
                eventCount++;
                lastKind = request.Kind;
            }

            view.Bind(_ => { });
            UIAudioEventGateway.AudioEventRequested += Capture;
            try
            {
                button.onClick.Invoke();
            }
            finally
            {
                UIAudioEventGateway.AudioEventRequested -= Capture;
            }

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(UIAudioEventKind.ButtonPrimaryClick, lastKind);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
