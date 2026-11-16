import importlib.util
import pathlib
import unittest


path = pathlib.Path(__file__).with_name('compare.py')
spec = importlib.util.spec_from_file_location('source_run_compare', path)
compare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(compare)


class NormalizeTests(unittest.TestCase):
    def test_run_ids_normalize_without_hiding_gameplay_differences(self):
        left = {'world_id': 'world-a', 'session': 'session-a',
                'action': 'world-a:session-a:move', 'cell': 3, 'ProjectionTicks': 10}
        right = {'world_id': 'world-b', 'session': 'session-b',
                 'action': 'world-b:session-b:move', 'cell': 3, 'ProjectionTicks': 20}
        self.assertEqual(compare.normalize(left, 'world-a', 'session-a'),
                         compare.normalize(right, 'world-b', 'session-b'))
        right['cell'] = 4
        self.assertNotEqual(compare.normalize(left, 'world-a', 'session-a'),
                            compare.normalize(right, 'world-b', 'session-b'))


if __name__ == '__main__':
    unittest.main()
