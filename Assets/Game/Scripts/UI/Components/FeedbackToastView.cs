using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class FeedbackToastView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform motionRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Color positiveColor = new(0.08f, 0.86f, 1f, 1f);
        [SerializeField] private Color warningColor = new(1f, 0.66f, 0.08f, 1f);
        [SerializeField] private Color errorColor = new(1f, 0.18f, 0.08f, 1f);

        private Coroutine _showRoutine;
        private Vector2 _basePosition;

        public enum ToastType
        {
            Positive,
            Warning,
            Error
        }

        private void Awake()
        {
            ResolveReferences();
            if (motionRoot != null)
                _basePosition = motionRoot.anchoredPosition;
            HideImmediate();
        }

        public void Show(string message, ToastType type, float holdSeconds = 1.6f, Sprite icon = null)
        {
            ResolveReferences();

            if (messageText != null)
                messageText.text = message;

            Color accent = type switch
            {
                ToastType.Positive => positiveColor,
                ToastType.Warning => warningColor,
                _ => errorColor
            };

            if (messageText != null)
                messageText.color = accent;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.color = accent;
                iconImage.enabled = icon != null;
            }

            if (_showRoutine != null)
                StopCoroutine(_showRoutine);

            _showRoutine = StartCoroutine(ShowRoutine(Mathf.Max(0.4f, holdSeconds)));
        }

        public void HideImmediate()
        {
            ResolveReferences();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (motionRoot != null)
                motionRoot.anchoredPosition = _basePosition;
        }

        private IEnumerator ShowRoutine(float holdSeconds)
        {
            if (canvasGroup == null || motionRoot == null)
                yield break;

            Vector2 hidden = _basePosition + new Vector2(0f, 18f);
            yield return Animate(hidden, _basePosition, 0f, 1f, 0.14f);
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Animate(_basePosition, hidden, 1f, 0f, 0.16f);
        }

        private IEnumerator Animate(Vector2 from, Vector2 to, float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth01(elapsed / duration);
                motionRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }

            motionRoot.anchoredPosition = to;
            canvasGroup.alpha = toAlpha;
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (motionRoot == null)
                motionRoot = transform as RectTransform;

            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>(true);

            if (iconImage == null)
            {
                Image[] images = GetComponentsInChildren<Image>(true);
                if (images.Length > 1)
                    iconImage = images[1];
            }
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
