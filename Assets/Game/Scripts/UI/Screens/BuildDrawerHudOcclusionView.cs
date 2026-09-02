using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// The V3 Build Drawer carries its own resource header and command footer.
    /// While it is open, hide the duplicate Match HUD chrome so the popup gutters
    /// show only the dimmed battlefield context from the target lock.
    /// </summary>
    [DefaultExecutionOrder(1800)]
    [DisallowMultipleComponent]
    public sealed class BuildDrawerHudOcclusionView : MonoBehaviour
    {
        private static readonly string[] SectionNames =
        {
            "HeaderContent",
            "LeftContent",
            "RightContent",
            "FooterContent"
        };

        private readonly List<GameObject> _sections = new();
        private readonly List<bool> _previousStates = new();
        private bool _captured;

        public void RefreshOcclusion()
        {
            if (_captured)
                return;

            // The shell can mount popup and content regions under separate nested
            // canvases. Search the loaded scene rather than assuming both regions
            // share this popup's nearest root Canvas.
            RectTransform[] candidates = Object.FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int nameIndex = 0; nameIndex < SectionNames.Length; nameIndex++)
            {
                string sectionName = SectionNames[nameIndex];
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    RectTransform candidate = candidates[candidateIndex];
                    if (candidate == null || candidate.name != sectionName ||
                        candidate.IsChildOf(transform) || !candidate.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    GameObject section = candidate.gameObject;
                    _sections.Add(section);
                    _previousStates.Add(section.activeSelf);
                    section.SetActive(false);
                    break;
                }
            }

            _captured = _sections.Count > 0;
        }

        private void OnEnable()
        {
            RefreshOcclusion();
        }

        private void Start()
        {
            RefreshOcclusion();
        }

        private void LateUpdate()
        {
            if (!_captured)
            {
                RefreshOcclusion();
                return;
            }

            // UIShell route projection may re-assert Match HUD visibility after
            // the popup mounts. The drawer remains the top-level modal, so keep
            // its duplicate underlay chrome suppressed until this view closes.
            for (int i = 0; i < _sections.Count; i++)
            {
                if (_sections[i] != null && _sections[i].activeSelf)
                    _sections[i].SetActive(false);
            }
        }

        private void OnDisable()
        {
            RestoreOcclusion();
        }

        private void OnDestroy()
        {
            RestoreOcclusion();
        }

        public void RestoreOcclusion()
        {
            for (int i = 0; i < _sections.Count; i++)
            {
                if (_sections[i] != null)
                    _sections[i].SetActive(_previousStates[i]);
            }

            _sections.Clear();
            _previousStates.Clear();
            _captured = false;
        }
    }
}
