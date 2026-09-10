using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BuildDrawerHudOcclusionView : MonoBehaviour
    {
        private readonly List<GameObject> sections = new();
        private readonly List<bool> previousStates = new();

        public void Configure(UIShellContentView shell)
        {
            RestoreOcclusion();
            foreach(var id in new[] { UIShellRegionId.HeaderRegion, UIShellRegionId.LeftRegion,
                         UIShellRegionId.RightRegion, UIShellRegionId.FooterRegion })
            {
                if(shell == null || !shell.TryGetRegionContentRoot(id, out var root) || root == null) continue;
                sections.Add(root.gameObject);
                previousStates.Add(root.gameObject.activeSelf);
            }
            RefreshOcclusion();
        }

        public void RefreshOcclusion()
        {
            foreach(var section in sections) if(section != null) section.SetActive(false);
        }

        private void OnDisable() => RestoreOcclusion();
        private void OnDestroy() => RestoreOcclusion();

        public void RestoreOcclusion()
        {
            for(int i = 0; i < sections.Count; i++)
                if(sections[i] != null) sections[i].SetActive(previousStates[i]);
            sections.Clear(); previousStates.Clear();
        }
    }
}
