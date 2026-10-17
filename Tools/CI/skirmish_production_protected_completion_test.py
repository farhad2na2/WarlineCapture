"""Protected completion controls; every receipt is synthetic, never gameplay evidence."""
import copy,json,shutil,unittest
from pathlib import Path
import skirmish_production_queue as q
from skirmish_production_matrix_authority import SamplingAuthority,build_baseline
import skirmish_production_queue_test as base_fixture

class ProtectedCompletionTests(unittest.TestCase):
    completion=base_fixture.ProductionQueueTests.completion

    def setUp(self):
        base_fixture.ProductionQueueTests.setUp(self)
        project=Path(q.__file__).resolve().parents[2]
        plan=build_baseline(project)
        for row in plan['source_pins']:
            target=self.root/row['path']
            target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(project/row['path'],target)
        self.plan=self.root/'sampling.json'
        self.plan.write_text(json.dumps(plan),encoding='utf-8')
        self.authority=SamplingAuthority(self.root,self.plan,q.digest(self.plan))
        slot=next(row for row in plan['slots'] if row['catalog_id']=='S001' and row['locale']=='EN')
        packet=SamplingAuthority.packet(self.authority.catalog['S001'])
        self.job.update(schema_version=2,job_id='S001-protected-completion',catalog_id='S001',
            seed=slot['seed'],packet_path=packet,packet_sha256=q.digest(self.root/packet))
        self.job.pop('definition_id');self.job.pop('definition_version')
        preset='Assets/Game/Resources/SkirmishBaseAssault.asset'
        target=self.root/preset;target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(project/preset,target)
        self.job.update(legacy_preset_index=0,legacy_preset_resource='SkirmishBaseAssault',
            legacy_preset_sha256=q.digest(target))
        self.directory=self.root/'queue'/self.job['job_id']

    def start(self):
        q.enqueue(self.root/'queue',self.job,self.root,self.authority)
        return q.claim(self.directory,'worker1',self.root,self.authority)

    def test_schema2_complete_contract_stays_unqualified(self):
        record,evidence,artifacts=self.completion()
        result=q.validate_completion(self.job,record,evidence,artifacts,self.root,
            sampling_authority=self.authority)
        self.assertEqual(result['completion_contract'],'passed')
        self.assertEqual(result['native_qualification'],'pending_adapter_canary')
        self.assertEqual(result['player_device_acceptance'],'pending')
        with self.assertRaises(q.ContractError):
            q.finish(self.directory,record['token'],'qualified')

    def test_schema2_still_requires_authority(self):
        record,evidence,artifacts=self.completion()
        with self.assertRaisesRegex(q.ContractError,'complete canonical sampling authority'):
            q.validate_completion(self.job,record,evidence,artifacts,self.root)

    def test_completion_rechecks_actual_sampling_plan(self):
        record,evidence,artifacts=self.completion()
        self.plan.write_text('{}',encoding='utf-8')
        with self.assertRaisesRegex(ValueError,'Sampling plan changed'):
            q.validate_completion(self.job,record,evidence,artifacts,self.root,
                sampling_authority=self.authority)

if __name__=='__main__':unittest.main(verbosity=2)
