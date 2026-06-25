public sealed class MatchOverlayCommandTabVisualUiSystemHelper
{
    private readonly MatchOverlayCommandTabGroupView _tabGroup;

    public MatchOverlayCommandTabVisualUiSystemHelper(MatchOverlayCommandTabGroupView tabGroup)
    {
        _tabGroup = tabGroup;
    }

    public void ApplyDefaultSelection()
    {
        MatchOverlayCommandTabView[] tabs = _tabGroup != null ? _tabGroup.Tabs : null;
        if (tabs == null)
            return;

        int index = _tabGroup.DefaultSelectedIndex;
        if (index < 0 || index >= tabs.Length)
        {
            Select(null);
            return;
        }

        Select(tabs[index]);
    }

    public bool Toggle(MatchOverlayCommandTabView tab)
    {
        bool selected = !IsSelected(tab);
        Select(selected ? tab : null);
        return selected;
    }

    public bool IsSelected(MatchOverlayCommandTabView tab)
    {
        return tab?.FrameImage != null &&
            tab.SelectedFrameSprite != null &&
            tab.FrameImage.sprite == tab.SelectedFrameSprite;
    }

    public void Select(MatchOverlayCommandTabView selectedTab)
    {
        MatchOverlayCommandTabView[] tabs = _tabGroup != null ? _tabGroup.Tabs : null;
        if (tabs == null)
            return;

        foreach (MatchOverlayCommandTabView tab in tabs)
        {
            if (tab?.FrameImage == null)
                continue;

            bool selected = ReferenceEquals(tab, selectedTab);
            UnityEngine.Sprite sprite = selected ? tab.SelectedFrameSprite : tab.NormalFrameSprite;
            if (sprite != null)
                tab.FrameImage.sprite = sprite;
        }
    }
}
