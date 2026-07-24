import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[3]
WRAPPER = ROOT / "Tools/CI/invoke_unity_macos.sh"
AGENTS = ROOT / "AGENTS.md"


class UnityMacInvocationContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = WRAPPER.read_text(encoding="utf-8")
        cls.agents = AGENTS.read_text(encoding="utf-8")

    def test_wrapper_does_not_launch_batchmode(self):
        self.assertNotIn('"$UNITY_EXE" -batchmode', self.source)
        self.assertIn('LicensingMode: gui (batchmode disabled on macOS)', self.source)

    def test_wrapper_rejects_batchmode_wrapper_flag(self):
        self.assertIn("--batchmode is forbidden on macOS for this project", self.source)

    def test_wrapper_rejects_batchmode_unity_argument(self):
        self.assertIn("-batchmode is forbidden on macOS", self.source)

    def test_wrapper_requires_unity_hub_running(self):
        self.assertIn("Unity Hub is not running", self.source)
        self.assertIn("exit 65", self.source)

    def test_agents_forbids_direct_unity_and_batchmode(self):
        self.assertIn("never passes `-batchmode`", self.agents)
        self.assertIn("Do not invoke the Unity executable directly", self.agents)
        self.assertIn("Never report \"Unity licensing blocked the lane\"", self.agents)


if __name__ == "__main__":
    unittest.main()
