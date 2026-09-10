namespace Game.UI.Runtime
{
    public sealed partial class UIShellContentView
    {
        // Motion moves the owning regions after a section's Start callback. Reconcile once
        // when the shell finishes that transition rather than polling every view each frame.
        public void RefreshMountedLayouts()
        {
            foreach(UIShellRegionId id in System.Enum.GetValues(typeof(UIShellRegionId)))
            {
                if(!TryGetRegionContentRoot(id, out var root) || root == null) continue;
                foreach(var layout in root.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                    if(layout.isActiveAndEnabled) layout.RefreshLayout();
            }
        }
    }
}
