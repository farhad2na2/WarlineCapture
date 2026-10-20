"""Real bounded artifact-path controls; no native qualification credit."""
import hashlib,os,shutil,subprocess,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import skirmish_production_operational_inspector as m
class ArtifactPaths(unittest.TestCase):
 def setUp(self):
  self.root=Path(tempfile.mkdtemp(prefix="warline-reader-")).resolve();self.links=[]
  self.project=self.root/"project";self.project.mkdir();self.addCleanup(self.cleanup)
 def cleanup(self):
  for link in self.links:
   if m._io_path(link).exists():os.rmdir(m._io_path(link))
  self.assertEqual(self.root.parent,Path(tempfile.gettempdir()).resolve())
  self.assertTrue(self.root.name.startswith("warline-reader-"))
  if os.name=="nt":self.assertFalse(m._io_path(self.root).lstat().st_file_attributes&0x400)
  shutil.rmtree(m._io_path(self.root))
 def deep(self,base):
  p=base
  while len(str(p/"data.json"))<310:p=p/("segment-"+str(len(p.parts))+"-"+"x"*18)
  m._io_path(p).mkdir(parents=True,exist_ok=True)
  f=p/"data.json";m._io_path(f).write_bytes(b'{"n":1}\n');return f
 def ref(self,p):return dict(path=str(p),sha256=hashlib.sha256(m._io_path(p).read_bytes()).hexdigest())
 def test_long_read_keeps_canonical_aliases_and_root(self):
  p=self.deep(self.project);r=m.Reader(self.project);ref=self.ref(p)
  actual,value=r.obj(ref);self.assertEqual(actual,p);self.assertEqual(value,{"n":1})
  r.obj(dict(ref,path=p.relative_to(self.project).as_posix()));self.assertEqual(len(r.pins),1)
  self.assertEqual(r.derived(p),p);r.stable()
  deep=m.Reader(p.parent);self.assertEqual(deep.root,p.parent);self.assertEqual(deep.obj(dict(ref,path=p.name))[1],value)
 def test_long_hash_and_stability_refuse_changed_bytes(self):
  p=self.deep(self.project);r=m.Reader(self.project);ref=self.ref(p);r.read(ref)
  m._io_path(p).write_bytes(b'{"n":2}\n')
  with self.assertRaisesRegex(ValueError,"changed"):r.stable()
  with self.assertRaisesRegex(ValueError,"SHA differs"):m.Reader(self.project).read(ref)
 def test_long_bounds_traversal_and_device_refs_refused(self):
  p=self.deep(self.project);r=m.Reader(self.project)
  with self.assertRaisesRegex(ValueError,"traversal"):r.path(str(p.parent/".."/p.name))
  with self.assertRaisesRegex(ValueError,"escapes"):r.path(self.root/"outside")
  if os.name=="nt":
   with self.assertRaisesRegex(ValueError,"Device"):r.path(m._io_path(p))
  with patch.object(m,"MAX_BYTES",4):
   with self.assertRaisesRegex(ValueError,"oversized"):r.path(p)
  m._io_path(p).write_bytes(b"")
  with self.assertRaisesRegex(ValueError,"Empty"):r.path(p)
 @unittest.skipUnless(os.name=="nt","Actual Windows junction control")
 def test_long_alias_cannot_hide_junction(self):
  target=self.root/"target";target.mkdir();p=self.deep(target);link=self.project/"jump"
  run=subprocess.run(["cmd.exe","/d","/c","mklink","/J",str(link),str(target)],capture_output=True,text=True)
  self.assertEqual(run.returncode,0,run.stdout+run.stderr);self.links.append(link)
  with self.assertRaisesRegex(ValueError,"reparse"):m.Reader(self.project).path(link/p.relative_to(target))
  self.assertEqual(m._io_path(p).read_bytes(),b'{"n":1}\n')
if __name__=="__main__":unittest.main(verbosity=2)
