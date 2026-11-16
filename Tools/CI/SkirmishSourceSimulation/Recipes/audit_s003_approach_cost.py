#!/usr/bin/env python3
"""Summarize measured source-host approach costs without changing gameplay."""
import argparse
import hashlib
import json
from pathlib import Path


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--trace', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    raw = args.trace.read_bytes()
    rows = raw.splitlines(keepends=True)
    groups = {}
    complete, truncated = 0, False
    for i, line in enumerate(rows):
        try:
            event = json.loads(line)
        except (ValueError, UnicodeDecodeError):
            if i != len(rows) - 1 or line.endswith(b'\n'):
                raise ValueError('Invalid trace row before final incomplete row')
            truncated = True
            break
        approach = event['observation']['SelectedOwnApproach']
        frequency = approach['TimerFrequency']
        ticks = approach['ProjectionTicks']
        if not isinstance(frequency, int) or frequency <= 0 or not isinstance(ticks, int) or ticks < 0:
            raise ValueError('Invalid approach timing')
        reason = approach['UnavailableReason'] or 'available'
        goal_ticks = approach['GoalEvaluationTicks']
        if not isinstance(goal_ticks, int) or goal_ticks < 0 or goal_ticks > ticks:
            raise ValueError('Invalid nested goal evaluation timing')
        item = groups.setdefault(reason, {'rows': 0, 'projection_seconds': 0.0,
                                          'goal_evaluation_seconds': 0.0, 'allocated_bytes': 0})
        item['rows'] += 1
        item['projection_seconds'] += ticks / frequency
        item['goal_evaluation_seconds'] += goal_ticks / frequency
        item['allocated_bytes'] += approach['AllocatedBytes']
        complete += 1
    report = {'kind': 'S003RetainedApproachCostAudit', 'trace_sha256': hashlib.sha256(raw).hexdigest(),
              'complete_rows': complete, 'final_incomplete_row_excluded': truncated, 'by_unavailable_reason': groups,
              'scope': 'Measured diagnostic source proxy only; incomplete timed-out run; no optimization, parity, outcome or readiness claim',
              'mission_ready': False}
    args.output.parent.mkdir(parents=True, exist_ok=False)
    args.output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'complete_rows': complete, 'final_incomplete_row_excluded': truncated, 'groups': groups}))


if __name__ == '__main__':
    main()
