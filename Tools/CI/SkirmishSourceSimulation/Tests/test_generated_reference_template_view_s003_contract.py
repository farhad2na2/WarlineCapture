import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[4]
VIEW = (REPO / "Tools/CI/SkirmishSourceSimulation/Adapters/419_GeneratedReferenceTemplateView.s003.cs").read_text()
CALLER = (REPO / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/307_GeneratedS002StartingWorld.lifetime.s003.cs").read_text()


class CurrentS003TemplateViewContract(unittest.TestCase):
    def test_current_capture_is_an_additive_proof_branch(self):
        self.assertIn('descriptor["current_capture_proof"] != null', VIEW)
        self.assertIn('ValidateCurrentS003Proof(descriptor,manifest,domain,source,sourceSha,expectedCaptureGraph)', VIEW)
        self.assertIn('Read(reference,out string referenceBinaryDirectory,expectedCaptureGraph)', CALLER)
        # No generated capture or T7 build/receipt output hash belongs in production source.
        self.assertNotIn("9b31b6863e80d3bd548cd78631a3385b1c7d0ed007da1785c00f1cb16d7a323e", VIEW)
        self.assertNotIn("45aed32434b6140700efd97dec9b16f4cf5836ca00608c2e83c1e274969b32b2", VIEW)

    def test_current_branch_binds_dynamic_receipts_to_capture_and_graph(self):
        for field in ("strict_capture_receipt", "importer_control_receipt", "source_graph"):
            self.assertIn('proof["' + field + '"]', VIEW)
        self.assertIn('strict["capture_sha256"]!=sourceSha', VIEW)
        self.assertIn('strict["graph_sha256"]!=(string)graphDescriptor["sha256"]', VIEW)
        self.assertIn('!JToken.DeepEquals(graphDescriptor,expectedCaptureGraph)', VIEW)
        self.assertIn('capture["strict_receipt_sha256"]!=(string)strictDescriptor["sha256"]', VIEW)

    def test_runtime_and_capture_graphs_are_separately_bound(self):
        self.assertIn('SourceConfigurationQualification.Context(request)', CALLER)
        self.assertIn('qualifiedContext["compiler_graph"]as JObject', CALLER)
        self.assertIn('qualifiedContext["source_graph"]as JObject', CALLER)
        self.assertIn('!JToken.DeepEquals(runtimeGraph,compilerGraph)', CALLER)
        self.assertNotIn('Read(reference,out string referenceBinaryDirectory,request["source_config_graph"] as JObject)', CALLER)

    def test_wrong_runtime_graph_mutant_is_rejected_before_template_read(self):
        guard = CALLER.index('!JToken.DeepEquals(runtimeGraph,compilerGraph)')
        read = CALLER.index('GeneratedReferenceTemplateView.Read(reference,out string referenceBinaryDirectory,expectedCaptureGraph)')
        self.assertLess(guard, read)

    def test_wrong_capture_graph_mutant_is_rejected_by_exact_context_binding(self):
        self.assertIn('!JToken.DeepEquals(graphDescriptor,expectedCaptureGraph)', VIEW)
        self.assertIn('Focused prefab proof graph differs from validated qualification capture graph', VIEW)

    def test_current_branch_checks_full_importer_build_pin_map(self):
        self.assertIn('foreach(var pin in pins.Properties())', VIEW)
        self.assertIn('AutonomousWorld.PinnedFile(pinPath,pinSha,64*1024*1024)', VIEW)
        self.assertIn('pins[importerPath]!=importerSha', VIEW)
        self.assertIn('EndsWith("/Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs"', VIEW)

    def test_current_source_scope_and_vfx_omissions_fail_closed(self):
        for expr in (
            'domain["compiler_seed"]!=104732', 'domain["compiler_setup_hash"]!=2394754492u',
            'domain["mission_ready"]!=false', 'strict["mission_ready"]!=false',
            'imported["unsupported_non_vfx_rejected_before_allocation"]!=true',
            'imported["successful_import_cleanup"]!=true',
            'ValidateUnsupportedVfxOnly((JArray)domain["unsupported_values"])',
        ):
            self.assertIn(expr, VIEW)
        self.assertIn('"Game.Components.AirMissileLauncherVfxReferenceComponent/AirTargetImpactPrefab"', VIEW)
        self.assertIn('"Game.Components.GroundMissileLauncherVfxReferenceComponent/MissileVisualPrefab"', VIEW)

    def test_historical_focused_source_allowlist_is_preserved(self):
        for sha in (
            "21e7474adfe2682a638bc6c23708f48b9611c526051871c0d93b4cb18a6ae4aa",
            "83aec3d79c2ac069527c438be1056df9639e40f325b4a3a3c479e090996dd70f",
            "15f69a537018de732331af82f4591a30dbc674a9689c40481e1b3f7324732bcc",
            "fa8933a86f1ada977e9f10b53a0af770c79b43e8c73da49da385a192985f8969",
            "05bfd096a7dab53ecb813d80b474ce33db0b411da2fe7405a1cbc00a3a340bad",
        ):
            self.assertIn(sha, VIEW)


if __name__ == "__main__":
    unittest.main()
