#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Read-only VRP-003 inventory. It classifies authoring renderer/material rows but does not
    /// create the VRP-021 persistent source-row join or mutate presentation ownership.
    /// </summary>
    internal static class OperationMapRenderEligibilityInventoryProbe
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_eligibility_inventory.json";
        internal const int ExpectedPackedRenderRowCount = 82797;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void Run()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                    OpenSceneMode.Single);
                InventoryReport report = Build(scene);
                string outputPath = Path.Combine(projectRoot, ReportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                string temporaryPath = outputPath + ".tmp";
                File.WriteAllText(
                    temporaryPath,
                    JsonUtility.ToJson(report, true) + "\n",
                    Utf8WithoutBom);
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                File.Move(temporaryPath, outputPath);

                Debug.Log(
                    $"[OperationMapRenderEligibilityInventoryProbe] result=Passed " +
                    $"rows={report.totalRenderRows} eligible={report.eligibleRenderRows} " +
                    $"excluded={report.excludedRenderRows} report={ReportPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[OperationMapRenderEligibilityInventoryProbe] result=Failed");
                throw;
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        private static InventoryReport Build(Scene scene)
        {
            Renderer[] renderers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .OrderBy(renderer => GetPath(renderer.transform), StringComparer.Ordinal)
                .ToArray();

            var rows = new List<Row>(ExpectedPackedRenderRowCount);
            var signatureCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;
                DenseCityPresentationIdentityAuthoring denseOwner =
                    renderer.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true);
                OperationMapEntityPresentationIdentityAuthoring acceptedOwner =
                    renderer.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>(true);
                for (int subMesh = 0; subMesh < materials.Length; subMesh++)
                {
                    string signature =
                        (denseOwner != null
                            ? denseOwner.Category.ToString()
                            : "AcceptedMapResident") +
                        "|" +
                        BuildSignature(renderer, materials[subMesh], subMesh);
                    if (denseOwner != null)
                    {
                        signatureCounts.TryGetValue(signature, out int count);
                        signatureCounts[signature] = count + 1;
                    }
                    rows.Add(new Row(renderer, materials[subMesh], subMesh, signature, denseOwner, acceptedOwner));
                }
            }

            var semantic = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var signatures = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var rendererTypes = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var policies = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var ownership = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var reasons = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            int eligible = 0;

            foreach (Row row in rows)
            {
                bool repeated =
                    signatureCounts.TryGetValue(row.Signature, out int signatureCount) &&
                    signatureCount > 1;
                bool policySupported = TryClassifyPolicy(row.Renderer, row.Material, out string policy);
                string reason = Classify(row, repeated, policySupported);
                bool isEligible = string.Equals(reason, "eligible", StringComparison.Ordinal);
                if (isEligible)
                    eligible++;

                Increment(semantic, Semantic(row), isEligible);
                Increment(signatures, row.Signature, isEligible);
                Increment(rendererTypes, row.Renderer.GetType().FullName ?? row.Renderer.GetType().Name, isEligible);
                Increment(policies, policy, isEligible);
                Increment(ownership, Ownership(row), isEligible);
                Increment(reasons, reason, isEligible);
            }

            if (rows.Count != ExpectedPackedRenderRowCount)
            {
                throw new InvalidOperationException(
                    $"Authoring render rows did not reconcile to packed evidence: " +
                    $"{rows.Count} != {ExpectedPackedRenderRowCount}.");
            }
            return new InventoryReport
            {
                schema = "warline.operation-map.render-virtualization-eligibility-inventory",
                schemaVersion = 1,
                result = "Passed",
                operationMapId = "opmap.skirmish.desert_base_01",
                candidateScenePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                candidateSceneDependencyHash = AssetDatabase
                    .GetAssetDependencyHash(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath)
                    .ToString(),
                packedEvidencePath =
                    "Design/AgentReports/2026-07-25_dense_city_packed_asset_sharing.json",
                packedEvidenceRenderRows = ExpectedPackedRenderRowCount,
                totalRenderRows = rows.Count,
                eligibleRenderRows = eligible,
                excludedRenderRows = rows.Count - eligible,
                mutationAuthorized = false,
                mutationBlocker = "VRP-002 raw Android profile and VRP-021 exact source-row join remain open.",
                bySemanticCategory = BuildBreakdown(semantic),
                byPrototypeSignature = BuildBreakdown(signatures),
                byRendererType = BuildBreakdown(rendererTypes),
                byPolicyBucket = BuildBreakdown(policies),
                byGameplayOwnership = BuildBreakdown(ownership),
                byReasonCode = BuildBreakdown(reasons)
            };
        }

        private static string Classify(Row row, bool repeated, bool policySupported)
        {
            if (row.DenseOwner == null)
                return row.AcceptedOwner != null
                    ? "accepted-map-resident-pending-vrp021"
                    : "unresolved-owner";
            if (row.Renderer is not MeshRenderer)
                return "unsupported-renderer";
            if (!policySupported)
                return "unsupported-render-policy";
            if (!repeated)
                return "unique-presentation-signature";

            return row.DenseOwner.Category switch
            {
                DenseCityPresentationSemanticCategory.Vegetation => "eligible",
                DenseCityPresentationSemanticCategory.Prop => "eligible",
                DenseCityPresentationSemanticCategory.GameplayBuildingIntact =>
                    "gameplay-building-state-sync-not-accepted",
                DenseCityPresentationSemanticCategory.Infrastructure =>
                    "infrastructure-deferred-after-render-only-pilot",
                DenseCityPresentationSemanticCategory.Horizon =>
                    "unique-environment-content-resident",
                _ => "unsupported-semantic-category"
            };
        }

        private static bool TryClassifyPolicy(Renderer renderer, Material material, out string policy)
        {
            if (material == null)
            {
                policy = "Unsupported:missing-material";
                return false;
            }

            OperationMapRenderMaterialSurface surface;
            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                surface = OperationMapRenderMaterialSurface.Transparent;
            else if (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f)
                surface = OperationMapRenderMaterialSurface.AlphaClipped;
            else
                surface = OperationMapRenderMaterialSurface.Opaque;

            OperationMapRenderShadowFlags shadowFlags = OperationMapRenderShadowFlags.None;
            if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                shadowFlags |= OperationMapRenderShadowFlags.CastShadows;
            if (renderer.receiveShadows)
                shadowFlags |= OperationMapRenderShadowFlags.ReceiveShadows;

            OperationMapRenderMotionVectorMode motion = renderer.motionVectorGenerationMode switch
            {
                MotionVectorGenerationMode.Camera => OperationMapRenderMotionVectorMode.Camera,
                MotionVectorGenerationMode.Object => OperationMapRenderMotionVectorMode.Object,
                MotionVectorGenerationMode.ForceNoMotion => OperationMapRenderMotionVectorMode.ForceNoMotion,
                _ => (OperationMapRenderMotionVectorMode)byte.MaxValue
            };
            var input = new OperationMapRenderPolicyClassificationInput(
                surface,
                renderer.gameObject.layer,
                renderer.renderingLayerMask,
                motion,
                shadowFlags);
            if (!OperationMapRenderPolicyClassifier.TryClassify(input, out OperationMapRenderPolicyKey key, out string error))
            {
                policy = "Unsupported:" + error;
                return false;
            }

            policy = string.Join(
                "|",
                key.Bucket,
                key.Layer.ToString(CultureInfo.InvariantCulture),
                key.RenderingLayerMask.ToString(CultureInfo.InvariantCulture),
                key.MotionVectorMode,
                (byte)key.ShadowFlags);
            return true;
        }

        private static string Semantic(Row row) =>
            row.DenseOwner != null ? row.DenseOwner.Category.ToString() : "AcceptedMapResident";

        private static string Ownership(Row row)
        {
            if (row.DenseOwner != null)
                return "DenseGenerated:" + row.DenseOwner.Role;
            if (row.AcceptedOwner != null)
                return "AcceptedMap:" + row.AcceptedOwner.Role;
            return "Unresolved";
        }

        private static string BuildSignature(Renderer renderer, Material material, int subMesh)
        {
            Mesh mesh = renderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : renderer.GetComponent<MeshFilter>()?.sharedMesh;
            DenseCityPresentationIdentityAuthoring denseOwner =
                renderer.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true);
            GameObject prefabSource = denseOwner != null
                ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(denseOwner.gameObject)
                : null;
            string prefab = PersistentIdentity(prefabSource);
            string rendererSource = PersistentIdentity(
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer));
            return string.Join(
                "|",
                string.IsNullOrEmpty(prefab) ? "mesh" : "prefab",
                prefab,
                rendererSource,
                PersistentIdentity(mesh),
                PersistentIdentity(material),
                subMesh.ToString(CultureInfo.InvariantCulture));
        }

        private static string PersistentIdentity(UnityEngine.Object value)
        {
            if (value == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId) ||
                string.IsNullOrEmpty(guid) ||
                localId == 0)
            {
                return string.Empty;
            }
            return guid + ":" + localId.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetPath(Transform transform)
        {
            var parts = new List<string>();
            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void Increment(Dictionary<string, CountPair> values, string key, bool eligible)
        {
            values.TryGetValue(key, out CountPair count);
            if (eligible)
                count.Eligible++;
            else
                count.Excluded++;
            values[key] = count;
        }

        private static List<Breakdown> BuildBreakdown(Dictionary<string, CountPair> values) =>
            values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new Breakdown
                {
                    key = pair.Key,
                    eligible = pair.Value.Eligible,
                    excluded = pair.Value.Excluded,
                    total = pair.Value.Eligible + pair.Value.Excluded
                })
                .ToList();

        private static void Restore(SceneSetup[] setup)
        {
            if (setup != null && setup.Any(entry => entry.isLoaded))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private readonly struct Row
        {
            internal Row(
                Renderer renderer,
                Material material,
                int subMesh,
                string signature,
                DenseCityPresentationIdentityAuthoring denseOwner,
                OperationMapEntityPresentationIdentityAuthoring acceptedOwner)
            {
                Renderer = renderer;
                Material = material;
                SubMesh = subMesh;
                Signature = signature;
                DenseOwner = denseOwner;
                AcceptedOwner = acceptedOwner;
            }

            internal Renderer Renderer { get; }
            internal Material Material { get; }
            internal int SubMesh { get; }
            internal string Signature { get; }
            internal DenseCityPresentationIdentityAuthoring DenseOwner { get; }
            internal OperationMapEntityPresentationIdentityAuthoring AcceptedOwner { get; }
        }

        private struct CountPair
        {
            internal int Eligible;
            internal int Excluded;
        }

        [Serializable]
        private sealed class InventoryReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string candidateScenePath;
            public string candidateSceneDependencyHash;
            public string packedEvidencePath;
            public int packedEvidenceRenderRows;
            public int totalRenderRows;
            public int eligibleRenderRows;
            public int excludedRenderRows;
            public bool mutationAuthorized;
            public string mutationBlocker;
            public List<Breakdown> bySemanticCategory;
            public List<Breakdown> byPrototypeSignature;
            public List<Breakdown> byRendererType;
            public List<Breakdown> byPolicyBucket;
            public List<Breakdown> byGameplayOwnership;
            public List<Breakdown> byReasonCode;
        }

        [Serializable]
        private sealed class Breakdown
        {
            public string key;
            public int eligible;
            public int excluded;
            public int total;
        }
    }
}

#endif
