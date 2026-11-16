import copy
import unittest

import audit_s004_standard_difficulty_setup as audit


class StandardDifficultyComparisonTests(unittest.TestCase):
    def setUp(self):
        self.setup = {name: 0 for name in audit.IGNORED}
        self.setup.update(Forces=[{'RoleId': 'rifle', 'Quantity': 12, 'SpawnWorldX': 1, 'SpawnWorldZ': 2}],
                          Structures=[{'RoleId': 'barracks', 'SpawnWorldX': 3, 'SpawnWorldZ': 4}],
                          MaterialsEach=900, EnemyPolicy=None)

    def test_ignores_only_explicit_layout_and_identity_fields(self):
        changed = copy.deepcopy(self.setup)
        changed['DifficultyId'] = 3
        changed['Forces'][0]['SpawnWorldX'] = 20
        self.assertEqual(audit.normalized(self.setup), audit.normalized(changed))

    def test_budget_or_actor_changes_remain_visible(self):
        for field in ('MaterialsEach', 'Forces'):
            changed = copy.deepcopy(self.setup)
            if field == 'Forces': changed[field][0]['Quantity'] += 1
            else: changed[field] += 1
            self.assertNotEqual(audit.normalized(self.setup), audit.normalized(changed))

    def test_missing_expected_field_fails_closed(self):
        del self.setup['MeasuredLayoutBound']
        with self.assertRaises(ValueError): audit.normalized(self.setup)
