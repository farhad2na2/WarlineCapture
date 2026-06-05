using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuNavigationView : MonoBehaviour
{
    private const string DefaultSelectedNavName = "Nav_Leaderboards";

    private static string activeNavName = DefaultSelectedNavName;

    private readonly List<TabBinding> bindings = new();
    private Sprite selectedFrameSprite;
    private Sprite inactiveFrameSprite;
    private Color selectedTextColor = Color.white;
    private Color inactiveTextColor = Color.white;
    private bool hasSelectedTextColor;
    private bool hasInactiveTextColor;

    private void Awake()
    {
        WireAll();
    }

    private void OnEnable()
    {
        WireAll();
        ApplyVisualState(activeNavName);
    }

    private void OnDisable()
    {
        for (int i = 0; i < bindings.Count; i++)
            bindings[i].Button.onClick.RemoveListener(bindings[i].Action);

        bindings.Clear();
    }

    private void WireAll()
    {
        if (bindings.Count > 0)
            return;

        Wire("Nav_Leaderboards");
        Wire("Nav_Armory");
        Wire("Nav_Store");
        Wire("Nav_Contests");
        Wire("Nav_Tutorials");
    }

    private void Wire(string navName)
    {
        Transform nav = FindDeep(transform, navName);
        if (nav == null)
            return;

        Button button = nav.GetComponent<Button>();
        if (button == null)
            return;

        Image frame = nav.Find("Frame")?.GetComponent<Image>();
        TMP_Text label = nav.Find("Text")?.GetComponent<TMP_Text>();
        CacheVisualState(frame, label);

        UnityEngine.Events.UnityAction action = () => SelectNav(navName);
        button.onClick.AddListener(action);
        bindings.Add(new TabBinding(navName, button, frame, label, action));
    }

    private void CacheVisualState(Image frame, TMP_Text label)
    {
        if (frame == null || frame.sprite == null)
            return;

        string spriteName = frame.sprite.name.ToLowerInvariant();
        if (spriteName.Contains("selected"))
        {
            selectedFrameSprite ??= frame.sprite;
            if (label != null && !hasSelectedTextColor)
            {
                selectedTextColor = label.color;
                hasSelectedTextColor = true;
            }
        }
        else
        {
            inactiveFrameSprite ??= frame.sprite;
            if (label != null && !hasInactiveTextColor)
            {
                inactiveTextColor = label.color;
                hasInactiveTextColor = true;
            }
        }
    }

    private void SelectNav(string navName)
    {
        activeNavName = navName;
        ApplyVisualState(navName);
    }

    private void ApplyVisualState(string selectedNavName)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            Image frame = bindings[i].Frame;
            if (frame == null)
                continue;

            bool selected = bindings[i].NavName == selectedNavName;
            Sprite sprite = selected ? selectedFrameSprite : inactiveFrameSprite;
            if (sprite != null)
                frame.sprite = sprite;

            TMP_Text label = bindings[i].Label;
            if (label != null)
                label.color = selected ? selectedTextColor : inactiveTextColor;
        }
    }

    private static Transform FindDeep(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform matched = FindDeep(root.GetChild(i), targetName);
            if (matched != null)
                return matched;
        }

        return null;
    }

    private readonly struct TabBinding
    {
        public readonly string NavName;
        public readonly Button Button;
        public readonly Image Frame;
        public readonly TMP_Text Label;
        public readonly UnityEngine.Events.UnityAction Action;

        public TabBinding(
            string navName,
            Button button,
            Image frame,
            TMP_Text label,
            UnityEngine.Events.UnityAction action)
        {
            NavName = navName;
            Button = button;
            Frame = frame;
            Label = label;
            Action = action;
        }
    }
}
