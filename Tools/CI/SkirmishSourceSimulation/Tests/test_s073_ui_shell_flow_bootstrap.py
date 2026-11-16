from pathlib import Path
import importlib.util
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[4]
ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073UiShellFlowBootstrap.cs"
GENERATOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_flow_owner/generate.py"
spec = importlib.util.spec_from_file_location("s073_ui_shell_flow_generator", GENERATOR)
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


class S073UiShellFlowBootstrapTests(unittest.TestCase):
    def test_bridge_is_owner_gated_and_oncreate_only(self):
        source = ADAPTER.read_text()
        self.assertIn('"S073RetainedSourceUiShellOwnerInitialization"', source)
        self.assertIn("ReferenceEquals(host.Entities, em)", source)
        self.assertIn("boundary.CalculateEntityCount()", source)
        self.assertIn("flow.OnCreate(ref state)", source)
        self.assertIn("flow.OnUpdate(ref state)", source)
        self.assertIn("afterFlow.ActiveRoute == UIRoute.Splash", source)
        self.assertIn("dispositionAfterFlow.Value == UiShellStartupDisposition.Pending", source)

    def test_bridge_rejects_missing_boundary_and_does_not_fabricate_flow_inputs(self):
        source = ADAPTER.read_text()
        self.assertIn('boundaryCount == 1', source)
        self.assertIn('routeRequests == 0 && routeHistory == 0 && loadingRequests == 0 && popupRequests == 0', source)
        self.assertIn('presentationCommands == 0 && completions == 0', source)
        self.assertIn('["input_injected"] = false', source)
        self.assertIn('["route_requested"] = false', source)
        self.assertIn('["player_or_ui_readiness"] = false', source)
        self.assertIn("SourceS073UiShellFlowBoundary.AudioCalls", source)
        self.assertIn("SourceS073UiShellFlowBoundary.MusicCalls", source)

    def test_generator_adds_separate_matching_opt_in_and_receipt_fields(self):
        owner_runner = '''        using var host = new AutonomousWorld(em, buildings);
        JToken uiShellStateOwnerDiagnosticOption = run["s073_ui_shell_state_owner_diagnostic"];
        bool uiShellStateOwnerDiagnosticRequested = (bool?)uiShellStateOwnerDiagnosticOption == true;
        JObject uiShellStateOwnerInitialization = null;
        if (uiShellStateOwnerDiagnosticRequested)
        {
            uiShellStateOwnerInitialization = SourceS073UiShellStateOwnerBootstrap.Initialize(em, host);
        }
            ["ui_shell_state_owner_initialization"] = uiShellStateOwnerInitialization,
'''
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            source = folder / "owner.cs"
            output = folder / "flow.cs"
            source.write_text(owner_runner)
            receipt = generator.generate(source, output)
            result = output.read_text()
        self.assertIn('run["s073_ui_shell_flow_diagnostic"]', result)
        self.assertIn("WARLINE_S073_UI_SHELL_FLOW_DIAGNOSTIC", result)
        self.assertIn("SourceS073UiShellFlowBootstrap.Initialize(em, host, uiShellStateOwnerInitialization)", result)
        self.assertIn('ui_shell_flow_diagnostic_admitted', result)
        self.assertIn("exactly one no-input flow OnUpdate", receipt["scope"])


if __name__ == "__main__":
    unittest.main()
