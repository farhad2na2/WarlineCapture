"""Fail-closed tests for the current helicopter diagnostic overload extraction."""
from pathlib import Path
import importlib.util
import shutil
import tempfile
import unittest


REPO = Path(__file__).resolve().parents[3]
GENERATOR = REPO / "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/generate.py"
INPUTS = (
    "Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs",
)


def load_generator():
    spec = importlib.util.spec_from_file_location("production_delivery_generate", GENERATOR)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class ProductionDeliveryDiagnosticExtractionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="s073-production-delivery-extraction-")
        self.root = Path(self.temp.name)
        for relative in INPUTS:
            target = self.root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO / relative, target)
        self.generator = load_generator()

    def tearDown(self):
        self.temp.cleanup()

    def test_current_diagnostic_overload_projects_functional_resolver_only(self):
        output = self.root / "generated"
        receipt = self.generator.generate(self.root, output)
        projected = (output / "SourceHelicopterDelivery.cs").read_text()
        self.assertEqual(receipt["kind"], "CURRENT_NATIVE_TYPED_GROUND_PRODUCTION_DELIVERY_PROJECTION")
        self.assertIn("HelicopterDropSearchDiagnostics diagnostics", projected)
        # The current extractor routes both the native lane path and the
        # presentation-free fixture through SourcePreferredClear. Keep checking
        # that this still reaches the extracted diagnostic resolver with the
        # actual retry radius and preserves its exhausted-budget result.
        compact = "".join(projected.split())
        self.assertIn(
            "boolSourcePreferredClear(RuntimeBuildingEntitybuilding,SourcePendingProductionpending,float3preferred,outint2cell,outfloat3committed)",
            compact,
        )
        self.assertIn(
            "SourceClear(building,pending,preferred,null,pending.TransportClearDropSearchStartRadius,null,outboolexhaustedSearchBudget,outintnextSearchRadius,outcell,outcommitted)",
            compact,
        )
        self.assertIn(
            "pending.TransportClearDropSearchStartRadius=exhaustedSearchBudget?nextSearchRadius:0;returnfalse;",
            compact,
        )
        self.assertIn("pending.TransportClearDropSearchStartRadius=exhaustedSearchBudget?nextSearchRadius:0", projected)
        self.assertNotIn("S073NativeProductionDeliveryObserver", projected)
        owner = (output / "SourceProductionDeliveryOwner.cs").read_text()
        self.assertIn('RecordDeliveryEvent("queue_accepted"', owner)
        self.assertIn('"source_time_seconds"', owner)
        self.assertIn('"entity_index"', owner)
        for event in ("helicopter_arrived", "helicopter_departed", "helicopter_drop_started",
                      "unit_spawned_and_placed", "unit_spawn_failed"):
            self.assertIn(event, projected)
        self.assertNotIn("S073NativeProductionDeliveryObserver", owner + projected)

    def test_generated_ground_and_helicopter_events_keep_actual_pending_receipt(self):
        output = self.root / "receipt-events"
        self.generator.generate(self.root, output)
        queue = (output / "SourceProductionDeliveryOwner.cs").read_text()
        helicopter = (output / "SourceHelicopterDelivery.cs").read_text()
        self.assertIn('"receipt_id"', queue)
        self.assertIn('Entity.Null,false,pending.ReceiptId', queue)
        self.assertIn('member,observed,p.ReceiptId', queue)
        self.assertIn('Entity.Null,false,p?.ReceiptId??0', queue)
        self.assertIn('pending?.ReceiptId??0', helicopter)
        self.assertIn('Entity.Null,true,pending.ReceiptId', helicopter)
        self.assertIn('spawned?member:Entity.Null,true,production.ReceiptId', helicopter)

    def test_unsupported_resolver_header_is_rejected_before_output(self):
        native = self.root / INPUTS[3]
        text = native.read_text()
        old = "HelicopterDropSearchDiagnostics diagnostics"
        self.assertIn(old, text)
        native.write_text(text.replace(old, "object diagnostics", 1))
        output = self.root / "rejected-header"
        with self.assertRaisesRegex(ValueError, "Unsupported current native member/header"):
            self.generator.generate(self.root, output)
        self.assertFalse(output.exists())

    def test_unsupported_diagnostic_caller_branch_is_rejected_before_output(self):
        managed = self.root / INPUTS[4]
        text = managed.read_text()
        old = "S073NativeProductionDeliveryObserver.ObserveHelicopterDropSearch("
        self.assertIn(old, text)
        managed.write_text(text.replace(old, "S073NativeProductionDeliveryObserver.ObserveUnknown(", 1))
        output = self.root / "rejected-caller"
        with self.assertRaisesRegex(ValueError, "Unsupported current native helicopter search diagnostic branch"):
            self.generator.generate(self.root, output)
        self.assertFalse((output / "SourceHelicopterDelivery.cs").exists())


if __name__ == "__main__":
    unittest.main()
