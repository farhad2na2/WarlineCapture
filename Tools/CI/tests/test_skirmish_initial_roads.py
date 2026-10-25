import hashlib,json,os,tempfile,unittest
from pathlib import Path
from unittest.mock import patch

HERE=Path(__file__).resolve().parents[1]/'SkirmishSourceSimulation'
RECIPE=HERE/'Recipes/regenerate_initial_roads.py'
NATIVE=Path(os.environ.get('SKIRMISH_SOURCE_REPO',str(HERE.parents[2]))).resolve()
NAMES=['Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs','Assets/Game/Scripts/Configs/GameplayConfigModels.cs','Assets/Game/Scripts/Systems/RoadBuildCompositionSystemHelper.cs','Assets/Game/Scripts/Systems/RoadBuildCompositionSourceCompositionSystemHelper.cs','Assets/Game/Scripts/Systems/RoadBuildCompositionLifecycleCompositionSystemHelper.cs','Assets/Game/Scripts/Environment/RuntimeCityStartupSystemHelper.cs','Assets/Game/Scripts/Systems/RoadNetworkCompositionSystemHelper.cs']

def load():
 raw=RECIPE.read_bytes();ns={'__name__':'owned_road_recipe_controls','__file__':str(RECIPE)};exec(compile(raw,str(RECIPE),'exec'),ns);return ns

class InitialRoadRecipeTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(prefix='road-source-controls-');self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)/'repo';self.out=Path(self.tmp.name)/'out';self.module=load()
  for name in NAMES:
   p=self.root/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes((NATIVE/name).read_bytes())
 def change(self,name,old,new):
  p=self.root/name;s=p.read_text();self.assertIn(old,s);p.write_text(s.replace(old,new,1))
 def test_complete_original_sync_clear_and_queries(self):
  proof=self.module['generate'](self.root,self.out);generated=(self.out/'SourceFreshRoadProjection.cs').read_text();native=(self.root/NAMES[0]).read_text()
  for signature in ['public void SyncRoadCellsToEcs(Context context)','private bool TryGetRoadBuffers(out RoadBuffersData roadBuffers)','private static void ClearRoadBuffers(RoadBuffersData roadBuffers)','public static bool IsGridCellCenterInsideBounds(','private static void GetGridBounds(']:self.assertIn(self.module['member'](native,signature),generated)
  self.assertFalse(proof['nonempty_road_capability_admitted']);self.assertEqual(proof['output'],hashlib.sha256((self.out/'SourceFreshRoadProjection.cs').read_bytes()).hexdigest())
  for path,sha in proof['source_inputs'].items():self.assertEqual(sha,hashlib.sha256(Path(path).read_bytes()).hexdigest())
 def test_current_native_getter_regenerates(self):
  self.change(NAMES[1],'public float RoadGridSize => roadGridSize;','public float RoadGridSize => Mathf.Max(.25f, roadGridSize);');self.module['generate'](self.root,self.out);self.assertIn('NativeRoadGridSize(float roadGridSize)=>Mathf.Max(.25f, roadGridSize);',(self.out/'SourceFreshRoadProjection.cs').read_text())
 def test_query_domain_change_rejected(self):
  self.change(NAMES[0],'ComponentType.ReadOnly<DynamicBlockerComponent>()','ComponentType.ReadOnly<GridWalkable>()')
  with self.assertRaisesRegex(ValueError,'query domain'):self.module['generate'](self.root,self.out)
 def test_population_entrance_change_rejected(self):
  self.change(NAMES[4],'public void Init(','public void Init(')
  p=self.root/NAMES[4];s=p.read_text();signature='public void Init(';start=s.index(signature);brace=s.index('{',start);p.write_text(s[:brace+1]+'\n // context.RoadTiles.Add requires real original population owner\n'+s[brace+1:])
  with self.assertRaisesRegex(ValueError,'population'):self.module['generate'](self.root,self.out)
 def test_source_drift_during_generation_rejected(self):
  original=self.module['write_lf']
  def write_then_change(path,text):
   original(path,text);p=self.root/NAMES[0];p.write_text(p.read_text()+'\n// actual fixture epoch changed\n')
  self.module['write_lf']=write_then_change
  with self.assertRaisesRegex(ValueError,'source drift'):self.module['generate'](self.root,self.out)
 def test_crlf_has_exact_raw_pins_and_portable_output(self):
  for name in NAMES:
   p=self.root/name;p.write_bytes(p.read_bytes().replace(b'\r\n',b'\n').replace(b'\n',b'\r\n'))
  proof=self.module['generate'](self.root,self.out);self.assertNotIn(b'\r',(self.out/'SourceFreshRoadProjection.cs').read_bytes());self.assertEqual(proof['output'],hashlib.sha256((self.out/'SourceFreshRoadProjection.cs').read_bytes()).hexdigest())
  for path,sha in proof['source_inputs'].items():self.assertEqual(sha,hashlib.sha256(Path(path).read_bytes()).hexdigest())

if __name__=='__main__':unittest.main()
