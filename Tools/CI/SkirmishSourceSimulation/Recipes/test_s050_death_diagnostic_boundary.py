import re
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[4]
WORLD = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/000-000_AutonomousWorld.cs'
MODEL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/SourceNativeDeathReadModel.cs'


class S050DeathDiagnosticBoundaryTests(unittest.TestCase):
    def test_diagnostic_dispatch_is_scoped_to_s050_and_explicit_nested_descriptor(self):
        source = WORLD.read_text()
        self.assertRegex(source, r'Setup\.CatalogId=="S050"&&deathDescriptor\?\.Count==1\?deathDescriptor\["s050_source_diagnostic"\]')
        self.assertIn('SourceNativeDeathReadModel.LoadS050SourceDiagnostic(s050DeathDiagnostic)', source)
        self.assertIn('deathDescriptor?.Count==5 && Setup.CatalogId=="S026" && inactiveDiagnostic!=null', source)
        self.assertIn('SourceNativeDeathReadModel.LoadS026Diagnostic(deathDescriptor)', source)

    def test_ordinary_qualified_boundary_keeps_its_one_mib_cap_and_digest_gate(self):
        model = MODEL.read_text()
        self.assertIn('bytes.Length>1024*1024', model)
        self.assertIn('Descriptor(request["death_mechanical_inputs"],1024*1024)', WORLD.read_text())
        ordinary = model[model.index('internal static SourceNativeDeathReadModel LoadAtBoundary(byte[] bytes)'):model.index('static SourceNativeDeathReadModel Decode')]
        self.assertIn('Changed qualified native death inputs', ordinary)
        self.assertIn('NativeQualifiedDeathMechanicalInputs', ordinary)
        self.assertNotIn('S050SourceDiagnosticDeathMechanicalProjection', ordinary)

    def test_s050_loader_checks_receipt_capture_and_current_pin_closure(self):
        model = MODEL.read_text()
        loader = model[model.index('internal static SourceNativeDeathReadModel LoadS050SourceDiagnostic'):model.index('internal static SourceNativeDeathReadModel LoadS074NoCameraDiagnostic')]
        for required in ('S050SourceDiagnosticDeathProjectionReceipt', 'source_pin_count',
                         'VerifyGenericFocusedSourcePins', 'exact_pass_marker_count',
                         'S050 compact death projection changed captured original owner facts',
                         'runtime_admitted', 'S050 captured original Prepare/fold/death-assignment bits differ'):
            self.assertIn(required, loader)
        self.assertRegex(loader, r'PinnedFile\(\(string\)sourceDesc\["path"\],\(string\)sourceDesc\["sha256"\],4\*1024\*1024\)')
        self.assertRegex(loader, r'Exact\(receiptDesc,"death projection receipt",1024\*1024\)')


if __name__ == '__main__':
    unittest.main()
