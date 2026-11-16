import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
import hashlib
import struct

import preflight

ROOT = Path(__file__).resolve().parent
SAMPLE = ROOT / 's026-source-derived-map-package.json'

class PreflightTests(unittest.TestCase):
    @staticmethod
    def _f32(value):
        return struct.unpack('<f', struct.pack('<f', value))[0]

    def _clearance_case(self, cell_size=1.0, query_rect=None):
        frame = {'width': 2048, 'height': 1024, 'cell_size': cell_size, 'origin': [0.0, 0.0, 0.0]}
        safe = max(self._f32(.01), self._f32(cell_size))
        high_x = self._f32(self._f32((frame['width'] - 1 - 98) * safe) + self._f32(197 * safe))
        high_z = self._f32(self._f32((frame['height'] - 1 - 98) * safe) + self._f32(197 * safe))
        low = self._f32(-98 * safe)
        exact_rect = [low, low, high_x - low, high_z - low]
        record = {'query_rect': query_rect if query_rect is not None else exact_rect}
        static_map = {'operation_map_id': 'map.test', 'definition': {'path': '/map.asset', 'sha256': 'a' * 64}}
        capture_pin = {'path': '/capture.json', 'sha256': 'b' * 64}
        water_descriptor = {'path': '/water.json', 'sha256': 'c' * 64}
        collector = {
            'authority': 'OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain',
            'query_rect': record['query_rect'], 'record_sha256': hashlib.sha256(
                json.dumps(record, sort_keys=True, separators=(',', ':'), allow_nan=False).encode()
            ).hexdigest(),
            'grid_width': frame['width'], 'grid_height': frame['height'],
            'cell_size': frame['cell_size'], 'grid_origin': frame['origin'],
        }
        projection = {
            'kind': 'SourceDerivedCampaignMapDropClearanceProjection', 'schema_version': 1,
            'package_capture': capture_pin, 'static_map': static_map,
            'frame': {'width': frame['width'], 'height': frame['height'],
                      'cell_size': frame['cell_size'], 'origin': list(frame['origin'])},
            'collector': collector, 'water_descriptor': water_descriptor,
            'scope': 'source-only', 'live_occupancy_qualified': False,
            'native_presentation_qualified': False, 'source_fit_certified': False,
            'mission_ready': False,
        }
        descriptor = {'path': '/clearance.json', 'sha256': 'd' * 64}
        return descriptor, capture_pin, static_map, frame, water_descriptor, record, projection

    def _validate_clearance_case(self, case):
        descriptor, capture_pin, static_map, frame, water_descriptor, record, projection = case
        with patch.object(preflight, '_pin'), patch.object(preflight, '_json', return_value=projection):
            return preflight._validate_clearance_descriptor(
                descriptor, capture_pin, static_map, frame, water_descriptor, record
            )

    def test_clearance_collector_accepts_exact_float32_98_cell_edge_envelope(self):
        self._validate_clearance_case(self._clearance_case())

    def test_clearance_collector_accepts_small_cell_safe_98_cell_envelope(self):
        self._validate_clearance_case(self._clearance_case(cell_size=.001))

    def test_clearance_collector_rejects_small_cell_raw_cell_margin(self):
        cell = self._f32(.001)
        low = self._f32(-98 * cell)
        high_x = self._f32(self._f32((2048 - 1 - 98) * cell) + self._f32(197 * cell))
        high_z = self._f32(self._f32((1024 - 1 - 98) * cell) + self._f32(197 * cell))
        with self.assertRaisesRegex(ValueError, 'omits original expanded domain'):
            self._validate_clearance_case(self._clearance_case(
                cell_size=.001, query_rect=[low, low, high_x - low, high_z - low]
            ))

    def test_clearance_collector_rejects_insufficient_98_cell_edge_margin(self):
        case = self._clearance_case()
        # Move the left edge inward while preserving a valid pinned record.
        rect = list(case[5]['query_rect'])
        rect[0] += .01
        case = self._clearance_case(query_rect=rect)
        with self.assertRaisesRegex(ValueError, 'omits original expanded domain'):
            self._validate_clearance_case(case)

    def test_clearance_projection_rejects_collector_frame_mismatch(self):
        case = self._clearance_case()
        case[6]['collector']['grid_width'] -= 1
        with self.assertRaisesRegex(ValueError, 'collector frame mismatch'):
            self._validate_clearance_case(case)

    def test_clearance_projection_rejects_collector_schema_mismatch(self):
        case = self._clearance_case()
        case[6]['collector']['unexpected'] = True
        with self.assertRaisesRegex(ValueError, 'collector identity invalid'):
            self._validate_clearance_case(case)

    def test_clearance_projection_rejects_projection_frame_mismatch(self):
        case = self._clearance_case()
        case[6]['frame']['width'] -= 1
        with self.assertRaisesRegex(ValueError, 'static map/frame mismatch'):
            self._validate_clearance_case(case)

    def test_duplicate_json_keys_rejected(self):
        with tempfile.TemporaryDirectory(dir='/private/tmp') as d:
            p = Path(d) / 'x.json'
            p.write_text('{"kind":"a","kind":"b"}')
            with self.assertRaisesRegex(ValueError, 'duplicate JSON property'):
                preflight._json(p)

    def test_pinned_file_tamper_rejected(self):
        with tempfile.TemporaryDirectory(dir='/private/tmp') as d:
            p = Path(d) / 'x.bin'
            p.write_bytes(b'changed')
            with self.assertRaisesRegex(ValueError, 'SHA-256 mismatch'):
                preflight._pin({'path': str(p), 'sha256': '0' * 64}, 'test')

    def test_malformed_binary_mask_rejected(self):
        meta = {'byteCount': 2, 'count': 2, 'sha256': '0' * 64}
        with self.assertRaisesRegex(ValueError, 'non-bool byte'):
            preflight._validate_binary_mask(b'\x00\x02', meta, 2, 'bad mask')

    def test_clearance_projection_wrong_kind_rejected(self):
        with tempfile.TemporaryDirectory(dir='/private/tmp') as d:
            p = Path(d) / 'clearance.json'
            p.write_text('{"kind":"Wrong","schema_version":1}')
            import hashlib
            with self.assertRaisesRegex(ValueError, 'clearance projection keys differ from schema'):
                preflight._validate_clearance_descriptor({'path': str(p), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}, {}, {}, {}, {}, {})

    def test_wrong_package_kind_rejected_before_source_load(self):
        package = json.loads(SAMPLE.read_text())
        package['kind'] = 'Untrusted'
        with self.assertRaisesRegex(ValueError, 'unsupported package kind/schema'):
            preflight.preflight_package(package)

    def test_real_s026_source_only_package(self):
        report = preflight.preflight_package(SAMPLE)
        self.assertEqual(report['status'], 'SourceOnlyPreflightPassed')
        self.assertEqual(report['source_pins_verified'], 6530)
        self.assertEqual(report['historical_producer_binaries_unavailable'], 7)
        self.assertEqual(report['signed_zero_decimal_loss_count'], 1990)
        self.assertEqual(report['water_footprint_count'], 398)
        self.assertEqual(report['source_derived_water_mask_sha256'], '5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee')
        self.assertFalse(report['world_created'])
        self.assertFalse(report['mission_ready'])
        self.assertTrue(report['verified_inputs'])

if __name__ == '__main__':
    unittest.main()
