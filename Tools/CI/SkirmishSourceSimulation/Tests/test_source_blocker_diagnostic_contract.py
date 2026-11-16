from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[4]
ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/307_GeneratedS002StartingWorld.lifetime.cs"
SIMULATION = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs"
OWNER = ROOT / "Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs"


class BlockerDiagnosticContractTests(unittest.TestCase):
    def test_opt_in_gate_and_owner_query_domains(self):
        adapter = ADAPTER.read_text()
        simulation = SIMULATION.read_text()
        owner = OWNER.read_text()

        disabled = (
            "if(!sourceBlockerDiagnosticsEnabled){dedupState.Begin();dedup.OnUpdate(ref dedupState);dedupState.End();"
            "storageState.Begin();storageOwner.OnUpdate(ref storageState);storageState.End();"
            "staticBlockerState.Begin();staticBlockers.OnUpdate(ref staticBlockerState);staticBlockerState.End();return;}"
        )
        self.assertIn('Environment.GetEnvironmentVariable("WARLINE_SOURCE_WORKLOAD_COUNTERS")=="1"', adapter)
        self.assertIn(disabled, adapter)
        self.assertLess(adapter.index(disabled), adapter.index("System.Diagnostics.Stopwatch.StartNew()"))
        self.assertIn('["source_blocker_diagnostics"]=world.SourceBlockerDiagnostics', simulation)
        self.assertIn('new JObject{["enabled"]=false}', adapter)

        self.assertIn("ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<GridBlockerSize>()},None=new[]{ComponentType.ReadOnly<StaticBlockerPrevBounds>()}", adapter)
        self.assertIn("ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<GridBlockerSize>(),ComponentType.ReadOnly<StaticBlockerPrevBounds>()", adapter)
        self.assertIn("All=new[]{ComponentType.ReadOnly<StaticBlockerPrevBounds>()},None=new[]{ComponentType.ReadOnly<StaticGridBlocker>()}", adapter)

        self.assertIn(".WithNone<StaticBlockerPrevBounds>()", owner)
        self.assertIn(".WithAll<StaticGridBlocker>()", owner)
        self.assertIn(".WithEntityAccess())", owner)
        self.assertIn(".WithNone<StaticGridBlocker>()", owner)


if __name__ == "__main__":
    unittest.main()
