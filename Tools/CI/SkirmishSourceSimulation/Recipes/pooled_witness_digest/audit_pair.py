"""Audit pooled/original bounded runs using the existing strict comparator."""
import os
import sys
from pathlib import Path

original = Path(__file__).resolve().parents[1] / 'fresh_witness_digest/audit_pair.py'
source = original.read_text()
old = "marker = '[SourceFreshWitnessDigest] enabled=1 scope=ExperimentalSourceOnly'"
repeat = os.environ.get('WARLINE_COMPARE_ORIGINAL_REPEAT') == '1'
new = "marker = '[SourcePooledWitness] enabled=' + str(" + ('0' if repeat else 'i') + ") + ' scope=ExperimentalSourceOnly'"
if source.count(old) != 1:
    raise ValueError('Exact existing comparison marker required')
source = source.replace(old, 'marker = None', 1)
seam = "    for i, (d, receipt, result) in enumerate(zip(directories, receipts, results)):\n"
if source.count(seam) != 1:
    raise ValueError('Exact existing comparison loop required')
source = source.replace(seam, seam + '        ' + new + '\n', 1)
check = "(d / 'process.log').read_text().count(marker) != i"
if source.count(check) != 1:
    raise ValueError('Exact existing comparison check required')
source = source.replace(check, "(d / 'process.log').read_text().count(marker) != 1", 1)
source = source.replace('S003FreshWitnessDigestPairAudit', 'S003OriginalDigestRepeatAudit' if repeat else 'S003PooledWitnessDigestPairAudit')
if repeat:
    source = source.replace('One sequential pair, source-only proxy planner and unqualified physical/camera data.',
                            'One sequential original/original repeat, source-only proxy planner and unqualified physical/camera data.')
sys.path.insert(0, str(original.parent))
exec(compile(source, str(original), 'exec'), {'__name__': '__main__', '__file__': str(original)})
