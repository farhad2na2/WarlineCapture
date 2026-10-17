import copy,hashlib,importlib.util,json,sys,tempfile,unittest
from pathlib import Path
D=Path(__file__).resolve().parent;ROOT=D.parents[3];P=D.parent/'ProductionBaselineBatch'
sys.path[:0]=[str(P),str(P/'Sources/Tools/CI'),str(ROOT/'Tools/CI')]
from skirmish_production_matrix_authority import SamplingAuthority,sha
import skirmish_production_queue as queue

class SuccessorControls(unittest.TestCase):
    def test_exact_original_s005_candidate_branch_only(self):
        m=json.loads((D/'manifest.json').read_text()); parent=json.loads((P/'manifest.json').read_text())
        changed=[]
        for row,old in zip(m['files'],parent['files']):
            self.assertEqual(row['target'],old['target'])
            if row['sha256']!=old['sha256']:
                changed.append(row['target']);before=(ROOT/old['source']).read_bytes();after=(ROOT/row['source']).read_bytes()
                self.assertEqual(after,before.replace(b'if (selectedCatalog == "S005" && productionCanary == null)',b'if (selectedCatalog == "S005")'))
                self.assertIn(b'fixture=EditorMemoryOnly publication=InProgress shippedCatalogModified=0',after)
                self.assertIn(b'UseEditorValidationCatalog(candidateCatalog)',after)
        self.assertEqual(changed,['Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.MenuProbe.cs'])
        self.assertEqual(m['tools'],parent['tools'])
    def test_real_plans_and_canonical_preflight_all1920_slots(self):
        for name,count in [('baseline720',720),('stress1200',1200)]:
            p=D/(name+'.plan.json');a=SamplingAuthority(ROOT,p,sha(p));self.assertEqual(len(a.slots),count)
            # Native source/build/setup identities are deliberately unavailable here.
            # Only canonical schema/seed/packet admission is exercised, never a run.
            for i,row in enumerate(a.slots.values()):
                packet=a.packet(a.catalog[row['catalog_id']]);job=dict(schema_version=1,job_id='admission-control-'+str(i),definition_id='skirmish.'+row['catalog_id'].lower(),definition_version=1,candidate_sha256='a'*64,build_sha256='b'*64,setup_sha256='c'*64,content_sha256='d'*64,source_manifest=[dict(path=packet,sha256=sha(ROOT/packet))],packet_path=packet,packet_sha256=sha(ROOT/packet),required_stages=sorted(queue.STAGES),timeout_seconds=1800,**row)
                self.assertEqual(queue.preflight(job,ROOT,a)['native_qualification'],'pending')
    def test_stress_derivation_is_reproducible_complete_and_not_results(self):
        receipt=json.loads((D/'PLAN_AUTHORITY.json').read_text());rows=json.loads((D/'stress1200.plan.json').read_text())['slots']
        self.assertEqual(len(receipt['seed_derivations']),1200);self.assertEqual(receipt['native_runs'],0)
        seeds={}
        for row,derived in zip(rows,receipt['seed_derivations']):
            expected=1+int.from_bytes(hashlib.sha256(derived['label'].encode('ascii')).digest()[:4],'big')%2147483647
            self.assertEqual(row['seed'],expected);self.assertEqual(expected,derived['seed']);self.assertTrue(1<=expected<=2147483647)
            seeds.setdefault(tuple(derived['stratum']),set()).add(expected)
        self.assertEqual(len(seeds),60);self.assertTrue(all(len(v)==20 for v in seeds.values()))
    def test_changed_plan_or_noncanonical_seed_refuses(self):
        plan=json.loads((D/'stress1200.plan.json').read_text())
        scratch=ROOT/'Temp/ProductionBaselineBatchCandidateAdmission';scratch.mkdir(parents=True,exist_ok=True)
        with tempfile.TemporaryDirectory(dir=scratch) as temp:
            p=Path(temp)/'plan.json';p.write_text(json.dumps(plan));a=SamplingAuthority(ROOT,p,sha(p));p.write_text(json.dumps(plan)+' ')
            with self.assertRaises(ValueError):a.recheck()
            for bad in [0,-1,2147483648,True,'104734']:
                changed=copy.deepcopy(plan);changed['slots'][0]['seed']=bad;p.write_text(json.dumps(changed))
                with self.subTest(seed=bad),self.assertRaises(ValueError):SamplingAuthority(ROOT,p,sha(p))

if __name__=='__main__':unittest.main(verbosity=2)
