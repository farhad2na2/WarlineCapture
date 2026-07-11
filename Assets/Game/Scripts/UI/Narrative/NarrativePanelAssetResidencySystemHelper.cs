using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.UI.Runtime
{
    public sealed class NarrativePanelAssetResidencySystemHelper
    {
        private Slot current;
        private Slot next;

        public int ResidentAssetCount => (current.IsValid ? 1 : 0) + (next.IsValid && !SameHandle(next, current) ? 1 : 0);
        public string CurrentKey => current.Key;
        public string NextKey => next.Key;

        public Sprite LoadCurrentAndPrepareNext(
            AssetReferenceSprite requestedCurrent,
            AssetReferenceSprite requestedNext,
            Sprite directFallback = null)
        {
            Slot oldCurrent = current;
            Slot oldNext = next;
            Slot loadedCurrent = Acquire(requestedCurrent, oldCurrent, oldNext);
            Slot loadedNext = SameReference(requestedNext, requestedCurrent)
                ? default
                : Acquire(requestedNext, oldCurrent, oldNext);

            current = loadedCurrent;
            next = loadedNext;
            ReleaseIfUnused(oldCurrent);
            ReleaseIfUnused(oldNext);
            return current.IsValid && current.Handle.Status == AsyncOperationStatus.Succeeded
                ? current.Handle.Result
                : directFallback;
        }

        public Sprite KeepCurrentAndPrepareNext(AssetReferenceSprite requestedNext)
        {
            return LoadCurrentAndPrepareNext(current.Reference, requestedNext, current.IsValid ? current.Handle.Result : null);
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

        private static Slot Acquire(AssetReferenceSprite reference, Slot oldCurrent, Slot oldNext)
        {
            if (!IsReferenceValid(reference))
                return default;
            if (Matches(oldCurrent, reference))
                return oldCurrent;
            if (Matches(oldNext, reference))
                return oldNext;

            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(reference.RuntimeKey);
            Sprite result = handle.WaitForCompletion();
            if (handle.Status == AsyncOperationStatus.Succeeded && result != null)
                return new Slot(reference, handle);
            if (handle.IsValid())
                Addressables.Release(handle);
            return default;
        }

        private void ReleaseIfUnused(Slot slot)
        {
            if (!slot.IsValid || SameHandle(slot, current) || SameHandle(slot, next))
                return;
            Release(slot);
        }

        private static void Release(Slot slot)
        {
            if (slot.IsValid)
                Addressables.Release(slot.Handle);
        }

        private static bool Matches(Slot slot, AssetReferenceSprite reference)
        {
            return slot.IsValid && IsReferenceValid(reference) && slot.Key == Key(reference);
        }

        private static bool SameReference(AssetReferenceSprite left, AssetReferenceSprite right)
        {
            return IsReferenceValid(left) && IsReferenceValid(right) && Key(left) == Key(right);
        }

        private static bool SameHandle(Slot left, Slot right)
        {
            return left.IsValid && right.IsValid && left.Handle.Equals(right.Handle);
        }

        private static bool IsReferenceValid(AssetReferenceSprite reference)
        {
            return reference != null && reference.RuntimeKeyIsValid();
        }

        private static string Key(AssetReferenceSprite reference)
        {
            return IsReferenceValid(reference) ? reference.RuntimeKey.ToString() : string.Empty;
        }

        private readonly struct Slot
        {
            public readonly AssetReferenceSprite Reference;
            public readonly AsyncOperationHandle<Sprite> Handle;

            public Slot(AssetReferenceSprite reference, AsyncOperationHandle<Sprite> handle)
            {
                Reference = reference;
                Handle = handle;
            }

            public bool IsValid => Handle.IsValid();
            public string Key => Reference != null ? NarrativePanelAssetResidencySystemHelper.Key(Reference) : string.Empty;
        }
    }
}
