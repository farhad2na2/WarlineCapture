using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchIntroCurtainView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;

        public GameObject Root => root;
        public CanvasGroup CanvasGroup => canvasGroup;

        private void Awake()
        {
            EnsureReferences();
        }

        public void Configure(GameObject configuredRoot, CanvasGroup configuredCanvasGroup)
        {
            root = configuredRoot;
            canvasGroup = configuredCanvasGroup;
            EnsureReferences();
        }

        public void SetVisible(bool visible)
        {
            EnsureReferences();
            if (root != null && root.activeSelf != visible)
                root.SetActive(visible);
        }

        public void SetAlpha(float alpha)
        {
            EnsureReferences();
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void ShowOpaque()
        {
            SetVisible(true);
            SetAlpha(1f);
        }

        public void HideIfTransparent()
        {
            EnsureReferences();
            if (canvasGroup == null || canvasGroup.alpha > 0.001f)
                return;

            SetVisible(false);
        }

        private void EnsureReferences()
        {
            if (root == null)
                root = gameObject;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
