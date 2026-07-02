using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-time (reversible) scene tool that swaps Synty ground-family tile renderers
/// to the Game/Environment/GroundMacroVariation shader so the ground gets
/// world-space macro variation while every prop keeps the untouched atlas material.
///
/// The runtime on/off checkbox lives in VisualQualityConfig.asset
/// (enableGroundVariation) and is applied by VisualQualitySettingsSystem via the
/// _GroundVariationDisabled global float - no material swapping happens at runtime.
/// </summary>

namespace Game.Editor
{
    public static class GroundVariationMaterialSwapTool
    {
        private const string ShaderName = "Game/Environment/GroundMacroVariation";
        private const string TemplateMaterialGuid = "d90068bfe6ce4c78a2d95bf6fd876882";
        private const string GeneratedFolder = "Assets/Game/Rendering/Materials/Generated";

        // Mesh name families treated as "ground". Extend as needed.
        private static readonly string[] GroundMeshPrefixes =
        {
            "SM_Env_Ground",
            "SM_Env_Sand",
            "SM_Env_DirtRoad",
            "SM_Env_Road",
            "SM_Env_Sidewalk",
            "SM_Env_Grass",
            "SM_Env_Port_Concrete_Slab",
            "SM_Env_Mountain",
            "SM_Env_Runway",
            "SM_Env_Beach",
        };

        [MenuItem("Tools/Game/Rendering/Ground Variation/Apply To Open Scene")]
        public static void Apply()
        {
            Shader groundShader = Shader.Find(ShaderName);
            if (groundShader == null)
            {
                Debug.LogError($"GroundVariationMaterialSwapTool: shader '{ShaderName}' not found.");
                return;
            }

            var variationByOriginal = new Dictionary<Material, Material>();
            int swappedSlots = 0;
            int touchedRenderers = 0;

            foreach (MeshRenderer renderer in EnumerateGroundRenderers())
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material original = materials[i];
                    if (original == null || original.shader == groundShader)
                        continue;
                    if (GetSourceAtlas(original) == null)
                        continue;

                    if (!variationByOriginal.TryGetValue(original, out Material variation))
                    {
                        variation = GetOrCreateVariationMaterial(original, groundShader);
                        variationByOriginal.Add(original, variation);
                    }

                    if (variation == null)
                        continue;

                    materials[i] = variation;
                    changed = true;
                    swappedSlots++;
                }

                if (changed)
                {
                    Undo.RecordObject(renderer, "Apply Ground Macro Variation");
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    touchedRenderers++;
                }
            }

            if (touchedRenderers > 0)
                EditorSceneManager.MarkAllScenesDirty();

            Debug.Log($"GroundVariationMaterialSwapTool: swapped {swappedSlots} material slot(s) on {touchedRenderers} renderer(s) using {variationByOriginal.Count} variation material(s).");
        }

        [MenuItem("Tools/Game/Rendering/Ground Variation/Revert Open Scene")]
        public static void Revert()
        {
            Shader groundShader = Shader.Find(ShaderName);
            if (groundShader == null)
                return;

            int revertedSlots = 0;
            int touchedRenderers = 0;

            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || material.shader != groundShader)
                        continue;

                    Material original = FindOriginalMaterial(material);
                    if (original == null)
                    {
                        Debug.LogWarning($"GroundVariationMaterialSwapTool: could not resolve original material for '{material.name}' on '{renderer.name}'.", renderer);
                        continue;
                    }

                    materials[i] = original;
                    changed = true;
                    revertedSlots++;
                }

                if (changed)
                {
                    Undo.RecordObject(renderer, "Revert Ground Macro Variation");
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    touchedRenderers++;
                }
            }

            if (touchedRenderers > 0)
                EditorSceneManager.MarkAllScenesDirty();

            Debug.Log($"GroundVariationMaterialSwapTool: reverted {revertedSlots} material slot(s) on {touchedRenderers} renderer(s).");
        }

        private static IEnumerable<MeshRenderer> EnumerateGroundRenderers()
        {
            foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                    continue;

                string meshName = filter.sharedMesh.name;
                for (int i = 0; i < GroundMeshPrefixes.Length; i++)
                {
                    if (meshName.StartsWith(GroundMeshPrefixes[i], System.StringComparison.Ordinal))
                    {
                        yield return renderer;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the atlas texture across the shaders used by the Synty packs:
        /// URP Lit (_BaseMap), Synty Generic_Basic shadergraph (_Albedo_Map),
        /// and legacy/standard (_MainTex).
        /// </summary>
        private static Texture GetSourceAtlas(Material material)
        {
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                return material.GetTexture("_BaseMap");
            if (material.HasProperty("_Albedo_Map") && material.GetTexture("_Albedo_Map") != null)
                return material.GetTexture("_Albedo_Map");
            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
                return material.GetTexture("_MainTex");
            return null;
        }

        private static Material GetOrCreateVariationMaterial(Material original, Shader groundShader)
        {
            Material template = LoadTemplateMaterial();
            Texture originalAtlas = GetSourceAtlas(original);

            // The hand-authored template already points at the PolygonMilitary atlas;
            // reuse it whenever the source material uses the same atlas.
            if (template != null && template.GetTexture("_BaseMap") == originalAtlas)
                return template;

            // Different atlas (other Synty pack, alt colors, ...): generate a sibling
            // material that keeps the source atlas/color but uses the ground shader.
            string assetPath = $"{GeneratedFolder}/GroundVariation_{original.name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                Directory.CreateDirectory(GeneratedFolder);
                AssetDatabase.Refresh();
            }

            Material variation = template != null ? new Material(template) : new Material(groundShader);
            variation.shader = groundShader;
            variation.name = $"GroundVariation_{original.name}";
            variation.SetTexture("_BaseMap", originalAtlas);
            if (original.HasProperty("_BaseColor"))
                variation.SetColor("_BaseColor", original.GetColor("_BaseColor"));
            else if (original.HasProperty("_Color"))
                variation.SetColor("_BaseColor", original.GetColor("_Color"));
            if (original.HasProperty("_Smoothness"))
                variation.SetFloat("_Smoothness", original.GetFloat("_Smoothness"));

            AssetDatabase.CreateAsset(variation, assetPath);
            return variation;
        }

        private static Material FindOriginalMaterial(Material variation)
        {
            // Generated materials are named "GroundVariation_<OriginalName>"; the
            // template maps back to the Synty material sharing its atlas texture.
            const string prefix = "GroundVariation_";
            string originalName = variation.name.StartsWith(prefix, System.StringComparison.Ordinal)
                ? variation.name.Substring(prefix.Length)
                : null;

            Texture atlas = variation.GetTexture("_BaseMap");

            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var candidate = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (candidate == null || candidate.shader == variation.shader)
                    continue;
                if (GetSourceAtlas(candidate) != atlas)
                    continue;

                if (originalName != null)
                {
                    if (candidate.name == originalName)
                        return candidate;
                }
                else if (candidate.name.StartsWith("PolygonMilitary_Mat_01", System.StringComparison.Ordinal))
                {
                    // Template case: prefer the default PolygonMilitary atlas material.
                    return candidate;
                }
            }

            return null;
        }

        private static Material LoadTemplateMaterial()
        {
            string path = AssetDatabase.GUIDToAssetPath(TemplateMaterialGuid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
