import importlib.util
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[5]
GENERATOR_PATH = Path(__file__).with_name('generate.py')
SPEC = importlib.util.spec_from_file_location('source_map_host_generate', GENERATOR_PATH)
GENERATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GENERATOR)
OLD_GENERATED_HOST = ROOT / 'T Logs/SkirmishProduction/s050-source-map-host-overlay-20261115-r3/generated/AutonomousWorld.cs'
CANONICAL_HOST = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/000-000_AutonomousWorld.cs'
FRESH_STAGED_HOST = ROOT / 'T Logs/SkirmishProduction/s050-s026-handoff-20261115-r1/generated/AutonomousWorld.cs'
OLD_GENERATED_WORLD = ROOT / 'T Logs/SkirmishProduction/s050-source-map-host-overlay-20261115-r3/generated/StartingWorld.cs'
FRESH_STAGED_WORLD = ROOT / 'T Logs/SkirmishProduction/s050-s026-handoff-20261115-r1/generated/StartingWorld.cs'
CANONICAL_WORLD = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/307_GeneratedS002StartingWorld.lifetime.cs'


class S050DeathDispatchRebaseTests(unittest.TestCase):
    def test_exact_s050_dispatch_rebases_from_canonical_without_changing_qualified_routes(self):
        result = GENERATOR.merge_s074_no_camera_host(OLD_GENERATED_HOST.read_text(), CANONICAL_HOST.read_text())
        self.assertIn('Setup.CatalogId=="S050"&&deathDescriptor?.Count==1', result)
        self.assertIn('SourceNativeDeathReadModel.LoadS050SourceDiagnostic(s050DeathDiagnostic)', result)
        self.assertIn('SourceNativeDeathReadModel.LoadS074NoCameraDiagnostic(deathDescriptor)', result)
        self.assertIn('SourceNativeDeathReadModel.LoadS026Diagnostic(deathDescriptor)', result)
        self.assertIn('Descriptor(request["death_mechanical_inputs"],1024*1024)', result)

    def test_rejects_unrelated_death_boundary_drift_while_rebasing(self):
        old = OLD_GENERATED_HOST.read_text().replace('LoadS026Diagnostic(deathDescriptor)', 'OtherLoader(deathDescriptor)', 1)
        with self.assertRaisesRegex(ValueError, 'differs outside the exact S050'):
            GENERATOR.merge_s074_no_camera_host(old, CANONICAL_HOST.read_text())

    def test_strips_only_exact_prior_overlay_or_accepts_exact_bare_staged_host(self):
        prior = GENERATOR.strip_existing_source_map_host_fields(OLD_GENERATED_HOST.read_text())
        fresh = GENERATOR.strip_existing_source_map_host_fields(FRESH_STAGED_HOST.read_text())
        self.assertIn('private SourceStagedLaunch stagedLaunch;', prior)
        self.assertIn('private SourceStagedLaunch stagedLaunch;', fresh)
        for text in (prior, fresh):
            self.assertNotIn('SourceDerivedMapRuntime', text)
            self.assertNotIn('BindSourceDerivedMap', text)

    def test_accepts_fresh_staged_host_when_death_block_matches_current_canonical(self):
        host = FRESH_STAGED_HOST.read_text()
        self.assertEqual(GENERATOR.merge_s074_no_camera_host(host, CANONICAL_HOST.read_text()), host)

    def test_rejects_partial_or_ambiguous_prior_overlay(self):
        host = OLD_GENERATED_HOST.read_text().replace('internal JObject SourceDerivedMapReceipt', 'internal int SourceDerivedMapReceipt', 1)
        with self.assertRaisesRegex(ValueError, 'exact prior source-map host fields'):
            GENERATOR.strip_existing_source_map_host_fields(host)
        with self.assertRaisesRegex(ValueError, 'exact prior source-map host fields'):
            GENERATOR.strip_existing_source_map_host_fields('class NotAHost {}')

    def test_strips_exact_existing_world_overlay_or_accepts_bare_staged_world(self):
        old = GENERATOR.strip_existing_source_map_starting_world(OLD_GENERATED_WORLD.read_text())
        fresh = GENERATOR.strip_existing_source_map_starting_world(FRESH_STAGED_WORLD.read_text())
        for text in (old, fresh):
            self.assertNotIn('sourceDerivedMap', text)
            self.assertNotIn('SourceDerivedMapReceipt', text)
            self.assertIn('if(StartingPipeline.SubmitOrBind(Session,Setup,out var reason)', text)

    def test_rejects_partial_starting_world_overlay_drift(self):
        world = FRESH_STAGED_WORLD.read_text().replace('var waterMask=domain.BuildWaterMask',
            'var waterMask=sourceDerivedMap!=null?sourceDerivedMap.Water:domain.BuildWaterMask', 1)
        with self.assertRaisesRegex(ValueError, 'exact prior source-map StartingWorld overlay'):
            GENERATOR.strip_existing_source_map_starting_world(world)

    def test_starting_world_reader_accepts_stale_and_current_exact_shapes_only(self):
        canonical = CANONICAL_WORLD.read_text()
        current_call = 'GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,SourceRepositoryRoot,request)'
        stale_call = 'GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,SourceRepositoryRoot)'
        stale = canonical.replace(current_call, stale_call, 1)
        self.assertEqual(GENERATOR.splice_current_starting_world_reader(canonical, canonical), canonical)
        self.assertEqual(GENERATOR.splice_current_starting_world_reader(stale, canonical), canonical)
        drift = canonical.replace(current_call, 'GeneratedReferenceTemplateView.Read(reference)', 1)
        with self.assertRaisesRegex(ValueError, 'exact stale form or exact current'):
            GENERATOR.splice_current_starting_world_reader(drift, canonical)

    def test_clearance_overlay_may_be_repository_owned_only_and_is_still_content_checked(self):
        canonical = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/527_GeneratedS002DropClearance.cs'
        overlay = ROOT / 'T Logs/SkirmishProduction/s050-source-map-host-overlay-20261115-r3/generated/SourceDerivedClearance.cs'
        source, candidate = GENERATOR.validate_clearance_overlay_path(canonical, overlay, ROOT)
        self.assertEqual(source, canonical.resolve())
        self.assertEqual(candidate, overlay.resolve())
        with self.assertRaisesRegex(ValueError, 'repository-owned'):
            GENERATOR.validate_clearance_overlay_path(canonical, Path('/private/tmp/s050-death-mechanical-readset-20261115-r3.log'), ROOT)


if __name__ == '__main__':
    unittest.main()
