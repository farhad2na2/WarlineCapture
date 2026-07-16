using System;
using System.Collections;
using System.Collections.Generic;
using Game.Configs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Runtime
{
    internal sealed class RuntimeOperationMapVisualRecipePresentationSystemHelper
    {
        private static readonly RuntimeOperationMapVisualStage[] RevealOrder =
        {
            RuntimeOperationMapVisualStage.TerrainAndRoads,
            RuntimeOperationMapVisualStage.DistrictModules,
            RuntimeOperationMapVisualStage.Market,
            RuntimeOperationMapVisualStage.Compound,
            RuntimeOperationMapVisualStage.Aftermath,
            RuntimeOperationMapVisualStage.Horizon
        };

        private Transform _recipeRoot;
        private RuntimeOperationMapVisualQualitySystemHelper _quality;
        private readonly List<Mesh> _generatedMeshes = new();

        public RuntimeCityGenerationProgress Progress { get; private set; } = RuntimeCityGenerationProgress.Idle;
        public RuntimeOperationMapVisualStage CurrentVisualStage { get; private set; }
        public int SpawnedEntryCount { get; private set; }
        public int RendererCount { get; private set; }
        public int FoundationVisualCount => _quality?.FoundationVisualCount ?? 0;
        public int SuppressedObstructionCount => _quality?.SuppressedObstructionCount ?? 0;
        public float MaxBatchMilliseconds { get; private set; }
        public int FrameBudgetYieldCount { get; private set; }

        public IEnumerator Build(
            RuntimeOperationMapVisualRecipe recipe,
            Transform runtimeRoot,
            int entriesPerFrame,
            float frameBudgetMilliseconds)
        {
            if (recipe == null || runtimeRoot == null)
                yield break;

            var root = new GameObject($"RuntimeVisualRecipe_{recipe.RecipeVersion}");
            _recipeRoot = root.transform;
            _recipeRoot.SetParent(runtimeRoot, false);
            _quality = new RuntimeOperationMapVisualQualitySystemHelper();

            GameObject foundation = _quality.CreateFoundation(recipe.Foundation, _recipeRoot);
            if (foundation != null)
            {
                RendererCount += CountActiveRenderers(foundation);
                Progress = new RuntimeCityGenerationProgress(
                    RuntimeCityGenerationStage.Roads,
                    recipe.Seed,
                    1,
                    0,
                    0,
                    Mathf.Max(1, recipe.Entries.Count),
                    0f);
                yield return null;
            }

            IReadOnlyList<RuntimeOperationMapVisualEntry> entries = recipe.Entries;
            IReadOnlyList<RuntimeOperationMapDistrictModuleRecipe> districtModules = recipe.DistrictModules;
            int districtSliceCount = 0;
            for (int moduleIndex = 0; moduleIndex < districtModules.Count; moduleIndex++)
                districtSliceCount += districtModules[moduleIndex]?.SlicePaths.Count ?? 0;
            int total = Mathf.Max(1, entries.Count + districtSliceCount);
            int batchSize = Mathf.Max(1, entriesPerFrame);
            float frameBudget = Mathf.Max(0.1f, frameBudgetMilliseconds);
            var stageRoots = new Dictionary<RuntimeOperationMapVisualStage, Transform>();
            int completed = 0;
            float batchStartedAt = Time.realtimeSinceStartup;
            for (int stageIndex = 0; stageIndex < RevealOrder.Length; stageIndex++)
            {
                RuntimeOperationMapVisualStage stage = RevealOrder[stageIndex];
                CurrentVisualStage = stage;
                float stageStartedAt = Time.unscaledTime;
                int stageEntryCount = 0;
                Debug.Log($"[RuntimeMapReveal] action=begin stage={stage} seed={recipe.Seed}");
                if (stage == RuntimeOperationMapVisualStage.DistrictModules)
                {
                    Transform stageRoot = GetOrCreateStageRoot(stage, stageRoots);
                    for (int moduleIndex = 0; moduleIndex < districtModules.Count; moduleIndex++)
                    {
                        RuntimeOperationMapDistrictModuleRecipe module = districtModules[moduleIndex];
                        if (module == null || !module.IsConfigured)
                            throw new InvalidOperationException($"Runtime district module {moduleIndex} is not configured.");

                        Transform moduleRoot = CreateDistrictModuleRoot(module, stageRoot);
                        for (int sliceIndex = 0; sliceIndex < module.SlicePaths.Count; sliceIndex++)
                        {
                            string slicePath = module.SlicePaths[sliceIndex];
                            GameObject visual = CreateDistrictSlice(module, slicePath, moduleRoot);
                            _quality.ApplyClearanceRules(visual, stage, module.Cleanup);
                            SpawnedEntryCount++;
                            RendererCount += CountActiveRenderers(visual);
                            stageEntryCount++;
                            completed++;
                            Progress = new RuntimeCityGenerationProgress(
                                MapStage(stage),
                                recipe.Seed,
                                1,
                                0,
                                completed,
                                total,
                                (float)completed / total);

                            float batchMilliseconds = (Time.realtimeSinceStartup - batchStartedAt) * 1000f;
                            MaxBatchMilliseconds = Mathf.Max(MaxBatchMilliseconds, batchMilliseconds);
                            bool reachedFrameBudget = batchMilliseconds >= frameBudget;
                            if (completed == 1 || completed % batchSize == 0 || reachedFrameBudget)
                            {
                                if (reachedFrameBudget)
                                    FrameBudgetYieldCount++;
                                yield return null;
                                batchStartedAt = Time.realtimeSinceStartup;
                            }
                        }

                        moduleRoot.gameObject.SetActive(module.Active);
                    }
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    RuntimeOperationMapVisualEntry entry = entries[i];
                    if (entry.Stage != stage)
                        continue;

                    stageEntryCount++;
                    Transform stageRoot = GetOrCreateStageRoot(stage, stageRoots);
                    GameObject visual = CreateEntry(entry, stageRoot, recipe.Seed);
                    if (visual != null)
                    {
                        _quality.ApplyClearanceRules(visual, stage, entry.Cleanup);
                        SpawnedEntryCount++;
                        RendererCount += CountActiveRenderers(visual);
                    }

                    completed++;
                    Progress = new RuntimeCityGenerationProgress(
                        MapStage(stage),
                        recipe.Seed,
                        1,
                        0,
                        completed,
                        total,
                        (float)completed / total);

                    float batchMilliseconds = (Time.realtimeSinceStartup - batchStartedAt) * 1000f;
                    MaxBatchMilliseconds = Mathf.Max(MaxBatchMilliseconds, batchMilliseconds);
                    bool reachedFrameBudget = batchMilliseconds >= frameBudget;
                    if (completed == 1 ||
                        completed % batchSize == 0 ||
                        reachedFrameBudget)
                    {
                        if (reachedFrameBudget)
                            FrameBudgetYieldCount++;
                        yield return null;
                        batchStartedAt = Time.realtimeSinceStartup;
                    }
                }

                if (stageEntryCount == 0)
                    continue;

                float minimumStageDuration = recipe.Reveal.GetMinimumDuration(stage);
                while (Time.unscaledTime - stageStartedAt < minimumStageDuration)
                    yield return null;
                Debug.Log(
                    $"[RuntimeMapReveal] action=complete stage={stage} seed={recipe.Seed} " +
                    $"elapsed={Time.unscaledTime - stageStartedAt:0.000}s entries={stageEntryCount}");
                batchStartedAt = Time.realtimeSinceStartup;
            }

            Progress = new RuntimeCityGenerationProgress(
                RuntimeCityGenerationStage.Completed,
                recipe.Seed,
                1,
                1,
                total,
                total,
                1f);
        }

        public void Dispose()
        {
            if (_recipeRoot != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_recipeRoot.gameObject);
                else
                    Object.DestroyImmediate(_recipeRoot.gameObject);
            }

            _quality?.Dispose();
            _quality = null;
            for (int i = 0; i < _generatedMeshes.Count; i++)
            {
                Mesh mesh = _generatedMeshes[i];
                if (mesh == null)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(mesh);
                else
                    Object.DestroyImmediate(mesh);
            }
            _generatedMeshes.Clear();
            _recipeRoot = null;
            SpawnedEntryCount = 0;
            RendererCount = 0;
            MaxBatchMilliseconds = 0f;
            FrameBudgetYieldCount = 0;
            CurrentVisualStage = RuntimeOperationMapVisualStage.TerrainAndRoads;
            Progress = RuntimeCityGenerationProgress.Idle;
        }

        private Transform GetOrCreateStageRoot(
            RuntimeOperationMapVisualStage stage,
            Dictionary<RuntimeOperationMapVisualStage, Transform> stageRoots)
        {
            if (stageRoots.TryGetValue(stage, out Transform root))
                return root;

            var stageObject = new GameObject(stage.ToString());
            root = stageObject.transform;
            root.SetParent(_recipeRoot, false);
            stageRoots.Add(stage, root);
            return root;
        }

        private static Transform CreateDistrictModuleRoot(
            RuntimeOperationMapDistrictModuleRecipe module,
            Transform stageRoot)
        {
            var moduleObject = new GameObject(module.Name);
            Transform moduleRoot = moduleObject.transform;
            moduleRoot.SetParent(stageRoot, false);
            moduleRoot.SetPositionAndRotation(module.Position, module.Rotation);
            moduleRoot.localScale = module.Scale;
            return moduleRoot;
        }

        private static GameObject CreateDistrictSlice(
            RuntimeOperationMapDistrictModuleRecipe module,
            string slicePath,
            Transform moduleRoot)
        {
            Transform sourceRoot = module.Prefab.transform;
            Transform sourceSlice = sourceRoot.Find(slicePath);
            if (sourceSlice == null)
            {
                throw new InvalidOperationException(
                    $"Runtime district module {module.Name} is missing prefab slice {slicePath}.");
            }

            GameObject visual = Object.Instantiate(sourceSlice.gameObject, moduleRoot);
            visual.name = $"{module.Name}/{slicePath}";
            Matrix4x4 relativeMatrix = sourceRoot.worldToLocalMatrix * sourceSlice.localToWorldMatrix;
            Transform visualTransform = visual.transform;
            visualTransform.localPosition = relativeMatrix.GetColumn(3);
            visualTransform.localRotation = relativeMatrix.rotation;
            visualTransform.localScale = relativeMatrix.lossyScale;
            DisableVisualOnlyColliders(visual);
            ParticleSystem[] particleSystems = visual.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
                particleSystems[i].gameObject.SetActive(false);
            return visual;
        }

        private GameObject CreateEntry(RuntimeOperationMapVisualEntry entry, Transform parent, uint recipeSeed)
        {
            GameObject visual;
            switch (entry.Kind)
            {
                case RuntimeOperationMapVisualEntryKind.Prefab:
                    if (entry.Prefab == null)
                        return null;
                    visual = Object.Instantiate(entry.Prefab, parent);
                    DisableVisualOnlyColliders(visual);
                    break;
                case RuntimeOperationMapVisualEntryKind.Box:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.transform.SetParent(parent, false);
                    ApplyMaterialAndRemoveCollider(visual, entry.Material);
                    break;
                case RuntimeOperationMapVisualEntryKind.Cylinder:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visual.transform.SetParent(parent, false);
                    ApplyMaterialAndRemoveCollider(visual, entry.Material);
                    break;
                case RuntimeOperationMapVisualEntryKind.PointLight:
                    visual = new GameObject(entry.Name);
                    visual.transform.SetParent(parent, false);
                    Light light = visual.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = entry.LightColor;
                    light.intensity = entry.LightIntensity;
                    light.range = entry.LightRange;
                    light.shadows = entry.LightShadows;
                    break;
                case RuntimeOperationMapVisualEntryKind.IrregularSurface:
                    visual = RuntimeOperationMapSurfaceGeometrySystemHelper.CreateIrregularSurface(
                        entry.Name,
                        recipeSeed,
                        parent);
                    MeshFilter irregularSurfaceFilter = visual.GetComponent<MeshFilter>();
                    if (irregularSurfaceFilter != null && irregularSurfaceFilter.sharedMesh != null)
                        _generatedMeshes.Add(irregularSurfaceFilter.sharedMesh);
                    ApplyMaterialAndRemoveCollider(visual, entry.Material);
                    break;
                default:
                    return null;
            }

            visual.name = entry.Name;
            visual.transform.SetPositionAndRotation(entry.Position, entry.Rotation);
            visual.transform.localScale = entry.Scale;
            visual.SetActive(entry.Active);
            if (!entry.AllowParticles)
            {
                ParticleSystem[] particleSystems = visual.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < particleSystems.Length; i++)
                    particleSystems[i].gameObject.SetActive(false);
            }
            return visual;
        }

        private static void ApplyMaterialAndRemoveCollider(GameObject visual, Material material)
        {
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }
        }

        private static void DisableVisualOnlyColliders(GameObject visual)
        {
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private static int CountActiveRenderers(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(false);
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                    count++;
            }

            return count;
        }

        private static RuntimeCityGenerationStage MapStage(RuntimeOperationMapVisualStage stage)
        {
            switch (stage)
            {
                case RuntimeOperationMapVisualStage.TerrainAndRoads:
                    return RuntimeCityGenerationStage.Roads;
                case RuntimeOperationMapVisualStage.DistrictModules:
                case RuntimeOperationMapVisualStage.Market:
                case RuntimeOperationMapVisualStage.Compound:
                    return RuntimeCityGenerationStage.Buildings;
                case RuntimeOperationMapVisualStage.Aftermath:
                case RuntimeOperationMapVisualStage.Horizon:
                    return RuntimeCityGenerationStage.Decorations;
                default:
                    return RuntimeCityGenerationStage.Planning;
            }
        }
    }
}
