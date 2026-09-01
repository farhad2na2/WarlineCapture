using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class V3SelectionFrameView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private bool visible = true;

        public void Configure(CanvasGroup selectionGroup)
        {
            group = selectionGroup;
            ApplyVisibility();
        }

        public void SetVisible(bool value)
        {
            visible = value;
            ApplyVisibility();
        }

        private void OnEnable() => ApplyVisibility();
        private void OnDisable() => ApplyVisibility();

        private void ApplyVisibility()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
