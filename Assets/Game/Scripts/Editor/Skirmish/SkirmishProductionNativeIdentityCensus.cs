// Import with the pure SkirmishProductionCanaryRequest serializer only.
// No harness seam, Play session, catalog publication or gameplay mutation.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishProductionNativeIdentityCensus
    {
        // Additive Edit-mode census only; never enters Play or changes publication.
        public static void ExportReviewedMatrixSliceFromEnvironment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Native compiled census requires idle Edit mode.");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string requestPath = CensusEvidencePath(root, Environment.GetEnvironmentVariable("WARLINE_NATIVE_IDENTITY_REQUEST"));
            string requestSha = Environment.GetEnvironmentVariable("WARLINE_NATIVE_IDENTITY_REQUEST_SHA256");
            string output = CensusEvidencePath(root, Environment.GetEnvironmentVariable("WARLINE_NATIVE_IDENTITY_OUTPUT"));
            CensusNeed(!File.Exists(output) && !string.Equals(output, requestPath, StringComparison.OrdinalIgnoreCase),
                "Fresh distinct census output required.");
            CensusNeed(System.Text.RegularExpressions.Regex.IsMatch(requestSha ?? "", "^[a-f0-9]{64}$") &&
                SkirmishProductionCanaryRequest.FileHash(requestPath) == requestSha, "Reviewed request SHA differs.");
            var request = SkirmishProductionCanaryRequest.ReadObject(requestPath);
            CensusExact(request, "schema_version sampling_plan_path sampling_plan_sha256 candidate_manifest_path candidate_manifest_sha256 slots");
            CensusNeed(request["schema_version"]?.Type == JTokenType.Integer && (int)request["schema_version"] == 1,
                "Unknown census request schema.");
            string planPath = CensusEvidencePath(root, (string)request["sampling_plan_path"]);
            var authority = new SkirmishProductionMatrixAuthority(root, planPath, (string)request["sampling_plan_sha256"]);
            var slots = request["slots"] as JArray;
            CensusNeed(slots != null && slots.Count > 0 && slots.Count <= 1200 && slots.All(v => v is JObject),
                "Finite nonempty census slice required.");
            foreach (JObject slot in slots)
                CensusExact(slot, "catalog_id seed locale size difficulty control_mode packet_path packet_sha256");
            authority.ValidateBatch(slots.Cast<JObject>());
            string snapshotPath = CensusEvidencePath(root, (string)request["candidate_manifest_path"]);
            string snapshotSha = (string)request["candidate_manifest_sha256"];
            CensusNeed(System.Text.RegularExpressions.Regex.IsMatch(snapshotSha ?? "", "^[a-f0-9]{64}$") &&
                SkirmishProductionCanaryRequest.FileHash(snapshotPath) == snapshotSha, "Immutable snapshot SHA differs.");
            var snapshot = SkirmishProductionCanaryRequest.ReadObject(snapshotPath);
            CensusExact(snapshot, "source_manifest candidate_sha256 build_manifest build_sha256");
            SkirmishProductionBatchManifest.Validate(snapshot);
            CheckCensusNativePins(root, snapshot);
            var catalog = SkirmishExpansionCatalogConfig.Load();
            CensusNeed(catalog != null && ReferenceEquals(catalog, Resources.Load<SkirmishExpansionCatalogConfig>(
                SkirmishExpansionCatalogConfig.ResourceName)), "Persistent normal catalog required; no validation override.");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                throw new InvalidDataException(matrixError);
            string previousLocale = GameLocalization.CurrentLocaleCode;
            var rows = new JArray();
            try
            {
                foreach (JObject slot in slots)
                {
                    string id = (string)slot["catalog_id"];
                    CensusNeed(!SkirmishLegacyProductionIdentity.IsProtected(id),
                        "Protected practice/counterpart needs its separate schema2/3 authority; not exported by this entry.");
                    CensusNeed(catalog.TryGetDefinition(id, out var definition) && definition != null &&
                        EditorUtility.IsPersistent(definition), "Actual persistent catalog definition missing: " + id);
                    string definitionPath = AssetDatabase.GetAssetPath(definition);
                    CensusNeed(!string.IsNullOrEmpty(definitionPath) &&
                        AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(definitionPath) == definition,
                        "Catalog/asset identity differs: " + id);
                    var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
                    if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard,
                        (int)slot["seed"], manifest, matrix, out var setup, out var reasons))
                        throw new InvalidDataException(id + ": actual native compile failed: " + string.Join(";", reasons));
                    string targetLocale = (string)slot["locale"] == "EN" ? "en" : "fa-IR";
                    CensusNeed(GameLocalization.SetLocale(targetLocale, false) && GameLocalization.CurrentLocaleCode == targetLocale,
                        "Actual native locale did not resolve: " + id);
                    var identity = SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,
                        setup.DefinitionId, setup.ContentVersion, setup.DifficultyId.ToString(), setup.SizeId.ToString(),
                        setup.Seed, GameLocalization.CurrentLocaleCode, setup.SetupHash, setup.ContentHash);
                    CensusNeed((string)identity["catalog_id"] == id && (int)identity["seed"] == (int)slot["seed"] &&
                        (string)identity["locale"] == (string)slot["locale"] &&
                        (string)identity["size"] == (string)slot["size"] &&
                        (string)identity["difficulty"] == (string)slot["difficulty"], "Actual compiled slot differs.");
                    rows.Add(new JObject {
                        ["schema_version"] = 1, ["collector_kind"] = "SkirmishNativeCompiledSetupCensus",
                        ["slot"] = slot.DeepClone(), ["native_setup_identity"] = identity,
                        ["actual_native_locale"] = GameLocalization.CurrentLocaleCode,
                        ["native_config_sha256"] = SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                        ["definition_asset_path"] = definitionPath,
                        ["definition_asset_sha256"] = SkirmishProductionCanaryRequest.FileHash(Path.Combine(root, definitionPath)) });
                }
            }
            finally
            {
                CensusNeed(GameLocalization.SetLocale(previousLocale, false) && GameLocalization.CurrentLocaleCode == previousLocale,
                    "Native census failed to restore previous locale.");
            }
            authority.Recheck();
            CensusNeed(SkirmishProductionCanaryRequest.FileHash(requestPath) == requestSha &&
                SkirmishProductionCanaryRequest.FileHash(snapshotPath) == snapshotSha, "Census inputs changed.");
            CheckCensusNativePins(root, snapshot);
            var envelope = new JObject {
                ["schema_version"] = 1, ["collector_kind"] = "SkirmishMatrixCompiledSetupCensus",
                ["request_path"] = requestPath, ["request_sha256"] = requestSha,
                ["candidate_manifest_path"] = snapshotPath, ["candidate_manifest_sha256"] = snapshotSha,
                ["candidate_sha256"] = snapshot["candidate_sha256"].DeepClone(),
                ["build_sha256"] = snapshot["build_sha256"].DeepClone(),
                ["sampling_plan_path"] = planPath, ["sampling_plan_sha256"] = request["sampling_plan_sha256"].DeepClone(),
                ["locale_before"] = previousLocale, ["locale_after"] = GameLocalization.CurrentLocaleCode,
                ["native_compiled_setups"] = rows, ["gameplay_certified"] = false, ["queue_success_enabled"] = false };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(envelope.ToString(Formatting.Indented));
            Debug.Log("[SkirmishMatrixCompiledSetupCensus] result=Passed slots=" + rows.Count +
                " gameplayCertified=0 queueSuccessEnabled=0 output=" + output);
        }


        // Pure persisted setup export. No dispatch, private availability, fit authority or publication gate.
        public static void ExportPersistedTypedConfigDigests()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Idle native Edit mode required.");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string requestPath=CensusEvidencePath(root,Environment.GetEnvironmentVariable("WARLINE_TYPED_CONFIG_REQUEST"));
            string requestSha=Environment.GetEnvironmentVariable("WARLINE_TYPED_CONFIG_REQUEST_SHA256");
            string output=CensusEvidencePath(root,Environment.GetEnvironmentVariable("WARLINE_TYPED_CONFIG_OUTPUT"));
            CensusNeed(Directory.Exists(Path.GetDirectoryName(output))&&!File.Exists(output)&&!Directory.Exists(output)&&output!=requestPath,"Fresh distinct output/parent required.");
            CensusNeed(System.Text.RegularExpressions.Regex.IsMatch(requestSha??"","^[a-f0-9]{64}$")&&SkirmishProductionCanaryRequest.FileHash(requestPath)==requestSha,"Exact request required.");
            var request=SkirmishProductionCanaryRequest.ReadObject(requestPath);
            CensusExact(request,"schema_version catalog_id seed locale candidate_manifest_path candidate_manifest_sha256");
            CensusNeed(request["schema_version"]?.Type==JTokenType.Integer&&(int)request["schema_version"]==1&&
                request["seed"]?.Type==JTokenType.Integer&&(int)request["seed"]==155926&&
                request["locale"]?.Type==JTokenType.String&&(string)request["locale"]=="en","Exact schema/seed155926/native EN scope.");
            string id=(string)request["catalog_id"];
            CensusNeed(System.Text.RegularExpressions.Regex.IsMatch(id??"","^S(00[1-9]|0[1-9][0-9]|1[01][0-9]|120)$"),"Canonical catalog required.");
            string snapshotPath=CensusEvidencePath(root,(string)request["candidate_manifest_path"]);
            string snapshotSha=(string)request["candidate_manifest_sha256"];
            CensusNeed(System.Text.RegularExpressions.Regex.IsMatch(snapshotSha??"","^[a-f0-9]{64}$")&&
                SkirmishProductionCanaryRequest.FileHash(snapshotPath)==snapshotSha,"Exact source/build snapshot required.");
            var snapshot=SkirmishProductionCanaryRequest.ReadObject(snapshotPath);
            CensusExact(snapshot,"source_manifest candidate_sha256 build_manifest build_sha256");
            SkirmishProductionBatchManifest.Validate(snapshot);CheckCensusNativePins(root,snapshot);
            var catalog=SkirmishExpansionCatalogConfig.Load();
            SkirmishScenarioDefinitionConfig definition=null;
            CensusNeed(catalog!=null&&ReferenceEquals(catalog,Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName))&&
                catalog.TryGetDefinition(id,out definition)&&definition!=null&&EditorUtility.IsPersistent(definition),"Persistent normal definition required.");
            string asset=AssetDatabase.GetAssetPath(definition);
            CensusNeed(!string.IsNullOrEmpty(asset)&&AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(asset)==definition,"Actual packaged asset identity.");
            CensusNeed(definition.CampaignMap!=null&&definition.CampaignMap.TryValidateMetadata(out _)&&
                definition.CampaignMap.TryValidateLocalContentReferences(out _),"Actual Campaign source required.");
            string assetSha=SkirmishProductionCanaryRequest.FileHash(Path.Combine(root,asset));
            bool dirty=EditorUtility.IsDirty(definition);
            if(!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error))throw new InvalidDataException(error);
            string previous=GameLocalization.CurrentLocaleCode;var rows=new JArray();
            try
            {
                CensusNeed(GameLocalization.SetLocale("en",false)&&GameLocalization.CurrentLocaleCode=="en","Actual EN locale.");
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
                    if(!SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,size,155926,manifest,matrix,out var setup,out var reasons))
                        throw new InvalidDataException("Actual persisted normal compile refused "+id+"/"+size+": "+string.Join(";",reasons));
                    CensusNeed(setup.CatalogId==id&&setup.DefinitionId==definition.DefinitionId&&setup.ContentVersion==definition.ContentVersion&&
                        setup.Seed==155926&&setup.SizeId==size&&setup.DifficultyId==SkirmishDifficultyId.Regular&&
                        setup.MeasuredLayoutBound&&setup.SetupHash==SkirmishSetupCompiler.ComputeHash(setup,setup.Seed),"Actual source-bound setup required.");
                    CensusNeed(global::Game.Runtime.SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(definition,setup,out var logical,out var logicalError),logicalError);
                    string raw=JsonUtility.ToJson(setup);
                    rows.Add(new JObject{
                        ["CatalogId"]=setup.CatalogId,["DefinitionId"]=setup.DefinitionId,["ContentVersion"]=setup.ContentVersion,
                        ["Difficulty"]=setup.DifficultyId.ToString(),["Size"]=setup.SizeId.ToString(),["Seed"]=setup.Seed,
                        ["Locale"]=GameLocalization.CurrentLocaleCode,["SetupHash"]=setup.SetupHash.ToString("X8",System.Globalization.CultureInfo.InvariantCulture),
                        ["ConfigDigest"]=SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(raw)),
                        ["ConfigDigestAlgorithm"]="UnityJsonResolvedSetupUtf8Sha256-v1",["ResolvedSetupJson"]=raw,
                        ["LegacyCensusConfigDigest"]=SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                        ["LayoutBindingFingerprint"]=setup.LayoutBindingFingerprint,
                        ["MissionLogicalBindingFingerprint"]=logical.Fingerprint,["ViewFingerprint"]=logical.ViewFingerprint,
                        ["MissionLogicalBindingJson"]=JsonUtility.ToJson(definition.MissionLogicalBinding),
                        ["ResolvedViewJson"]=JsonUtility.ToJson(logical.View),
                        ["LayoutSemanticFingerprint"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout),
                        ["PhysicalOperationMapId"]=definition.CampaignMap.OperationMapId,
                        ["SourceIdentityHash"]=definition.CampaignMap.SourceIdentityHash,["SourceContentHash"]=definition.CampaignMap.ContentHash,
                        ["FullTupleNativeFitPassed"]=false,["PrivateLaunchAuthorized"]=false});
                }
            }
            finally { CensusNeed(GameLocalization.SetLocale(previous,false)&&GameLocalization.CurrentLocaleCode==previous,"Actual locale restoration."); }
            CensusNeed(EditorUtility.IsDirty(definition)==dirty&&SkirmishProductionCanaryRequest.FileHash(Path.Combine(root,asset))==assetSha,"Persisted definition changed.");
            CensusNeed(SkirmishProductionCanaryRequest.FileHash(requestPath)==requestSha&&SkirmishProductionCanaryRequest.FileHash(snapshotPath)==snapshotSha,"Inputs changed.");
            CheckCensusNativePins(root,snapshot);
            var report=new JObject{["schema_version"]=1,["collector_kind"]="PersistedTypedConfigDigestExport-v1",
                ["request_path"]=requestPath,["request_sha256"]=requestSha,["candidate_manifest_path"]=snapshotPath,["candidate_manifest_sha256"]=snapshotSha,
                ["candidate_sha256"]=snapshot["candidate_sha256"].DeepClone(),["build_sha256"]=snapshot["build_sha256"].DeepClone(),
                ["definition_asset_path"]=asset,["definition_asset_sha256"]=assetSha,["definition_asset_guid"]=AssetDatabase.AssetPathToGUID(asset),
                ["locale_before"]=previous,["locale_after"]=GameLocalization.CurrentLocaleCode,["entries"]=rows,
                ["FullTupleNativeFitPassed"]=false,["PrivateLaunchAuthorized"]=false,["missionAccepted"]=false};
            using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(report.ToString(Formatting.Indented));
            Debug.Log("[PersistedTypedConfigDigestExport] result=Passed rows=3 privateAuthorized=0 fullTupleFit=0 output="+output);
        }

        private static void CheckCensusNativePins(string root, JObject snapshot)
        {
            CensusNeed(SkirmishCandidateSourceHash.Compute(root) == (string)snapshot["candidate_sha256"],
                "Actual complete source differs from frozen census candidate.");
            var folder = Path.Combine(root, "Library", "ScriptAssemblies");
            var builds = new JArray(Directory.GetFiles(folder, "*.dll").OrderBy(p => p, StringComparer.Ordinal).Select(p =>
                new JObject { ["path"] = p.Substring(root.Length + 1).Replace('\\', '/'),
                    ["sha256"] = SkirmishProductionCanaryRequest.FileHash(p) }));
            CensusNeed(builds.Count > 0 && JToken.DeepEquals(builds, snapshot["build_manifest"]) &&
                SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(
                    SkirmishProductionCanaryRequest.Canonical(builds))) == (string)snapshot["build_sha256"],
                "Actual compiled DLL census differs.");
        }
        private static string CensusEvidencePath(string root, string path)
        {
            CensusNeed(!string.IsNullOrEmpty(path) && Path.IsPathRooted(path), "Absolute census evidence path required.");
            string full = Path.GetFullPath(path);
            string allowed = Path.GetFullPath(Path.Combine(root, "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
            CensusNeed(full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase), "Census evidence must remain under Logs/SkirmishProduction.");
            for (var current = new FileInfo(full) as FileSystemInfo; current != null;
                current = current is FileInfo ? ((FileInfo)current).Directory : ((DirectoryInfo)current).Parent)
                if (current.Exists) CensusNeed((current.Attributes & FileAttributes.ReparsePoint) == 0, "Reparse census path rejected.");
            return full;
        }
        private static void CensusExact(JObject value, string fields)
            => CensusNeed(value != null && value.Properties().Select(p => p.Name).OrderBy(p => p, StringComparer.Ordinal)
                .SequenceEqual(fields.Split(' ').OrderBy(p => p, StringComparer.Ordinal)), "Missing/unknown census fields.");
        private static void CensusNeed(bool condition, string reason)
        { if (!condition) throw new InvalidDataException(reason); }

        public static void ExportS005RegularStandard155926Fa()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Native compiled census requires idle Edit mode.");
            const string path = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset";
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(path);
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (definition == null || catalog == null || !catalog.TryGetDefinition("S005", out var packaged) || packaged != definition)
                throw new InvalidDataException("Exact packaged native S005 asset/registry binding is missing.");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error)) throw new InvalidDataException(error);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard,
                155926, manifest, matrix, out var setup, out var reasons))
                throw new InvalidDataException("Actual packaged S005 compile failed: " + string.Join(";", reasons));
            string previousLocale = GameLocalization.CurrentLocaleCode;
            JObject envelope;
            try
            {
                // Temporary native localization context only; persist=false never
                // writes PlayerPrefs. Actual applied locale is independently read.
                if (!GameLocalization.SetLocale(GameLocalization.PersianLocaleCode, false) ||
                    GameLocalization.CurrentLocaleCode != GameLocalization.PersianLocaleCode)
                    throw new InvalidDataException("Native FA locale did not resolve exactly.");
                var identity = SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,
                    setup.DefinitionId, setup.ContentVersion, setup.DifficultyId.ToString(), setup.SizeId.ToString(),
                    setup.Seed, GameLocalization.CurrentLocaleCode, setup.SetupHash, setup.ContentHash);
                envelope = new JObject { ["schema_version"] = 1, ["collector_kind"] = "SkirmishNativeCompiledSetupCensus",
                    ["native_setup_identity"] = identity, ["actual_native_locale"] = GameLocalization.CurrentLocaleCode,
                    ["native_config_sha256"] = SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup) };
            }
            finally
            {
                if (!GameLocalization.SetLocale(previousLocale, false) || GameLocalization.CurrentLocaleCode != previousLocale)
                    throw new InvalidDataException("Native census failed to restore previous locale.");
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Environment.GetEnvironmentVariable("WARLINE_NATIVE_IDENTITY_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) output = Path.Combine(root, "Logs", "SkirmishProduction", "NativeIdentity",
                "s005-regular-standard-155926-fa-" + Guid.NewGuid().ToString("N") + ".json");
            output = Path.GetFullPath(output);
            foreach (string tree in new[] { "Assets", "Packages", "ProjectSettings" })
            {
                string directory = Path.Combine(root, tree);
                if (string.Equals(output, directory, StringComparison.OrdinalIgnoreCase) ||
                    output.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Census output cannot enter source trees.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(envelope.ToString(Formatting.Indented));
            Debug.Log("[SkirmishProductionNativeIdentityCensus] result=Passed catalog=S005 seed=155926 locale=FA nativeSetupHash=" +
                setup.SetupHash.ToString("X8") + " output=" + output + " gameplayCertified=0");
        }
    }
}
#endif
