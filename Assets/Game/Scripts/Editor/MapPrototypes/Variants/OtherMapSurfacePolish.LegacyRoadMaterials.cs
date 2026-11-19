using System;
using System.IO;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using Unity.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static readonly Dictionary<(string, Material, RoadLayer), Material> LegacyMaterials = new();

        [InitializeOnLoadMethod]
        static void InvalidateCachedRoadAssets()
        {
            EditorApplication.projectChanged += () =>
            {
                LegacyMaterials.Clear();
                SoilAreas.Clear();
            };
        }

        internal static bool IsRunwayRenderSurface(MeshRenderer renderer) => RunwayRenderer(renderer);

        [Serializable]
        struct SoilCoverage { public Bounds bounds; }

        static readonly Dictionary<string, (TextAsset asset, Bounds bounds)> SoilAreas = new();
        internal static TextAsset LegacySoilArea(string map, out Bounds bounds)
        {
            if (!SoilAreas.TryGetValue(map, out var area))
            {
                string path = $"{ShoulderArt}/{map}_SoilCoverage.json";
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (!asset) throw new InvalidDataException("Missing baked road coverage at "+path);
                area = (asset, JsonUtility.FromJson<SoilCoverage>(asset.text).bounds);
                SoilAreas[map] = area;
            }
            bounds = area.bounds;
            return area.asset;
        }

        static void SaveLegacySoilArea(string map, Bounds bounds)
        {
            string path = $"{ShoulderArt}/{map}_SoilCoverage.json";
            File.WriteAllText(path, JsonUtility.ToJson(new SoilCoverage { bounds = bounds }, true)+"\n");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            SoilAreas.Remove(map);
        }

        internal static string LegacyRoadMap(string scenePath)
        {
            return scenePath switch
            {
                "Assets/Game/Scenes/OperationMaps/Variants/Frontier/PreparedEntities.unity" => "Frontier",
                "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_candidate.unity" => "DesertBase",
                StaticMapPresentationBaker.CurrentStagedOperationMapScenePath => "StagedDesertBase",
                _ => null
            };
        }

        internal static Material LegacyRoadMaterial(string map, MeshRenderer renderer, Mesh mesh, Material original)
        {
            if (map == null || !mesh || !original || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return null;
            var layer = ShoulderLayer(renderer, mesh, 0);
            if (layer != RoadLayer.Paved && layer != RoadLayer.Soil)
                return null;
            if (layer == RoadLayer.Soil)
            {
                LegacySoilArea(map, out var bounds);
                if (!bounds.Intersects(renderer.bounds)) return null;
            }
            var key = (map, original, layer);
            if (!LegacyMaterials.TryGetValue(key, out var replacement))
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                replacement = AssetDatabase.LoadAssetAtPath<Material>($"{ShoulderArt}/{map}_{guid}_{layer}.mat");
                // The bake can create a previously missing material later in this Editor session.
                if (replacement) LegacyMaterials.Add(key, replacement);
            }
            return replacement;
        }

        internal static void ApplyLegacyRoadMaterials(Scene scene)
        {
            string map = LegacyRoadMap(scene.path);
            if (map == null) return;
            foreach (var renderer in SceneRenderers(scene))
            {
                var replacement = LegacyRoadMaterial(map, renderer, renderer.GetComponent<MeshFilter>()?.sharedMesh, renderer.sharedMaterial);
                if (!replacement) continue;
                var slots = renderer.sharedMaterials;
                slots[0] = replacement;
                renderer.sharedMaterials = slots;
            }
        }
    }

    // Classic scene builds and editor play both receive the same first-slot binding.
    internal sealed class LegacyRoadBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report) => OtherMapSurfacePolish.ApplyLegacyRoadMaterials(scene);

        [InitializeOnLoadMethod]
        static void RegisterPlayScenes() => SceneManager.sceneLoaded += (scene, _) =>
        {
            if (Application.isPlaying) OtherMapSurfacePolish.ApplyLegacyRoadMaterials(scene);
        };
    }

    [BakingType]
    internal struct LegacyRoadBakeMaterial : IComponentData
    {
        public UnityObjectRef<Material> Original;
        public UnityObjectRef<Material> Replacement;
        public EntityId RendererId;
    }

    internal sealed class RoadSurfacePresentationBaker : Baker<MeshRenderer>
    {
        public override void Bake(MeshRenderer renderer)
        {
            string path = renderer.gameObject.scene.path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/Game/Scenes/OperationMaps/", System.StringComparison.Ordinal) ||
                !renderer.enabled || !renderer.gameObject.activeInHierarchy) return;
            var mesh = GetComponent<MeshFilter>()?.sharedMesh;
            if (!mesh) return;
            if (OtherMapSurfacePolish.IsRunwayRenderSurface(renderer)) BakeRunway(renderer, mesh);
            string map = OtherMapSurfacePolish.LegacyRoadMap(path);
            if (map == null) return;
            DependsOn(OtherMapSurfacePolish.LegacySoilArea(map, out _));
            var original = renderer.sharedMaterial;
            var replacement = OtherMapSurfacePolish.LegacyRoadMaterial(map, renderer, mesh, original);
            if (!replacement) return;
            DependsOn(mesh);
            DependsOn(original);
            DependsOn(replacement);
            AddComponent(GetEntity(TransformUsageFlags.None), new LegacyRoadBakeMaterial
            {
                Original = original, Replacement = replacement, RendererId = renderer.GetEntityId()
            });
        }

        void BakeRunway(MeshRenderer renderer, Mesh mesh)
        {
            DependsOn(mesh);
            using var data = Mesh.AcquireReadOnlyMeshData(mesh);
            var geometry = data[0];
            if (geometry.subMeshCount == 0 || geometry.GetSubMesh(0).topology != MeshTopology.Triangles) return;
            using var vertices = new NativeArray<Vector3>(geometry.vertexCount, Allocator.Temp);
            using var indices = new NativeArray<int>(geometry.GetSubMesh(0).indexCount, Allocator.Temp);
            geometry.GetVertices(vertices);
            geometry.GetIndices(indices,0);
            var entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
            var triangles = AddBuffer<RunwayRenderTriangle>(entity);
            var world = renderer.localToWorldMatrix;
            for (int i=0; i<indices.Length; i+=3)
            {
                var a=vertices[indices[i]]; var b=vertices[indices[i+1]]; var c=vertices[indices[i+2]];
                var normal=Vector3.Cross(world.MultiplyVector(b-a), world.MultiplyVector(c-a));
                if (Mathf.Abs(normal.y) < 0.65f*normal.magnitude || normal.sqrMagnitude<1e-10f) continue;
                triangles.Add(new RunwayRenderTriangle { A=a, B=b, C=c });
            }
        }

    }

    // Replace slot zero before Entities Graphics builds its material array. Single-slot
    // meshes render on their primary entity. A multi-slot renderer may have a child per slot.
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    internal partial class LegacyRoadMaterialBakingSystem : SystemBase
    {
        EntityQuery bindings;
        protected override void OnCreate()
        {
            bindings = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<LegacyRoadBakeMaterial>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
            RequireForUpdate(bindings);
        }

        protected override void OnUpdate()
        {
            using var entities = bindings.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var binding = EntityManager.GetComponentData<LegacyRoadBakeMaterial>(entity);
                if (Replace(entity, binding) || !EntityManager.HasBuffer<AdditionalEntitiesBakingData>(entity)) continue;
                var additional = EntityManager.GetBuffer<AdditionalEntitiesBakingData>(entity, true);
                foreach (var child in additional)
                    if (child.AuthoringComponentID == binding.RendererId && Replace(child.Value, binding)) break;
            }
        }

        bool Replace(Entity entity, LegacyRoadBakeMaterial binding)
        {
            if (!EntityManager.HasComponent<RenderMeshUnmanaged>(entity)) return false;
            var mesh = EntityManager.GetComponentData<RenderMeshUnmanaged>(entity);
            if (mesh.materialForSubMesh.Equals(binding.Replacement)) return true;
            if (!mesh.materialForSubMesh.Equals(binding.Original)) return false;
            mesh.materialForSubMesh = binding.Replacement;
            EntityManager.SetComponentData(entity, mesh);
            return true;
        }
    }
}
