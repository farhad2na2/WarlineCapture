using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.UI.Runtime
{
    public sealed class NarrativePanelAssetResidencyPresentationSystemHelper
    {
        private Slot current;
        private Slot next;

        public event Action<ulong, Sprite> CurrentReady;
        public event Action<ulong> CurrentFailed;

        public int ResidentAssetCount => (current.IsValid ? 1 : 0) +
                                         (next.IsValid && !SameHandle(next, current) ? 1 : 0);
        public string CurrentKey => current.Key;
        public string NextKey => next.Key;
        public bool IsCurrentReady => TryGetResult(current, out _);

        public Sprite RequestCurrentAndPrepareNext(
            AssetReferenceSprite requestedCurrent,
            AssetReferenceSprite requestedNext,
            ulong transitionToken,
            Sprite directFallback = null)
        {
            Slot oldCurrent = current;
            Slot oldNext = next;
            current = Acquire(requestedCurrent, transitionToken, oldCurrent, oldNext);
            next = SameReference(requestedNext, requestedCurrent)
                ? default
                : Acquire(requestedNext, transitionToken, oldCurrent, oldNext);
            ReleaseIfUnused(oldCurrent);
            ReleaseIfUnused(oldNext);
            return TryGetResult(current, out Sprite loaded) ? loaded : directFallback;
        }

        public Sprite KeepCurrentAndPrepareNext(
            AssetReferenceSprite requestedNext,
            ulong transitionToken)
        {
            Sprite fallback = TryGetResult(current, out Sprite loaded) ? loaded : null;
            return RequestCurrentAndPrepareNext(current.Reference, requestedNext, transitionToken, fallback);
        }

        public void ReleaseAll()
        {
            Slot oldCurrent = current;
            Slot oldNext = next;
            current = default;
            next = default;
            Release(oldCurrent);
            if (!SameHandle(oldNext, oldCurrent))
                Release(oldNext);
        }

        private Slot Acquire(
            AssetReferenceSprite reference,
            ulong transitionToken,
            Slot oldCurrent,
            Slot oldNext)
        {
            if (!IsReferenceValid(reference))
                return default;
            if (Matches(oldCurrent, reference))
                return oldCurrent.WithToken(transitionToken);
            if (Matches(oldNext, reference))
                return oldNext.WithToken(transitionToken);

            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(reference.RuntimeKey);
            handle.Completed += HandleCompleted;
            return new Slot(reference, handle, transitionToken);
        }

        private void HandleCompleted(AsyncOperationHandle<Sprite> handle)
        {
            if (!handle.IsValid())
                return;
            if (SameHandle(current, handle))
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    CurrentReady?.Invoke(current.TransitionToken, handle.Result);
                else
                {
                    ulong token = current.TransitionToken;
                    ReleaseFailed(ref current, handle);
                    CurrentFailed?.Invoke(token);
                }
                return;
            }
            if (SameHandle(next, handle) && handle.Status != AsyncOperationStatus.Succeeded)
                ReleaseFailed(ref next, handle);
        }

        private void ReleaseFailed(ref Slot slot, AsyncOperationHandle<Sprite> handle)
        {
            slot = default;
            if (handle.IsValid())
            {
                handle.Completed -= HandleCompleted;
                Addressables.Release(handle);
            }
        }

        private void ReleaseIfUnused(Slot slot)
        {
            if (!slot.IsValid || SameHandle(slot, current) || SameHandle(slot, next))
                return;
            Release(slot);
        }

        private void Release(Slot slot)
        {
            if (!slot.IsValid)
                return;
            slot.Handle.Completed -= HandleCompleted;
            Addressables.Release(slot.Handle);
        }

        private static bool TryGetResult(Slot slot, out Sprite sprite)
        {
            sprite = slot.IsValid && slot.Handle.Status == AsyncOperationStatus.Succeeded
                ? slot.Handle.Result
                : null;
            return sprite != null;
        }

        private static bool Matches(Slot slot, AssetReferenceSprite reference) =>
            slot.IsValid && IsReferenceValid(reference) && slot.Key == Key(reference);

        private static bool SameReference(AssetReferenceSprite left, AssetReferenceSprite right) =>
            IsReferenceValid(left) && IsReferenceValid(right) && Key(left) == Key(right);

        private static bool SameHandle(Slot left, Slot right) =>
            left.IsValid && right.IsValid && left.Handle.Equals(right.Handle);

        private static bool SameHandle(Slot slot, AsyncOperationHandle<Sprite> handle) =>
            slot.IsValid && handle.IsValid() && slot.Handle.Equals(handle);

        private static bool IsReferenceValid(AssetReferenceSprite reference) =>
            reference != null && reference.RuntimeKeyIsValid();

        private static string Key(AssetReferenceSprite reference) =>
            IsReferenceValid(reference) ? reference.RuntimeKey.ToString() : string.Empty;

        private readonly struct Slot
        {
            public readonly AssetReferenceSprite Reference;
            public readonly AsyncOperationHandle<Sprite> Handle;
            public readonly ulong TransitionToken;

            public Slot(
                AssetReferenceSprite reference,
                AsyncOperationHandle<Sprite> handle,
                ulong transitionToken)
            {
                Reference = reference;
                Handle = handle;
                TransitionToken = transitionToken;
            }

            public bool IsValid => Handle.IsValid();
            public string Key => Reference != null
                ? NarrativePanelAssetResidencyPresentationSystemHelper.Key(Reference)
                : string.Empty;
            public Slot WithToken(ulong token) => new(Reference, Handle, token);
        }
    }
}
