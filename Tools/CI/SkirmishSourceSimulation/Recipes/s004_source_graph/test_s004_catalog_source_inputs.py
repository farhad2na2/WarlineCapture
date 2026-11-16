import unittest

import read_s004_catalog_source_inputs as source
import run_s004_catalog_getter_control as control


class CatalogSourceInputTests(unittest.TestCase):
    def test_valid_source_values(self):
        for name, value in [('canAttack', '1'), ('maxHealth', '560'), ('speed', '7.5')]:
            source.validate_scalar(name, value)
        self.assertEqual(source.sprite_reference('{fileID: 21300000, guid: ' + 'a'*32 + ', type: 3}')['local_id'], 21300000)

    def test_invalid_scalars_fail_closed(self):
        for name, value in [('canAttack', '2'), ('maxHealth', '-1'), ('maxHealth', '١'),
                            ('speed', 'nan'), ('attackRange', '-1'), ('speed', 'inf')]:
            with self.subTest(name=name, value=value), self.assertRaises(ValueError):
                source.validate_scalar(name, value)

    def test_invalid_sprite_references_fail_closed(self):
        for reference in ['{fileID: 0}', '{fileID: 1, guid: ' + 'a'*32 + ', type: 3}',
                          '{fileID: 21300000, guid: ' + 'g'*32 + ', type: 3}',
                          '{fileID: 21300000, guid: ' + 'a'*32 + ', type: 2}']:
            with self.subTest(reference=reference), self.assertRaises(ValueError):
                source.sprite_reference(reference)

    def test_original_getter_extraction_rejects_missing_or_duplicate(self):
        original = control.AUTHORING.read_text()
        generated = control.generate(original)
        self.assertIn('public float ConfiguredSpeed => Mathf.Max', generated)
        getter = 'public bool IsProductionTransportUnit => isProductionTransportUnit;'
        self.assertEqual(original.count(getter), 1)
        with self.assertRaises(ValueError):
            control.generate(original.replace(getter, ''))
        with self.assertRaises(ValueError):
            control.generate(original + '\n' + getter + '\n')


if __name__ == '__main__':
    unittest.main()
