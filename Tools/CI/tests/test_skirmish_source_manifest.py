import importlib.util,json,shutil,sys,tempfile,unittest,os
from pathlib import Path
from unittest.mock import patch
REAL=Path(os.environ.get("SKIRMISH_SOURCE_REPO",str(Path.cwd()))).resolve()
sys.dont_write_bytecode=True
sys.path.insert(0,str(REAL/"Tools/CI"))
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import skirmish_causal_worker,skirmish_source_syntax
SPEC=importlib.util.spec_from_file_location("source_manifest",Path(__file__).resolve().parents[1]/"skirmish_source_manifest.py")
m=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(m)
class SourceManifestTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.base=Path(self.tmp.name);self.repo=self.base/"repo";self.package=self.base/"package";(self.repo/"Tools/CI").mkdir(parents=True);self.package.mkdir()
  for name in ["skirmish_causal_worker.py"]:shutil.copyfile(REAL/"Tools/CI"/name,self.repo/"Tools/CI"/name)
  (self.repo/"A.cs").write_text("public static class A { public static int F(){return 1;} public static int G(){return 3;} }\n")
 def manifest(self,entries,defines=None):
  path=self.base/"manifest.json";path.write_text(json.dumps({"schema_version":1,"profile":{"mission":"S002","seed":104731,"numeric_runtime":"InstalledMono"},"compiler_defines":defines or[],"sources":entries}));return path
 def runresolve(self,entries,name="out",defines=None):
  return m.resolve_manifest(self.repo,self.package,self.manifest(entries,defines),self.base/name)
 def entry(self,output="A.cs"):
  return {"id":"original","kind":"current_original","path":"A.cs","signature":"public static int F()","prefix":"public class X{","suffix":"}","output":output}
 def test_native_direct_records_fresh_hash_without_old_pin(self):
  e={"id":"direct","kind":"native_direct","path":"A.cs"};a=self.runresolve([e],"one");(self.repo/"A.cs").write_text("public class A{public int Updated;}")
  b=self.runresolve([e],"two");self.assertNotEqual(a["resolved"][0]["sha256"],b["resolved"][0]["sha256"])
 def test_current_body_fix_regenerates_without_old_body_hash(self):
  self.runresolve([self.entry()],"one");self.assertIn("return 1",(self.base/"one/generated/A.cs").read_text())
  p=self.repo/"A.cs";p.write_text(p.read_text().replace("return 1","return 2"));self.runresolve([self.entry()],"two");self.assertIn("return 2",(self.base/"two/generated/A.cs").read_text())
 def test_crlf_working_projection_keeps_raw_pin_and_emits_lf(self):
  path=self.repo/"A.cs";raw=b"public static class A {\r\n public static int F(){\r\n return 1;\r\n }\r\n}\r\n";path.write_bytes(raw)
  result=self.runresolve([self.entry()]);self.assertEqual(result["pins"][str(path.resolve())],m.sha(raw));self.assertNotIn(b"\r",(self.base/"out/generated/A.cs").read_bytes())
 def test_repeated_read_mutation_rejected(self):
  p=self.repo/"A.cs";pins={};m.read(p,pins);p.write_text("changed")
  with self.assertRaisesRegex(m.UnsupportedSourceShape,"between member reads"):m.read(p,pins)
 def test_final_pin_recheck_catches_earlier_source_change(self):
  (self.repo/"B.cs").write_text("public class B{}");real=m.read
  def race(path,pins):
   text=real(path,pins)
   if path.name=="B.cs":(self.repo/"A.cs").write_text("changed after its read")
   return text
  entries=[{"id":"a","kind":"native_direct","path":"A.cs"},{"id":"b","kind":"native_direct","path":"B.cs"}]
  with patch.object(m,"read",side_effect=race):
   with self.assertRaisesRegex(m.UnsupportedSourceShape,"before emission"):self.runresolve(entries)
  self.assertFalse((self.base/"out").exists())
 def test_helper_and_manifest_pin_rechecked(self):
  real=m.read
  def race(path,pins):
   value=real(path,pins);(self.repo/"Tools/CI/skirmish_causal_worker.py").write_text("changed");return value
  with patch.object(m,"read",side_effect=race):
   with self.assertRaisesRegex(m.UnsupportedSourceShape,"before emission"):self.runresolve([{"id":"a","kind":"native_direct","path":"A.cs"}])
 def test_destinations_reject_noncanonical_forms(self):
  for output in ["", ".", "./A.cs","x//A.cs","x/../A.cs","x/",r"x\A.cs"]:
   with self.subTest(output=output),self.assertRaises(m.UnsupportedSourceShape):self.runresolve([self.entry(output)],"bad"+str(len(output)))
 def test_casefold_normalized_duplicate_rejected(self):
  a=self.entry("A.cs");b=self.entry("a.cs");b["id"]="second"
  with self.assertRaisesRegex(m.UnsupportedSourceShape,"Unique canonical"):self.runresolve([a,b])
 def test_unsupported_signature_no_emission(self):
  e=self.entry();e["signature"]="int Missing()"
  with self.assertRaisesRegex(m.UnsupportedSourceShape,"Unsupported/ambiguous"):self.runresolve([e])
  self.assertFalse((self.base/"out").exists())
 def test_profile_editor_gate_applies_without_direct_links(self):
  with self.assertRaisesRegex(m.UnsupportedSourceShape,"Editor source profile"):self.runresolve([self.entry()],defines=["UNITY_EDITOR"])
 def test_mission_seed_gate(self):
  path=self.manifest([self.entry()])
  for mission,seed in [("S003",104731),("S002",1)]:
   with self.assertRaisesRegex(m.UnsupportedSourceShape,"Unsupported mission/seed"):m.resolve_manifest(self.repo,self.package,path,self.base/"out",mission,seed)
 def test_frozen_gameplay_adapter_declaration_rejected(self):
  (self.package/"Frozen.cs").write_text("public class Frozen{}")
  e={"id":"frozen","kind":"finite_adapter","path":"Frozen.cs","role":"engine_boundary","contains_original_gameplay":True}
  with self.assertRaisesRegex(m.UnsupportedSourceShape,"smuggle frozen"):self.runresolve([e])
 def test_unresolved_is_explicit_non_runnable(self):
  result=self.runresolve([{"id":"missing","kind":"unresolved","reason":"Recipe pending"}]);self.assertEqual(result["status"],"NeedsExplicitRecipes");self.assertEqual(len(result["unresolved"]),1)
 def test_original_constants_refresh_and_nonliteral_rejects(self):
  source=self.repo/"C.cs";source.write_text('class C{\n public const float Limit = .5f;\n}')
  # Deliberately unsupported shorthand starts with a dot; no silent numeric rewrite.
  member={"path":"C.cs","constant_name":"Limit"};(self.package/"T.txt").write_text('class T{/*SOURCE:value*/}')
  entry={"id":"c","kind":"current_original","template":"T.txt","output":"C.cs","members":[dict(member,id="value")]}
  with self.assertRaisesRegex(m.UnsupportedSourceShape,"literal constant"):self.runresolve([entry],"badliteral")
  source.write_text('class C{\n public const float Limit = 0.5f;\n}');self.runresolve([entry],"one");source.write_text('class C{\n public const float Limit = 1.5f;\n}');self.runresolve([entry],"two");self.assertIn('1.5f',(self.base/'two/generated/C.cs').read_text())
 def test_simple_original_expression_refresh_and_block_rejects(self):
  source=self.repo/'C.cs';source.write_text('class C{ public bool Allowed(int value) => value > 2; }');(self.package/'T.txt').write_text('class T{public bool Allowed(int value) => /*SOURCE:e*/;}');entry={"id":"e","kind":"current_original","template":"T.txt","output":"C.cs","members":[{"id":"e","path":"C.cs","signature":"public bool Allowed(int value)","expression_body":True}]};self.runresolve([entry],"one");source.write_text('class C{ public bool Allowed(int value) => value > 7; }');self.runresolve([entry],"two");self.assertIn('value > 7',(self.base/'two/generated/C.cs').read_text());source.write_text('class C{ public bool Allowed(int value) {return true;} }')
  with self.assertRaisesRegex(m.UnsupportedSourceShape,'simple original expression'):self.runresolve([entry],"badblock")
 def test_omitted_unsupported_entry_keeps_current_supported_body(self):
  e={"id":"all","kind":"current_original","path":"A.cs","omit_members":["public static int G()"],"output":"A.cs"};self.runresolve([e]);text=(self.base/'out/generated/A.cs').read_text();self.assertIn('return 1',text);self.assertNotIn('G()',text)
 def test_identifier_binding_does_not_rename_method(self):
  proof=[];result=m.bind('Rect.MinMaxRect(rect);',[{"from":"Rect","to":"NativeRect","word":True,"count":1}],proof);self.assertEqual(result,'NativeRect.MinMaxRect(rect);')
 def test_current_prefix_flows_guard_and_rejects_frontier_drift(self):
  source=self.repo/'P.cs';source.write_text('class P{public bool F(int x){if(x==0)return false; Active();return true;}}');(self.package/'T.txt').write_text('class T{public bool F(int x){/*SOURCE:p*/throw new System.NotSupportedException();}}');e={"id":"p","kind":"current_original","template":"T.txt","output":"P.cs","members":[{"id":"p","path":"P.cs","signature":"public bool F(int x)","body_only":True,"stop_before":"Active();"}]};self.runresolve([e],"one");source.write_text(source.read_text().replace('x==0','x<=0'));self.runresolve([e],"two");self.assertIn('x<=0',(self.base/'two/generated/P.cs').read_text());source.write_text(source.read_text().replace('Active();','ChangedActive();'))
  with self.assertRaisesRegex(m.UnsupportedSourceShape,'active-domain frontier'):self.runresolve([e],"badfrontier")
 def test_statement_slice_keeps_new_source_policy_not_unrelated_ui(self):
  source=self.repo/'P.cs';source.write_text('class P{public void F(){Unrelated(); model.A = value.A; model.B = value.B; Finish();}}');(self.package/'T.txt').write_text('class T{public void F(){/*SOURCE:p*/}}');e={"id":"p","kind":"current_original","template":"T.txt","output":"P.cs","members":[{"id":"p","path":"P.cs","signature":"public void F()","start_at":"model.A = value.A;","through":"model.B = value.B;"}]};self.runresolve([e],"one");text=(self.base/'one/generated/P.cs').read_text();self.assertNotIn('Unrelated',text);self.assertNotIn('Finish',text);source.write_text(source.read_text().replace('model.B = value.B;','model.NewFlag = value.NewFlag; model.B = value.B;'));self.runresolve([e],"two");self.assertIn('model.NewFlag = value.NewFlag',(self.base/'two/generated/P.cs').read_text())
if __name__=="__main__":unittest.main()
