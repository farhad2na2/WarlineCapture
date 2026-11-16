import unittest
import importlib.util
from pathlib import Path
import tempfile


ROOT = Path(__file__).resolve().parents[4]
ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073OriginalAriaStepDiagnostic.cs"
HOST = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs"
MERGER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/aria_s073_original_step/prepare_merged_manifest.py"
UI_TYPE_EXTRACTOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/aria_s073_original_step/extract_ui_lifecycle_types.py"


class OriginalAriaStepProbeContract(unittest.TestCase):
    def test_probe_has_no_ecs_mutation_or_input_owner_calls(self):
        source = ADAPTER.read_text(encoding="utf-8")
        for forbidden in (".AddComponent(", ".SetComponentData(", ".CreateEntity(",
                          ".DestroyEntity(", "AriaPlayInputSystem", "TryGesture(", ".Tick("):
            self.assertNotIn(forbidden, source)
        self.assertIn("AriaSkirmishPlanSystem.Step(view, ref projectedPlan", source)

    def test_step_is_after_complete_original_tuple_and_s073_gates(self):
        source = ADAPTER.read_text(encoding="utf-8")
        step = source.index("AriaSkirmishPlanSystem.Step(view, ref projectedPlan")
        for gate in ("rootEntities.Length != 1", "linkedOwnerCount != 1",
                     "!storageHasView || !storageHasPlan || !boundaryHasSession || !boundaryHasOutput",
                     "!view.Active || view.SourceScenarioIndex != 3",
                     "AriaS002NativeAdapter.CanHandle(view)"):
            self.assertLess(source.index(gate), step)
        self.assertIn('["step_invoked"] = false', source)
        self.assertIn('["native_readiness_established"] = false', source)
        self.assertIn('["mission_ready"] = false', source)
        self.assertIn('["planner_suggestion_accepted"] = false', source)

    def test_unavailable_branch_controls_cover_absent_partial_ambiguous_and_stale(self):
        source = ADAPTER.read_text(encoding="utf-8")
        step = source.index("AriaSkirmishPlanSystem.Step(view, ref projectedPlan")
        for reason in (
            "OriginalUiRootAbsentInSourceWorld",
            "OriginalAriaObservationOwnerAbsent",
            "AmbiguousOriginalAriaObservationOwners",
            "StaleOriginalAriaObservationOwner",
            "OriginalAriaLifecycleTupleIncomplete",
        ):
            with self.subTest(reason=reason):
                branch = source.index('["unavailable_reason"]', source.index(reason) - 100)
                self.assertLess(branch, step)
                self.assertIn('"status"] = "Unavailable"', source)

    def test_hook_and_source_are_derived_define_only(self):
        host = HOST.read_text(encoding="utf-8")
        merger = MERGER.read_text(encoding="utf-8")
        self.assertIn("#if WARLINE_SOURCE_S073_ORIGINAL_ARIA_PROBE", host)
        self.assertIn("WARLINE_S073_ORIGINAL_ARIA_STEP_PROBE", host)
        self.assertIn("WARLINE_SOURCE_S073_ORIGINAL_ARIA_PROBE", merger)
        self.assertIn("SourceS073OriginalAriaStepDiagnostic.cs", merger)

    def test_derived_ui_types_are_exact_current_declarations_without_root_duplicate(self):
        spec = importlib.util.spec_from_file_location("aria_ui_type_extractor", UI_TYPE_EXTRACTOR)
        extractor = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(extractor)
        with tempfile.TemporaryDirectory(prefix="aria-ui-types-test-") as temp:
            extractor.generate(ROOT, Path(temp) / "generated")
            generated = (Path(temp) / "generated" / extractor.OUTPUT).read_text(encoding="utf-8")
        self.assertNotIn("UiShellRootComponent", generated)
        for source, name, kind in extractor.DECLARATIONS:
            original = (ROOT / source).read_text(encoding="utf-8")
            self.assertIn(extractor.declaration(original, name, kind), generated)


if __name__ == "__main__":
    unittest.main()
