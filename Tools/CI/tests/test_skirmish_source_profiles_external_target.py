import subprocess
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
PROFILES = ROOT / "Tools/CI/skirmish_source_profiles.py"
QUALIFICATION = (
    ROOT
    / "T Logs/SkirmishProduction/s075-s003-derived-loader-control-20261114-r1/configuration-qualification.json"
)


class ExternalTargetGuardTests(unittest.TestCase):
    def test_s075_external_guard_admits_only_bound_target_and_preserves_s026(self):
        if not QUALIFICATION.is_file():
            self.skipTest("Current S075 source qualification is unavailable")

        code = r'''
import runpy, sys
from pathlib import Path
root = Path(sys.argv[1])
sys.dont_write_bytecode = True
namespace = runpy.run_path(str(root / "Tools/CI/run_skirmish_source_simulation.py"), run_name="external-profile-guard-test")
profiles = namespace["owned"].require("skirmish_source_profiles")
profiles.bind_configuration_qualification(root / "T Logs/SkirmishProduction/s075-s003-derived-loader-control-20261114-r1/configuration-qualification.json")
assert profiles.require_profile("S075", 104804, 2, 1)
source, _ = profiles.generate_guard("S075")
start = source.index("internal static void Require(string")
guard = source[start:source.index(" } }", start)]
expected = 'mission=="S075" && difficulty==2 && size==1 && (seed==104804)'
assert guard.count(expected) == 1, guard
assert guard.count('mission=="S075"') == 1, guard
assert 'mission=="S075" && difficulty==2 && size==1 && (seed==104805)' not in guard, guard
assert 'mission=="S075" && difficulty==3' not in guard, guard
assert 'mission=="S075" && size==2' not in guard, guard
assert "SourceConfigurationQualification.RequireInitialization" in source

s026, _ = profiles.generate_guard("S026")
start = s026.index("internal static void Require(string")
s026_guard = s026[start:s026.index(" } }", start)]
assert s026_guard.count('mission=="S026"') == 3, s026_guard
assert "seed==104755" in s026_guard
assert "mission==\"S075\"" not in s026_guard and "seed==104804" not in s026_guard, s026_guard
'''
        result = subprocess.run(
            [sys.executable, "-B", "-c", code, str(ROOT)],
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 0, result.stderr or result.stdout)


if __name__ == "__main__":
    unittest.main()
