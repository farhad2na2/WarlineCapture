using System;
using System.Collections.Generic;
using Game.Authoring;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal static class DenseCityRenderOnlyPresentationRealizer
    {
        private const float MatrixAbsoluteTolerance = 0.0001f;
        private const float MatrixRelativeTolerance = 0.0000002f;

        internal static Transform Realize(
            DenseCityPresentationBakeRecord presentation,
            DenseCityPresentationHierarchyContext hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));
            DenseCityRenderOnlyPresentationRecordFactory.RequireRenderOnlyCategory(
                presentation.Category);
            if (string.IsNullOrEmpty(presentation.PrefabAssetGuid) ||
                !string.IsNullOrEmpty(presentation.MeshAssetGuid))
            {
                throw new InvalidOperationException(
                    "Record-driven render-only realization currently requires one persistent prefab source.");
            }

            GameObject prefab = LoadRequiredPrefab(presentation, out string prefabPath);

            Transform parent = hierarchy.ResolveIndependentParent(presentation.Category);
            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance == null)
                    throw new InvalidOperationException($"Failed to instantiate dense-city prefab '{prefabPath}'.");
                DenseCityPhysicsComponentStripper.StripInstanceHierarchy(instance);
                instance.name = $"{prefab.name}_{presentation.Identity.DeterministicSequence:D6}";
                ApplyWorldMatrix(instance.transform, presentation.WorldMatrix);
                ApplyRecordedSingleMaterialOverride(instance, presentation);
                RequireMaterialIdentity(instance, presentation);
                hierarchy.RequireIndependentRoot(presentation.Category, instance.transform);
                var identity = instance.AddComponent<DenseCityPresentationIdentityAuthoring>();
                identity.ConfigureForEditor(
                    presentation.Identity.CreateBakedStableId(),
                    OperationMapEntityPresentationRole.RenderOnly,
                    (Game.Components.DenseCityPresentationSemanticCategory)presentation.Category,
                    presentation.AllowsProtectedOverlap);
                if (!identity.TryValidate(out string identityError))
                {
                    throw new InvalidOperationException(
                        $"Dense-city presentation identity is invalid: {identityError}");
                }
                RequireMatrixParity(instance.transform.localToWorldMatrix, presentation);
                return instance.transform;
            }
            catch
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
                throw;
            }
        }

        internal static Transform RealizeAttachment(
            DenseCityPresentationBakeRecord presentation,
            Transform declaredBuildingVisualRoot,
            DenseCityPresentationHierarchyContext hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));
            if (presentation.Category is not (DenseCityPresentationCategory.BuildingAttachmentIntact or
                DenseCityPresentationCategory.BuildingAttachmentDestroyed))
            {
                throw new ArgumentOutOfRangeException(nameof(presentation));
            }

            Transform parent = hierarchy.RequireAttachmentParent(
                presentation.Category,
                declaredBuildingVisualRoot);
            GameObject prefab = LoadRequiredPrefab(presentation, out string prefabPath);
            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance == null)
                    throw new InvalidOperationException($"Failed to instantiate dense-city prefab '{prefabPath}'.");
                DenseCityPhysicsComponentStripper.StripInstanceHierarchy(instance);
                instance.name = $"{prefab.name}_{presentation.Identity.DeterministicSequence:D6}";
                ApplyWorldMatrix(instance.transform, presentation.WorldMatrix);
                ApplyRecordedSingleMaterialOverride(instance, presentation);
                RequireMaterialIdentity(instance, presentation);
                hierarchy.RequireAttachmentRoot(
                    presentation.Category,
                    declaredBuildingVisualRoot,
                    instance.transform);
                OperationMapBuildingAuthoring buildingOwner =
                    declaredBuildingVisualRoot.GetComponentInParent<OperationMapBuildingAuthoring>(true);
                if (buildingOwner == null)
                {
                    throw new InvalidOperationException(
                        "Dense-city building attachment has no gameplay-building owner.");
                }
                var attachmentAuthoring = instance.AddComponent<OperationMapBuildingAttachmentAuthoring>();
                attachmentAuthoring.ConfigureForEditor(
                    buildingOwner,
                    presentation.Category == DenseCityPresentationCategory.BuildingAttachmentIntact
                        ? OperationMapBuildingVisualState.Intact
                        : OperationMapBuildingVisualState.Destroyed);
                if (!attachmentAuthoring.TryValidate(out string attachmentError))
                    throw new InvalidOperationException(attachmentError);
                RequireMatrixParity(instance.transform.localToWorldMatrix, presentation);
                return instance.transform;
            }
            catch
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
                throw;
            }
        }

        internal static GameObject LoadRequiredPrefab(
            DenseCityPresentationBakeRecord presentation,
            out string prefabPath)
        {
            if (string.IsNullOrEmpty(presentation.PrefabAssetGuid) ||
                !string.IsNullOrEmpty(presentation.MeshAssetGuid))
            {
                throw new InvalidOperationException(
                    "Record-driven presentation realization requires one persistent prefab source.");
            }

            prefabPath = AssetDatabase.GUIDToAssetPath(presentation.PrefabAssetGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    prefab,
                    out string actualGuid,
                    out long actualLocalId) ||
                !string.Equals(actualGuid, presentation.PrefabAssetGuid, StringComparison.Ordinal) ||
                actualLocalId != presentation.Identity.SourceLocalId)
            {
                throw new InvalidOperationException(
                    $"Dense-city presentation source identity is unavailable or mismatched: " +
                    $"'{presentation.Identity.StableKey}'.");
            }
            return prefab;
        }

        private static void ApplyRecordedSingleMaterialOverride(
            GameObject instance,
            DenseCityPresentationBakeRecord presentation)
        {
            ReadOnlySpan<string> expected = presentation.MaterialAssetGuids.Span;
            if (expected.Length != 1)
                return;

            string materialPath = AssetDatabase.GUIDToAssetPath(expected[0]);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Dense-city recorded material is unavailable: '{expected[0]}'.");
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials[materialIndex] == null || materials[materialIndex] == material)
                        continue;
                    materials[materialIndex] = material;
                    changed = true;
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        internal static void ApplyWorldMatrix(Transform transform, Matrix4x4 matrix)
        {
            Matrix4x4 localMatrix = transform.parent != null
                ? transform.parent.worldToLocalMatrix * matrix
                : matrix;
            Vector3 position = localMatrix.GetColumn(3);
            Vector3 right = localMatrix.GetColumn(0);
            Vector3 up = localMatrix.GetColumn(1);
            Vector3 forward = localMatrix.GetColumn(2);
            var scale = new Vector3(right.magnitude, up.magnitude, forward.magnitude);
            if (!float.IsFinite(scale.x) || !float.IsFinite(scale.y) || !float.IsFinite(scale.z) ||
                scale.x <= 0.000001f || scale.y <= 0.000001f || scale.z <= 0.000001f)
            {
                throw new InvalidOperationException("Dense-city presentation matrix has invalid scale.");
            }
            if (localMatrix.determinant < 0f)
            {
                scale.x = -scale.x;
                right = -right;
            }

            Quaternion rotation = Quaternion.LookRotation(forward / scale.z, up / scale.y);
            Matrix4x4 reconstructed = Matrix4x4.TRS(position, rotation, scale);
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(reconstructed[index] - localMatrix[index]) <= 0.0001f)
                    continue;
                throw new InvalidOperationException(
                    "Dense-city presentation matrix contains unsupported shear or decomposition drift.");
            }

            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }

        internal static void RequireMaterialIdentity(
            GameObject instance,
            DenseCityPresentationBakeRecord presentation)
        {
            var actualGuids = new SortedSet<string>(StringComparer.Ordinal);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                        continue;
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                            material,
                            out string materialGuid,
                            out long materialLocalId) ||
                        string.IsNullOrEmpty(materialGuid) || materialLocalId <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Dense-city realized material is not persistent: '{material.name}'.");
                    }
                    actualGuids.Add(materialGuid);
                }
            }

            ReadOnlyMemory<string> expectedMemory = presentation.MaterialAssetGuids;
            ReadOnlySpan<string> expected = expectedMemory.Span;
            if (actualGuids.Count != expected.Length)
                throw MaterialMismatch(presentation);

            var unmatchedActualGuids = new HashSet<string>(
                actualGuids,
                StringComparer.Ordinal);
            for (int expectedIndex = 0; expectedIndex < expected.Length; expectedIndex++)
            {
                string expectedGuid = expected[expectedIndex];
                if (unmatchedActualGuids.Remove(expectedGuid))
                    continue;

                string deterministicReplacement = null;
                foreach (string actualGuid in unmatchedActualGuids)
                {
                    if (DenseCityCandidateAuthoringTransaction
                        .IsDeterministicSyntyMaterialReplacement(expectedGuid, actualGuid))
                    {
                        deterministicReplacement = actualGuid;
                        break;
                    }
                }
                if (deterministicReplacement == null)
                    throw MaterialMismatch(presentation);
                unmatchedActualGuids.Remove(deterministicReplacement);
            }
            if (unmatchedActualGuids.Count != 0)
                throw MaterialMismatch(presentation);
        }

        private static InvalidOperationException MaterialMismatch(
            DenseCityPresentationBakeRecord presentation) =>
            new(
                $"Dense-city presentation material identity differs from its record: " +
                $"'{presentation.Identity.StableKey}'.");

        internal static void RequireMatrixParity(
            Matrix4x4 actual,
            DenseCityPresentationBakeRecord presentation)
        {
            for (int index = 0; index < 16; index++)
            {
                float expected = presentation.WorldMatrix[index];
                float delta = Mathf.Abs(actual[index] - expected);
                float tolerance = Mathf.Max(
                    MatrixAbsoluteTolerance,
                    Mathf.Abs(expected) * MatrixRelativeTolerance);
                if (delta <= tolerance)
                    continue;
                throw new InvalidOperationException(
                    $"Dense-city realized transform differs from its record: " +
                    $"'{presentation.Identity.StableKey}' matrix[{index}] " +
                    $"expected={expected:R} actual={actual[index]:R} " +
                    $"delta={delta:R} tolerance={tolerance:R}.");
            }
        }
    }
}
