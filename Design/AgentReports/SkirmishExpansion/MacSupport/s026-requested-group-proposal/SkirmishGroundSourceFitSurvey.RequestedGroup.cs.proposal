#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Proposal only: add as a new partial after review. Existing Run is unchanged.
    public static partial class SkirmishGroundSourceFitSurvey
    {
        static string ParseRequestedSourceSurveyGroup(string text)
        {
            Require(!string.IsNullOrWhiteSpace(text) && text.Length <= 128, "One requested source group required");
            var groups = text.Split(',').Select(v => v.Trim()).ToArray();
            Require(groups.All(v => new[] { "CC", "AP", "MP", "IB" }.Contains(v)) &&
                groups.Distinct(StringComparer.Ordinal).Count() == groups.Length && groups.Length == 1,
                "Exactly one known, nonempty, unique source group required");
            return groups[0];
        }

        static JObject NewRequestedSourceSurveyReport(string group)
        {
            return new JObject { ["schema"] = 1, ["kind"] = "FreshRequestedSourceGroupSurvey-v1",
                ["scope"] = "RequestedSingleSourceGroup_NewGeneration_Unqualified",
                ["requestedGroups"] = new JArray(group), ["attemptedGroups"] = new JArray(),
                ["measuredGroups"] = new JArray(), ["sources"] = new JArray(),
                ["requestedScopeComplete"] = false, ["collectionErrors"] = 0, ["result"] = "Incomplete",
                ["historicalSurvey11Compatible"] = false, ["supersedesHistoricalAuthority"] = false,
                ["runtimeFitCertified"] = false, ["sourceFitCertified"] = false, ["strictServiceCertified"] = false,
                ["cameraAccepted"] = false, ["normalMissionReady"] = false, ["paidAircraftLifecycleCertified"] = false, ["playerReady"] = false };
        }

        static bool RequestedSourceSurveyScopeComplete(JObject report)
        {
            bool One(JToken token, string group) => token is JArray a && a.Count == 1 &&
                a[0].Type == JTokenType.String && (string)a[0] == group;
            var requested = report?["requestedGroups"] as JArray;
            if (requested == null || requested.Count != 1 || requested[0].Type != JTokenType.String) return false;
            string group = (string)requested[0];
            if (!new[] { "CC", "AP", "MP", "IB" }.Contains(group) ||
                !One(report["attemptedGroups"], group) || !One(report["measuredGroups"], group) ||
                !(report["sources"] is JArray sources) || sources.Count != 1 || !(sources[0] is JObject row)) return false;
            return row["group"]?.Type == JTokenType.String && (string)row["group"] == group &&
                row["collectionCompleted"]?.Type == JTokenType.Boolean && (bool)row["collectionCompleted"] &&
                row["error"] == null && report["collectionErrors"]?.Type == JTokenType.Integer &&
                (int)report["collectionErrors"] == 0;
        }

        // Pure scope seam: production supplies the SAME SurveyGroup method; controls supply no-native facts.
        static void CollectRequestedSourceSurveyGroup(JObject input, string text, JObject report,
            Action<JObject, string, JObject> collect)
        {
            string group = ParseRequestedSourceSurveyGroup(text); // reject before callback or scope mutation
            var sources = report?["sources"] as JArray;
            var attempted = report?["attemptedGroups"] as JArray;
            var measured = report?["measuredGroups"] as JArray;
            Require(report != null && collect != null &&
                JToken.DeepEquals(report["requestedGroups"], new JArray(group)) &&
                sources != null && sources.Count == 0 && attempted != null && attempted.Count == 0 &&
                measured != null && measured.Count == 0, "Fresh exact requested source scope required");
            var row = new JObject { ["group"] = group }; sources.Add(row); attempted.Add(group);
            try
            {
                collect(input, group, row);
                Require((string)row["group"] == group && row["collectionCompleted"]?.Type == JTokenType.Boolean &&
                    (bool)row["collectionCompleted"] && row["error"] == null, "Requested collector did not complete exact group");
                measured.Add(group);
            }
            catch (Exception error)
            {
                row["error"] = error.ToString(); row["collectionCompleted"] = false; report["collectionErrors"] = 1;
            }
            bool complete = RequestedSourceSurveyScopeComplete(report);
            report["requestedScopeComplete"] = complete;
            report["result"] = complete ? "CollectedUnqualified" : "FailedRequestedGroupCollection";
        }

        static string RequestedSourceSurveyOutput(string root, string output)
        {
            Require(!string.IsNullOrEmpty(output) && Path.IsPathRooted(output), "Absolute fresh requested-survey output required");
            string full = Path.GetFullPath(output);
            string basePath = Path.GetFullPath(Path.Combine(root, "Logs", "SkirmishProduction", "RequestedSourceSurvey"));
            string parent = Path.GetDirectoryName(full), run = Path.GetFileName(parent);
            Require(Path.GetFileName(full) == "report.json" && Path.GetDirectoryName(parent) == basePath &&
                Guid.TryParseExact(run, "N", out var nonce) && run == nonce.ToString("N"),
                "Unique RequestedSourceSurvey/<lowercase-guid>/report.json required; never historical survey path");
            return full;
        }

        public static void RunRequestedSourceGroupSurvey()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating, "Idle owned requested-survey Editor required");
            string group = ParseRequestedSourceSurveyGroup(Environment.GetEnvironmentVariable("WARLINE_GROUND_SOURCE_SURVEY_GROUPS"));
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = RequestedSourceSurveyOutput(root, Environment.GetEnvironmentVariable("WARLINE_GROUND_SOURCE_SURVEY_OUTPUT"));
            string parent = Path.GetDirectoryName(output);
            Require(!Directory.Exists(parent) && !File.Exists(parent) && !File.Exists(output), "Fresh unique run path required");
            for (var directory = Directory.GetParent(parent); directory != null; directory = directory.Parent)
                if (directory.Exists) Require((File.GetAttributes(directory.FullName) & FileAttributes.ReparsePoint) == 0,
                    "Requested survey output cannot traverse a reparse parent");
            Directory.CreateDirectory(parent);
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                var report = NewRequestedSourceSurveyReport(group);
                report["runId"] = Path.GetFileName(parent); report["outputPath"] = output;
                try
                {
                    RunPilotLogicalAuthoringControls();
                    RunPilotCompoundReservationControls();
                    var input = JObject.Parse(File.ReadAllText(Inventory));
                    Require((int?)input["schema"] == 1 && Hash((string)input["budgetAuthority"]) == (string)input["matrixAssetSha256"], "Pinned matrix changed");
                    Require(input["sources"]?[group] is JObject && ((JArray)input["requests"]).OfType<JObject>().Any(r => (string)r["mapGroup"] == group),
                        "Requested group needs actual pinned source and canonical requests");
                    report["inventorySha256"] = Hash(Inventory);
                    report["requestedCatalogIds"] = new JArray(((JArray)input["requests"]).OfType<JObject>()
                        .Where(r => (string)r["mapGroup"] == group).Select(r => r["catalogId"].DeepClone()));
                    report["actualBakedPrefabHierarchies"] = RegistryHierarchies();
                    CollectRequestedSourceSurveyGroup(input, group, report, SurveyGroup);
                }
                catch (Exception error)
                {
                    report["error"] = error.ToString(); report["result"] = "FailedRequestedGroupCollection";
                    throw;
                }
                finally
                {
                    using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 1024, true))
                    { writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented)); writer.Flush(); stream.Flush(true); }
                }
                Require((string)report["result"] == "CollectedUnqualified" && RequestedSourceSurveyScopeComplete(report),
                    "Requested source survey failed; partial evidence retained at " + output);
            }
            Debug.Log("[SkirmishRequestedSourceGroupSurvey] result=CollectedUnqualified requested=" + group + " measured=" + group +
                " sourceGroups=1 sourceFitCertified=0 serviceCertified=0 cameraAccepted=0 normalMissionReady=0 output=" + output);
        }
    }
}
#endif
