"""Experimental source-only support approach; retain original formation checks."""
import hashlib
from pathlib import Path

SOURCE = 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs'
OUTPUT = 'S003RifleSupportApproach.cs'


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    path = repo / SOURCE
    raw = path.read_bytes()
    text = raw.decode('utf-8').replace('\r\n', '\n')
    marker = '        static SkirmishPublicSelectedApproach ReadS003OrdinaryCore('
    if text.count(marker) != 1:
        raise ValueError('Exact S003 source owner required')
    prefix, body = text.split(marker, 1)
    refusal = '''            if (result.StructureCapableCount == 0)
            { result.UnavailableReason = "Selected fighters cannot attack structures."; return result; }
'''
    selection = '                if (!members[i].StructureCapable) continue;'
    if body.count(refusal) != 1 or body.count(selection) != 1:
        raise ValueError('Exact original formation and capability guards required')
    body = body.replace(refusal, '', 1)
    body = body.replace(selection,
        '                if (result.StructureCapableCount > 0 && !members[i].StructureCapable) continue;', 1)
    if path.read_bytes() != raw:
        raise ValueError('Original source changed during generation')
    data = (prefix + marker + body).encode('utf-8')
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    (output / OUTPUT).write_bytes(data)
    return {'source_pins': {str(path): hashlib.sha256(raw).hexdigest()},
            'outputs': {OUTPUT: hashlib.sha256(data).hexdigest()},
            'experimental_only': True,
            'scope': 'Source-only rifle support approach candidate; original game and native readiness unchanged'}
