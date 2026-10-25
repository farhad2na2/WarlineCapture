"""Assembly ownership/coverage controls; no compiler, Unity or world execution."""
import json,sys,unittest,subprocess,tempfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import skirmish_owning_assembly_validation as m

def data(value):return json.dumps(value).encode()
class OwningAssemblyTests(unittest.TestCase):
 def fixture(self):return{'Assets/Game/Game.Runtime.asmdef':data({'name':'Game.Runtime','references':['Game.Configs']}),'Assets/Game/Configs/Game.Configs.asmdef':data({'name':'Game.Configs','references':[]}),'Assets/Game/Thing.cs':b'class Root {}','Assets/Game/Configs/Changed.cs':b'class Changed {}','Assets/Game/Configs/Untouched.cs':b'class FullOwner {}'}
 def test_nearest_asmdef_owns_config(self):self.assertEqual(m.discover(self.fixture())[1]['Assets/Game/Configs/Changed.cs'],'Game.Configs')
 def test_affected_owner_automatic_not_hardcoded_runtime(self):
  _,owners,_,order,coverage=m.choose_owners(self.fixture(),['Assets/Game/Configs/Changed.cs'],['Game.Runtime']);self.assertEqual(order,['Game.Configs','Game.Runtime']);self.assertEqual(coverage['Assets/Game/Configs/Changed.cs'],{'owner':'Game.Configs','compiled_count':0});self.assertEqual(sum(x=='Game.Configs'for x in owners.values()),2)
 def test_new_boundary_changes_virtual_ownership(self):
  files=self.fixture();files['Assets/Game/Configs/Sub/New.asmdef']=data({'name':'New','references':[]});files['Assets/Game/Configs/Sub/Changed.cs']=b'class NewThing {}';self.assertEqual(m.discover(files)[1]['Assets/Game/Configs/Sub/Changed.cs'],'New')
 def test_guid_asmref_resolves(self):
  files=self.fixture();files['Assets/Game/Configs/Game.Configs.asmdef.meta']=b'guid: abc\n';files['Assets/Elsewhere/Link.asmref']=data({'reference':'GUID:abc'});files['Assets/Elsewhere/Owned.cs']=b'class Owned {}';self.assertEqual(m.discover(files)[1]['Assets/Elsewhere/Owned.cs'],'Game.Configs')
 def test_unknown_asmref_rejects(self):
  files=self.fixture();files['Assets/Elsewhere/Link.asmref']=data({'reference':'GUID:missing'})
  with self.assertRaisesRegex(ValueError,'Unknown asmref'):m.discover(files)
 def test_cycle_rejects(self):
  files=self.fixture();files['Assets/Game/Configs/Game.Configs.asmdef']=data({'name':'Game.Configs','references':['Game.Runtime']})
  with self.assertRaisesRegex(ValueError,'cycle'):m.choose_owners(files,['Assets/Game/Configs/Changed.cs'],['Game.Runtime'])
 def test_duplicate_assembly_name_rejects(self):
  files=self.fixture();files['Assets/Other/Game.Configs.asmdef']=data({'name':'Game.Configs','references':[]})
  with self.assertRaisesRegex(ValueError,'Duplicate assembly'):m.discover(files)
 def test_unowned_default_assembly_is_not_silently_compiled_elsewhere(self):
  files=self.fixture();files['Assets/Outside/Changed.cs']=b'class Outside {}'
  with self.assertRaisesRegex(ValueError,'Explicit existing owning assemblies'):m.choose_owners(files,['Assets/Outside/Changed.cs'])
 def test_changed_asmref_is_explicitly_unsupported(self):
  before=self.fixture();after=dict(before);after['Assets/Link.asmref']=data({'reference':'Game.Configs'})
  with self.assertRaisesRegex(ValueError,'boundary/meta edits unsupported'):m.check_supported_boundaries(before,after,['Assets/Link.asmref'])
 def test_changed_asmdef_meta_is_explicitly_unsupported(self):
  before=self.fixture();before['Assets/Game/Game.Runtime.asmdef.meta']=b'guid: old';after=dict(before);after['Assets/Game/Game.Runtime.asmdef.meta']=b'guid: new'
  with self.assertRaisesRegex(ValueError,'boundary/meta edits unsupported'):m.check_supported_boundaries(before,after,['Assets/Game/Game.Runtime.asmdef.meta'])
 def test_new_asmdef_boundary_is_explicitly_unsupported(self):
  before=self.fixture();after=dict(before);after['Assets/New/New.asmdef']=data({'name':'New'})
  with self.assertRaisesRegex(ValueError,'New assembly boundary unsupported'):m.check_supported_boundaries(before,after,['Assets/New/New.asmdef'])
 def test_reference_only_asmdef_edit_remains_supported(self):
  before=self.fixture();after=dict(before);path='Assets/Game/Configs/Game.Configs.asmdef';after[path]=data({'name':'Game.Configs','references':['Game.Runtime']});m.check_supported_boundaries(before,after,[path])
 def test_rename_asmdef_rejects(self):
  before=self.fixture();after=dict(before);path='Assets/Game/Configs/Game.Configs.asmdef';after[path]=data({'name':'Changed','references':[]})
  with self.assertRaisesRegex(ValueError,'Only existing asmdef reference edits'):m.check_supported_boundaries(before,after,[path])
 def test_unsupported_profile_constraints_reject(self):
  for property,value in [('includePlatforms',['WindowsStandalone64']),('excludePlatforms',['LinuxStandalone64']),('defineConstraints',['CUSTOM']),('versionDefines',[{'name':'somePackage'}])]:
   with self.subTest(property=property),self.assertRaisesRegex(ValueError,'unsupported'):m.check_profile({'name':'A',property:value},'editor')
 def test_editor_only_not_mislabelled_noneditor(self):
  m.check_profile({'includePlatforms':['Editor']},'editor')
  with self.assertRaisesRegex(ValueError,'includePlatforms unsupported'):m.check_profile({'includePlatforms':['Editor']},'noneditor')
 def test_cli_requires_explicit_compiler_and_response_inputs(self):
  with tempfile.TemporaryDirectory()as tmp:
   out=Path(tmp)/'output';x=subprocess.run([sys.executable,str(Path(m.__file__)),'--repo',tmp,'--parent','unused','--payload',tmp,'--output',str(out),'--profile','editor'],capture_output=True,text=True)
   self.assertEqual(x.returncode,2);self.assertIn('--dotnet',x.stderr);self.assertIn('--csc',x.stderr);self.assertIn('--response-directory',x.stderr);self.assertFalse(out.exists())
 def test_editor_rsp_symbol_required(self):
  with self.assertRaisesRegex(ValueError,'missing UNITY_EDITOR'):m.check_defines('editor',['UNITY_STANDALONE'])
  m.check_defines('editor',['UNITY_EDITOR','UNITY_EDITOR_OSX']);m.check_defines('noneditor',['UNITY_STANDALONE'])
if __name__=='__main__':unittest.main()
