from pathlib import Path
import importlib.util
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[4]
GENERATOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/alias_shared_delivery_kernel.py"
SOURCE = "Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs"
PREFIX = (
    "extern alias ActualCore;\n"
    "using ActualVector3=ActualCore::UnityEngine.Vector3;\n"
    "using ActualMathf=ActualCore::UnityEngine.Mathf;\n"
)


def load_generator():
    spec = importlib.util.spec_from_file_location("alias_shared_delivery_kernel", GENERATOR)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SharedDeliveryKernelAliasTests(unittest.TestCase):
    def test_current_source_is_mechanically_aliased_without_policy_edits(self):
        original = (ROOT / SOURCE).read_text(encoding="utf-8")
        with tempfile.TemporaryDirectory() as directory:
            fixture_repo = Path(directory) / "repo"
            fixture_source = fixture_repo / SOURCE
            fixture_source.parent.mkdir(parents=True)
            fixture_source.write_text(original, encoding="utf-8")
            output = Path(directory) / "generated"

            proof = load_generator().generate(fixture_repo, output)

            expected_body = original.replace("UnityEngine.Vector3", "ActualVector3").replace(
                "UnityEngine.Mathf", "ActualMathf")
            generated = (output / "SharedProductionDeliveryKernel.cs").read_text(encoding="utf-8")
            self.assertEqual(PREFIX + expected_body, generated)
            self.assertEqual("Mechanical UnityEngine.Vector3/Mathf aliases only; policy body preserved",
                             proof["transform"])
            self.assertEqual(1, expected_body.count(
                "ProductionTransportLaneOffset(bool hasCameraForward,ActualVector3 cameraForward,int laneIndex,int maxConcurrent)"))

    def test_rejects_an_unreviewed_unity_vector_reference(self):
        original = (ROOT / SOURCE).read_text(encoding="utf-8")
        altered = original.replace(
            "public static class SharedProductionDeliveryKernel {",
            "public static class SharedProductionDeliveryKernel {\n  UnityEngine.Vector3 unexpected;",
            1)
        self.assertNotEqual(original, altered)
        with tempfile.TemporaryDirectory() as directory:
            fixture_repo = Path(directory) / "repo"
            fixture_source = fixture_repo / SOURCE
            fixture_source.parent.mkdir(parents=True)
            fixture_source.write_text(altered, encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "Unsupported UnityEngine vector/math reference shape"):
                load_generator().generate(fixture_repo, Path(directory) / "generated")


if __name__ == "__main__":
    unittest.main()
