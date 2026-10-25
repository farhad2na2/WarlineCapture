import json,sys,unittest
from pathlib import Path
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from skirmish_source_syntax import no_duplicates,method_span
class FiniteSyntaxTests(unittest.TestCase):
 def test_duplicate_fields_reject_at_any_json_object(self):
  for text in ['{"a":1,"a":2}','{"nested":{"a":1,"a":2}}']:
   with self.subTest(text=text),self.assertRaisesRegex(ValueError,'Duplicate JSON field'):json.loads(text,object_pairs_hook=no_duplicates)
 def test_distinct_json_fields_preserve_order_and_values(self):
  result=json.loads('{"b":2,"a":{"x":true}}',object_pairs_hook=no_duplicates);self.assertEqual(list(result),['b','a']);self.assertEqual(result['a'],{'x':True})
 def test_exact_member_span_keeps_nested_original_control_flow(self):
  text='class P{public int Before(){return 0;} public int Target(int x){if(x>0){return x;}return -1;} public int After(){return 2;}}';a,b=method_span(text,'public int Target(int x)');self.assertEqual(text[a:b],'public int Target(int x){if(x>0){return x;}return -1;}')
 def test_missing_signature_rejects(self):
  with self.assertRaises(ValueError):method_span('class P{void Existing(){}}','void Missing()')
if __name__=='__main__':unittest.main()
