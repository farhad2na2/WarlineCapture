import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
CONTRACTS = ROOT / "Assets/Game/Scripts/Simulation/AriaS002Contracts.cs"
POLICY = ROOT / "Assets/Game/Scripts/Simulation/AriaGroundBaseAssaultPolicy.cs"
ADAPTERS = [
    ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.cs",
    ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.s003.cs",
]


class AriaS002BurstTraceBoundaryTests(unittest.TestCase):
    def test_burst_observation_does_not_embed_managed_approach_contract(self):
        source = CONTRACTS.read_text(encoding="utf-8")
        observation = source.split("public struct AriaS002Observation {", 1)[1].split(
            "public struct AriaS002PlanState {", 1
        )[0]
        self.assertNotIn("SkirmishPublicSelectedApproach", observation)
        self.assertNotIn("string ", observation)

    def test_policy_consumes_same_projection_derived_boolean(self):
        source = POLICY.read_text(encoding="utf-8")
        self.assertRegex(source, r"\b(?:v|view)\.AssaultAtBase\b")

    def test_both_source_traces_preserve_full_managed_projection_outside_policy_data(self):
        for path in ADAPTERS:
            with self.subTest(adapter=path.name):
                source = path.read_text(encoding="utf-8")
                self.assertIn("selectedOwnApproachTrace=own", source)
                self.assertIn("SelectedObjectiveContactReach(own)", source)
                self.assertIn("TraceObservation(in AriaS002Observation facts,in SkirmishPublicSelectedApproach selectedOwn)", source)
                self.assertIn("JObject.FromObject(selectedOwn)", source)
                self.assertIn('new JProperty("SelectedOwnApproach"', source)
                self.assertIn("traceObservation=TraceObservation(facts,selectedOwnApproachTrace)", source)
                self.assertRegex(source, r"\bTime\s*=\s*time\b")
                self.assertIn('["time"]=time', source)
                self.assertNotIn("facts.SelectedOwnApproach", source)


if __name__ == "__main__":
    unittest.main()
