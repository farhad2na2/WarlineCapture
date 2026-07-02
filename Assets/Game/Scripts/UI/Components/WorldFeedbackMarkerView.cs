using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class WorldFeedbackMarkerView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform motionRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Color acceptedColor = new(0.05f, 0.84f, 1f, 1f);
        [SerializeField] private Color invalidColor = new(1f, 0.18f, 0.08f, 1f);

        private Coroutine _routine;

        public enum MarkerType
        {
            Move,
            Attack,
            Invalid,
            Objective
        }

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
        }

        public void Show(MarkerType type, Vector2 anchoredPosition, string label = "")
        {
            ResolveReferences();

            if (motionRoot == null)
                return;

            motionRoot.anchoredPosition = anchoredPosition;
            Color color = type == MarkerType.Invalid ? invalidColor : acceptedColor;

            if (iconImage != null)
                iconImage.color = color;

            if (labelText != null)
            {
                labelText.text = label;
                labelText.color = color;
                labelText.enabled = !string.IsNullOrEmpty(label);
            }

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(ShowRoutine(type == MarkerType.Invalid ? 0.55f : 0.75f));
        }

        public void HideImmediate()
        {
            ResolveReferences();
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private IEnumerator ShowRoutine(float duration)
        {
            if (canvasGroup == null || motionRoot == null)
                yield break;

            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = t < 0.70f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.70f) / 0.30f);
                motionRoot.localScale = baseScale * Mathf.Lerp(0.86f, 1.16f, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            motionRoot.localScale = baseScale;
            canvasGroup.alpha = 0f;
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (motionRoot == null)
                motionRoot = transform as RectTransform;

            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>(true);

            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
