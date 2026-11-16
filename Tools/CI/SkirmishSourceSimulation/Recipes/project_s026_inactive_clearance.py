#!/usr/bin/env python3
"""Extract an unqualified original collector projection from the pinned S026 capture."""
import hashlib
import json
import sys
from pathlib import Path

source = Path(sys.argv[1]).resolve(strict=True)
output = Path(sys.argv[2]).absolute()
raw = source.read_bytes()
digest = hashlib.sha256(raw).hexdigest()
if digest != 'fa0d0dedd774ba7648ee2cedaa872f506339878bff45429abadf9585157e2710':
    raise ValueError('Exact retained inactive capture required')
data = json.loads(raw)
domain = data['fixedMapReadDomain']
record = domain['dropClearanceBounds']
if (data['worldDisposed'] is not True or data['missionInitialized'] is not False
        or data['sourceFitCertified'] is not False or data['missionReady'] is not False
        or data['liveOccupancyMeasured'] is not False
        or domain['operationMapId'] != 'opmap.skirmish.desert_base_01'
        or record['authority'] != 'OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain'
        or record['runtime_presentation_activation_sampled'] is not False
        or len(record['bounds']) != 90865):
    raise ValueError('Exact inactive original collector provenance required')
result = {'schema_version': 1, 'kind': 'S026InactiveOriginalDropBoundsProjection',
          'source': {'path': str(source), 'sha256': digest}, 'record': record,
          'live_occupancy_qualified': False, 'native_presentation_qualified': False,
          'source_fit_certified': False, 'mission_ready': False}
encoded = (json.dumps(result, separators=(',', ':'), allow_nan=False) + '\n').encode()
if len(encoded) > 8 * 1024 * 1024 or output.exists():
    raise ValueError('Fresh bounded projection output required')
output.write_bytes(encoded)
print(json.dumps({'path': str(output), 'sha256': hashlib.sha256(encoded).hexdigest(),
                  'bytes': len(encoded), 'bounds': len(record['bounds']), 'mission_ready': False}))
