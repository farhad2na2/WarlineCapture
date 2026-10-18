#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Editor
{
    // Pure controls for future reviewed compilation; NO native collector, World or asset IO.
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static void RunRequestedSourceGroupSurveyControls()
        {
            int controls = 0;
            void Refused(Action action)
            {
                bool refused = false;
                try { action(); } catch (InvalidOperationException) { refused = true; }
                Require(refused, "Requested-group negative control did not refuse"); controls++;
            }
            JObject Complete(string group)
            {
                var report = NewRequestedSourceSurveyReport(group); int calls = 0;
                CollectRequestedSourceSurveyGroup(new JObject(), group, report, (input, actual, row) =>
                {
                    calls++; Require(actual == group, "Selection changed before callback");
                    row["collectionCompleted"] = true;
                });
                Require(calls == 1 && RequestedSourceSurveyScopeComplete(report) &&
                    (string)report["result"] == "CollectedUnqualified", "One exact callback/scope required");
                foreach (string flag in new[] { "runtimeFitCertified", "sourceFitCertified", "strictServiceCertified", "cameraAccepted", "normalMissionReady",
                    "paidAircraftLifecycleCertified", "playerReady", "historicalSurvey11Compatible", "supersedesHistoricalAuthority" })
                    Require((bool?)report[flag] == false, "Pure collection cannot manufacture acceptance");
                return report;
            }
            foreach (string group in new[] { "CC", "AP", "MP", "IB" }) { Complete(group); controls++; }
            Require(ParseRequestedSourceSurveyGroup(" CC ") == "CC", "Trim must preserve exact group identity"); controls++;
            foreach (string text in new[] { null, "", " ", "DB", "cc", "CC,CC", "CC,AP", "CC,", ",CC", "AP,,CC", new string('C', 129) })
            {
                int calls = 0; var report = NewRequestedSourceSurveyReport("CC");
                Refused(() => CollectRequestedSourceSurveyGroup(new JObject(), text, report,
                    (input, group, row) => { calls++; row["collectionCompleted"] = true; }));
                Require(calls == 0 && ((JArray)report["attemptedGroups"]).Count == 0 &&
                    ((JArray)report["sources"]).Count == 0, "Invalid input must refuse before callback/scope mutation");
            }
            var failed = NewRequestedSourceSurveyReport("CC");
            CollectRequestedSourceSurveyGroup(new JObject(), "CC", failed, (input, group, row) =>
            { row["partialMeasurement"] = "preserved"; throw new InvalidOperationException("injected-native-failure"); });
            Require((string)failed["result"] == "FailedRequestedGroupCollection" && !RequestedSourceSurveyScopeComplete(failed) &&
                ((JArray)failed["attemptedGroups"]).Count == 1 && ((JArray)failed["measuredGroups"]).Count == 0 &&
                (string)failed["sources"][0]["partialMeasurement"] == "preserved" &&
                ((string)failed["sources"][0]["error"]).Contains("injected-native-failure"), "Failure/partial scope must survive"); controls++;
            var incomplete = NewRequestedSourceSurveyReport("CC");
            CollectRequestedSourceSurveyGroup(new JObject(), "CC", incomplete, (input, group, row) => { });
            Require(!RequestedSourceSurveyScopeComplete(incomplete) && (int)incomplete["collectionErrors"] == 1,
                "Missing completion cannot count as measured"); controls++;
            var foreign = NewRequestedSourceSurveyReport("CC");
            CollectRequestedSourceSurveyGroup(new JObject(), "CC", foreign, (input, group, row) =>
            { row["group"] = "AP"; row["collectionCompleted"] = true; });
            Require(!RequestedSourceSurveyScopeComplete(foreign) && ((JArray)foreign["measuredGroups"]).Count == 0,
                "Foreign source row cannot gain requested credit"); controls++;
            foreach (string field in new[] { "requestedGroups", "attemptedGroups", "measuredGroups", "sources", "collectionErrors" })
            {
                var report = Complete("CC");
                if (field == "sources") ((JArray)report[field]).Add(report[field][0].DeepClone());
                else if (field == "collectionErrors") report[field] = true;
                else ((JArray)report[field]).Add("AP");
                Require(!RequestedSourceSurveyScopeComplete(report), "Duplicate/foreign/malformed scope accepted: " + field); controls++;
            }
            var root = Path.Combine(Path.GetTempPath(), "warline-pure-requested-scope");
            var valid = Path.Combine(root, "Logs", "SkirmishProduction", "RequestedSourceSurvey", new string('a', 32), "report.json");
            Require(RequestedSourceSurveyOutput(root, valid) == Path.GetFullPath(valid), "Exact unique output shape required"); controls++;
            foreach (string bad in new[] { "relative.json", Path.Combine(root, "Logs", "SkirmishProduction", "ground-source-fit-20261015-11.json"),
                Path.Combine(root, "Logs", "SkirmishProduction", "RequestedSourceSurvey", "not-a-guid", "report.json"),
                Path.Combine(root, "Logs", "SkirmishProduction", "RequestedSourceSurvey", new string('a', 32), "survey11.json") })
                Refused(() => RequestedSourceSurveyOutput(root, bad));
            Require(controls == 29, "Exact bounded pure control count required");
            Debug.Log("[RequestedSourceGroupSurveyControls] result=Passed controls=29 nativeQueries=0 sourceWorlds=0 sourceFitCertified=0");
        }
    }
}
#endif
