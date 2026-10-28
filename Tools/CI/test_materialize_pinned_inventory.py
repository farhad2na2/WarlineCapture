import hashlib,json,subprocess,sys,tempfile,unittest
from pathlib import Path
M=Path(__file__).with_name("materialize_pinned_inventory.py");PACK=Path(__file__).with_name("pack_pinned_source_receipt.py")
def sha(b):return hashlib.sha256(b).hexdigest()
class Materialize(unittest.TestCase):
 def setUp(self):
  self.t=tempfile.TemporaryDirectory(dir="/private/tmp");self.root=Path(self.t.name);self.original=self.root/"original";self.original.mkdir();self.a=self.original/"a";self.a.write_bytes(b"alpha");self.b=self.original/"b";self.b.write_bytes(b"bravo");self.hash=sha(b"alpha");self.desc=self.root/"d.json";self.receipt=self.root/"receipt.json";self.inventory=self.root/"inventory";self.out=self.root/"out"
 def tearDown(self):self.t.cleanup()
 def pack(self,descriptor):
  self.desc.write_text(json.dumps(descriptor));pins={str(self.a):self.hash,str(self.desc):sha(self.desc.read_bytes())};self.receipt.write_text(json.dumps({"kind":"SkirmishSourceSimulationRunReceipt","mission":"S049","seed":1,"input_and_runtime_pins":pins}));r=subprocess.run([sys.executable,str(PACK),"--receipt",str(self.receipt),"--output",str(self.inventory),"--receipt-sha256",sha(self.receipt.read_bytes())],capture_output=True);self.assertEqual(r.returncode,0,r.stderr)
 def runmat(self,*extra):return subprocess.run([sys.executable,str(M),"--inventory",str(self.inventory),"--receipt-sha256",sha(self.receipt.read_bytes()),"--historical-root",str(self.original),"--output",str(self.out),*extra],capture_output=True,text=True)
 def test_inventory_qualification_flags_preserved(self):
  self.pack({});verify=Path(__file__).with_name("verify_pinned_source_inventory.py");r=subprocess.run([sys.executable,str(verify),str(self.inventory),"--receipt-sha256",sha(self.receipt.read_bytes())],capture_output=True);self.assertEqual(r.returncode,0,r.stderr);p=self.inventory/"inventory.json";j=json.loads(p.read_text());j["validation_passed"]=True;p.write_text(json.dumps(j));r=subprocess.run([sys.executable,str(verify),str(self.inventory),"--receipt-sha256",sha(self.receipt.read_bytes())],capture_output=True);self.assertNotEqual(r.returncode,0)
 def test_original_absent(self):
  self.pack({});self.a.unlink();r=self.runmat();self.assertEqual(r.returncode,0,r.stderr);self.assertEqual((self.out/"a").read_bytes(),b"alpha")
 def test_changed_blob(self):
  self.pack({});(self.inventory/"blobs"/self.hash).write_bytes(b"changed");self.assertNotEqual(self.runmat().returncode,0)
 def test_symlink_blob(self):
  self.pack({});p=self.inventory/"blobs"/self.hash;p.unlink();p.symlink_to(self.a);self.assertNotEqual(self.runmat().returncode,0)
 def test_pinned_supplement(self):
  self.pack({"sources":[{"path":str(self.b),"sha256":sha(b"bravo")} ]});r=self.runmat("--supplemental-descriptors",str(self.desc));self.assertEqual(r.returncode,0,r.stderr);self.assertEqual((self.out/"b").read_bytes(),b"bravo")
 def test_supplement_hash_rejected(self):
  self.pack({"x":{"path":str(self.b),"sha256":"0"*64}});self.assertNotEqual(self.runmat("--supplemental-descriptors",str(self.desc)).returncode,0)
 def test_descriptor_escape_rejected(self):
  self.pack({"x":{"path":str(self.original)+"/../escape","sha256":"0"*64}});self.assertNotEqual(self.runmat("--supplemental-descriptors",str(self.desc)).returncode,0)
 def test_changed_receipt_rejected(self):
  self.pack({});p=self.inventory/"source-receipt.json";p.write_text(p.read_text()+" ");self.assertNotEqual(self.runmat().returncode,0)
 def test_malformed_inventory_rejected(self):
  self.pack({});(self.inventory/"inventory.json").write_text("{bad");self.assertNotEqual(self.runmat().returncode,0)
 def test_duplicate_key_descriptor_rejected(self):
  self.desc.write_text('{"a":1,"a":2}');pins={str(self.a):self.hash,str(self.desc):sha(self.desc.read_bytes())};self.receipt.write_text(json.dumps({"kind":"SkirmishSourceSimulationRunReceipt","mission":"S049","seed":1,"input_and_runtime_pins":pins}));r=subprocess.run([sys.executable,str(PACK),"--receipt",str(self.receipt),"--output",str(self.inventory),"--receipt-sha256",sha(self.receipt.read_bytes())],capture_output=True);self.assertEqual(r.returncode,0);self.assertNotEqual(self.runmat("--supplemental-descriptors",str(self.desc)).returncode,0)
 def test_unpinned_descriptor_rejected(self):
  self.pack({});self.assertNotEqual(self.runmat("--supplemental-descriptors",str(self.root/"unknown.json")).returncode,0)
if __name__=="__main__":unittest.main()
