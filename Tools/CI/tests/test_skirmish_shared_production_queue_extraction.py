"""Controls that native and source queue owners compile the same queue transaction body."""
from pathlib import Path
import importlib.util
import shutil
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[3]
GENERATOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_delivery_queue.py"
INPUTS = (
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
    "Assets/Game/Scripts/Configs/BuildingProductionRecipe.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionSlotUtilitySystemHelper.cs",
)
SOURCE_PREP = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/prepare_queue.py"


def load_generator():
    spec = importlib.util.spec_from_file_location("regenerate_delivery_queue", GENERATOR)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SharedProductionQueueExtractionTests(unittest.TestCase):
    def test_source_host_contains_same_transaction_and_native_wrapper_delegates_to_it(self):
        with tempfile.TemporaryDirectory(prefix="shared-production-queue-") as temporary:
            root = Path(temporary)
            for relative in INPUTS:
                destination = root / relative
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(ROOT / relative, destination)

            output = root / "generated"
            receipt = load_generator().generate(root, output)
            self.assertIn(
                "TryQueuePlayerUnitFromBuildingShared",
                receipt["method_inputs"],
            )
            projected = (output / "BuildingProductionQueueCompositionSystemHelper.delivery.cs").read_text()
            shared = projected[projected.index("TryQueuePlayerUnitFromBuildingShared<") :]
            self.assertIn("TryRefundFailedDispatch", shared)
            self.assertIn("priorPending[i].StartedAt = priorStartedAt[i]", shared)
            self.assertIn("priorPending[i].ReadyAt = priorReadyAt[i]", shared)
            self.assertLess(shared.index("TryReserve("), shared.index("acquirePendingProduction()"))

        native = (ROOT / INPUTS[0]).read_text()
        native_entry = native[native.index("internal bool TryQueuePlayerUnitFromBuilding(") :]
        native_entry = native_entry[: native_entry.index("// One presentation-free queue transaction")]
        self.assertIn("return TryQueuePlayerUnitFromBuildingShared(", native_entry)

        source_call = SOURCE_PREP.read_text()
        self.assertIn("Production.TryQueuePlayerUnitFromBuildingShared<SourcePendingProduction>(", source_call)
        self.assertIn("SourceAcquirePending,SourceReleasePending", source_call)


if __name__ == "__main__":
    unittest.main()
