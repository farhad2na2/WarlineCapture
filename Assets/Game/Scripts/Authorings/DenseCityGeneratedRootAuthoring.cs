using System;
using Game.Configs;
using UnityEngine;

namespace Game.Authoring
{
    public enum DenseCityGeneratedRootRole : byte
    {
        Unknown = 0,
        MapBakeSource = 1,
        EntityPresentationSource = 2
    }

    [DisallowMultipleComponent]
    public sealed class DenseCityGeneratedRootAuthoring : MonoBehaviour
    {
        [SerializeField] private DenseCityGeneratedRootRole role;
        [SerializeField] private string generationId;
        [SerializeField] private string generatorSchema;
        [SerializeField, Min(1)] private int generatorSchemaVersion = 1;
        [SerializeField] private int deterministicSeed;
        [SerializeField] private string deterministicGenerationHash;

        public DenseCityGeneratedRootRole Role => role;
        public string GenerationId => generationId;
        public string GeneratorSchema => generatorSchema;
        public int GeneratorSchemaVersion => generatorSchemaVersion;
        public int DeterministicSeed => deterministicSeed;
        public string DeterministicGenerationHash => deterministicGenerationHash;

        public bool TryValidate(out string error)
        {
            if (role != DenseCityGeneratedRootRole.MapBakeSource &&
                role != DenseCityGeneratedRootRole.EntityPresentationSource)
            {
                error = $"Unknown dense-city generated-root role: {(byte)role}.";
                return false;
            }
            if (!IsStableIdentifier(generationId, 128))
            {
                error = "Dense-city generation id must be a stable non-whitespace identifier.";
                return false;
            }
            if (!IsStableIdentifier(generatorSchema, 96))
            {
                error = "Dense-city generator schema must be a stable non-whitespace identifier.";
                return false;
            }
            if (generatorSchemaVersion <= 0)
            {
                error = "Dense-city generator schema version must be positive.";
                return false;
            }
            if (!OperationMapHashRules.IsValidSha256(deterministicGenerationHash))
            {
                error = "Dense-city deterministic generation hash must be lowercase SHA-256.";
                return false;
            }
            if (!HasFiniteTransform(transform))
            {
                error = "Dense-city generated-root transform must be finite with non-zero scale.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsStableIdentifier(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]) || char.IsControl(value[i]))
                    return false;
            }
            return true;
        }

        private static bool HasFiniteTransform(Transform owner)
        {
            Vector3 position = owner.localPosition;
            Quaternion rotation = owner.localRotation;
            Vector3 scale = owner.localScale;
            return IsFinite(position.x) && IsFinite(position.y) && IsFinite(position.z) &&
                   IsFinite(rotation.x) && IsFinite(rotation.y) && IsFinite(rotation.z) &&
                   IsFinite(rotation.w) && IsFinite(scale.x) && IsFinite(scale.y) &&
                   IsFinite(scale.z) && Mathf.Abs(scale.x) > 0.000001f &&
                   Mathf.Abs(scale.y) > 0.000001f && Mathf.Abs(scale.z) > 0.000001f;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
