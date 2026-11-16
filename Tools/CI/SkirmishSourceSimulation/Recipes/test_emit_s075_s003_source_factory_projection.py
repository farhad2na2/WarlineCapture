import importlib.util
from pathlib import Path
import unittest


script = Path(__file__).with_name('emit_s075_s003_source_factory_projection.py')
spec = importlib.util.spec_from_file_location('s075_factory_projection', script)
projection = importlib.util.module_from_spec(spec)
spec.loader.exec_module(projection)


class SelectedDriftTests(unittest.TestCase):
    def test_detects_overlap_in_repository_relative_paths(self):
        changed = [{'path': 'Assets/Game/Scripts/Factory.cs'}]
        selected = [{'path': 'Assets/Game/Scripts/Factory.cs'},
                    {'path': 'Packages/manifest.json'}]
        self.assertEqual(projection.selected_drift(changed, selected),
                         {'Assets/Game/Scripts/Factory.cs'})

    def test_allows_disjoint_changed_paths(self):
        self.assertFalse(projection.selected_drift(
            [{'path': 'Assets/Game/Scripts/Other.cs'}],
            [{'path': 'Assets/Game/Scripts/Factory.cs'}]))


class ExtractMethodTests(unittest.TestCase):
    def test_ignores_braces_in_comments_and_string_literals(self):
        source = '''class Example {
    // } ignored
    void Target() {
        var text = "{ not a block }";
        /* { ignored too } */
        if (true) { Run(); }
    }
    void Other() {}
}'''
        extracted = projection.extract_method(source, 'void Target()')
        self.assertIn('void Target()', extracted)
        self.assertIn('if (true) { Run(); }', extracted)
        self.assertNotIn('void Other()', extracted)

    def test_missing_marker_reports_requested_marker(self):
        with self.assertRaisesRegex(ValueError, r'Missing exact method marker: void Missing\(\)'):
            projection.extract_method('class Example {}', 'void Missing()')

    def test_unclosed_method_is_rejected(self):
        with self.assertRaisesRegex(ValueError, r'Unclosed original method: void Target\(\)'):
            projection.extract_method('void Target() { if (true) { Run(); }', 'void Target()')


if __name__ == '__main__':
    unittest.main()
