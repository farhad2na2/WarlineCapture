import copy,json,unittest
from build_skirmish_ground_objective30 import build,ROOT
from skirmish_ground_objective30_contract import validate

class GroundObjectiveAuthoringTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):cls.document=build()
    def test_complete_actual_authority_and_four_source_groups(self):
        result=validate(self.document,ROOT)
        self.assertEqual((30,90,4),(result['missions'],result['canonical_size_vectors'],result['source_worlds_planned']))
        self.assertFalse(result['source_fit_certified'])
    def test_deterministic_source_pinned_generation(self):self.assertEqual(self.document,build())
    def test_all_typed_geometry_remains_explicit_unqualified_and_in_bounds(self):
        for row in self.document['missions']:
            areas=row['typed_objective_areas'];candidate=row['layout_input']['pilot_candidate'];bounds=candidate['proposed_logical_world_rect']
            self.assertIsNone(areas['measurement']);self.assertIsNone(row['view_intent']['scalars'])
            self.assertEqual(candidate['canonical_budget_rows_sha256'],row['canonical_size_rows_sha256'])
            for area in areas['areas']:
                lo,hi=area['Points'];self.assertLess(lo['WorldX'],hi['WorldX']);self.assertLess(lo['WorldZ'],hi['WorldZ'])
                self.assertTrue(bounds[0]<=lo['WorldX']<hi['WorldX']<=bounds[2]);self.assertTrue(bounds[1]<=lo['WorldZ']<hi['WorldZ']<=bounds[3])
    def test_budget_mutation_rejects(self):
        row=copy.deepcopy(self.document);row['missions'][0]['canonical_size_rows'][0]['player_rifle']='999'
        with self.assertRaises(ValueError):validate(row,ROOT)
    def test_source_substitution_rejects(self):
        row=copy.deepcopy(self.document);row['missions'][0]['source_candidate']['contentHash']='foreign'
        with self.assertRaises(ValueError):validate(row,ROOT)
    def test_invented_measurement_rejects(self):
        row=copy.deepcopy(self.document);row['missions'][0]['typed_objective_areas']['measurement']={'ArtifactId':'fake'}
        with self.assertRaises(ValueError):validate(row,ROOT)
    def test_publication_and_protected_id_change_reject(self):
        for key,value in [('publication_enabled',True),('catalog_id','S002')]:
            row=copy.deepcopy(self.document);row['missions'][0][key]=value
            with self.assertRaises(ValueError):validate(row,ROOT)
    def test_all_three_objectives_and_start_packages_remain_canonical(self):
        from collections import Counter
        self.assertEqual(Counter({'FC':10,'BT':10,'CE':10}),Counter(r['tuple']['objective_id'] for r in self.document['missions']))
        self.assertEqual(Counter({'F':15,'E':15}),Counter(r['tuple']['start_profile'] for r in self.document['missions']))
        self.assertTrue(all(len(r['canonical_size_rows'])==3 for r in self.document['missions']))

if __name__=='__main__':unittest.main()
