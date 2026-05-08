using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ResourceFlyoutView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform motionRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private float flightSeconds = 0.52f;

    private Coroutine _flightRoutine;

    private void Awake()
    {
        ResolveReferences();
        HideImmediate();
    }

    public void Play(RectTransform from, RectTransform to, Sprite icon, string value, Color accent)
    {
        ResolveReferences();

        if (from == null || to == null || motionRoot == null)
            return;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = accent;
            iconImage.enabled = icon != null;
        }

        if (valueText != null)
        {
            valueText.text = value;
            valueText.color = accent;
        }

        Vector2 start = WorldToParentPosition(from);
        Vector2 end = WorldToParentPosition(to);

        if (_flightRoutine != null)
            StopCoroutine(_flightRoutine);

        _flightRoutine = StartCoroutine(FlightRoutine(start, end));
    }

    public void HideImmediate()
    {
        ResolveReferences();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private IEnumerator FlightRoutine(Vector2 start, Vector2 end)
    {
        if (motionRoot == null || canvasGroup == null)
            yield break;

        float elapsed = 0f;
        Vector2 control = (start + end) * 0.5f + new Vector2(0f, -72f);
        while (elapsed < flightSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flightSeconds);
            float eased = t * t * (3f - 2f * t);
            Vector2 a = Vector2.LerpUnclamped(start, control, eased);
            Vector2 b = Vector2.LerpUnclamped(control, end, eased);
            motionRoot.anchoredPosition = Vector2.LerpUnclamped(a, b, eased);
            motionRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1.06f, Mathf.Sin(t * Mathf.PI));
            canvasGroup.alpha = t < 0.82f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.82f) / 0.18f);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        motionRoot.localScale = Vector3.one;
    }

    private Vector2 WorldToParentPosition(RectTransform source)
    {
        RectTransform parent = motionRoot.parent as RectTransform;
        if (parent == null)
            return source.position;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, source.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private void ResolveReferences()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (motionRoot == null)
            motionRoot = transform as RectTransform;

        if (valueText == null)
            valueText = GetComponentInChildren<TMP_Text>(true);

        if (iconImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            if (images.Length > 0)
                iconImage = images[0];
        }
    }
}
