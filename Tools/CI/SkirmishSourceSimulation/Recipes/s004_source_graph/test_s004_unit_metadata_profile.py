import tempfile
import unittest
from pathlib import Path

import s004_unit_metadata_profile as profile

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/413_GeneratedUnitProductionMetadata.cs'
PRODUCTION = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/522_GeneratedS002ProductionInputs.cs'
CATALOG = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/473_GeneratedS002CatalogAvailabilityBinding.cs'
FAMILY = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceGroundMissionFamily.cs'
PAID = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/530_GeneratedS002PaidContext.cs'
TICK = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/528_GeneratedS002ProductionTick.cs'


class UnitMetadataProfileTests(unittest.TestCase):
    def test_generated_profile_keeps_original_loader_and_qualification(self):
        original = SOURCE.read_bytes()
        with tempfile.TemporaryDirectory() as directory:
            arguments = ['-define:EXISTING', str(SOURCE)]
            profile.apply(arguments, Path(directory), 'a' * 64)
            generated = Path(arguments[1]).read_text()
            guard = (Path(directory) / 'S004DiagnosticUnitMetadataGuard.cs').read_text()
            self.assertIn('s004Candidate?20:', generated)
            self.assertIn('sha, compatibility?.FactoryDomainQualified==true, sourceAuthority!=null)', generated)
            self.assertIn('sourceAuthority!=null?sourceAuthority.UnitCount:', generated)
            self.assertIn('runtime_admission', guard)
            self.assertIn('a' * 64, guard)
            self.assertIn('WARLINE_S004_UNIT_METADATA_DIAGNOSTIC', arguments[0])
        self.assertEqual(SOURCE.read_bytes(), original)

    def test_wrong_or_ambiguous_source_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(ValueError):
                profile.apply(['-define:X', str(SOURCE), str(SOURCE)], Path(directory), 'a' * 64)

    def test_historical_count_guard_remains_supported(self):
        current='units.Count != (sourceAuthority!=null?sourceAuthority.UnitCount:compatibility?.FactoryDomainQualified==true?compatibility.UnitCount:12)'
        historical='units.Count != (compatibility?.FactoryDomainQualified==true?compatibility.UnitCount:12)'
        text=SOURCE.read_text();self.assertEqual(text.count(current),1)
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);source=root/'413_GeneratedUnitProductionMetadata.cs'
            source.write_text(text.replace(current,historical));arguments=['-define:EXISTING',str(source)]
            profile.apply(arguments,root,'a'*64,True)
            self.assertIn('units.Count != (s004Candidate?19:(compatibility?.FactoryDomainQualified==true?compatibility.UnitCount:12))',
                          Path(arguments[1]).read_text())

    def test_catalog_projection_checks_original_owner_before_skipping(self):
        original = PRODUCTION.read_bytes()
        with tempfile.TemporaryDirectory() as directory:
            arguments = [str(PRODUCTION)]
            profile.apply_catalog_rejection_probe(arguments, Path(directory))
            projected = Path(arguments[0]).read_text()
            self.assertIn('SkirmishNativeProduction.CanQueue(Entities, Session, building, row.Prefab)', projected)
            self.assertIn('Source catalog rejection proof changed', projected)
            self.assertIn('if (!catalog.TryRole(slot.SpawnUnitPrefab.name, out _))', projected)
        self.assertEqual(PRODUCTION.read_bytes(), original)

    def test_catalog_getter_projection_scopes_source_sprite_codec(self):
        original = CATALOG.read_bytes()
        with tempfile.TemporaryDirectory() as directory:
            arguments = ['-define:EXISTING', str(CATALOG)]
            profile.apply_catalog_getter_probe(arguments, Path(directory), 'b' * 64)
            projected = Path(arguments[1]).read_text()
            self.assertIn('s004Source&&(string)a?["codec"]=="source-sprite-reference-v1"', projected)
            self.assertIn('(!sourceSprite&&string.IsNullOrEmpty(key))', projected)
            self.assertIn('admitted.ArtifactSha256!=', (Path(directory) / 'S004CatalogSourceGuard.cs').read_text())
        self.assertEqual(CATALOG.read_bytes(), original)

    def test_catalog_binding_uses_exact_compiled_setup_boundary(self):
        original = FAMILY.read_bytes()
        with tempfile.TemporaryDirectory() as directory:
            arguments = ['-define:EXISTING', str(FAMILY)]
            profile.apply_catalog_binding_probe(arguments, Path(directory), Path('/test/setup.json'), 'c' * 64)
            projected = Path(arguments[1]).read_text()
            self.assertIn('SourceS004CompiledSetupInput.RequireExact(setup,', projected)
            self.assertIn('c' * 64, projected)
            self.assertIn('WARLINE_S004_CATALOG_BINDING_PROBE', arguments[0])
        self.assertEqual(FAMILY.read_bytes(), original)

    def test_role_domain_is_explicit_and_remains_unqualified(self):
        with tempfile.TemporaryDirectory() as directory:
            arguments=['-define:EXISTING',str(SOURCE),str(CATALOG)]
            profile.apply(arguments,Path(directory),'d'*64,True)
            profile.apply_catalog_getter_probe(arguments,Path(directory),'d'*64,True)
            materializer=Path(arguments[1]).read_text();catalog=Path(arguments[2]).read_text()
            self.assertIn('s004Candidate?19:',materializer)
            self.assertIn('s004Source?19:12',catalog)
            self.assertIn('!admitted.SourceAuthoredDomainQualified',catalog)
            for name in ('S004DiagnosticUnitMetadataGuard.cs','S004CatalogSourceGuard.cs'):
                guard=(Path(directory)/name).read_text()
                self.assertIn('S004SourceRoleCatalogMetadataCandidate',guard)
                self.assertIn('runtime_admission',guard)

    def test_unplaced_airport_guards_faction_and_production_ticks(self):
        with tempfile.TemporaryDirectory() as directory:
            arguments=[str(PRODUCTION),str(PAID),str(TICK)]
            profile.apply_unplaced_airport_probe(arguments,Path(directory))
            paid=Path(arguments[1]).read_text();tick=Path(arguments[2]).read_text()
            self.assertIn('SourceCampProvenNoRunwayReads(Buildings,true)',paid)
            self.assertIn('AdvanceActualFactionRequests(){RequireS004NoLiveRunway();',paid)
            self.assertIn('AdvanceActualPaidProduction(){RequireS004NoLiveRunway();',tick)
            self.assertIn('building.Definition.HasRunway', (Path(directory)/'S004NoLiveRunwayGuard.cs').read_text())

    def test_retained_map_probe_reuses_existing_paid_context_only(self):
        with tempfile.TemporaryDirectory() as directory:
            arguments=[str(PRODUCTION),str(PAID),str(TICK)]
            profile.apply_unplaced_airport_probe(arguments,Path(directory))
            profile.apply_retained_map_probe(arguments,Path(directory))
            paid=Path(arguments[1]).read_text()
            self.assertIn('RequireS004RetainedMapPublication();',paid)
            self.assertNotIn('InitializeActualSelectedOperationMapPublication();',paid)


if __name__ == '__main__':
    unittest.main()
