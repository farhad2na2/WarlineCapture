"""Constructed normal-artifact controls; no native execution or acceptance credit."""
import copy
from pathlib import Path
import unittest
from skirmish_operational_test_fixture import (ROOT, OperationalFixture, owned_workspace,
    cleanup_workspace, build_baseline, write, q)
from skirmish_production_matrix_authority import SamplingAuthority
from skirmish_native_identity import sha256, content_identity
import skirmish_production_operational_inspector as inspector


class ProtectedEvidenceScope(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary=owned_workspace("protected-scope-")
        cls.work=Path(cls.temporary.name);cls.number=0;cls.plan=build_baseline(ROOT)

    @classmethod
    def tearDownClass(cls):cleanup_workspace(cls.temporary)

    def fixture(self, catalog=None, schema=3):
        type(self).number+=1
        f=OperationalFixture(self.work/str(type(self).number),self.plan)
        if catalog is None:return f
        slots=[s for s in self.plan['slots'] if s['catalog_id']==catalog][:3]
        for i,slot in enumerate(slots):
            paths=f.paths[i];job=q.load(paths['job']);old=q.fingerprint(job)
            job.update(schema_version=schema,catalog_id=catalog,definition_id='skirmish.'+catalog.lower(),seed=slot['seed'],locale=slot['locale'])
            authority=SamplingAuthority(f.root,f.planpath,q.digest(f.planpath))
            packet=authority.packet(authority.catalog[catalog])
            job.update(packet_path=packet,packet_sha256=authority.pins[packet])
            if schema==3:
                # Untrusted fixture authority is NOT treated as reviewed normal authority.
                job['counterpart_authority']={'path':'Logs/development-only.json','sha256':'f'*64}
            identity=copy.deepcopy(f.records[i][1]['facts']['native_setup_identity'])
            identity.update(catalog_id=catalog,definition_id=job['definition_id'],seed=slot['seed'],locale=slot['locale'])
            job.update(setup_sha256=sha256(identity),content_sha256=sha256(content_identity(identity)))
            # Every artifact retains exact claim/dispatch ownership after fixture mutation.
            new=q.fingerprint(job);claim=q.load(paths['claim']);claim['job_sha256']=new
            write(paths['job'],job);write(paths['claim'],claim);write(paths['immutable'],claim)
            locale='en' if slot['locale']=='EN' else 'fa-IR'
            for row in f.records[i]:
                row['job_sha256']=new
                facts=row['facts']
                if 'native_setup_identity' in facts:
                    facts['native_setup_identity']=copy.deepcopy(identity);facts['actual_native_locale']=locale
                for key in ('rendered_result_presentation','localized_main_menu_presentation'):
                    if key in facts:facts[key]['locale']=locale
            for row in f.batch:
                if row['facts'].get('job_sha256')==old:row['facts']['job_sha256']=new
            f.csvrows[i].update(catalog_id=catalog,seed=str(slot['seed']),locale=locale)
        return f

    def test_existing_expanded_normal_outcomes_unchanged(self):
        f=self.fixture();r=inspector.inspect(f.root,f.materialize())
        self.assertEqual('NormalCompletedUnqualified',r['result'],r)
        self.assertEqual(1,r['wins']);self.assertEqual(['Victory','Defeat','Draw'],[a['outcome'] for a in r['attempts']])
        self.assertTrue(all(a['evidence_scope']=='ExpandedNormalUnqualified' and a['sampling_coverage_credit']==0 for a in r['attempts']))

    def test_all_three_protected_development_counterparts_retain_outcomes_with_zero_credit(self):
        for catalog in ('S001','S025','S073'):
            with self.subTest(catalog=catalog):
                f=self.fixture(catalog);spec=f.materialize()
                before={p:q.digest(p) for p in f.root.rglob('*') if p.is_file()}
                r=inspector.inspect(f.root,spec)
                self.assertEqual('NormalCompletedUnqualified',r['result'],r)
                self.assertEqual(0,r['wins']);self.assertFalse(r['qualified']);self.assertFalse(r['queue_success_enabled'])
                self.assertEqual(['Victory','Defeat','Draw'],[a['outcome'] for a in r['attempts']])
                self.assertEqual([1,0,0],[a['actual_native_wins'] for a in r['attempts']])
                self.assertTrue(all(a['evidence_scope']=='DevelopmentCounterpartZeroBaselineCredit' and a['wins']==0 and a['sampling_coverage_credit']==0 for a in r['attempts']))
                self.assertEqual(before,{p:q.digest(p) for p in f.root.rglob('*') if p.is_file()})

    def test_schema1_cannot_relabel_protected_development_as_ordinary_normal(self):
        f=self.fixture('S025',schema=1);r=inspector.inspect(f.root,f.materialize())
        self.assertEqual('Incomplete',r['result']);self.assertEqual(0,r['wins'])
        self.assertIn('separate review',' '.join(r['errors']))

    def test_new_schema_and_wrong_catalog_schema_cannot_gain_credit(self):
        for job in ({'schema_version':4,'catalog_id':'S025'}, {'schema_version':True,'catalog_id':'S025'},
                    {'schema_version':3,'catalog_id':'S026'}, {'schema_version':2,'catalog_id':'S026'}):
            with self.subTest(job=job):
                with self.assertRaises(ValueError):inspector.sampling_scope(job)

    def test_failed_development_batch_stays_failed_without_partial_credit(self):
        f=self.fixture('S001');f.records[1][3]['facts']['result_session_id']='foreign'
        r=inspector.inspect(f.root,f.materialize())
        self.assertEqual('Incomplete',r['result']);self.assertEqual(0,r['wins'])
        self.assertEqual(0,r['attempts'][0]['wins']);self.assertEqual(1,r['attempts'][0]['actual_native_wins'])

if __name__=='__main__':unittest.main()
