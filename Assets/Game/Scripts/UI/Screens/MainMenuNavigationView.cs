using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MainMenuNavigationView : MonoBehaviour
    {
        private const MainMenuNavigationTabId DefaultSelectedTab = MainMenuNavigationTabId.Leaderboards;

        [SerializeField] private MainMenuNavigationTabView[] tabs;

        private static MainMenuNavigationTabId activeTab = DefaultSelectedTab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveTab()
        {
            activeTab = DefaultSelectedTab;
        }

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
            ApplyVisualState(activeTab);
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

            if (tabs == null)
                return;

            for (int i = 0; i < tabs.Length; i++)
                Wire(tabs[i]);
        }

        private void Wire(MainMenuNavigationTabView tab)
        {
            Button button = tab.Button;
            if (button == null)
                return;

            Image frame = tab.Frame;
            TMP_Text label = tab.Label;
            CacheVisualState(frame, label);

            MainMenuNavigationTabId tabId = tab.TabId;
            UnityEngine.Events.UnityAction action = () => SelectNav(tabId);
            button.onClick.AddListener(action);
            bindings.Add(new TabBinding(tabId, button, frame, label, action));
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

        private void SelectNav(MainMenuNavigationTabId tabId)
        {
            activeTab = tabId;
            ApplyVisualState(tabId);
        }

        private void ApplyVisualState(MainMenuNavigationTabId selectedTab)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                Image frame = bindings[i].Frame;
                if (frame == null)
                    continue;

                bool selected = bindings[i].TabId == selectedTab;
                Sprite sprite = selected ? selectedFrameSprite : inactiveFrameSprite;
                if (sprite != null)
                    frame.sprite = sprite;

                TMP_Text label = bindings[i].Label;
                if (label != null)
                    label.color = selected ? selectedTextColor : inactiveTextColor;
            }
        }

        private readonly struct TabBinding
        {
            public readonly MainMenuNavigationTabId TabId;
            public readonly Button Button;
            public readonly Image Frame;
            public readonly TMP_Text Label;
            public readonly UnityEngine.Events.UnityAction Action;

            public TabBinding(
                MainMenuNavigationTabId tabId,
                Button button,
                Image frame,
                TMP_Text label,
                UnityEngine.Events.UnityAction action)
            {
                TabId = tabId;
                Button = button;
                Frame = frame;
                Label = label;
                Action = action;
            }
        }
    }
}
