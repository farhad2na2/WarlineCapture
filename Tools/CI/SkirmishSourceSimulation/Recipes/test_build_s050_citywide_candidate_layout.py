import importlib.util
from pathlib import Path
import unittest

SCRIPT = Path(__file__).with_name("build_s050_citywide_candidate_layout.py")
spec = importlib.util.spec_from_file_location("build_s050_citywide_candidate_layout", SCRIPT)
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


class S050GridAlignedCandidateLayoutTests(unittest.TestCase):
    def test_source_lower_left_origin_is_shifted_by_half_exact_footprint(self):
        self.assertEqual(builder.grid_aligned_pad_center((890, 442), (28, 15)), [904.0, 449.5])
        self.assertEqual(builder.grid_aligned_pad_center((903, 438), (15, 4)), [910.5, 440.0])
        self.assertEqual(builder.grid_aligned_pad_center((908, 467), (14, 7)), [915.0, 470.5])

    def test_vehicle_spawn_pad_adds_one_positive_z_grid_row_at_current_source_site(self):
        self.assertEqual(builder.grid_aligned_deployment_pad_center((72, 126), 9, 7), [932.5, 426.5])
        self.assertEqual(builder.grid_aligned_deployment_pad_center((72, 126), 9, 8), [932.5, 427.0])
        self.assertEqual(builder.grid_aligned_deployment_pad_center((318, 126), 9, 8), [1178.5, 427.0])

    def test_rejects_unbounded_or_non_grid_pad_extension(self):
        for cell, width, depth, extension in (((72.0, 126), 9, 8, 1), ((72, 126), 0, 8, 1),
                                               ((72, 126), 9, 8, -1), ((72, 126), 9, 8, 0.5)):
            with self.subTest(cell=cell, width=width, depth=depth, extension=extension):
                with self.assertRaisesRegex(ValueError, "Integer route cell"):
                    builder.grid_aligned_deployment_pad_center(cell, width, depth, extension)

    def test_rejects_invalid_source_site_geometry(self):
        for origin, footprint in (((1,), (2, 2)), ((1, 2), (0, 2)), ((1, 2), (2.5, 2))):
            with self.subTest(origin=origin, footprint=footprint):
                with self.assertRaisesRegex(ValueError, "Finite source lower-left origin"):
                    builder.grid_aligned_pad_center(origin, footprint)


if __name__ == "__main__":
    unittest.main(verbosity=2)
