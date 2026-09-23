#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishMapLayoutBuilder
    {
        public const string SharedLayoutPath = "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishLayout_DB_BA.asset";
        public const string ScenarioLayoutPath = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishLayout_S002.asset";
        public const string DesertBaseMapPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";

        [MenuItem("Tools/Warline/Skirmish/Rebuild Desert Base Layout")]
        public static void RebuildMenu() => Debug.Log(RebuildDesertBaseAssault());

        /// <summary>
        /// Recomputes the Desert Base world binding from the operation map's authored
        /// deployment anchors and world bounds, fits the across extent so every authored
        /// anchor and legal pad stays inside the map, and reports drift against the
        /// binding pinned in the layout. Writes nothing; rebuild the layout to adopt a
        /// corrected frame after review.
        /// </summary>
        [MenuItem("Tools/Warline/Skirmish/Derive Desert Base World Binding")]
        public static void DeriveBindingMenu() => Debug.Log(DeriveDesertBaseWorldBinding());

        public static string DeriveDesertBaseWorldBinding()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Derive world bindings in Edit mode.");

            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DesertBaseMapPath);
            if (map == null)
                throw new InvalidOperationException("Desert Base operation map definition missing at " + DesertBaseMapPath);
            if (!TryFindDeployment(map, 1, out Vector3 player) || !TryFindDeployment(map, 2, out Vector3 enemy))
                throw new InvalidOperationException("Desert Base deployment anchors are missing.");

            SkirmishLayoutWorldBindingConfig derived = DeriveBinding(
                player,
                enemy,
                map.Bounds.WorldMin,
                map.Bounds.WorldMax,
                SkirmishExpansionCatalogFactory.CreateInMemory().LayoutDbBa,
                out float fittedAcrossMetres);

            SkirmishLayoutWorldBindingConfig pinned = Game.Configs.SkirmishMapLayoutBuilder.DesertBaseWorldBinding;
            bool matches =
                Mathf.Abs(derived.OriginX - pinned.OriginX) < 0.5f &&
                Mathf.Abs(derived.OriginZ - pinned.OriginZ) < 0.5f &&
                Mathf.Abs(derived.ForwardX - pinned.ForwardX) < 0.001f &&
                Mathf.Abs(derived.ForwardZ - pinned.ForwardZ) < 0.001f &&
                Mathf.Abs(derived.ForwardMetres - pinned.ForwardMetres) < 0.5f &&
                Mathf.Abs(derived.AcrossMetres - pinned.AcrossMetres) < 0.5f;
            return "[SkirmishMapLayoutBuilder] deriveBinding origin=(" + derived.OriginX.ToString("F2") + "," +
                   derived.OriginZ.ToString("F2") + ") forward=(" + derived.ForwardX.ToString("F4") + "," +
                   derived.ForwardZ.ToString("F4") + ") forwardMetres=" + derived.ForwardMetres.ToString("F2") +
                   " acrossMetres=" + fittedAcrossMetres.ToString("F2") +
                   " pinnedMatch=" + (matches ? "1" : "0");
        }

        internal static SkirmishLayoutWorldBindingConfig DeriveBinding(
            Vector3 player,
            Vector3 enemy,
            Vector3 worldMin,
            Vector3 worldMax,
            SkirmishMapLayoutConfig layout,
            out float fittedAcrossMetres)
        {
            float dx = enemy.x - player.x;
            float dz = enemy.z - player.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            float forwardX = dx / distance;
            float forwardZ = dz / distance;
            // base.player/base.enemy sit at u 0.12/0.88 of the forward axis.
            float forwardMetres = distance / 0.76f;
            float originX = player.x - 0.12f * forwardMetres * forwardX;
            float originZ = player.z - 0.12f * forwardMetres * forwardZ;
            // v+ runs to the left of the forward axis (toward higher z for DB).
            float acrossX = -forwardZ;
            float acrossZ = forwardX;

            var binding = new SkirmishLayoutWorldBindingConfig
            {
                OriginX = originX,
                OriginZ = originZ,
                ForwardX = forwardX,
                ForwardZ = forwardZ,
                AcrossX = acrossX,
                AcrossZ = acrossZ,
                ForwardMetres = forwardMetres,
                AcrossMetres = 0f,
                PlayerAnchorId = "anchor.skirmish.desert_base_01.deployment.faction_1",
                EnemyAnchorId = "anchor.skirmish.desert_base_01.deployment.faction_2",
                SourceHash = "db.deploy-anchors.v1"
            };

            float envelopeAspect = layout.WorldDepthMetres / layout.WorldWidthMetres;
            float across = forwardMetres * envelopeAspect;
            while (across > 40f)
            {
                binding.AcrossMetres = across;
                if (AllContentInsideBounds(layout, binding, worldMin, worldMax))
                    break;
                across -= 5f;
            }

            fittedAcrossMetres = binding.AcrossMetres;
            return binding;
        }

        private static bool AllContentInsideBounds(
            SkirmishMapLayoutConfig layout,
            in SkirmishLayoutWorldBindingConfig binding,
            Vector3 worldMin,
            Vector3 worldMax)
        {
            foreach (SkirmishLayoutAnchorConfig anchor in layout.Anchors)
            {
                if (!TryProject(binding, anchor.NormalizedU, anchor.NormalizedV, out float x, out float z) ||
                    x < worldMin.x || x > worldMax.x || z < worldMin.z || z > worldMax.z)
                    return false;
            }

            foreach (SkirmishLegalPadConfig pad in layout.Pads)
            {
                float u = (pad.CenterX - layout.OriginX) / layout.WorldWidthMetres;
                float v = (pad.CenterZ - layout.OriginZ) / layout.WorldDepthMetres;
                if (!TryProject(binding, u, v, out float x, out float z))
                    return false;
                if (x - pad.WidthMetres * 0.5f < worldMin.x || x + pad.WidthMetres * 0.5f > worldMax.x ||
                    z - pad.DepthMetres * 0.5f < worldMin.z || z + pad.DepthMetres * 0.5f > worldMax.z)
                    return false;
            }

            return true;
        }

        private static bool TryProject(
            in SkirmishLayoutWorldBindingConfig binding,
            float u,
            float v,
            out float x,
            out float z)
        {
            x = binding.OriginX +
                u * binding.ForwardMetres * binding.ForwardX +
                (v - 0.5f) * binding.AcrossMetres * binding.AcrossX;
            z = binding.OriginZ +
                u * binding.ForwardMetres * binding.ForwardZ +
                (v - 0.5f) * binding.AcrossMetres * binding.AcrossZ;
            return binding.ForwardMetres > 0f && binding.AcrossMetres > 0f;
        }

        private static bool TryFindDeployment(OperationMapDefinition map, int factionId, out Vector3 position)
        {
            foreach (OperationMapAnchorConfig anchor in map.Anchors)
            {
                if (anchor.Kind == OperationMapAnchorKind.Deployment && anchor.FactionId == factionId)
                {
                    position = anchor.Position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public static string RebuildDesertBaseAssault()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Author measured layouts in Edit mode.");

            SkirmishExpansionAuthoredSet set = SkirmishExpansionCatalogFactory.CreateInMemory();
            var reasons = new List<SkirmishCompileReason>();
            if (!SkirmishMapLayoutValidation.TryValidateDesertBaseAssault(set.LayoutDbBa, reasons))
                throw new InvalidOperationException(reasons.Count == 0 ? "layout validation failed" : reasons[0].ToString());

            Persist(set.LayoutDbBa, SharedLayoutPath);
            var scenarioCopy = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            EditorUtility.CopySerialized(set.LayoutDbBa, scenarioCopy);
            Persist(scenarioCopy, ScenarioLayoutPath);
            AssetDatabase.SaveAssets();
            return "[SkirmishMapLayoutBuilder] result=Passed layout=layout.skirmish.db.ba";
        }

        private static T Persist<T>(T asset, string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
#endif
