using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum UIEase
    {
        Linear,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseOutBackSubtle
    }

    public readonly struct UIMotionStep
    {
        private UIMotionStep(IReadOnlyList<Func<IEnumerator>> factories, bool runInParallel)
        {
            Factories = factories;
            RunInParallel = runInParallel;
        }

        public IReadOnlyList<Func<IEnumerator>> Factories { get; }
        public bool RunInParallel { get; }

        public static UIMotionStep Single(Func<IEnumerator> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return new UIMotionStep(new[] { factory }, false);
        }

        public static UIMotionStep Parallel(params Func<IEnumerator>[] factories)
        {
            if (factories == null || factories.Length == 0)
                throw new ArgumentException("A parallel motion step needs at least one tween.", nameof(factories));

            return new UIMotionStep(factories, true);
        }
    }

    [DisallowMultipleComponent]
    public sealed class UIMotionHostView : MonoBehaviour
    {
        [SerializeField] private float defaultDurationSeconds = 0.26f;
        [SerializeField] private UIEase defaultEnterEase = UIEase.EaseOutCubic;
        [SerializeField] private UIEase defaultExitEase = UIEase.EaseInCubic;
        [SerializeField] private UIEase defaultSwapEase = UIEase.EaseInOutCubic;

        private readonly List<Coroutine> runningCoroutines = new();
        private int activeTransitionId;

        public float DefaultDurationSeconds => defaultDurationSeconds;
        public UIEase DefaultEnterEase => defaultEnterEase;
        public UIEase DefaultExitEase => defaultExitEase;
        public UIEase DefaultSwapEase => defaultSwapEase;
        public int ActiveTransitionId => activeTransitionId;

        public int BeginTransition(bool cancelExisting = true)
        {
            activeTransitionId++;
            if (cancelExisting)
                StopRunningCoroutines();
            return activeTransitionId;
        }

        public void CancelActiveTransition()
        {
            activeTransitionId++;
            StopRunningCoroutines();
        }

        public bool IsCurrentTransition(int transitionId) => transitionId == activeTransitionId;

        public Coroutine PlayAnchoredPosition(
            RectTransform target,
            Vector2 to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return Track(TweenAnchoredPosition(target, target.anchoredPosition, to, durationSeconds, ease, transitionId, completed));
        }

        public Coroutine PlayAnchoredPosition(
            RectTransform target,
            Vector2 from,
            Vector2 to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return Track(TweenAnchoredPosition(target, from, to, durationSeconds, ease, transitionId, completed));
        }

        public Coroutine PlayScale(
            RectTransform target,
            Vector3 to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return Track(TweenScale(target, target.localScale, to, durationSeconds, ease, transitionId, completed));
        }

        public Coroutine PlayScale(
            RectTransform target,
            Vector3 from,
            Vector3 to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return Track(TweenScale(target, from, to, durationSeconds, ease, transitionId, completed));
        }

        public Coroutine PlayAlpha(
            CanvasGroup target,
            float to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return Track(TweenAlpha(target, target.alpha, to, durationSeconds, ease, transitionId, completed));
        }

        public Coroutine PlayAlpha(
            CanvasGroup target,
            float from,
            float to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return Track(TweenAlpha(target, from, to, durationSeconds, ease, transitionId, completed));
        }

        public Coroutine PlaySequence(int transitionId, Action completed, params UIMotionStep[] steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));

            return Track(SequenceRoutine(transitionId, completed, steps));
        }

        public Func<IEnumerator> AnchoredPositionStep(
            RectTransform target,
            Vector2 to,
            float durationSeconds,
            UIEase ease,
            int transitionId)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return () => TweenAnchoredPosition(target, target.anchoredPosition, to, durationSeconds, ease, transitionId, null);
        }

        public Func<IEnumerator> ScaleStep(
            RectTransform target,
            Vector3 to,
            float durationSeconds,
            UIEase ease,
            int transitionId)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return () => TweenScale(target, target.localScale, to, durationSeconds, ease, transitionId, null);
        }

        public Func<IEnumerator> AlphaStep(
            CanvasGroup target,
            float to,
            float durationSeconds,
            UIEase ease,
            int transitionId)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return () => TweenAlpha(target, target.alpha, to, durationSeconds, ease, transitionId, null);
        }

        public static float EvaluateEase(UIEase ease, float progress01)
        {
            float t = Mathf.Clamp01(progress01);
            return ease switch
            {
                UIEase.EaseInCubic => t * t * t,
                UIEase.EaseOutCubic => 1f - Mathf.Pow(1f - t, 3f),
                UIEase.EaseInOutCubic => t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f,
                UIEase.EaseOutBackSubtle => EaseOutBackSubtle(t),
                _ => t
            };
        }

        private Coroutine Track(IEnumerator routine)
        {
            Coroutine coroutine = StartCoroutine(TrackedRoutine(routine));
            runningCoroutines.Add(coroutine);
            return coroutine;
        }

        private IEnumerator TrackedRoutine(IEnumerator routine)
        {
            yield return routine;
        }

        private IEnumerator SequenceRoutine(int transitionId, Action completed, IReadOnlyList<UIMotionStep> steps)
        {
            foreach (UIMotionStep step in steps)
            {
                if (!IsCurrentTransition(transitionId))
                    yield break;

                if (step.RunInParallel)
                {
                    int remaining = step.Factories.Count;
                    foreach (Func<IEnumerator> factory in step.Factories)
                        StartCoroutine(ParallelChildRoutine(factory(), () => remaining--));

                    while (remaining > 0)
                    {
                        if (!IsCurrentTransition(transitionId))
                            yield break;

                        yield return null;
                    }
                }
                else
                {
                    foreach (Func<IEnumerator> factory in step.Factories)
                        yield return factory();
                }
            }

            if (IsCurrentTransition(transitionId))
                completed?.Invoke();
        }

        private IEnumerator ParallelChildRoutine(IEnumerator routine, Action completed)
        {
            yield return routine;
            completed?.Invoke();
        }

        private IEnumerator TweenAnchoredPosition(
            RectTransform target,
            Vector2 from,
            Vector2 to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed)
        {
            if (!IsCurrentTransition(transitionId))
                yield break;

            target.anchoredPosition = from;
            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f)
            {
                target.anchoredPosition = to;
                completed?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsCurrentTransition(transitionId))
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float eased = EvaluateEase(ease, elapsed / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }

            if (!IsCurrentTransition(transitionId))
                yield break;

            target.anchoredPosition = to;
            completed?.Invoke();
        }

        private IEnumerator TweenScale(
            RectTransform target,
            Vector3 from,
            Vector3 to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed)
        {
            if (!IsCurrentTransition(transitionId))
                yield break;

            target.localScale = from;
            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f)
            {
                target.localScale = to;
                completed?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsCurrentTransition(transitionId))
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float eased = EvaluateEase(ease, elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            if (!IsCurrentTransition(transitionId))
                yield break;

            target.localScale = to;
            completed?.Invoke();
        }

        private IEnumerator TweenAlpha(
            CanvasGroup target,
            float from,
            float to,
            float durationSeconds,
            UIEase ease,
            int transitionId,
            Action completed)
        {
            if (!IsCurrentTransition(transitionId))
                yield break;

            target.alpha = from;
            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f)
            {
                target.alpha = to;
                completed?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsCurrentTransition(transitionId))
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float eased = EvaluateEase(ease, elapsed / duration);
                target.alpha = Mathf.LerpUnclamped(from, to, eased);
                yield return null;
            }

            if (!IsCurrentTransition(transitionId))
                yield break;

            target.alpha = to;
            completed?.Invoke();
        }

        private void StopRunningCoroutines()
        {
            foreach (Coroutine coroutine in runningCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }

            runningCoroutines.Clear();
        }

        private static float EaseOutBackSubtle(float t)
        {
            const float c1 = 0.7f;
            const float c3 = c1 + 1f;
            float shifted = t - 1f;
            return 1f + c3 * shifted * shifted * shifted + c1 * shifted * shifted;
        }
    }
}
