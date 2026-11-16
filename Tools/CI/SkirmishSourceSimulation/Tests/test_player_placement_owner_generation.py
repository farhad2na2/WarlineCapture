import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[4]
RECIPE=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/generate_player_placement.py"
spec=importlib.util.spec_from_file_location("player_placement_recipe",RECIPE);recipe=importlib.util.module_from_spec(spec);spec.loader.exec_module(recipe)

class PlayerPlacementGenerationControl(unittest.TestCase):
 def test_original_observation_body_is_enforced(self):
  with tempfile.TemporaryDirectory() as temporary:
   result=recipe.generate(ROOT,temporary)
   source=(ROOT/"Assets/Game/Scripts/Systems/BuildingConstructionResourceTransactionSystemHelper.cs").read_text()
   projected=(Path(temporary)/"Reservation.cs").read_text()
   self.assertEqual(recipe.body(source,"ObservePlacement("),recipe.body(projected,"ObservePlacement("))
   self.assertFalse(result["native_preview_bound"])
 def test_changed_observation_is_rejected_by_actual_generator(self):
  with tempfile.TemporaryDirectory() as temporary:
   target=Path(temporary)/"repo";out=Path(temporary)/"out"
   paths=["Tools/CI/SkirmishSourceSimulation/Adapters/PlayerPlacement/"+name for name in recipe.FILES]+["Assets/Game/Scripts/Simulation/ConstructionTransactionKernel.cs"]+["Assets/Game/Scripts/Systems/"+name for name in ("BuildingPlacementConstructionTransaction.cs","BuildingConstructionResourceTransactionSystemHelper.cs","BuildingPlacementSessionCompositionSystemHelper.cs","RuntimeFactionResourceSystemHelper.cs")]
   for name in paths:
    path=target/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes((ROOT/name).read_bytes())
   reservation=target/paths[1];text=reservation.read_text();self.assertEqual(text.count('"PlayerPlacementOutcome"'),1);reservation.write_text(text.replace('"PlayerPlacementOutcome"','"ChangedOutcome"'))
   with self.assertRaisesRegex(ValueError,"Original reservation method changed: ObservePlacement"):recipe.generate(target,out)
if __name__=="__main__":unittest.main()
