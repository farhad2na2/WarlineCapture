"""Retained-state integrity controls for incremental ARIA diagnostics."""
from copy import deepcopy
from unittest.mock import patch
import unittest
import skirmish_aria_trace as current


def row(index, actions):
    return dict(time=float(index), frame=index, observation=dict(EnemyHealth=100),
                before=dict(session=dict(Actions=actions-1, LastCompletedTargetId=71)),
                after=dict(session=dict(Actions=actions, Phase='Observing')),
                target_id=71, intent='Recruit')


class Controls(unittest.TestCase):
    def test_reports_cannot_mutate_retained_checker(self):
        stream = current.TraceAnalyzer()
        value = stream.feed(row(0, 1))
        wanted = deepcopy(value)
        value['issues'].append(dict(severity='error'))
        value['thresholds']['repeat_limit'] = -1
        self.assertEqual(stream.report(), wanted)

    def test_later_session_mutation_cannot_reset_counter(self):
        sample = row(0, 2)
        stream = current.TraceAnalyzer()
        stream.feed(sample)
        sample['after']['session']['Actions'] = 0
        with self.assertRaisesRegex(ValueError, 'reset'):
            stream.feed(row(1, 1))

    def test_invalid_frame_permanently_stops_checker(self):
        stream = current.TraceAnalyzer()
        stream.feed(row(0, 1))
        with self.assertRaises(ValueError):
            stream.feed(dict(observation={}))
        with self.assertRaisesRegex(ValueError, 'stopped'):
            stream.report()
        with self.assertRaisesRegex(ValueError, 'stopped'):
            stream.feed(row(0, 1))

    def test_empty_snapshot_is_not_a_pass(self):
        with self.assertRaisesRegex(ValueError, 'nonempty'):
            current.TraceAnalyzer().report()

    def test_frame_limit_cannot_be_bypassed_by_streaming(self):
        with patch.object(current, 'MAX_FRAMES', 2):
            stream = current.TraceAnalyzer()
            stream.feed(row(0, 1))
            stream.feed(row(1, 2))
            with self.assertRaisesRegex(ValueError, 'limit'):
                stream.feed(row(2, 3))

    def test_progress_field_configuration_cannot_change_after_start(self):
        fields = ['EnemyHealth']
        stream = current.TraceAnalyzer(progress_fields=fields)
        fields.append('Frame')
        self.assertEqual(stream.feed(row(0, 1))['progress_fields'], ['EnemyHealth'])

    def test_push_consumes_once_without_exposing_internal_report(self):
        stream = current.TraceAnalyzer()
        self.assertIsNone(stream.push(row(0, 1)))
        self.assertIsNone(stream.push(row(1, 2)))
        self.assertEqual(stream.report()['frames'], 2)
        self.assertEqual(stream.report()['executed_actions'], 2)


if __name__ == '__main__':
    unittest.main(verbosity=2)
