"""Bounded SOURCE-STRUCTURE controls only. Never compiles/executes C# or calls Unity."""
import hashlib
import json
from pathlib import Path
import unittest

HERE = Path(__file__).resolve().parent
MAIN = "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.cs"
PROPOSAL = "SkirmishGroundSourceFitSurvey.RequestedGroup.cs.proposal"
CONTROLS = "SkirmishGroundSourceFitSurvey.RequestedGroup.Controls.cs.proposal"
FLAGS = ("runtimeFitCertified", "sourceFitCertified", "strictServiceCertified", "cameraAccepted", "normalMissionReady", "paidAircraftLifecycleCertified",
         "playerReady", "historicalSurvey11Compatible", "supersedesHistoricalAuthority")


def require(ok, message):
    if not ok:
        raise ValueError(message)


def validate_structure(main_raw, source, controls, pins):
    require(hashlib.sha256(main_raw).hexdigest() == pins["existing_full_survey_raw_sha256"], "Original source raw bytes changed")
    start = main_raw.index(b"        public static void Run()")
    end = main_raw.index(b"        public static void RunRegistryHierarchyAudit()", start)
    require(hashlib.sha256(main_raw[start:end]).hexdigest() == pins["existing_Run_raw_block_sha256"], "Existing full Run changed")
    require("public static void Run()" not in source, "Proposal must not redefine existing Run")
    require("CollectRequestedSourceSurveyGroup(input, group, report, SurveyGroup);" in source,
            "Existing SurveyGroup delegate must remain production measurement path")
    for call in ("RunPilotLogicalAuthoringControls();", "RunPilotCompoundReservationControls();", "RegistryHierarchies();"):
        require(source.count(call) == 1, "Same shared call graph required: " + call)
    for shortcut in ("new World(", "StreamingWorld(", "World.DefaultGameObjectInjectionWorld", "CreateAsset(",
                     "ImportAsset(", "JsonUtility.FromJsonOverwrite", "IsWithinServiceRegion", "MpPilotServiceAccess",
                     "GetMethod(", "run_script", ".Replace("):
        require(shortcut not in source, "Native/source/service shortcut forbidden: " + shortcut)
    for flag in FLAGS:
        require('["' + flag + '"] = false' in source and '["' + flag + '"] = true' not in source,
                "Pending acceptance flag changed: " + flag)
    seam = source[source.index("static void CollectRequestedSourceSurveyGroup"):source.index("static string RequestedSourceSurveyOutput")]
    require(seam.index("ParseRequestedSourceSurveyGroup(text)") < seam.index("sources.Add(row)") < seam.index("collect(input, group, row)"),
            "Selection must precede callback/scope mutation")
    entry = source[source.index("public static void RunRequestedSourceGroupSurvey()") :]
    require(entry.index("ParseRequestedSourceSurveyGroup(") < entry.index("FileMode.CreateNew") < entry.index("RunPilotLogicalAuthoringControls();"),
            "Reject selection and reserve fresh evidence before native/shared work")
    require("groups.Length == 1" in source and "Distinct(StringComparer.Ordinal)" in source and
            'new[] { "CC", "AP", "MP", "IB" }' in source, "Exact single known group contract required")
    require('"RequestedSourceSurvey"' in source and 'Guid.TryParseExact(run, "N"' in source and
            '"report.json"' in source and "FileAttributes.ReparsePoint" in source and "stream.Flush(true)" in source,
            "Fresh distinct output shape and durable exclusive receipt required")
    for evidence in ("injected-native-failure", "calls == 0", "partialMeasurement", "FailedRequestedGroupCollection",
                     "CC,CC", "CC,AP", "ground-source-fit-20261015-11.json", "controls == 29"):
        require(evidence in controls, "Pure control case missing: " + evidence)
    return True


class ProposalStructureControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.pins = json.loads((HERE / "before-pins.json").read_text())
        cls.main = (Path(cls.pins["project"]) / MAIN).read_bytes()
        cls.source = (HERE / PROPOSAL).read_text()
        cls.controls = (HERE / CONTROLS).read_text()

    def test_actual_proposal_structure_and_frozen_Run(self):
        self.assertTrue(validate_structure(self.main, self.source, self.controls, self.pins))

    def test_real_excluded_before_pins_still_match(self):
        root = Path(self.pins["project"])
        for name, pin in self.pins["existing_file_pins"].items():
            with self.subTest(path=name):
                self.assertEqual(pin["sha256"], hashlib.sha256((root / name).read_bytes()).hexdigest())

    def test_mutated_original_source_is_detected(self):
        with self.assertRaisesRegex(ValueError, "raw bytes"):
            validate_structure(self.main + b"// mutated", self.source, self.controls, self.pins)

    def test_removed_existing_collector_is_detected(self):
        with self.assertRaisesRegex(ValueError, "SurveyGroup delegate"):
            validate_structure(self.main, self.source.replace("report, SurveyGroup);", "report, FakeCollector);"), self.controls, self.pins)

    def test_fit_promotion_is_detected(self):
        with self.assertRaisesRegex(ValueError, "acceptance flag"):
            validate_structure(self.main, self.source.replace('["runtimeFitCertified"] = false', '["runtimeFitCertified"] = true'), self.controls, self.pins)

    def test_native_world_shortcut_is_detected(self):
        with self.assertRaisesRegex(ValueError, "shortcut"):
            validate_structure(self.main, self.source + "\n// new World( forbidden", self.controls, self.pins)

    def test_existing_Run_redefinition_is_detected(self):
        with self.assertRaisesRegex(ValueError, "redefine"):
            validate_structure(self.main, self.source + "\npublic static void Run() {}", self.controls, self.pins)

    def test_missing_failure_preservation_control_is_detected(self):
        with self.assertRaisesRegex(ValueError, "control case"):
            validate_structure(self.main, self.source, self.controls.replace("partialMeasurement", "removed"), self.pins)


if __name__ == "__main__":
    unittest.main(verbosity=2)
