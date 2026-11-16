from pathlib import Path
import unittest


SOURCE = Path(__file__).with_name("S004OriginalCompilerSourceBinding.cs.txt")
RUNNER = Path(__file__).with_name("run_s004_original_compiler.py")


class S004OriginalCompilerBindingTests(unittest.TestCase):
    def test_binding_requires_external_authored_graph_and_current_pins(self):
        text = SOURCE.read_text()
        for required in (
            '"AuthoredConfigAssets"', '"compiled_setup"] != false',
            '"world_initialized"] != false', '"native_world_state_used"] != false',
            '"Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset"',
            '"Assets/Game/Resources/SkirmishExpansionCatalog.asset"',
            'SourceConfigMaterializer(graph)', 'source.ByPath<SkirmishScenarioDefinitionConfig>',
            'source.ByPath<SkirmishExpansionCatalogConfig>', 'SkirmishSetupMatrixTable.TryLoadFromText',
            'SkirmishSetupCompiler.TryCompile(', '"physical_source_fit_verified"] = false',
            '"mission_readiness"] = false', '"world_initialized"] = false',
        ):
            with self.subTest(required=required):
                self.assertIn(required, text)

    def test_binding_does_not_create_synthetic_authored_configs(self):
        text = SOURCE.read_text()
        self.assertNotIn("ScriptableObject.CreateInstance", text)
        self.assertNotIn("new SkirmishScenarioDefinitionConfig", text)
        self.assertNotIn("SkirmishExpansionCatalogFactory.Create", text)

    def test_runner_requires_external_graph_and_successful_s002_build(self):
        text = RUNNER.read_text()
        for required in ("build.get(\"mission\") == \"S002\"", "build.get(\"status\") == \"Passed\"",
                         "--graph-sha256", "compiled_setup"):
            with self.subTest(required=required):
                self.assertIn(required, text)
        self.assertNotIn("source_manifest.plan.json", text)
        self.assertIn('"physical_source_fit_verified": False', text)
        self.assertIn('"mission_readiness": False', text)


if __name__ == "__main__":
    unittest.main()
