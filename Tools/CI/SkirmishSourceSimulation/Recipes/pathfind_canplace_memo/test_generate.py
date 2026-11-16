import hashlib, importlib.util, tempfile, unittest
from pathlib import Path
HERE=Path(__file__).resolve().parent
SPEC=importlib.util.spec_from_file_location("memo_recipe",HERE/"generate.py")
M=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(M)

class CanPlaceMemoRecipeControls(unittest.TestCase):
    def setUp(self): self.source=(M.ROOT/M.SOURCE).read_text()
    def test_source_host_only_request_scope_and_key(self):
        candidate=M.transform(self.source)
        self.assertEqual(candidate.count("GridPathRoutingKernel.Route(ref context, request, ref state);"),1)
        self.assertIn("new Dictionary<CanPlaceMemoKey,bool>()",candidate)
        self.assertIn("CanPlaceMemo?.Clear()",candidate)
        self.assertIn("finally",candidate)
        self.assertIn("CanPlaceMemo.Count<4096",candidate)
        for key_part in ("Target", "Current", "Moving", "Ignored", "Footprint", "IgnoredCell", "IgnoredSize", "Width", "Height", "CellSize", "Origin", "Faction", "Vehicle", "Manual"):
            self.assertIn(key_part,candidate)
        self.assertIn("bool actual=CanPlaceOriginal(x,z,cx,cz)",candidate)
        self.assertIn("actual!=cached",candidate)
        self.assertIn("CanPlaceMemo.Clear();throw new System.InvalidOperationException",candidate)
        self.assertIn("CanPlaceOriginal(int x, int z, int cx, int cz)",candidate)
        self.assertIn("if(CanPlaceMemo==null)return CanPlaceOriginal(x,z,cx,cz)",candidate)
        self.assertFalse((M.ROOT/M.SOURCE).read_text()!=self.source)
    def test_anchor_mismatch_fails_closed(self):
        for anchor in (M.ANCHOR_CONTEXT,M.ANCHOR_ROUTE,M.ANCHOR_CANPLACE):
            with self.assertRaisesRegex(ValueError,"exact PathfindBatchJob anchor"):
                M.transform(self.source.replace(anchor,"",1))
    def test_generation_pins_candidate_identity_and_preserves_input(self):
        p=M.ROOT/M.SOURCE;before=p.read_bytes()
        with tempfile.TemporaryDirectory() as temp:
            receipt=M.generate(M.ROOT,Path(temp)/"candidate")
            data=(Path(temp)/"candidate"/receipt["candidate"]).read_bytes()
            self.assertEqual(hashlib.sha256(data).hexdigest(),receipt["candidate_sha256"])
            self.assertEqual("sha256(candidate bytes)",receipt["output_identity"])
            self.assertTrue(receipt["original_route_unchanged"])
            self.assertFalse(receipt["build_inputs_changed"])
        self.assertEqual(before,p.read_bytes())

if __name__=="__main__":unittest.main()
