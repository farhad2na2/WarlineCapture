"""Actual immutable Full120 input joins; synthetic authored geometry is not fit."""
import copy
import json
from pathlib import Path
import unittest
from skirmish_ground_objective30_contract import validate,sha

ROOT=Path(__file__).resolve().parents[2]
BASE='Design/AgentReports/SkirmishExpansion/'


def fixture():
    pins={key:dict(path=BASE+path,sha256=sha((ROOT/(BASE+path)).read_bytes())) for key,path in (
        ('full_index','Full120/source-fit-authoring-batches-20261015-v2.json'),
        ('logical_inputs_manifest','Full120/LogicalInputs/20261015-01/manifest.json'),
        ('pilot_candidates','GroundPilots/measured-pad-bounds-proposals-survey11-v2.json'))}
    path='Logs/SkirmishProduction/ground-source-fit-20261015-11.json';pins['source_survey']=dict(path=path,sha256=sha((ROOT/path).read_bytes()))
    index=json.loads((ROOT/pins['full_index']['path']).read_bytes());ids=set(next(r['catalog_ids'] for r in index['parallel_batches'] if r['id']=='ground-objective-thirty'))
    rows=[]
    for old in index['missions']:
        if old['catalog_id'] not in ids:continue
        row=copy.deepcopy(old);group=row['tuple']['map_id'];row.update(
            source_candidate=copy.deepcopy(index['source_candidates'][group]),collector_source_group='CC' if group=='DB' else group,
            layout_input=dict(status='ExplicitMissing'),typed_objective_areas=dict(status='ExplicitMissing',areas=[],measurement=None),
            view_intent=dict(required_roles=[],scalars=None,status='ActualNativeReadabilityPending'),pending_gates=['SourceFit','NormalMission'],publication_enabled=False)
        rows.append(row)
    return dict(schema_version=1,kind='UnqualifiedGroundObjective30Authoring',authority=pins,mission_count=30,canonical_size_vectors=90,
                protected_catalog_ids=sorted(r['catalog_id'] for r in index['missions'] if r.get('protected_binding')),missions=rows)


class Ground30ContractControls(unittest.TestCase):
    def test_actual30_and90_budget_rows_four_shared_source_groups_stay_unqualified(self):
        report=validate(fixture(),ROOT)
        self.assertEqual(30,report['missions']);self.assertEqual(90,report['canonical_size_vectors'])
        self.assertEqual(4,report['source_worlds_planned']);self.assertFalse(report['source_fit_certified'])
        self.assertEqual(30,sum(len(r['catalog_ids']) for r in report['source_world_groups']))

    def test_missing_duplicate_id_or_edited_budget_tuple_packet_source_refused(self):
        for mutate in (lambda v:v['missions'].pop(),lambda v:v['missions'].__setitem__(1,copy.deepcopy(v['missions'][0])),
            lambda v:v['missions'][0]['canonical_size_rows'][0].update(player_rifle='999'),
            lambda v:v['missions'][0]['tuple'].update(army_profile='A'),
            lambda v:v['missions'][0]['packet'].update(sha256='0'*64),
            lambda v:v['missions'][0]['source_candidate'].update(sourceIdentityHash='foreign'),
            lambda v:v['missions'][0].update(collector_source_group='AP')):
            value=fixture();mutate(value)
            with self.assertRaises(ValueError):validate(value,ROOT)

    def test_authority_splice_publication_camera_numbers_and_invented_proof_refused(self):
        for mutate in (lambda v:v['authority']['full_index'].update(sha256='0'*64),
            lambda v:v['missions'][0].update(publication_enabled=True),
            lambda v:v['missions'][0]['view_intent'].update(scalars={'height':18}),
            lambda v:v['missions'][0]['typed_objective_areas'].update(measurement={'invented':True})):
            value=fixture();mutate(value)
            with self.assertRaises(ValueError):validate(value,ROOT)

    def test_typed_roles_and_finite_points_checked_without_measuring_source_fit(self):
        value=fixture();row=value['missions'][0]
        row['typed_objective_areas']['areas']=[dict(StableAreaId='synthetic.'+str(role),Role=role,Shape=1,Points=[dict(WorldX=role,WorldZ=0),dict(WorldX=role+.5,WorldZ=1)]) for role in (1,2,3)]
        self.assertFalse(validate(value,ROOT)['source_fit_certified'])
        for mutate in (lambda a:a[0].update(Role=True),lambda a:a[0]['Points'][0].update(WorldX=float('nan')),
                       lambda a:a[0].update(StableAreaId=a[1]['StableAreaId'])):
            bad=copy.deepcopy(value);mutate(bad['missions'][0]['typed_objective_areas']['areas'])
            with self.assertRaises(ValueError):validate(bad,ROOT)


if __name__=='__main__':unittest.main()
