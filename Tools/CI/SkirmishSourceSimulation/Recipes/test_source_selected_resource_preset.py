from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[4]


class SourceSelectedResourcePresetControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.authoring = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceMissionAuthoringTool.cs").read_text()
        cls.validation = (ROOT / "Assets/Game/Scripts/Editor/Skirmish/CompilerSelectedAuthoringEvidence.cs").read_text()
        cls.loader = cls.authoring
        cls.owner = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/294-312_PaidSourceOwnerStep.retry.cs").read_text()
        cls.authority = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/489-513_FreshSourceStorageLifetimeAuthority.cs").read_text()
        cls.composer = (ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/compose_s074_source_only_bundle.py").read_text()

    def test_legacy_authoring_request_remains_optional(self):
        self.assertIn('"resource_preset"};if(request.Properties().Any', self.authoring)
        self.assertIn('if(request["resource_preset"]!=null)', self.authoring)
        self.assertIn('"source_owner_authoring"', self.authority)
        self.assertIn('if (authority == null) return;', self.loader)
        self.assertIn("if author.get('resource_preset') is not None:", self.composer)
        self.assertIn("initial['source_owner_authoring']", self.composer)

    def test_selected_preset_must_be_a_hash_pinned_graph_asset(self):
        self.assertIn('RequirePresetSupplyDaySeconds(graph,presetPath,(string)presetDescriptor["sha256"],presetRelative)', self.authoring)
        self.assertIn('projectedField.FieldType!=typeof(float)', self.authoring)
        self.assertIn('token.Type!=JTokenType.Integer&&token.Type!=JTokenType.Float', self.authoring)
        self.assertIn('double.IsNaN(value)||double.IsInfinity(value)||value<=0', self.authoring)
        self.assertIn('sourceValue!=value', self.authoring)
        self.assertIn('Explicit resource preset must be an exact source-graph pin', self.authoring)
        self.assertIn('"SkirmishPresetConfig"', self.validation)
        self.assertIn('Selected resource preset is not an exact graph-owned preset asset', self.validation)

    def test_compiler_input_closure_and_runtime_descriptor_must_join(self):
        self.assertIn('PinRecord(pins, presetPath, 4 * 1024 * 1024, retainedPins)', self.validation)
        self.assertIn('!JToken.DeepEquals(request["resource_preset"], presetDescriptor)', self.loader)
        self.assertIn('PinMatches(pins, requestPath, (string)requestDescriptor["sha256"])', self.loader)
        self.assertIn('PinMatches(pins, presetPath, (string)presetDescriptor["sha256"])', self.loader)
        self.assertIn('SourceSelectedResourcePreset.RequireExactAuthoringJoin(request,Setup)', self.owner)

    def test_ordinary_runtime_owner_still_requires_finite_cadence(self):
        self.assertIn('Descriptor(request["resource_preset"],1024*1024)', self.owner)
        self.assertIn('Actual S002 supply-day input required', self.owner)
        self.assertIn('resourceSecondsPerDay>0', self.owner)
        self.assertIn('float.IsInfinity(resourceSecondsPerDay)', self.owner)


if __name__ == "__main__":
    unittest.main()
