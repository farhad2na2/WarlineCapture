import ast
import copy
import importlib.util
import json
import math
import re
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BUILD = ROOT / "Tools/CI/skirmish_source_build.py"
PACKAGE_PATH = ROOT / "T Logs/SkirmishProduction/s050-source-derived-map-package-20261115-r2/map-package.json"
PREFLIGHT_PATH = ROOT / "T Logs/SkirmishProduction/s050-source-derived-map-package-20261115-r2/preflight-report-final.json"
GENERATOR_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate.py"
DECLARED_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceDeclaredRuntimeInputs.cs"
CAMERA_PROVIDER_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/495_SourceS002NumericCameraProvider.cs"
CAMERA_PROFILE_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceDeclaredEngineeringCameraProfile.cs"


def load_exact_map_selector():
    tree = ast.parse(BUILD.read_text())
    nodes = [node for node in tree.body if isinstance(node, ast.FunctionDef) and
             node.name == "_source_derived_map_exact_selection"]
    if len(nodes) != 1:
        raise AssertionError("Expected one exact source map selector")
    namespace = {"math": math, "re": re, "Path": Path}
    exec(compile(ast.Module(body=nodes, type_ignores=[]), str(BUILD), "exec"), namespace)
    return namespace[nodes[0].name]


class SourceDerivedExactMapAdmissionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.matches = staticmethod(load_exact_map_selector())

    def setUp(self):
        self.package = json.loads(PACKAGE_PATH.read_text())
        self.preflight = json.loads(PREFLIGHT_PATH.read_text())
        self.initial = {"static_map": copy.deepcopy(self.package["static_map"]),
                        "selected_operation_map": copy.deepcopy(self.package["static_map"]["definition"])}

    def test_current_s050_exact_selected_map_is_admitted_as_source_only(self):
        self.assertTrue(self.matches(self.initial, self.package, self.preflight))

    def test_map_frame_or_preflight_mismatch_is_rejected(self):
        changed = copy.deepcopy(self.package)
        changed["frame"]["width"] += 1
        self.assertFalse(self.matches(self.initial, changed, self.preflight))

        changed = copy.deepcopy(self.package)
        changed["static_map"]["operation_map_id"] = "opmap.unpinned.other"
        self.assertFalse(self.matches(self.initial, changed, self.preflight))

        changed = copy.deepcopy(self.preflight)
        changed["source_fit_certified"] = True
        self.assertFalse(self.matches(self.initial, self.package, changed))

    def test_selected_definition_must_match_and_failed_preflight_is_rejected(self):
        changed = copy.deepcopy(self.initial)
        changed["selected_operation_map"]["sha256"] = "0" * 64
        self.assertFalse(self.matches(changed, self.package, self.preflight))

        changed = copy.deepcopy(self.preflight)
        changed["status"] = "Failed"
        self.assertFalse(self.matches(self.initial, self.package, changed))

    def test_generated_declared_inputs_keeps_alias_branch_and_adds_exact_map_check(self):
        spec = importlib.util.spec_from_file_location("source_map_host_generator", GENERATOR_PATH)
        generator = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(generator)
        with tempfile.TemporaryDirectory() as temporary:
            result = generator.generate_declared_inputs_output(ROOT, Path(temporary), GENERATOR_PATH)
            generated = (Path(temporary) / "SourceDeclaredRuntimeInputs.cs").read_text()
        self.assertIn("sourceDerivedMap.IsLogicalAlias", generated)
        self.assertIn("setup.OperationMapId!=sourceDerivedMap.PhysicalMapId", generated)
        self.assertIn("JToken.DeepEquals(exactMap,initial[\"static_map\"])", generated)
        self.assertIn("exactFrame[\"width\"]!=selectedGrid.Width", generated)
        self.assertIn("else if(inactive!=null)", generated)
        self.assertTrue(result["native_or_player_readiness"] is False)

    def test_declared_engineering_camera_uses_selected_profile_and_allowlisted_adapter(self):
        provider = CAMERA_PROVIDER_PATH.read_text()
        profile = CAMERA_PROFILE_PATH.read_text()
        self.assertIn("if(request[\"metadata_compiler_context\"]!=null){var derived=SourceDeclaredEngineeringCameraProfile.Create(setup,actualDefinition,request", provider)
        self.assertIn("SourceNumericCameraEngineAdapterContract.ReadEngineering(request,headerSha)", provider)
        self.assertIn("SourceNumericCameraEngineAdapterContract.FunctionProfileSha(request)", profile)
        self.assertIn("profile.Matches(setup,definition)", profile)
        self.assertIn("if(setup.CatalogId==\"S002\")", provider)
        self.assertIn("SourceS026TypedConfiguration.RequireFrozenMeasuredLayout", provider)


if __name__ == "__main__":
    unittest.main()
