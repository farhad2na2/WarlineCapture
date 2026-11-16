#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using System.IO;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Read-only, opt-in capture of the original S004 native JSON layout contract.</summary>
    public static class S004OriginalLayoutNativeJsonCapture
    {
        private const int Seed = 104733;

        public static void RunS004OriginalLayoutNativeJsonCapture()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog == null) throw new InvalidOperationException("Packaged expansion catalog missing.");
            SkirmishScenarioDefinitionConfig definition = null;
            foreach (var item in catalog.Definitions)
                if (item != null && item.CatalogId == "S004") { definition = item; break; }
            if (definition == null || definition.MapLayout == null)
                throw new InvalidOperationException("Actual authored packaged S004 definition/layout missing.");

            var layout = definition.MapLayout;
            Type projection = typeof(SkirmishMeasuredLayoutBinder).GetNestedType("LayoutValues", BindingFlags.NonPublic);
            MethodInfo from = projection?.GetMethod("From", BindingFlags.Public | BindingFlags.Static);
            if (projection == null || from == null) throw new InvalidOperationException("Original private LayoutValues.From projection unavailable.");
            object values = from.Invoke(null, new object[] { layout });
            string json = JsonUtility.ToJson(values);
            string fingerprint = SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout);
            var assembly = typeof(SkirmishMeasuredLayoutBinder).Assembly;
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string layoutPath = AssetDatabase.GetAssetPath(layout);
            string[] pinnedFiles = { definitionPath, layoutPath,
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishMeasuredLayoutBinder.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupMatrixTable.cs",
                "Assets/Game/Scripts/Skirmish/Contracts/SkirmishScenarioContracts.cs",
                "Assets/Game/Scripts/Editor/Skirmish/S004OriginalLayoutNativeJsonCapture.cs" };
            var sourceHashes = new Dictionary<string, string>();
            foreach (string path in pinnedFiles) sourceHashes[path] = HashFile(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)));
            string assemblyPath = assembly.Location;
            string assemblyHash = HashFile(assemblyPath);
            var captureAssembly = typeof(S004OriginalLayoutNativeJsonCapture).Assembly;
            string captureAssemblyPath = captureAssembly.Location;
            string captureAssemblyHash = HashFile(captureAssemblyPath);
            Debug.Log("[S004OriginalLayoutNativeJson] authored asset=" + definitionPath +
                      " assetGuid=" + AssetDatabase.AssetPathToGUID(definitionPath) + " layoutAsset=" + layoutPath +
                      " layoutGuid=" + AssetDatabase.AssetPathToGUID(layoutPath) +
                      " assembly=" + assembly.GetName().Name + " assemblyVersion=" + assembly.GetName().Version + " assemblyMvid=" + assembly.ManifestModule.ModuleVersionId +
                      " assemblyPath=" + assemblyPath + " assemblySha256=" + assemblyHash +
                      " captureAssembly=" + captureAssembly.GetName().Name + " captureAssemblyMvid=" + captureAssembly.ManifestModule.ModuleVersionId +
                      " captureAssemblyPath=" + captureAssemblyPath + " captureAssemblySha256=" + captureAssemblyHash + " projectionToken=" + from.MetadataToken +
                      " layout=" + layout.LayoutId + " format=" + layout.BindingValueFormat + " fingerprint=" + fingerprint + " json=" + json);
            foreach (var pair in sourceHashes) Debug.Log("[S004OriginalLayoutNativeJson] sourcePin path=" + pair.Key + " sha256=" + pair.Value);

            EmitSynthetic("null-empty", values, projection, "Anchors", null, "Routes", Array.Empty<SkirmishLayoutRouteConfig>(), "Pads", Array.Empty<SkirmishLegalPadConfig>());
            EmitSynthetic("signed-zero-floats-enums-nested-arrays", values, projection,
                "OriginX", -0f, "OriginZ", -0f,
                "Anchors", new[] { new SkirmishLayoutAnchorConfig { AnchorId = "synthetic", RoleId = "synthetic.role", NormalizedU = -0f, NormalizedV = 0.125f, WorldX = -0f, WorldZ = 1.25f } },
                "Routes", new[] { new SkirmishLayoutRouteConfig { RouteId = "synthetic.route", WaypointAnchorIds = new[] { "a", "b" }, Kind = SkirmishMeasuredRouteKind.FlankNorthRuins, LengthMetres = 12.5f, ProvisionalTimes = true } },
                "Pads", new[] { new SkirmishLegalPadConfig { PadId = "synthetic.pad", AnchorId = "synthetic", RoleId = "synthetic.role", Kind = SkirmishLegalPadKind.GroundStaging, CenterX = 2.5f, CenterZ = -0f, WidthMetres = 3f, DepthMetres = 4f, FactionId = 2 } });

            var matrixAsset = Resources.Load<TextAsset>(SkirmishSetupMatrixTable.PackagedResourceName);
            if (matrixAsset == null) throw new InvalidOperationException("Packaged setup matrix asset missing.");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                throw new InvalidOperationException("Packaged setup matrix unavailable: " + matrixError);
            string matrixPath = AssetDatabase.GetAssetPath(matrixAsset);
            sourceHashes[matrixPath] = HashFile(Path.GetFullPath(Path.Combine(Application.dataPath, "..", matrixPath)));
            using (var sha = SHA256.Create())
                Debug.Log("[S004OriginalLayoutNativeJson] compilerInput matrixPath=" + matrixPath +
                          " matrixGuid=" + AssetDatabase.AssetPathToGUID(matrixPath) +
                          " matrixSha256=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(matrixAsset.text))).Replace("-", "").ToLowerInvariant());
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            foreach (SkirmishSizeId size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                bool accepted = SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, Seed, manifest, matrix,
                    out SkirmishResolvedSetup setup, out List<SkirmishCompileReason> reasons);
                string outcome = accepted ? "accepted" : "rejected";
                string details = accepted
                    ? "setupHash=" + setup.SetupHash + " measuredLayoutBound=" + setup.MeasuredLayoutBound
                    : Format(reasons);
                Debug.Log("[S004OriginalLayoutNativeJson] compiler size=" + size + " difficulty=Regular seed=" + Seed + " outcome=" + outcome + " " + details);
            }
            foreach (var pair in sourceHashes)
                if (HashFile(Path.GetFullPath(Path.Combine(Application.dataPath, "..", pair.Key))) != pair.Value)
                    throw new InvalidOperationException("Pinned source or authored asset changed during capture: " + pair.Key);
            if (HashFile(assemblyPath) != assemblyHash) throw new InvalidOperationException("Loaded assembly file changed during capture.");
            if (HashFile(captureAssemblyPath) != captureAssemblyHash) throw new InvalidOperationException("Capture assembly file changed during capture.");
            Debug.Log("[S004OriginalLayoutNativeJson] result=Passed compilerCapture=Completed gameplay=NotRun world=NotCreated authoredInputsUnchanged=1 assemblyUnchanged=1");
        }

        private static void EmitSynthetic(string label, object original, Type type, params object[] pairs)
        {
            object fixture = Activator.CreateInstance(type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                field.SetValue(fixture, field.GetValue(original));
            for (int i = 0; i < pairs.Length; i += 2)
                type.GetField((string)pairs[i], BindingFlags.Instance | BindingFlags.Public).SetValue(fixture, pairs[i + 1]);
            Debug.Log("[S004OriginalLayoutNativeJson] synthetic=" + label + " json=" + JsonUtility.ToJson(fixture));
        }

        private static string Format(List<SkirmishCompileReason> reasons)
        {
            if (reasons == null || reasons.Count == 0) return "reasons=[]";
            var parts = new List<string>();
            foreach (var reason in reasons) parts.Add(reason.Code + ":" + reason.Field + ":" + reason.Detail);
            return "reasons=[" + string.Join(" | ", parts) + "]";
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
