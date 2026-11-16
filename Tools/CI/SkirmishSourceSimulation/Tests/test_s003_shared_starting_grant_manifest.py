"""Keep current typed grant helpers in the S003 source-simulation manifest."""

import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[4]
MANIFEST = ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json"


class S003SharedStartingGrantManifestTests(unittest.TestCase):
    def test_shared_dependencies_are_current_native_sources(self):
        data = json.loads(MANIFEST.read_text(encoding="utf-8"))
        sources = {entry["id"]: entry for entry in data["sources"]}
        expected = {
            "typed-SkirmishStartingGrantPlacementConstraint": (
                "Assets/Game/Scripts/Runtime/Skirmish/SkirmishStartingGrantPlacementConstraint.cs",
                ["public static class SkirmishStartingGrantPlacementConstraint"],
            ),
            "typed-SkirmishNativeCheckpointStructureRequestGuard": (
                "Assets/Game/Scripts/Runtime/Skirmish/SkirmishNativeCheckpointStructureRequestGuard.cs",
                [
                    "internal struct SkirmishNativeCheckpointStructureRequestGuardTag",
                    "internal sealed class SkirmishNativeCheckpointStructureRequestGuard",
                ],
            ),
            "typed-BuildingRuntimeProcessingCompositionSystemHelper.TypedGrant": (
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.TypedGrant.cs",
                ["internal static bool IsExactCheckpointReconstructionRequest"],
            ),
        }
        for identity, (path, declarations) in expected.items():
            with self.subTest(identity=identity):
                entry = sources[identity]
                self.assertEqual(entry["kind"], "native_direct")
                self.assertEqual(entry["path"], path)
                self.assertEqual(entry["require_declarations"], declarations)

    def test_spawn_processor_binds_extracted_calls_to_current_helper(self):
        data = json.loads(MANIFEST.read_text(encoding="utf-8"))
        sources = {entry["id"]: entry for entry in data["sources"]}
        self.assertEqual(
            sources["336_RuntimeSpawnRequestProcessor.source.cs"]["bindings"],
            [
                {
                    "from": "IsExactCheckpointReconstructionRequest(em, request, definition.FootprintCells)",
                    "to": "BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(em, request, definition.FootprintCells)",
                    "count": 1,
                },
                {
                    "from": "TryResolveStartingGrantSetup(em, request.PlanEntity,",
                    "to": "BuildingRuntimeProcessingCompositionSystemHelper.TryResolveStartingGrantSetup(em, request.PlanEntity,",
                    "count": 1,
                },
            ],
        )


if __name__ == "__main__":
    unittest.main()
