import random
import math
import unittest


def original_occupied_key_order(width, height, center_x, center_z, radius, occupied):
    """Mirror the owner's z-major, then x-major, bounded cell sweep."""
    result = []
    for z in range(center_z - radius, center_z + radius + 1):
        if z < 0 or z >= height:
            continue
        for x in range(center_x - radius, center_x + radius + 1):
            if x < 0 or x >= width:
                continue
            key = x + z * width
            if key in occupied:
                result.append(key)
    return result


def sparse_occupied_key_order(width, height, center_x, center_z, radius, occupied):
    """Candidate: sorted unique flat keys filtered to the same rectangle."""
    result = []
    for key in sorted(occupied):
        z, x = divmod(key, width)
        if center_z - radius <= z <= center_z + radius and center_x - radius <= x <= center_x + radius:
            result.append(key)
    return result


def visit_candidates(cell_order, buckets):
    visited = []
    winner = None
    best_score = float("inf")
    for key in cell_order:
        # Keep each cell's original multimap bucket order unchanged.
        for candidate_id, score, eligible in buckets[key]:
            visited.append((key, candidate_id))
            if eligible and score < best_score:
                winner = candidate_id
                best_score = score
    return visited, winner, best_score


def building_index_candidates(source_x, source_z, max_distance, origin_x, origin_z, cell_size, targets):
    """Return broad-phase target indexes using a conservative square of grid cells."""
    source_cell_x = math.floor((source_x - origin_x) / cell_size)
    source_cell_z = math.floor((source_z - origin_z) / cell_size)
    radius_cells = math.ceil(max_distance / cell_size) + 1
    return sorted(
        index
        for index, target in enumerate(targets)
        if abs(target[0] - source_cell_x) <= radius_cells
        and abs(target[1] - source_cell_z) <= radius_cells
    )


def try_build_building_index_candidates(
    source_x, source_z, max_distance, origin_x, origin_z, cell_size, targets, snapshot_cells=None
):
    """Return None to require the owner's original full scan if index facts are unsafe."""
    values = (source_x, source_z, max_distance, origin_x, origin_z, cell_size)
    if not all(math.isfinite(value) for value in values) or cell_size <= 0 or max_distance < 0:
        return None
    if not math.isfinite(max_distance * max_distance):
        return None
    target_cells = []
    for target in targets:
        if target is None or not math.isfinite(target[0]) or not math.isfinite(target[1]):
            return None
        cell_x = math.floor((target[0] - origin_x) / cell_size)
        cell_z = math.floor((target[1] - origin_z) / cell_size)
        if not (-(2**31) <= cell_x < 2**31 and -(2**31) <= cell_z < 2**31):
            return None
        target_cells.append((cell_x, cell_z))
    if snapshot_cells is not None and target_cells != snapshot_cells:
        return None
    radius_cells = math.ceil(max_distance / cell_size) + 1
    if radius_cells >= 2**30:
        return None
    source_cell_x = math.floor((source_x - origin_x) / cell_size)
    source_cell_z = math.floor((source_z - origin_z) / cell_size)
    if not (-(2**31) <= source_cell_x < 2**31 and -(2**31) <= source_cell_z < 2**31):
        return None
    return sorted(
        index
        for index, (cell_x, cell_z) in enumerate(target_cells)
        if abs(cell_x - source_cell_x) <= radius_cells and abs(cell_z - source_cell_z) <= radius_cells
    )


def original_building_winner(source_x, source_z, max_distance, targets):
    winner = None
    best_score = float("inf")
    in_range = []
    for index, target in enumerate(targets):
        x, z, attacker_penalty, eligible = target
        dx, dz = x - source_x, z - source_z
        distance_squared = dx * dx + dz * dz
        if distance_squared > max_distance * max_distance:
            continue
        in_range.append(index)
        if not eligible:
            continue
        score = distance_squared + attacker_penalty
        if score < best_score:
            winner, best_score = index, score
    return in_range, winner, best_score


def indexed_building_winner(source_x, source_z, max_distance, targets, candidate_indexes):
    winner = None
    best_score = float("inf")
    in_range = []
    for index in candidate_indexes:
        x, z, attacker_penalty, eligible = targets[index]
        dx, dz = x - source_x, z - source_z
        distance_squared = dx * dx + dz * dz
        if distance_squared > max_distance * max_distance:
            continue
        in_range.append(index)
        if not eligible:
            continue
        score = distance_squared + attacker_penalty
        if score < best_score:
            winner, best_score = index, score
    return in_range, winner, best_score


class SparseAcquisitionCellOrderTests(unittest.TestCase):
    def assert_equivalent(self, width, height, center_x, center_z, radius, occupied, buckets):
        original = original_occupied_key_order(width, height, center_x, center_z, radius, occupied)
        sparse = sparse_occupied_key_order(width, height, center_x, center_z, radius, occupied)
        self.assertEqual(original, sparse)
        self.assertEqual(visit_candidates(original, buckets), visit_candidates(sparse, buckets))

    def test_boundaries_and_tie_selection_preserve_first_candidate(self):
        width, height = 13, 9
        occupied = {0, 1, 12, 13, 14, 25, 26, 37, 103, 104, 116}
        buckets = {key: [] for key in occupied}
        # Equal-score targets deliberately straddle keys and a shared cell bucket.
        buckets[13] = [("west-first", 5.0, True), ("west-second", 5.0, True)]
        buckets[14] = [("east-tie", 5.0, True)]
        buckets[26] = [("later-tie", 5.0, True)]
        for center_x, center_z, radius in [(-3, -2, 8), (0, 0, 2), (12, 8, 4), (6, 4, 32)]:
            self.assert_equivalent(width, height, center_x, center_z, radius, occupied, buckets)
        sequence = original_occupied_key_order(width, height, 0, 0, 2, occupied)
        self.assertEqual(visit_candidates(sequence, buckets)[1], "west-first")

    def test_randomized_sparse_dense_and_boundary_sweeps(self):
        rng = random.Random(0x5A002)
        for density in (0.002, 0.03, 0.5, 0.95, 1.0):
            for _ in range(100):
                width = rng.randint(1, 70)
                height = rng.randint(1, 55)
                occupied = {key for key in range(width * height) if rng.random() < density}
                center_x = rng.randint(-width, width * 2)
                center_z = rng.randint(-height, height * 2)
                radius = rng.randint(0, max(width, height) + 3)
                buckets = {}
                next_id = 0
                for key in occupied:
                    rows = []
                    for _ in range(rng.randint(0, 4)):
                        # Include ties, rejected candidates, and varied first winners.
                        score = float(rng.randint(0, 12))
                        rows.append((next_id, score, rng.random() < 0.75))
                        next_id += 1
                    if rows:
                        buckets[key] = rows
                active = set(buckets)
                self.assert_equivalent(width, height, center_x, center_z, radius, active, buckets)

    def test_building_target_broad_phase_preserves_in_range_order_and_ties(self):
        rng = random.Random(0xB017D1)
        for _ in range(500):
            origin_x = rng.uniform(-200.0, 200.0)
            origin_z = rng.uniform(-200.0, 200.0)
            cell_size = rng.uniform(0.25, 20.0)
            source_x = origin_x + rng.uniform(-4.0, 84.0) * cell_size
            source_z = origin_z + rng.uniform(-4.0, 84.0) * cell_size
            max_distance = rng.uniform(0.0, 50.0) * cell_size
            targets = []
            for _target in range(rng.randint(1, 300)):
                x = origin_x + rng.uniform(-8.0, 92.0) * cell_size
                z = origin_z + rng.uniform(-8.0, 92.0) * cell_size
                targets.append((x, z, float(rng.randint(0, 20)), rng.random() < 0.8))
            source_cell = (
                math.floor((source_x - origin_x) / cell_size),
                math.floor((source_z - origin_z) / cell_size),
            )
            target_cells = [
                (math.floor((target[0] - origin_x) / cell_size), math.floor((target[1] - origin_z) / cell_size))
                for target in targets
            ]
            indexed_targets = [(*cell, target[2], target[3]) for cell, target in zip(target_cells, targets)]
            candidates = building_index_candidates(
                source_x, source_z, max_distance, origin_x, origin_z, cell_size, indexed_targets
            )
            expected, expected_winner, expected_score = original_building_winner(
                source_x, source_z, max_distance, targets
            )
            actual, actual_winner, actual_score = indexed_building_winner(
                source_x, source_z, max_distance, targets, candidates
            )
            self.assertEqual(expected, actual)
            self.assertEqual((expected_winner, expected_score), (actual_winner, actual_score))

        # Explicit equal-score targets verify that candidate-index sorting restores
        # the original ToEntityArray order after spatial buckets are merged.
        targets = [(3.0, 0.0, 0.0, True), (-3.0, 0.0, 0.0, True)]
        candidates = building_index_candidates(0.0, 0.0, 4.0, 0.0, 0.0, 1.0, [(3, 0, 0.0, True), (-3, 0, 0.0, True)])
        self.assertEqual(candidates, [0, 1])
        self.assertEqual(original_building_winner(0.0, 0.0, 4.0, targets), (list(candidates), 0, 9.0))
        self.assertEqual(indexed_building_winner(0.0, 0.0, 4.0, targets, candidates), (list(candidates), 0, 9.0))

    def test_invalid_or_changed_index_facts_require_original_full_scan(self):
        targets = [(1.0, 1.0, 0.0, True), (3.0, 3.0, 1.0, True)]
        valid_cells = [(1, 1), (3, 3)]
        base = (0.0, 0.0, 4.0, 0.0, 0.0, 1.0, targets)
        self.assertIsNotNone(try_build_building_index_candidates(*base, snapshot_cells=valid_cells))
        for args in [
            (float("nan"), 0.0, 4.0, 0.0, 0.0, 1.0, targets),
            (0.0, 0.0, float("inf"), 0.0, 0.0, 1.0, targets),
            (0.0, 0.0, 4.0, float("nan"), 0.0, 1.0, targets),
            (0.0, 0.0, 4.0, 0.0, 0.0, 0.0, targets),
            (0.0, 0.0, 4.0, 0.0, 0.0, 1.0, [None]),
            (0.0, 0.0, 4.0, 0.0, 0.0, 1.0, [(float("inf"), 1.0, 0.0, True)]),
        ]:
            self.assertIsNone(try_build_building_index_candidates(*args))
        changed_targets = [(1.0, 1.0, 0.0, True), (8.0, 3.0, 1.0, True)]
        self.assertIsNone(
            try_build_building_index_candidates(*base[:6], changed_targets, snapshot_cells=valid_cells)
        )


if __name__ == "__main__":
    unittest.main()
