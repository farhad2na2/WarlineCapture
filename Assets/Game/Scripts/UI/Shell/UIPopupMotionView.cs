using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIPopupMotionView : MonoBehaviour
{
    [SerializeField] private RectTransform motionRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float showDurationSeconds = 0.24f;
    [SerializeField, Min(0f)] private float hideDurationSeconds = 0.18f;
    [SerializeField] private Vector3 hiddenScale = Vector3.zero;
    [SerializeField] private Vector3 visibleScale = Vector3.one;
    [SerializeField] private UIEase showEase = UIEase.EaseOutBackSubtle;
    [SerializeField] private UIEase hideEase = UIEase.EaseInCubic;

    private Coroutine activeRoutine;
    private bool hiding;

    public bool IsHiding => hiding;

    private void Awake()
    {
        ResolveReferences();
    }

    public static UIPopupMotionView Ensure(GameObject popup)
    {
        if (popup == null)
            return null;

        UIPopupMotionView view = popup.GetComponent<UIPopupMotionView>();
        if (view == null)
            view = popup.AddComponent<UIPopupMotionView>();

        view.ResolveReferences();
        return view;
    }

    public void PlayShow()
    {
        ResolveReferences();
        hiding = false;
        if (!Application.isPlaying)
        {
            ApplyFinalState(visibleScale, 1f);
            return;
        }

        Play(hiddenScale, visibleScale, 0f, 1f, showDurationSeconds, showEase, null);
    }

    public bool PlayHide(Action completed)
    {
        ResolveReferences();
        if (hiding)
            return false;

        hiding = true;
        if (!Application.isPlaying)
        {
            ApplyFinalState(hiddenScale, 0f);
            completed?.Invoke();
            activeRoutine = null;
            return true;
        }

        Play(
            motionRoot != null ? motionRoot.localScale : visibleScale,
            hiddenScale,
            canvasGroup != null ? canvasGroup.alpha : 1f,
            0f,
            hideDurationSeconds,
            hideEase,
            completed);
        return true;
    }

    public bool PlayHideAndDestroy(GameObject target)
    {
        return PlayHide(() =>
        {
            if (target == null)
                return;

            Destroy(target);
        });
    }

    private void ResolveReferences()
    {
        if (motionRoot == null)
            motionRoot = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Play(
        Vector3 scaleFrom,
        Vector3 scaleTo,
        float alphaFrom,
        float alphaTo,
        float durationSeconds,
        UIEase ease,
        Action completed)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(TweenRoutine(scaleFrom, scaleTo, alphaFrom, alphaTo, durationSeconds, ease, completed));
    }

    private IEnumerator TweenRoutine(
        Vector3 scaleFrom,
        Vector3 scaleTo,
        float alphaFrom,
        float alphaTo,
        float durationSeconds,
        UIEase ease,
        Action completed)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = alphaFrom;
        }

        if (motionRoot != null)
            motionRoot.localScale = scaleFrom;

        float duration = Mathf.Max(0f, durationSeconds);
        if (duration <= 0f)
        {
            ApplyFinalState(scaleTo, alphaTo);
            completed?.Invoke();
            activeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float eased = UIMotionHostView.EvaluateEase(ease, elapsed / duration);

            if (motionRoot != null)
                motionRoot.localScale = Vector3.LerpUnclamped(scaleFrom, scaleTo, eased);
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.LerpUnclamped(alphaFrom, alphaTo, eased);

            yield return null;
        }

        ApplyFinalState(scaleTo, alphaTo);
        completed?.Invoke();
        activeRoutine = null;
    }

    private void ApplyFinalState(Vector3 scale, float alpha)
    {
        if (motionRoot != null)
            motionRoot.localScale = scale;

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        bool visible = alpha > 0.99f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
