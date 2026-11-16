#!/usr/bin/env python3
"""Source-contract controls for the opt-in disposable-copy clearance adapter."""
import hashlib
import json
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[4]
PRODUCTION = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073TacticalAIDisposableCopyFixture.Production.cs"
ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073CapturedClearanceProjection.cs"
S002_ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/527_GeneratedS002DropClearance.cs"
S002_SHA256 = "1fb293d2093e798a3c4cde1f93bf8a50a90315c3b2d4399866b316b9dd58eba4"
BRIDGE = ROOT / "Logs/SkirmishProduction/s073-source-clearance-identity-bridge-20261102-r1.json"


class S073SourceClearanceProjectionAdapterTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = PRODUCTION.read_text(encoding="utf-8")
        cls.adapter = ADAPTER.read_text(encoding="utf-8")

    def test_separate_opt_in_keeps_default_fail_closed_and_does_not_open_delivery(self):
        self.assertIn('"WARLINE_S073_SOURCE_CLEARANCE_PROJECTION_DIAGNOSTIC"', self.source)
        self.assertIn("sourceClearanceProjectionRequested", self.source)
        self.assertIn("sourceClearanceProjection != null", self.source)
        self.assertIn("This bounded S073 diagnostic stops at the real queue append and does not advance or deliver", self.source)
        self.assertIn('["callback_invoked"] = false', self.adapter)
        self.assertIn('["delivery_invoked"] = false', self.adapter)
        self.assertIn('["scheduler_admitted"] = false', self.adapter)
        self.assertNotIn("Camera", self.adapter)
        self.assertNotIn("randomState", self.adapter)
        self.assertNotIn("TryQueue", self.adapter)
        owner = self.source[self.source.index("static JObject RunSourceProductionOwnerOnCopy"):]
        owner = owner[:owner.index("namespace Game.Runtime")]
        self.assertNotIn("UpdatePendingProductions(", owner)
        self.assertNotIn("UpdateActiveProductionTransports(", owner)

    def test_exact_identity_bridge_and_current_copy_frame_are_required(self):
        for text in (
            's073-source-clearance-identity-bridge-20261102-r1.json',
            '"08b8431f6481f7ddabe6a044fb7f45d658021dce8380e533feb97dc19f3a4e53"',
            'campaign-physical-source.json',
            '"8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"',
            'OperationMap_IndustrialBasin.asset',
            '"b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a"',
            'source-world.json',
            '1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7'
            , 'RenderBoundsPresentation.cs'
            , '82bf379f5bb3e5ae55705f608c3e4a26ed915afffeec7488c79b9448cecfb5a4'
            , 'SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs'
            , 'f278e4383dfb4ae3ad3f00d7b1da4cedce75625cbdceeaff7bdf60d9d9614346'
        ):
            self.assertIn(text, self.source)
        self.assertIn("RequireGrid(sourceCopyGrid", self.adapter)
        self.assertIn("RequireGrid(world.Grid", self.adapter)
        self.assertIn("S073 disposable copy reservation mask required", self.adapter)
        self.assertIn("sourceEnvelope[2] != 2284f", self.adapter)
        self.assertIn("sourceEnvelope[3] != 1260f", self.adapter)
        self.assertIn("JToken.DeepEquals(sourceRecord[\"query_rect\"], sourceEnvelope)", self.adapter)

    def test_bridge_failure_flags_are_fail_closed_and_never_qualify_parity(self):
        bridge = json.loads(BRIDGE.read_text(encoding="utf-8"))
        self.assertEqual(bridge["kind"], "S073SourceClearanceIdentityBridge")
        for field in ("provider_parity_established", "historical_capture_dependency_pins_revalidated",
                      "source_capture_current_producer_equivalence", "capture_relocation_path_remapping_validated",
                      "delivery_established", "mission_ready", "source_fit_certified"):
            self.assertIs(bridge[field], False)
            self.assertIn(f'bridge["{field}"] != false', self.adapter)

    def test_original_source_filter_inequalities_and_reservation_padding_are_present(self):
        for fragment in (
            "extent < safeCellSize * 0.25f || extent > maxExtent",
            "axMax < xMin || ayMax < yMin || axMin >= xMax || ayMin >= yMax",
            "GridUtils.WorldToCell(grid, c - e)",
            "GridUtils.WorldToCell(grid, c + e)",
            "minCell.x - staticPadding",
            "maxCell.x + staticPadding + 1",
            "Local S073 drop query exceeds the complete captured source bounds envelope",
        ):
            self.assertIn(fragment, self.adapter)

    def test_compiled_differential_control_is_separate_and_compares_actual_full_masks(self):
        self.assertIn('"WARLINE_S073_SOURCE_CLEARANCE_DIFFERENTIAL_CONTROL"', self.source)
        self.assertIn("new GeneratedS002DropClearance(projectionPath, S002ProjectionSha256)", self.adapter)
        self.assertIn("baseline.Reserve(world, preferred, ref s002Mask)", self.adapter)
        self.assertIn("Reserve(world, preferred, ref s073Mask)", self.adapter)
        self.assertIn("for (int i = 0; i < checked(Width * Height); i++)", self.adapter)
        self.assertIn("s002Mask.IsSet(i), actual = s073Mask.IsSet(i)", self.adapter)
        self.assertIn("new int2(0, 0)", self.adapter)
        self.assertIn("new int2(Width - 1, Height - 1)", self.adapter)
        self.assertIn("new int2(2067, 1043)", self.adapter)
        self.assertIn("new int2(-21, 0)", self.adapter)
        self.assertIn('"compiled_differential_executed"] = true', self.adapter)
        self.assertIn('"source_provider_parity_qualified"] = false', self.adapter)
        self.assertIn('"delivery_invoked"] = false', self.adapter)
        projection = ROOT / "Logs/SkirmishProduction/s073-clearance-projection-input-20261102-r1/static-clearance-from-capture.json"
        self.assertEqual(hashlib.sha256(projection.read_bytes()).hexdigest(),
                         "8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065")

    def test_existing_s002_capture_and_strict_adapter_are_unchanged(self):
        digest = hashlib.sha256(S002_ADAPTER.read_bytes()).hexdigest()
        self.assertEqual(digest, S002_SHA256)
        s002 = S002_ADAPTER.read_text(encoding="utf-8")
        self.assertIn('"CurrentS002SourceOnlyInactiveFixture_NoActiveProviderEquivalence"', s002)
        self.assertIn("JToken.DeepEquals(record,native[\"fixedMapReadDomain\"]?[\"dropClearanceBounds\"])", s002)


if __name__ == "__main__":
    unittest.main()
