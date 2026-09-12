using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class BuildDrawerHudOcclusionView : MonoBehaviour
    {
        private readonly List<GameObject> sections = new();
        private readonly List<bool> previousStates = new();

        public void Configure(UIShellContentView shell)
        {
            Configure(shell, UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) &&
                RequiresTutorialAccess(panel));
        }

        internal void Configure(UIShellContentView shell, bool preserveTutorial)
        {
            RestoreOcclusion();
            foreach(var id in new[] { UIShellRegionId.HeaderRegion, UIShellRegionId.LeftRegion,
                         UIShellRegionId.RightRegion, UIShellRegionId.FooterRegion })
            {
                if(shell == null || !shell.TryGetRegionContentRoot(id, out var root) || root == null) continue;
                var aria = id == UIShellRegionId.HeaderRegion && preserveTutorial
                    ? root.GetComponentInChildren<AriaTutorialBriefingView>(true) : null;
                if (aria != null)
                {
                    PreserveTutorial(aria);
                    HideOtherHeaderChildren(root, aria.transform);
                }
                else RememberSection(root.gameObject);
            }
            RefreshOcclusion();
        }

        private void RememberSection(GameObject section)
        {
            sections.Add(section);
            previousStates.Add(section.activeSelf);
        }

        public void RefreshOcclusion()
        {
            foreach(var section in sections) if(section != null) section.SetActive(false);
        }

        private void OnDisable() => RestoreOcclusion();
        private void OnDestroy() => RestoreOcclusion();

        public void RestoreOcclusion()
        {
            RestoreTutorial();
            for(int i = 0; i < sections.Count; i++)
                if(sections[i] != null) sections[i].SetActive(previousStates[i]);
            sections.Clear(); previousStates.Clear();
        }
    }
}
