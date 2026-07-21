using System;
using UnityEngine;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class DenseCityAuthoredOverrideAuthoring : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private Vector3 localCenter;
        [SerializeField] private Vector3 localSize = Vector3.one;
        [SerializeField] private bool excludePresentation = true;
        [SerializeField] private bool excludeSurface = true;
        [SerializeField] private bool excludeBlockers = true;

        public string StableId => stableId;
        public Vector3 LocalCenter => localCenter;
        public Vector3 LocalSize => localSize;
        public bool ExcludePresentation => excludePresentation;
        public bool ExcludeSurface => excludeSurface;
        public bool ExcludeBlockers => excludeBlockers;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(stableId) || stableId.Length > 128 ||
                !string.Equals(stableId, stableId.Trim(), StringComparison.Ordinal))
            {
                error = "Dense-city authored override stable id is invalid.";
                return false;
            }
            for (int i = 0; i < stableId.Length; i++)
            {
                if (char.IsWhiteSpace(stableId[i]) || char.IsControl(stableId[i]))
                {
                    error = "Dense-city authored override stable id cannot contain whitespace.";
                    return false;
                }
            }
            if (!IsFinite(localCenter) || !IsFinite(localSize) ||
                localSize.x <= 0f || localSize.y <= 0f || localSize.z <= 0f)
            {
                error = "Dense-city authored override center/size must be finite and size must be positive.";
                return false;
            }
            if (!excludePresentation && !excludeSurface && !excludeBlockers)
            {
                error = "Dense-city authored override must exclude at least one generated ownership surface.";
                return false;
            }
            if (GetComponentInChildren<Collider>(true) != null)
            {
                error = "Dense-city authored overrides use serialized bounds and cannot own colliders.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
