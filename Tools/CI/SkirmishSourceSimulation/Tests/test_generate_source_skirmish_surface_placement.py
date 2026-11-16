import importlib.util,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];source=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/generate_source_skirmish_surface_placement.py";spec=importlib.util.spec_from_file_location("surface_owner",source);mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
class SourceSurfaceExtractionTests(unittest.TestCase):
 def test_exact_original_body_and_thresholds(self):
  native=(ROOT/mod.SOURCE).read_text();constants,body=mod.extract(native)
  with tempfile.TemporaryDirectory() as tmp:
   result=mod.generate(ROOT,tmp);generated=(Path(tmp)/"SourceSkirmishSurfacePlacementOwner.cs").read_text()
   self.assertIn(body,generated)
   for c in constants:self.assertIn(c,generated)
   self.assertEqual(len(result["source_inputs"]),1)
 def test_missing_owner_rejected(self):
  native=(ROOT/mod.SOURCE).read_text().replace(mod.SIGNATURE,"internal static bool ChangedName()")
  with self.assertRaisesRegex(ValueError,"Exact original surface owner"):mod.extract(native)
 def test_duplicate_threshold_rejected(self):
  native=(ROOT/mod.SOURCE).read_text()+"\nprivate const float SkirmishMaxBuildingSlopeDegrees = 8f;\n"
  with self.assertRaisesRegex(ValueError,"Exact positive original surface threshold"):mod.extract(native)
 def test_threshold_is_extracted_from_source(self):
  native=(ROOT/mod.SOURCE).read_text().replace("SkirmishMaxBuildingSlopeDegrees = 8f;","SkirmishMaxBuildingSlopeDegrees = 7f;")
  constants,_=mod.extract(native);self.assertIn("private const float SkirmishMaxBuildingSlopeDegrees = 7f;",constants)
if __name__=="__main__":unittest.main()
