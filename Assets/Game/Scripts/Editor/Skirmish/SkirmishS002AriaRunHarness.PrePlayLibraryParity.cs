#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.Configs;
using Game.UI.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        // PROPOSAL: one named fragment only. No whole live_api admission.
        private static void RecordProductionPostProjectionLibraryParity(string selectedCatalog)
        {
            if (productionCanary == null || (selectedCatalog != "S002" && selectedCatalog != "S005")) return;
            var request = productionCanary.Request;
            if (request.IsLegacyPreset || request.CatalogId != selectedCatalog)
                throw new InvalidDataException("Exact expanded owned fragment subject required.");
            RequireStoppedParityEditor();
            var actual = SkirmishBattleCatalogConfig.Load();
            if (selectedCatalog == "S005" && (candidateCatalog == null || actual != candidateCatalog || candidateCatalogScope == null))
                throw new InvalidDataException("Actual already-created S005 candidate scope required; raw catalog is not equivalent.");
            string currentClaim = SkirmishProductionCanaryRequest.Canonical(SkirmishProductionCanaryRequest.ReadObject(request.ClaimPath));
            string immutablePath = Path.Combine(request.ArtifactDirectory, "claim.json");
            if (currentClaim != SkirmishProductionCanaryRequest.Canonical(request.Claim) ||
                currentClaim != SkirmishProductionCanaryRequest.Canonical(SkirmishProductionCanaryRequest.ReadObject(immutablePath)) ||
                SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(
                    SkirmishProductionCanaryRequest.ReadObject(request.JobPath)))) != request.JobHash)
                throw new InvalidDataException("Actual job/current/immutable claim changed before fragment observation.");
            var facts = MeasurePrePlayLibraryParityFragment(actual, selectedCatalog,
                (string)request.Job["definition_id"], (int)request.Job["definition_version"]);
            facts["job_sha256"] = request.JobHash;
            facts["attempt_id"] = request.Claim["attempt_id"].DeepClone();
            facts["worker"] = request.Claim["worker"].DeepClone();
            facts["job_file_sha256"] = SkirmishProductionCanaryRequest.FileHash(request.JobPath);
            facts["current_claim_sha256"] = SkirmishProductionCanaryRequest.FileHash(request.ClaimPath);
            facts["immutable_claim_sha256"] = SkirmishProductionCanaryRequest.FileHash(immutablePath);
            facts["candidate_sha256"] = request.Job["candidate_sha256"].DeepClone();
            facts["build_sha256"] = request.Job["build_sha256"].DeepClone();
            var modules = new JArray();
            foreach (var module in new[] { typeof(SkirmishS002AriaRunHarness).Module,
                typeof(Game.UI.Runtime.QuickCustomScreenView).Module, typeof(SkirmishMissionDispatch).Module }.Distinct())
            {
                string location = module.Assembly.Location;
                if (string.IsNullOrEmpty(location) || !File.Exists(location)) throw new InvalidDataException("Actual loaded module file unavailable.");
                modules.Add(new JObject { ["assembly"] = module.Assembly.FullName, ["mvid"] = module.ModuleVersionId.ToString("N"),
                    ["location"] = location, ["file_sha256"] = SkirmishProductionCanaryRequest.FileHash(location) });
            }
            facts["loaded_modules"] = modules;
            facts["dependency_closure"] = "Incomplete_NamedFragmentOnly";
            var sources = new JArray();
            foreach (string name in new[] { "Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.MenuProbe.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.PrePlayLibraryParity.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishMissionDispatch.cs",
                "Assets/Game/Scripts/UI/Screens/QuickCustomScreenView.Scenarios.cs" })
                sources.Add(new JObject { ["path"] = name, ["sha256"] = SkirmishProductionCanaryRequest.FileHash(Path.Combine(request.ProjectRoot, name)) });
            facts["collector_sources"] = sources;
            facts["original_wrapper_completion"] = "PendingExternalJoin";
            // A current wrapper cannot attest to its own future exit before Play.
            // This fragment is NOT a replacement for reusable completed pre-Play proof.
            if (SkirmishProductionCanaryRequest.FileHash(request.JobPath) != (string)facts["job_file_sha256"] ||
                SkirmishProductionCanaryRequest.FileHash(request.ClaimPath) != (string)facts["current_claim_sha256"] ||
                SkirmishProductionCanaryRequest.FileHash(immutablePath) != (string)facts["immutable_claim_sha256"])
                throw new InvalidDataException("Owned fragment input changed during observation.");
            RequireStoppedParityEditor();
            string output = Path.Combine(request.ArtifactDirectory, "preplay-library-parity-fragment.json");
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(facts.ToString(Newtonsoft.Json.Formatting.Indented));
            productionCanary.Record("PrePlayLibraryParityFragment", new JObject {
                ["path"] = output, ["sha256"] = SkirmishProductionCanaryRequest.FileHash(output),
                ["coverage"] = "PostProjectionCodedLibraryParity-v1", ["live_api_admitted"] = false });
        }

        private static void RequireStoppedParityEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Fragment requires stopped stable Editor before Play.");
        }

        internal static JObject MeasurePrePlayLibraryParityFragment(SkirmishBattleCatalogConfig actual,
            string id, string definition, int version)
        {
            RequireStoppedParityEditor();
            if ((id != "S002" && id != "S005") || actual == null || !actual.TryGet(id, out var row))
                throw new InvalidDataException("Named S002/S005 actual catalog fragment only.");
            var before = JToken.FromObject(actual.Entries);
            // Reuse the exact projection and pure selection API exercised by the existing focused controls.
            var projected = row;
            SkirmishExpandedCopyProjection.ApplyLibraryCopy(ref projected);
            var method = typeof(Game.UI.Runtime.QuickCustomScreenView).GetMethod("LibrarySelection",
                BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SkirmishBattleCatalogEntry) }, null);
            if (method == null || method.ReturnType != typeof(UiSkirmishLibrarySelection))
                throw new InvalidDataException("Actual loaded library selection API differs.");
            var selected = (UiSkirmishLibrarySelection)method.Invoke(null, new object[] { projected });
            if (selected.Kind != UiSkirmishLibraryLaunchKind.ExpandedDefinition || selected.CatalogId != id ||
                selected.DefinitionId != definition || selected.ContentVersion != version)
                throw new InvalidDataException("Actual UI selection differs from exact requested definition/version.");
            bool Accept(SkirmishBattleCatalogConfig catalog, string d, int v, int index) =>
                SkirmishMissionDispatch.TryGetSelectedExpanded(catalog, id, d, v, index, out _);
            int controls = 0;
            void Check(bool okay) { if (!okay) throw new InvalidDataException("Post-projection parity semantic control failed: " + controls); controls++; }
            Check(Accept(actual, definition, version, row.PlayableScenarioIndex));
            Check(!Accept(actual, "skirmish.foreign", version, row.PlayableScenarioIndex));
            Check(!Accept(actual, definition, version == 1 ? 2 : 1, row.PlayableScenarioIndex));
            Check(!Accept(actual, definition, version, row.PlayableScenarioIndex + 1));
            var owned = ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
            owned.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                owned.Configure(new[] { row, row });
                Check(!Accept(owned, definition, version, row.PlayableScenarioIndex));
                var planned = row; planned.Status = SkirmishBattleCatalogStatus.Planned; planned.PlayableScenarioIndex = -1;
                owned.Configure(new[] { planned });
                Check(!Accept(owned, definition, version, row.PlayableScenarioIndex));
            }
            finally { UnityEngine.Object.DestroyImmediate(owned); }
            Check(JToken.DeepEquals(before, JToken.FromObject(actual.Entries)));
            RequireStoppedParityEditor();
            return new JObject { ["schema_version"] = 1, ["coverage"] = "PostProjectionCodedLibraryParity-v1",
                ["result"] = "MeasuredLibraryParityFragment", ["catalog_id"] = id, ["definition_id"] = definition,
                ["definition_version"] = version, ["playable_index"] = row.PlayableScenarioIndex,
                ["controls"] = controls, ["candidate_rows_unchanged"] = true,
                ["catalog_rows_sha256"] = SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(
                    SkirmishProductionCanaryRequest.Canonical(before))),
                ["editor_play_stopped"] = true, ["live_api_admitted"] = false, ["qualified"] = false,
                ["queue_success_enabled"] = false, ["mission_ready"] = false, ["publication_changed"] = false,
                ["sampling_coverage_credit"] = 0 };
        }
    }
}
#endif
