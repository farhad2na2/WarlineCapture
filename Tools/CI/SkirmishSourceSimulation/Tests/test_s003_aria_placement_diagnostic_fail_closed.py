from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.s003.cs"
POLICY = ROOT / "Assets/Game/Scripts/Simulation/AriaGroundBaseAssaultPolicy.cs"


def diagnostic_branch(source: str) -> str:
    marker = 'if(groundContext.CatalogId=="S003"&&Environment.GetEnvironmentVariable("WARLINE_S003_ARIA_PLACEMENT_DIAGNOSTIC")=="1")'
    start = source.index(marker)
    opening = source.index("{", start)
    depth = 0
    for index in range(opening, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[opening + 1:index]
    raise AssertionError("S003 placement diagnostic branch has unbalanced braces")


class S003AriaPlacementDiagnosticFailClosedTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = SOURCE.read_text(encoding="utf-8")
        cls.branch = diagnostic_branch(cls.source)

    def test_source_eligibility_is_read_but_never_used_as_control_availability(self):
        self.assertIn("ReadS003GuardTowerControl(em)", self.branch)
        self.assertNotIn('control["can_request"]', self.branch)
        self.assertIn('["can_request"]=row.CanRequest==1', self.source)
        self.assertIn('["native_control_visible_known"]=false', self.source)
        self.assertIn('["native_touch_reachability_known"]=false', self.source)

    def test_native_control_availability_stays_unknown_and_unadmitted(self):
        self.assertIn("facts.DefenseControlsKnown=false", self.branch)
        self.assertNotIn("facts.PlacementOpen=", self.branch)
        self.assertIn("facts.DefenseBuild=default", self.branch)
        self.assertIn("facts.PlacementCancel=default", self.branch)
        self.assertIn("facts.PlacementConfirm=default", self.branch)
        self.assertNotIn("facts.DefenseControlsKnown=true", self.branch)
        self.assertNotIn("facts.DefenseBuild=Cap", self.branch)
        self.assertNotIn("facts.PlacementCancel=Cap", self.branch)
        self.assertNotIn("facts.PlacementConfirm=Cap", self.branch)
        for site in range(6):
            self.assertNotIn("facts.Site%d=" % site, self.branch)

    def test_source_and_site_diagnostics_remain_available_as_diagnostics(self):
        self.assertIn("ProjectS003DefenseSite(home,enemy,site)", self.source)
        self.assertIn('["screen_projection_known"]=candidate.ScreenProjectionKnown', self.source)
        self.assertIn('["touch_reachability_known"]=candidate.TouchReachabilityKnown', self.source)
        self.assertIn('["placement_legal_known"]=candidate.PlacementLegalKnown', self.source)
        self.assertIn('["camera_qualified"]=false', self.source)
        self.assertIn('["s003_defense_site_diagnostics"]=s003DefenseSiteDiagnostics', self.source)

    def test_unknown_controls_fail_closed_before_build_or_placement_policy(self):
        policy = POLICY.read_text(encoding="utf-8")
        guard = 'if (!view.DefenseControlsKnown)'
        guard_at = policy.index(guard)
        build_gate = policy.index("view.DefenseBuild.Admitted", guard_at)
        self.assertLess(guard_at, build_gate)
        self.assertIn("output.ManagedGuardFailure=1;return true;", policy[guard_at:build_gate])
        self.assertIn("ThrowManagedGuardFailure(output.ManagedGuardFailure);", policy)
        self.assertIn('throw new NotSupportedException("Opening-defense source control availability unknown")', policy)
        self.assertIn("facts.DefenseControlsKnown=false", self.branch)


if __name__ == "__main__":
    unittest.main()
