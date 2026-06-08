using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIMotionFeedbackView : MonoBehaviour, IPointerClickHandler
{
    private const string ReducedMotionPlayerPrefKey = "Game.ReducedMotion";

    [SerializeField] private RectTransform motionTarget;
    [SerializeField] private Graphic flashGraphic;
    [SerializeField] private bool playAcceptedPulseOnClick = true;
    [SerializeField] private bool playLockedFeedbackWhenNotInteractable = true;
    [SerializeField] private bool playOpenOnEnable;
    [SerializeField] private MotionKind openMotion = MotionKind.Modal;
    [SerializeField] private Color acceptedFlashColor = new(0.05f, 0.85f, 1f, 0.42f);
    [SerializeField] private Color invalidFlashColor = new(1f, 0.18f, 0.08f, 0.55f);

    private Button _button;
    private CanvasGroup _canvasGroup;
    private Coroutine _motionRoutine;
    private Vector3 _baseScale = Vector3.one;
    private Vector2 _baseAnchoredPosition;
    private Color _baseGraphicColor;
    private bool _hasBaseGraphicColor;

    public enum MotionKind
    {
        None,
        Modal,
        DrawerLeft,
        DrawerRight,
        DrawerBottom
    }

    private RectTransform Target
    {
        get
        {
            if (motionTarget == null)
                motionTarget = transform as RectTransform;

            return motionTarget;
        }
    }

    private void Awake()
    {
        CaptureBaseState();
    }

    private void OnEnable()
    {
        CaptureBaseState();

        if (playOpenOnEnable)
            PlayOpen();
    }

    public void ConfigureButtonDefaults(bool selected)
    {
        playAcceptedPulseOnClick = true;
        playLockedFeedbackWhenNotInteractable = true;

        if (selected)
            acceptedFlashColor = new Color(0.08f, 0.88f, 1f, 0.52f);
    }

    public void ConfigureOpenMotionDefaults(MotionKind motionKind)
    {
        playAcceptedPulseOnClick = false;
        playLockedFeedbackWhenNotInteractable = false;
        playOpenOnEnable = motionKind != MotionKind.None;
        openMotion = motionKind;
    }

    public void PlayOpen()
    {
        if (openMotion == MotionKind.None)
            return;

        if (openMotion == MotionKind.Modal)
            PlayModalOpen();
        else
            PlayDrawerOpen(openMotion);
    }

    public void PlayModalOpen()
    {
        if (UseReducedMotion())
        {
            FadeCanvasGroup(1f);
            return;
        }

        StartMotion(ScaleAndFade(new Vector3(0.94f, 0.94f, 1f), _baseScale, 0f, 1f, 0.16f));
    }

    public void PlayModalClose()
    {
        if (UseReducedMotion())
        {
            FadeCanvasGroup(0f);
            return;
        }

        StartMotion(ScaleAndFade(_baseScale, new Vector3(0.96f, 0.96f, 1f), 1f, 0f, 0.10f));
    }

    public void PlayDrawerOpen(MotionKind drawerDirection)
    {
        if (UseReducedMotion())
        {
            FadeCanvasGroup(1f);
            return;
        }

        Vector2 offset = drawerDirection switch
        {
            MotionKind.DrawerLeft => new Vector2(-80f, 0f),
            MotionKind.DrawerRight => new Vector2(80f, 0f),
            MotionKind.DrawerBottom => new Vector2(0f, 80f),
            _ => Vector2.zero
        };

        StartMotion(SlideAndFade(_baseAnchoredPosition + offset, _baseAnchoredPosition, 0f, 1f, 0.18f));
    }

    public void PlayDrawerClose(MotionKind drawerDirection)
    {
        if (UseReducedMotion())
        {
            FadeCanvasGroup(0f);
            return;
        }

        Vector2 offset = drawerDirection switch
        {
            MotionKind.DrawerLeft => new Vector2(-60f, 0f),
            MotionKind.DrawerRight => new Vector2(60f, 0f),
            MotionKind.DrawerBottom => new Vector2(0f, 60f),
            _ => Vector2.zero
        };

        StartMotion(SlideAndFade(_baseAnchoredPosition, _baseAnchoredPosition + offset, 1f, 0f, 0.12f));
    }

    public void PlayAcceptedPulse()
    {
        if (UseReducedMotion())
        {
            PlayFlash(acceptedFlashColor, 0.10f);
            return;
        }

        StartMotion(PulseScale(0.96f, 1.035f, 0.16f, acceptedFlashColor));
    }

    public void PlaySelectionPulse()
    {
        if (UseReducedMotion())
        {
            PlayFlash(acceptedFlashColor, 0.12f);
            return;
        }

        StartMotion(PulseScale(1f, 1.045f, 0.22f, acceptedFlashColor));
    }

    public void PlayLockedWiggle()
    {
        if (UseReducedMotion())
        {
            PlayFlash(invalidFlashColor, 0.16f);
            return;
        }

        StartMotion(Wiggle(7f, 0.22f, invalidFlashColor));
    }

    public void PlayInvalidFlash()
    {
        if (UseReducedMotion())
        {
            PlayFlash(invalidFlashColor, 0.14f);
            return;
        }

        StartMotion(PulseScale(1f, 1.015f, 0.12f, invalidFlashColor));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
            return;

        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null && !_button.interactable)
        {
            if (playLockedFeedbackWhenNotInteractable)
                PlayLockedWiggle();

            return;
        }

        if (playAcceptedPulseOnClick)
            PlayAcceptedPulse();
    }

    private void CaptureBaseState()
    {
        RectTransform target = Target;
        if (target != null)
        {
            _baseScale = target.localScale;
            _baseAnchoredPosition = target.anchoredPosition;
        }

        if (_button == null)
            _button = GetComponent<Button>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (flashGraphic == null)
            flashGraphic = GetComponent<Graphic>();

        if (flashGraphic != null && !_hasBaseGraphicColor)
        {
            _baseGraphicColor = flashGraphic.color;
            _hasBaseGraphicColor = true;
        }
    }

    private void StartMotion(IEnumerator routine)
    {
        if (_motionRoutine != null)
            StopCoroutine(_motionRoutine);

        _motionRoutine = StartCoroutine(routine);
    }

    private IEnumerator PulseScale(float downScale, float upScale, float duration, Color flashColor)
    {
        RectTransform target = Target;
        if (target == null)
            yield break;

        float half = duration * 0.5f;
        yield return AnimateScale(_baseScale * downScale, _baseScale * upScale, half, flashColor);
        yield return AnimateScale(_baseScale * upScale, _baseScale, half, Color.clear);
        RestoreGraphicColor();
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration, Color flashColor)
    {
        RectTransform target = Target;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth01(elapsed / duration);
            target.localScale = Vector3.LerpUnclamped(from, to, t);
            LerpFlash(flashColor, 1f - t);
            yield return null;
        }

        target.localScale = to;
    }

    private IEnumerator Wiggle(float amplitude, float duration, Color flashColor)
    {
        RectTransform target = Target;
        if (target == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float decay = 1f - t;
            float offset = Mathf.Sin(t * Mathf.PI * 6f) * amplitude * decay;
            target.anchoredPosition = _baseAnchoredPosition + new Vector2(offset, 0f);
            LerpFlash(flashColor, decay);
            yield return null;
        }

        target.anchoredPosition = _baseAnchoredPosition;
        RestoreGraphicColor();
    }

    private IEnumerator ScaleAndFade(Vector3 fromScale, Vector3 toScale, float fromAlpha, float toAlpha, float duration)
    {
        RectTransform target = Target;
        EnsureCanvasGroup();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth01(elapsed / duration);
            if (target != null)
                target.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
            FadeCanvasGroup(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        if (target != null)
            target.localScale = toScale;
        FadeCanvasGroup(toAlpha);
    }

    private IEnumerator SlideAndFade(Vector2 fromPosition, Vector2 toPosition, float fromAlpha, float toAlpha, float duration)
    {
        RectTransform target = Target;
        EnsureCanvasGroup();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth01(elapsed / duration);
            if (target != null)
                target.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, t);
            FadeCanvasGroup(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        if (target != null)
            target.anchoredPosition = toPosition;
        FadeCanvasGroup(toAlpha);
    }

    private void PlayFlash(Color color, float duration)
    {
        StartMotion(FlashOnly(color, duration));
    }

    private IEnumerator FlashOnly(Color color, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            LerpFlash(color, 1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        RestoreGraphicColor();
    }

    private void LerpFlash(Color flashColor, float strength)
    {
        if (flashGraphic == null || !_hasBaseGraphicColor || flashColor == Color.clear)
            return;

        flashGraphic.color = Color.Lerp(_baseGraphicColor, flashColor, Mathf.Clamp01(strength));
    }

    private void RestoreGraphicColor()
    {
        if (flashGraphic != null && _hasBaseGraphicColor)
            flashGraphic.color = _baseGraphicColor;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void FadeCanvasGroup(float alpha)
    {
        EnsureCanvasGroup();
        _canvasGroup.alpha = alpha;
    }

    private static bool UseReducedMotion()
    {
        return PlayerPrefs.GetInt(ReducedMotionPlayerPrefKey, 0) != 0;
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
