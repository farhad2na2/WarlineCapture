"""Read-only, multi-error diagnostics; never a replacement for strict admission."""
import argparse
import csv
import hashlib
import json
import math
from collections import Counter
from itertools import product
from pathlib import Path

from build_skirmish_authoring_batches import PILOTS, validate as validate_catalog
from skirmish_ground_objective30_contract import SOURCE_KEYS, ROLES, validate as validate_ground30

CATALOG = "Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv"
MATRIX = "Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv"
TUPLE = ("map_id", "objective_id", "army_profile", "start_profile")
SIZES = {"Standard", "War", "LargeWar"}
KINDS = {"UnqualifiedFullCatalogLogicalAuthoringInputs", "UnqualifiedGroundObjective30Authoring"}

# Narrow diagnostic portability proof, not native/raw provenance admission.
SERIALIZED_TEXT_GRID = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"


def _serialized_grid_representations(raw):
    if len(raw) > 65536 or any(c < 32 and c not in (9, 10, 13) for c in raw):
        return None
    try:
        raw.decode("utf-8")
    except UnicodeError:
        return None
    lf = raw.replace(b"\r\n", b"\n")
    crlf = lf.replace(b"\n", b"\r\n")
    if b"\r" in lf or raw not in (lf, crlf):
        return None  # Reject bare CR and mixed endings, not arbitrary normalization.
    if not lf.startswith(b"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n"):
        return None
    if lf.count(b"  m_Name: MatchSubScene_GridAuthoring_Config\n") != 1 or lf.count(
            b"  m_Script: {fileID: 11500000, guid: a101000000000000000000000000000b, type: 3}\n") != 1:
        return None
    return {"actual_line_ending": "LF" if raw == lf else "CRLF", "actual_bytes": len(raw),
            "LF": {"sha256": hashlib.sha256(lf).hexdigest(), "bytes": len(lf)},
            "CRLF": {"sha256": hashlib.sha256(crlf).hexdigest(), "bytes": len(crlf)}}


class Diagnostics:
    def __init__(self, root, max_pin_bytes=64 * 1024 * 1024):
        self.root = Path(root).resolve()
        self.max_pin_bytes = max_pin_bytes
        self.errors = []
        self.blocked = []
        self.inputs = []
        self.hashes = {}
        self.grid_representations = {}
        self.representation_equivalences = []

    def error(self, source, location, code, message):
        self.errors.append(dict(source=str(source), location=location, code=code, message=message))

    def block(self, source, check, reason):
        self.blocked.append(dict(source=str(source), check=check, reason=reason))

    def csv(self, path, required):
        try:
            with Path(path).open(encoding="utf-8-sig", newline="") as stream:
                reader = csv.DictReader(stream)
                fields = reader.fieldnames or []
                if len(fields) != len(set(fields)) or not set(required).issubset(fields):
                    self.error(path, "header", "csv.header", "Missing or duplicate required columns")
                    return None
                result = list(reader)
            for i, row in enumerate(result, 2):
                if None in row or any(value is None for value in row.values()):
                    self.error(path, "row:" + str(i), "csv.row", "Row width differs from header")
            return result
        except (OSError, UnicodeError, csv.Error) as exc:
            self.error(path, "$", "csv.read", str(exc))
            return None

    def document(self, path):
        # Reject duplicate keys: otherwise a later value silently replaces authority.
        def pairs(items):
            result = {}
            for key, value in items:
                if key in result:
                    raise ValueError("Duplicate JSON key: " + key)
                result[key] = value
            return result
        try:
            data = json.loads(Path(path).read_text(encoding="utf-8-sig"), object_pairs_hook=pairs,
                              parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Nonfinite JSON: " + value)))
            if not isinstance(data, dict):
                raise ValueError("JSON object required")
            return data
        except (OSError, UnicodeError, ValueError) as exc:
            self.error(path, "$", "json.read", str(exc))
            return None

    def pin(self, source, location, pin, *, serialized_grid=False):
        if not isinstance(pin, dict) or not isinstance(pin.get("path"), str) or not pin["path"]:
            self.error(source, location, "pin.shape", "Explicit path and SHA256 required")
            return None
        digest = pin.get("sha256")
        if not isinstance(digest, str) or len(digest) != 64 or any(c not in "0123456789abcdef" for c in digest):
            self.error(source, location + "/sha256", "pin.hash", "Lowercase SHA256 required")
            return None
        path = (self.root / pin["path"]).resolve()
        if not path.is_relative_to(self.root):
            self.error(source, location + "/path", "pin.escape", "Pin escapes project root")
            return None
        try:
            if not path.is_file() or path.stat().st_size > self.max_pin_bytes:
                raise ValueError("Missing file or exceeds bounded pin-read limit")
            # Cache only within this diagnostic invocation. Strict admission rechecks later.
            if path not in self.hashes:
                raw = path.read_bytes()
                if raw.startswith(b"version https://git-lfs.github.com/spec/v1"):
                    raise ValueError("LFS pointer only; payload unavailable, no transfer attempted")
                self.hashes[path] = hashlib.sha256(raw).hexdigest()
                if path == self.root / SERIALIZED_TEXT_GRID:
                    self.grid_representations[path] = _serialized_grid_representations(raw)
            if self.hashes[path] != digest:
                representations = self.grid_representations.get(path) if serialized_grid else None
                expected_style = next((style for style in ("LF", "CRLF") if representations and
                                       representations[style]["sha256"] == digest), None)
                if expected_style is None:
                    raise ValueError("Pinned bytes differ: " + pin["path"])
                self.representation_equivalences.append(dict(source=str(source), location=location,
                    path=pin["path"], actual_raw_sha256=self.hashes[path], expected_raw_sha256=digest,
                    actual_bytes=representations["actual_bytes"],
                    equivalent_bytes=representations[expected_style]["bytes"],
                    transformation=representations["actual_line_ending"] + "To" + expected_style,
                    proof="ExpectedRawSha256EqualsInMemoryLineEndingOnlyRepresentation",
                    native_provenance_substitute=False))
            return path
        except (OSError, ValueError) as exc:
            self.error(source, location, "pin.drift", str(exc))
            return None

    def canonical(self, catalog_path, matrix_path):
        start = len(self.errors)
        catalog = self.csv(catalog_path, ("scenario_id",) + TUPLE)
        matrix = self.csv(matrix_path, ("catalog_id", "size_id", "army_profile", "start_profile", "objective_id"))
        expected_ids = {"S%03d" % i for i in range(1, 121)}
        by_id = {}
        catalog_ok = catalog is not None
        if catalog is not None:
            counts = Counter(row.get("scenario_id") for row in catalog)
            for i, row in enumerate(catalog, 2):
                sid = row.get("scenario_id")
                if sid not in expected_ids or counts[sid] != 1:
                    self.error(catalog_path, "row:" + str(i), "catalog.id", "Unknown or duplicate catalog ID: " + str(sid))
                values = tuple(row.get(k) for k in TUPLE)
                if values not in set(product(("DB", "CC", "MP", "IB", "AP"), ("BA", "FC", "BT", "CE"), ("G", "A", "C"), ("F", "E"))):
                    self.error(catalog_path, "row:" + str(i), "catalog.tuple", "Unsupported catalog tuple")
                if counts[sid] == 1:
                    by_id[sid] = row
            for sid in sorted(expected_ids - set(counts)):
                self.error(catalog_path, "$", "catalog.missing", "Missing catalog ID: " + sid)
            tuples = Counter(tuple(row.get(k) for k in TUPLE) for row in catalog)
            expected_tuples = set(product(("DB", "CC", "MP", "IB", "AP"), ("BA", "FC", "BT", "CE"), ("G", "A", "C"), ("F", "E")))
            for value in sorted(expected_tuples - set(tuples)):
                self.error(catalog_path, "$", "catalog.tuple_missing", "Missing tuple: " + repr(value))
            for value, count in tuples.items():
                if count > 1:
                    self.error(catalog_path, "$", "catalog.tuple_duplicate", "Duplicate tuple: " + repr(value))
            catalog_ok = len(self.errors) == start
        if matrix is not None:
            keys = Counter((row.get("catalog_id"), row.get("size_id")) for row in matrix)
            for i, row in enumerate(matrix, 2):
                key = (row.get("catalog_id"), row.get("size_id"))
                if key[0] not in expected_ids or key[1] not in SIZES or keys[key] != 1:
                    self.error(matrix_path, "row:" + str(i), "matrix.key", "Unknown or duplicate size vector: " + repr(key))
                if catalog_ok and key[0] in by_id:
                    for field in ("army_profile", "start_profile", "objective_id"):
                        if row.get(field) != by_id[key[0]].get(field):
                            self.error(matrix_path, "row:" + str(i) + "/" + field, "matrix.tuple", "Vector differs from canonical catalog")
            for key in sorted(set(product(expected_ids, SIZES)) - set(keys)):
                self.error(matrix_path, "$", "matrix.missing", "Missing size vector: " + repr(key))
            if not catalog_ok:
                self.block(matrix_path, "matrix_catalog_join", "Catalog authority is malformed; independent vector shape/coverage only")
        if len(self.errors) != start or catalog is None or matrix is None:
            self.block("canonical", "authored_budget_and_identity_joins", "Canonical authority invalid")
            return None
        try:
            validate_catalog(catalog, matrix)  # Original strict canonical rule remains authoritative.
        except (ValueError, KeyError, TypeError) as exc:
            self.error("canonical", "$", "canonical.strict", str(exc))
            return None
        self.canonical_data = (catalog, matrix)
        return catalog, matrix

    def areas(self, path, location, mission):
        areas = mission.get("typed_objective_areas")
        if not isinstance(areas, dict):
            self.error(path, location, "logical.areas", "Typed objective-area object required")
            return
        if not isinstance(areas.get("status"), str) or areas.get("measurement") is not None:
            self.error(path, location, "logical.measurement", "Unmeasured status required, no invented proof")
        values = areas.get("areas")
        if not isinstance(values, list):
            self.error(path, location + "/areas", "logical.area_list", "Explicit area list required")
            return
        objective = mission.get("tuple", {}).get("objective_id") if isinstance(mission.get("tuple"), dict) else None
        if values:
            roles = [a.get("Role") if isinstance(a, dict) else None for a in values]
            if objective not in ROLES or any(type(r) is not int for r in roles) or set(roles) != ROLES.get(objective) or len(roles) != len(ROLES.get(objective, ())):
                self.error(path, location + "/areas", "logical.roles", "Exact objective-specific area roles required")
            ids = []
            for i, area in enumerate(values):
                loc = location + "/areas/" + str(i)
                if not isinstance(area, dict):
                    self.error(path, loc, "logical.area", "Area object required")
                    continue
                aid = area.get("StableAreaId")
                if not isinstance(aid, str) or not aid or aid in ids:
                    self.error(path, loc + "/StableAreaId", "logical.area_id", "Distinct nonempty area ID required")
                ids.append(aid)
                points = area.get("Points")
                shape = area.get("Shape")
                if type(shape) is not int or not isinstance(points, list) or not ((shape == 1 and len(points) == 2) or (shape == 2 and len(points) >= 3)):
                    self.error(path, loc, "logical.shape", "Typed rectangle/polygon point count required")
                if isinstance(points, list):
                    for j, point in enumerate(points):
                        for field in ("WorldX", "WorldZ"):
                            v = point.get(field) if isinstance(point, dict) else None
                            try:
                                finite = type(v) in (int, float) and math.isfinite(v)
                            except OverflowError:
                                finite = False
                            if not finite:
                                self.error(path, loc + "/Points/" + str(j) + "/" + field, "logical.coordinate", "Finite world coordinate required")

    def source(self, path, location, source):
        if not isinstance(source, dict):
            self.error(path, location, "source.shape", "Source pin object required")
            return
        for key in SOURCE_KEYS:
            if not isinstance(source.get(key), str) or not source[key]:
                self.error(path, location + "/" + key, "source.field", "Explicit source identity field required")
        for k, h in (("sourcePath", "sourceAssetSha256"), ("gridPath", "gridAssetSha256"), ("surfacePath", "surfaceAssetSha256")):
            self.pin(path, location + "/" + k, dict(path=source.get(k), sha256=source.get(h)),
                     serialized_grid=(k == "gridPath"))

    def authority_object(self, path, key, checked):
        value = self.document(checked)
        if value is None:
            return False
        valid = False
        if key == "full_index":
            missions = value.get("missions")
            valid = (value.get("mission_count") == 120 and value.get("canonical_size_vectors") == 360
                     and isinstance(missions, list) and len(missions) == 120
                     and all(isinstance(m, dict) and isinstance(m.get("catalog_id"), str) for m in missions)
                     and {m["catalog_id"] for m in missions} == {"S%03d" % i for i in range(1, 121)}
                     and isinstance(value.get("source_candidates"), dict))
            if valid and hasattr(self, "canonical_data"):
                catalog, matrix = self.canonical_data
                by_id = {r["scenario_id"]: r for r in catalog}
                for row in missions:
                    sid = row["catalog_id"]
                    if row.get("tuple") != {k: by_id[sid][k] for k in TUPLE} or row.get("canonical_size_rows") != [r for r in matrix if r["catalog_id"] == sid]:
                        valid = False
                        break
        elif key == "logical_inputs_manifest":
            families = value.get("families")
            valid = (value.get("files") == 20 and value.get("missions") == 120
                     and isinstance(families, list) and len(families) == 20 and all(isinstance(f, dict) for f in families))
            if valid:
                for i, family in enumerate(families):
                    valid = bool(self.pin(path, "/authority/logical_inputs_manifest/families/" + str(i), family)) and valid
        elif key == "pilot_candidates":
            values = value.get("candidates")
            valid = (isinstance(values, list) and len(values) == 6
                     and all(isinstance(v, dict) and isinstance(v.get("catalog_id"), str) for v in values)
                     and {v["catalog_id"] for v in values} == PILOTS)
        elif key == "source_survey":
            values = value.get("sources")
            valid = isinstance(values, list) and bool(values) and all(isinstance(v, dict) for v in values)
        if not valid:
            self.error(path, "/authority/" + key, "authority.shape", "Pinned authority has invalid structural contract")
        return valid

    def authored(self, path, canonical, catalog_path, matrix_path):
        start = len(self.errors)
        doc = self.document(path)
        if doc is None:
            return
        kind = doc.get("kind")
        if kind not in KINDS or doc.get("schema_version") != 1:
            self.error(path, "$", "input.schema", "Supported version1 Full120 or GroundObjective30 authoring object required")
            return
        expected_count = 120 if kind == "UnqualifiedFullCatalogLogicalAuthoringInputs" else 30
        if doc.get("mission_count") != expected_count or doc.get("canonical_size_vectors") != expected_count * 3:
            self.error(path, "$", "input.count", "Declared mission/vector counts differ from schema")
        authority_ok = canonical is not None
        if expected_count == 120:
            for field, source in (("catalog_sha256", catalog_path), ("matrix_sha256", matrix_path)):
                checked = self.pin(path, "/" + field, dict(path=str(Path(source).resolve()), sha256=doc.get(field)))
                authority_ok = bool(checked) and authority_ok
        else:
            auth = doc.get("authority")
            auth = auth if isinstance(auth, dict) else {}
            for key in ("full_index", "logical_inputs_manifest", "pilot_candidates", "source_survey"):
                checked = self.pin(path, "/authority/" + key, auth.get(key))
                authority_ok = bool(checked) and authority_ok
                if checked:
                    authority_ok = self.authority_object(path, key, checked) and authority_ok
        if expected_count == 120:
            sources = doc.get("source_candidates")
            if not isinstance(sources, dict) or set(sources) != {"DB", "CC", "MP", "IB", "AP"}:
                self.error(path, "/source_candidates", "source.groups", "Exact five logical source groups required")
            if isinstance(sources, dict):
                for group, source in sources.items():
                    self.source(path, "/source_candidates/" + group, source)
            for flag in ("native_qualification", "player_ready", "physical_map_changes_authorized"):
                if doc.get(flag) is not False:
                    self.error(path, "/" + flag, "input.qualification", "Unqualified input must not grant readiness or geometry authority")
        missions = doc.get("missions")
        if not isinstance(missions, list):
            self.error(path, "/missions", "input.missions", "Mission array required")
            return
        if not authority_ok:
            self.block(path, "mission_canonical_joins_and_strict_admission", "Canonical or pinned document authority invalid; structural/pin diagnostics only")
        if canonical:
            catalog, matrix = canonical
            by_id = {r["scenario_id"]: r for r in catalog}
            expected = {r["scenario_id"] for r in catalog if expected_count == 120 or (r["army_profile"] == "G" and r["objective_id"] != "BA")}
        else:
            by_id, matrix, expected = {}, [], None
        seen = []
        for i, mission in enumerate(missions):
            loc = "/missions/" + str(i)
            if not isinstance(mission, dict):
                self.error(path, loc, "mission.shape", "Mission object required")
                continue
            sid = mission.get("catalog_id")
            if not isinstance(sid, str) or not sid:
                self.error(path, loc + "/catalog_id", "mission.id", "Nonempty catalog ID required")
            elif sid in seen:
                self.error(path, loc + "/catalog_id", "mission.duplicate", "Duplicate mission: " + sid)
            seen.append(sid)
            if authority_ok and isinstance(sid, str) and sid in by_id:
                entry = by_id[sid]
                if mission.get("tuple") != {k: entry[k] for k in TUPLE}:
                    self.error(path, loc + "/tuple", "mission.tuple", "Authored tuple differs from catalog")
                for field, value in (("definition_id", "skirmish." + sid.lower()), ("scenario_setup_id", "scenario.skirmish." + sid.lower())):
                    if mission.get(field) != value:
                        self.error(path, loc + "/" + field, "mission.identity", "Stable mission identity differs")
                budget = [r for r in matrix if r["catalog_id"] == sid]
                if mission.get("canonical_size_rows") != budget:
                    self.error(path, loc + "/canonical_size_rows", "mission.budget", "Exact canonical matrix rows differ (including fields/order)")
                digest = hashlib.sha256(json.dumps(budget, sort_keys=True).encode()).hexdigest()
                if mission.get("canonical_size_rows_sha256") != digest:
                    self.error(path, loc + "/canonical_size_rows_sha256", "mission.budget_hash", "Canonical row hash differs")
            elif authority_ok:
                self.error(path, loc + "/catalog_id", "mission.unknown", "Unknown catalog ID")
            self.pin(path, loc + "/packet", mission.get("packet"))
            if expected_count == 30:
                if mission.get("publication_enabled") is not False:
                    self.error(path, loc + "/publication_enabled", "logical.publication", "Unqualified input must not enable publication")
                self.areas(path, loc + "/typed_objective_areas", mission)
                view = mission.get("view_intent")
                if not isinstance(view, dict) or view.get("scalars") is not None or not isinstance(view.get("required_roles"), list) or not isinstance(view.get("status"), str):
                    self.error(path, loc + "/view_intent", "logical.view", "Unmeasured per-role view intent required")
                if not isinstance(mission.get("pending_gates"), list) or not mission["pending_gates"]:
                    self.error(path, loc + "/pending_gates", "logical.gates", "Explicit pending source/native/readability gates required")
                self.source(path, loc + "/source_candidate", mission.get("source_candidate"))
            else:
                logical = mission.get("required_logical_inputs")
                if not isinstance(logical, dict):
                    self.error(path, loc + "/required_logical_inputs", "logical.requirements", "Explicit logical input requirements required")
                else:
                    for key in ("view_profile", "typed_objective_areas", "route_independence", "ordinary_facility_and_all_size_force_fit"):
                        if not isinstance(logical.get(key), str) or not logical[key]:
                            self.error(path, loc + "/required_logical_inputs/" + key, "logical.requirement", "Explicit logical requirement required")
                    if logical.get("camera_scalars") is not None:
                        self.error(path, loc + "/required_logical_inputs/camera_scalars", "logical.scalars", "Unmeasured scalars required")
        if len(missions) != expected_count:
            self.error(path, "/missions", "mission.count", "Actual mission count differs from schema")
        if authority_ok and expected is not None:
            actual = {s for s in seen if isinstance(s, str)}
            for sid in sorted(expected - actual):
                self.error(path, "/missions", "mission.missing", "Missing mission: " + sid)
            for sid in sorted(actual - expected):
                self.error(path, "/missions", "mission.extra", "Unexpected mission: " + sid)
        status = "NotAdmission_Full120DiagnosticsOnly"
        if expected_count == 30 and authority_ok and len(self.errors) == start:
            # Retain the entire existing validator, including authority joins not duplicated here.
            try:
                validate_ground30(doc, self.root)
                status = "ExistingGround30ContractPassed" if len(self.errors) == start else "DiagnosticsFailed"
            except (ValueError, KeyError, TypeError, IndexError, OSError, AttributeError) as exc:
                self.error(path, "$", "existing_contract.failure", str(exc))
                status = "ExistingGround30ContractFailed"
        elif expected_count == 30:
            status = "BlockedByAuthority" if not authority_ok else "BlockedByDiagnostics"
            if authority_ok:
                self.block(path, "existing_ground30_contract", "Preflight errors; do not pass unsafe or malformed inputs into strict file reads")
        self.inputs.append(dict(source=str(path), kind=kind, strict_contract=status))


def diagnose(root, catalog=None, matrix=None, inputs=()):
    d = Diagnostics(root)
    catalog = Path(catalog) if catalog else d.root / CATALOG
    matrix = Path(matrix) if matrix else d.root / MATRIX
    canonical = d.canonical(catalog, matrix)
    for path in inputs:
        d.authored(Path(path), canonical, catalog, matrix)
    return dict(schema_version=1, result="ErrorsFound" if d.errors else "NoDiagnosticErrors",
                errors=d.errors, blocked_checks=d.blocked, inputs=d.inputs,
                representation_equivalences=d.representation_equivalences,
                canonical_authority_valid=canonical is not None,
                admission_replacement=False, source_fit_certified=False, mission_ready=False,
                scope="Read-only config/data diagnostics only; no objective replay, combat simulation, Unity, LFS or native acceptance")


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    p.add_argument("--catalog", type=Path)
    p.add_argument("--matrix", type=Path)
    p.add_argument("--input", action="append", type=Path, default=[])
    args = p.parse_args(argv)
    report = diagnose(args.root, args.catalog, args.matrix, args.input)
    print(json.dumps(report, indent=2, allow_nan=False))
    return int(bool(report["errors"]))


if __name__ == "__main__":
    raise SystemExit(main())
