using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MatchOverlayCommandTabFeedbackSystem
{
    public void ApplyCommandMode(MatchOverlayCommandTabGroupView[] configuredGroups, TacticalCommandMode mode)
    {
        if (mode == TacticalCommandMode.None)
        {
            ClearCommandMode(configuredGroups);
            return;
        }

        bool handledConfiguredGroup = false;
        if (configuredGroups != null)
        {
            for (int i = 0; i < configuredGroups.Length; i++)
            {
                MatchOverlayCommandTabGroupView group = configuredGroups[i];
                if (!IsLiveGroup(group))
                    continue;

                if (TrySelectCommandMode(group, mode))
                    handledConfiguredGroup = true;
            }
        }

        if (handledConfiguredGroup)
            return;

        MatchOverlayCommandTabGroupView[] groups = Resources.FindObjectsOfTypeAll<MatchOverlayCommandTabGroupView>();
        for (int i = 0; i < groups.Length; i++)
        {
            MatchOverlayCommandTabGroupView group = groups[i];
            if (!IsLiveGroup(group))
                continue;

            TrySelectCommandMode(group, mode);
        }
    }

    public void ClearCommandMode(MatchOverlayCommandTabGroupView[] configuredGroups)
    {
        bool handledConfiguredGroup = false;
        if (configuredGroups != null)
        {
            for (int i = 0; i < configuredGroups.Length; i++)
            {
                MatchOverlayCommandTabGroupView group = configuredGroups[i];
                if (!IsLiveGroup(group))
                    continue;

                new MatchOverlayCommandTabVisualSystem(group).Select(null);
                ClearSelectedUiObjectIfCommandTab(group);
                handledConfiguredGroup = true;
            }
        }

        if (handledConfiguredGroup)
            return;

        MatchOverlayCommandTabGroupView[] groups = Resources.FindObjectsOfTypeAll<MatchOverlayCommandTabGroupView>();
        for (int i = 0; i < groups.Length; i++)
        {
            MatchOverlayCommandTabGroupView group = groups[i];
            if (!IsLiveGroup(group))
                continue;

            new MatchOverlayCommandTabVisualSystem(group).Select(null);
            ClearSelectedUiObjectIfCommandTab(group);
        }
    }

    private static bool TrySelectCommandMode(MatchOverlayCommandTabGroupView group, TacticalCommandMode mode)
    {
        MatchOverlayCommandTabView tab = FindCommandModeTab(group, mode);
        if (tab == null)
            return false;

        new MatchOverlayCommandTabVisualSystem(group).Select(tab);
        return true;
    }

    private static MatchOverlayCommandTabView FindCommandModeTab(MatchOverlayCommandTabGroupView group, TacticalCommandMode mode)
    {
        MatchOverlayCommandTabView[] tabs = group != null ? group.Tabs : null;
        if (tabs == null)
            return null;

        string modeName = ToCommandModeName(mode);
        if (string.IsNullOrEmpty(modeName))
            return null;

        for (int i = 0; i < tabs.Length; i++)
        {
            MatchOverlayCommandTabView tab = tabs[i];
            if (TabNameContains(tab, modeName))
                return tab;
        }

        return null;
    }

    private static string ToCommandModeName(TacticalCommandMode mode)
    {
        return mode switch
        {
            TacticalCommandMode.Select => "Select",
            TacticalCommandMode.Move => "Move",
            TacticalCommandMode.Attack => "Attack",
            TacticalCommandMode.Hold => "Hold",
            TacticalCommandMode.Stop => "Stop",
            TacticalCommandMode.Scan => "Scan",
            TacticalCommandMode.Build => "Build",
            _ => string.Empty
        };
    }

    private static bool TabNameContains(MatchOverlayCommandTabView tab, string name)
    {
        if (tab?.Button == null)
            return false;

        Transform transform = tab.Button.transform;
        while (transform != null)
        {
            if (transform.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsLiveGroup(MatchOverlayCommandTabGroupView group)
    {
        return group != null &&
            group.gameObject.scene.IsValid() &&
            group.gameObject.activeInHierarchy;
    }

    private static void ClearSelectedUiObjectIfCommandTab(MatchOverlayCommandTabGroupView group)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            return;

        MatchOverlayCommandTabView[] tabs = group.Tabs;
        if (tabs == null)
            return;

        GameObject selectedObject = eventSystem.currentSelectedGameObject;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i]?.Button == null)
                continue;

            if (tabs[i].Button.gameObject == selectedObject)
            {
                eventSystem.SetSelectedGameObject(null);
                return;
            }
        }
    }
}
