using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    // Living effects for the runtime binding scene: flare stacks, smoke plumes, burning wrecks and
    // blowing dust. Particles cannot live in the packed entity scene, and the binding's map root must
    // stay renderer-free, so the effects get their own root in the binding scene.
    internal static class MapVariantAtmosphere
    {
        internal const string RootName = "Atmosphere";
        private const string MaterialFolder = "Assets/Game/Art/MapBeautify/FX";
        private const string Fx = "Assets/PolygonMilitary/Prefabs/FX/";
        private const string Fire = Fx + "FX_Fire_01.prefab";
        private const string FireSmall = Fx + "FX_Fire_Small_01.prefab";
        private const string SmokeLarge = Fx + "FX_Smoke_Large_01.prefab";
        private const string SmokeMedium = Fx + "FX_Smoke_Medium_01.prefab";
        private const string SmokeHuge = Fx + "FX_Smoke_Huge_01.prefab";
        private const string Dust = Fx + "FX_Dust_Blowing_Soft_Large_01.prefab";
        private const string ParticlesUnlit = "Universal Render Pipeline/Particles/Unlit";

        public static int Build(Scene binding, Scene source, string map, Rect focus)
        {
            if (!MapVariantBeautify.Supports(map)) return 0;
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, binding);
            var transforms = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            int count = 0;

            var stacks = transforms.Where(t => t.name.StartsWith("SM_Prop_Pipeline_SmokeStack_0", StringComparison.Ordinal))
                .Select(t => (t, top: Top(t))).Where(s => s.top.HasValue && focus.Contains(new Vector2(s.top.Value.x, s.top.Value.z)))
                .OrderBy(s => s.top.Value.x).ThenBy(s => s.top.Value.z).ToList();
            for (int i = 0; i < stacks.Count; i++)
            {
                Vector3 top = stacks[i].top.Value;
                if (i % 3 == 0)
                {
                    count += Spawn(root, Fire, "FlareFire", top, 3.2f);
                    count += Spawn(root, SmokeLarge, "FlareSmoke", top + Vector3.up * 4f, 2.6f);
                }
                else if (i % 3 == 1)
                    count += Spawn(root, SmokeMedium, "StackSmoke", top, 2.4f);
            }

            foreach (Transform t in transforms.Where(t => t.name.StartsWith("SM_Prop_Pipeline_SmokeStack_Background", StringComparison.Ordinal)))
            {
                Vector3? top = Top(t);
                if (top.HasValue) count += Spawn(root, SmokeHuge, "HorizonPlume", top.Value, 5f);
            }

            int wrecks = 0;
            foreach (Transform t in transforms.Where(t => t.name.StartsWith("SM_Prop_Vehicle_Debris", StringComparison.Ordinal)))
            {
                Vector3? top = Top(t);
                if (!top.HasValue || !focus.Contains(new Vector2(top.Value.x, top.Value.z)) || wrecks % 4 != 0) { wrecks++; continue; }
                wrecks++;
                count += Spawn(root, FireSmall, "WreckFire", top.Value, 1.6f);
                count += Spawn(root, SmokeMedium, "WreckSmoke", top.Value + Vector3.up, 1.6f);
            }

            var grounds = transforms.Where(t => t.name.StartsWith("Ground_", StringComparison.Ordinal) && t.GetComponent<MeshFilter>() != null).ToList();
            var colliders = grounds.Select(t => t.gameObject.AddComponent<MeshCollider>()).ToList();
            try
            {
                Physics.SyncTransforms();
                var random = new System.Random(7231);
                for (int i = 0; i < 7; i++)
                {
                    var p = new Vector2(focus.xMin + (float)random.NextDouble() * focus.width, focus.yMin + (float)random.NextDouble() * focus.height);
                    if (!Physics.Raycast(new Vector3(p.x, 500f, p.y), Vector3.down, out RaycastHit hit, 1000f)) continue;
                    count += Spawn(root, Dust, "BlowingDust", hit.point + Vector3.up * .5f, 2.2f, (float)random.NextDouble() * 360f);
                }
            }
            finally { foreach (var c in colliders) UnityEngine.Object.DestroyImmediate(c); }
            Debug.Log($"[MapBeautify] atmosphere map={map} effects={count} stacks={stacks.Count}");
            return count;
        }

        private static Vector3? Top(Transform t)
        {
            var renderers = t.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) return null;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        private static int Spawn(GameObject root, string prefabPath, string name, Vector3 position, float scale, float yaw = 0f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) ??
                         throw new InvalidOperationException("[MapBeautify] Missing effect " + prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = Vector3.one * scale;
            foreach (var c in instance.GetComponentsInChildren<Component>(true))
                if (c is Collider or Rigidbody or AudioSource) UnityEngine.Object.DestroyImmediate(c);
            foreach (ParticleSystem system in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                main.loop = true;
                main.playOnAwake = true;
                main.prewarm = true;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            foreach (ParticleSystemRenderer renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(Convert).ToArray();
                if (renderer.trailMaterial != null) renderer.trailMaterial = Convert(renderer.trailMaterial);
            }
            return 1;
        }

        // Synty ships these effects with built-in legacy particle shaders, which URP renders pink.
        private static readonly Dictionary<Material, Material> Converted = new();

        private static Material Convert(Material source)
        {
            if (source == null || source.shader == null) return source;
            string shader = source.shader.name;
            if (!shader.StartsWith("Legacy Shaders/Particles", StringComparison.Ordinal) && !shader.StartsWith("Particles/", StringComparison.Ordinal) &&
                shader != "Hidden/InternalErrorShader")
                return source;
            if (Converted.TryGetValue(source, out Material cached) && cached != null) return cached;
            MapVariantBuilder.EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{source.name}_URP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew) material = new Material(Shader.Find(ParticlesUnlit) ?? throw new InvalidOperationException("[MapBeautify] Missing " + ParticlesUnlit));
            bool additive = shader.Contains("Additive");
            bool dark = shader.Contains("Multiply");
            material.SetTexture("_BaseMap", source.mainTexture);
            material.SetColor("_BaseColor", dark ? new Color(0.24f, 0.22f, 0.21f, 0.8f) : additive ? Color.white : new Color(1f, 1f, 1f, 0.9f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            if (isNew) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            Converted[source] = material;
            return material;
        }
    }
}
