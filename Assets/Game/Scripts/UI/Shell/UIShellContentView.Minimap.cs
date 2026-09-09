using Game.UI.Runtime;

namespace Game.UI.Runtime
{
    public sealed partial class UIShellContentView
    {
        private MatchHudMinimapView ResolveInstalledMinimap(MatchHudFooterContentView footer)
        {
            // Sections are instantiated separately. A reference across prefab sections
            // still points at the prefab asset, never at the installed header instance.
            var headerMap = _matchHudHeaderContent != null
                ? _matchHudHeaderContent.GetComponentInChildren<MatchHudMinimapView>(true) : null;
            if (headerMap != null) return headerMap;
            var footerMap = footer != null ? footer.Minimap : null;
            return footerMap != null && footerMap.gameObject.scene.IsValid() ? footerMap : null;
        }
    }
}
