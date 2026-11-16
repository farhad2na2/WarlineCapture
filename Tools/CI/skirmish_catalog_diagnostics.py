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
KINDS = {"UnqualifiedFullCatalogLogicalAuthoringInputs", "UnqualifiedGroundObjective30Authoring", "UnqualifiedAirCombined80Authoring"}

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

    def air_template(self, path, location, mission):
        """Internal references only; the source/native anchor registry remains a separate gate."""
        tup = mission.get("tuple")
        if not isinstance(tup, dict) or not isinstance(tup.get("objective_id"), str):
            self.error(path, location + "/tuple", "air.template_tuple", "Existing ground objective tuple required")
            return
        self.areas(path, location + "/typed_objective_areas", mission)
        layout = mission.get("layout_input")
        if not isinstance(layout, dict):
            self.error(path, location + "/layout_input", "air.template_layout", "Existing ground layout input required")
            return
        routes = layout.get("retained_routes")
        pilot = layout.get("pilot_candidate")
        pads = pilot.get("typed_pad_world_axis_candidates") if isinstance(pilot, dict) else None
        if not isinstance(routes, list) or not isinstance(pads, list):
            self.error(path, location, "air.template_references", "Retained routes and typed pads required")
            return
        route_map, pad_ids = {}, []
        for i, route in enumerate(routes):
            loc = location + "/layout_input/retained_routes/" + str(i)
            if not isinstance(route, dict) or not isinstance(route.get("RouteId"), str) or not route["RouteId"] or route["RouteId"] in route_map:
                self.error(path, loc, "air.route_id", "Unique nonempty retained route identity required")
                continue
            anchors = route.get("WaypointAnchorIds")
            if not isinstance(anchors, list) or len(anchors) < 2 or any(not isinstance(a, str) or not a for a in anchors):
                self.error(path, loc, "air.route_anchors", "Explicit route waypoint references required")
                anchors = []
            route_map[route["RouteId"]] = anchors
        for i, pad in enumerate(pads):
            pid = pad.get("pad_id") if isinstance(pad, dict) else None
            if not isinstance(pid, str) or not pid or pid in pad_ids:
                self.error(path, location + "/layout_input/pilot_candidate/typed_pad_world_axis_candidates/" + str(i), "air.pad_id", "Unique typed pad identity required")
            else:
                pad_ids.append(pid)
        areas = mission.get("typed_objective_areas")
        derivations = areas.get("derivation") if isinstance(areas, dict) else None
        if not isinstance(derivations, list):
            self.error(path, location, "air.area_derivation", "Explicit existing typed-area derivations required")
            return
        expected_roles = ROLES.get(tup["objective_id"], set())
        derivation_roles = [v.get("role") for v in derivations if isinstance(v, dict)]
        if any(type(v) is not int for v in derivation_roles) or len(derivation_roles) != len(expected_roles) or set(derivation_roles) != expected_roles:
            self.error(path, location + "/typed_objective_areas/derivation", "air.area_roles", "One derivation per existing objective-specific typed role required")
        for i, item in enumerate(derivations):
            loc = location + "/typed_objective_areas/derivation/" + str(i)
            if not isinstance(item, dict):
                self.error(path, loc, "air.area_reference", "Typed-area reference object required")
                continue
            if "route_id" in item:
                rid = item["route_id"]
                if not isinstance(rid, str) or rid not in route_map or item.get("anchor_id") not in route_map.get(rid, []):
                    self.error(path, loc, "air.area_reference", "Typed-area route/anchor must reference that retained route")
            elif "pad_id" in item:
                if item["pad_id"] not in pad_ids:
                    self.error(path, loc, "air.area_reference", "Typed-area pad must exist in the retained typed pads")
            else:
                self.error(path, loc, "air.area_reference", "Explicit retained route/anchor or pad reference required")

    def air_authored(self, path, doc, canonical, catalog_path, matrix_path):
        # Reuse existing diagnostics for the actual pinned dependencies, without replacing strict native admission.
        dependencies = {}
        authority = doc.get("authority")
        for key, kind in (("full_index", "UnqualifiedFullCatalogLogicalAuthoringInputs"),
                          ("ground_authoring", "UnqualifiedGroundObjective30Authoring"), ("map_reuse_policy", None)):
            pin = authority.get(key) if isinstance(authority, dict) else None
            checked = self.pin(path, "/authority/" + key, pin)
            value = self.document(checked) if checked and kind else None
            if kind and checked:
                if not value or value.get("kind") != kind or value.get("schema_version") != 1:
                    self.error(path, "/authority/" + key, "air.authority_schema", "Exact existing dependency schema required")
                else:
                    dependencies[key] = value
                    if not any(Path(v["source"]).resolve() == checked for v in self.inputs):
                        self.authored(checked, canonical, catalog_path, matrix_path)
        original = self.pin(path, "/native_original_authority", dict(
            path="Tools/CI/SkirmishProductionDrafts/AirCombined80/air-combined-eighty.authoring.json",
            sha256="a869c03469d655f87389961242ffc0dc05720248ab6f6480cec43138793d8926"))
        if not original:
            self.block(path, "air_native_original_successor_semantics", "Missing original a869 authority; no reconstruction or native envelope admission")
        requirements = doc.get("requirement_source_pins")
        wanted = {"Assets/Game/Scripts/Configs/Skirmish/" + n + ".cs" for n in (
            "SkirmishScenarioDefinitionConfig", "SkirmishSetupCompiler", "SkirmishMissionLogicalBindingConfig",
            "SkirmishMeasuredLayoutBinder", "SkirmishObjectiveConfig", "SkirmishArmyProfileConfig")}
        if not isinstance(requirements, list) or len(requirements) != 6 or {p["path"] for p in requirements if isinstance(p, dict) and isinstance(p.get("path"), str)} != wanted:
            self.error(path, "/requirement_source_pins", "air.requirement_pins", "All six existing requirement source pins required")
        for i, pin in enumerate(requirements if isinstance(requirements, list) else []):
            self.pin(path, "/requirement_source_pins/" + str(i), pin)
        profiles, roles = doc.get("profile_asset_pins"), {}
        if not isinstance(profiles, dict) or set(profiles) != {"A", "C"}:
            self.error(path, "/profile_asset_pins", "air.profile_pins", "Exact A/C typed profile pins required")
        for key in ("A", "C"):
            checked = self.pin(path, "/profile_asset_pins/" + key, profiles.get(key) if isinstance(profiles, dict) else None)
            if checked:
                try:
                    lines = checked.read_text(encoding="utf-8").splitlines()
                    if lines.count("  profileId: " + key) != 1: raise ValueError()
                    start = lines.index("  allowedRoleIds:") + 1
                    values = []
                    for line in lines[start:]:
                        if not line.startswith("  - "): break
                        values.append(line[4:])
                    if not values or len(values) != len(set(values)) or any(not x.startswith("role.") for x in values): raise ValueError()
                    roles[key] = set(values)
                except (OSError, UnicodeError, ValueError):
                    self.error(path, "/profile_asset_pins/" + key, "air.profile_roles", "Expected existing serialized typed profile/allowed role list required")
        for key, value in (("mission_count", 80), ("canonical_size_vectors", 240), ("generic_candidate_count", 77), ("protected_read_only_count", 3)):
            if doc.get(key) != value: self.error(path, "/" + key, "air.count", "Exact80/240/77/3 schema counts required")
        for key in ("publication_enabled", "source_fit_certified", "player_ready"):
            if doc.get(key) is not False: self.error(path, "/" + key, "air.qualification", "Unqualified authoring cannot grant publication/fit/readiness")
        sources = doc.get("source_candidates")
        if not isinstance(sources, dict) or set(sources) != {"DB", "CC", "MP", "IB", "AP"}:
            self.error(path, "/source_candidates", "air.source_groups", "Exact existing five source groups required")
        if isinstance(sources, dict):
            for key, value in sources.items(): self.source(path, "/source_candidates/" + key, value)
        if isinstance(sources, dict):
            scenes = {v["sourceSceneGuid"] for v in sources.values() if isinstance(v, dict) and isinstance(v.get("sourceSceneGuid"), str)}
            if doc.get("physical_candidate_world_count") != len(scenes):
                self.error(path, "/physical_candidate_world_count", "air.count", "Physical-source count must match existing source scene identities")
        index = dependencies.get("full_index")
        ground = dependencies.get("ground_authoring")
        if index and sources != index.get("source_candidates"):
            self.error(path, "/source_candidates", "air.source_join", "Family source candidates differ from pinned Full120 input")
        index_rows = index.get("missions", []) if index else []
        ground_rows = ground.get("missions", []) if ground else []
        index_rows = index_rows if isinstance(index_rows, list) else []
        ground_rows = ground_rows if isinstance(ground_rows, list) else []
        full = {m.get("catalog_id"): m for m in index_rows if isinstance(m, dict) and isinstance(m.get("catalog_id"), str)}
        templates = {m.get("catalog_id"): m for m in ground_rows if isinstance(m, dict) and isinstance(m.get("catalog_id"), str)}
        if not index or not ground or canonical is None:
            self.block(path, "air_canonical_template_joins", "Missing/invalid pinned dependencies or canonical authority; structural diagnostics only")
        expected = {m["scenario_id"] for m in canonical[0] if m["army_profile"] in ("A", "C")} if canonical else None
        missions = doc.get("missions")
        if not isinstance(missions, list):
            self.error(path, "/missions", "air.missions", "Explicit eighty mission rows required")
            missions = []
        seen, used, protected = [], set(), []
        for i, mission in enumerate(missions):
            loc = "/missions/" + str(i)
            if not isinstance(mission, dict):
                self.error(path, loc, "air.mission", "Mission object required")
                continue
            sid = mission.get("catalog_id")
            if not isinstance(sid, str) or not sid or sid in seen or (expected is not None and sid not in expected):
                self.error(path, loc + "/catalog_id", "air.identity", "Unique canonical A/C catalog identity required")
            if not isinstance(sid, str): continue
            seen.append(sid)
            original_row = full.get(sid) if isinstance(sid, str) else None
            if original_row and canonical:
                for key in ("definition_id", "scenario_setup_id", "tuple", "packet", "packet_section", "canonical_size_rows", "canonical_size_rows_sha256", "required_capabilities", "protected_binding"):
                    if mission.get(key) != original_row.get(key): self.error(path, loc + "/" + key, "air.canonical_join", "Field differs from existing Full120 row")
            self.pin(path, loc + "/packet", mission.get("packet"))
            if mission.get("publication_enabled") is not False or mission.get("normal_mission_qualified") is not False or mission.get("camera_scalars") is not None:
                self.error(path, loc, "air.qualification", "Unpublished/unqualified row with unmeasured camera required")
            pending = mission.get("pending_gates")
            if not isinstance(pending, list) or not pending or any(not isinstance(v, str) or not v for v in pending):
                self.error(path, loc + "/pending_gates", "air.gates", "Explicit pending mechanics/source/native gates required")
            if sid in {"S003", "S004", "S005"}:
                protected.append(sid)
                if not isinstance(mission.get("protected_binding"), dict) or mission.get("action") != "PreserveCodedBinding_ReadOnlyNativeCensus" or mission.get("family_source_candidate_key") is not None or mission.get("ground_template_catalog_id") is not None or mission.get("typed_areas") != "NoReplacementForProtectedBinding" or mission.get("view_intent") is not None:
                    self.error(path, loc, "air.protected", "Existing coded binding must remain read-only without family/template replacement")
                continue
            tup = mission.get("tuple")
            if not isinstance(tup, dict):
                self.error(path, loc + "/tuple", "air.tuple", "Canonical tuple object required")
                continue
            if mission.get("protected_binding") is not None or mission.get("action") != "DisposableGenericAuthoringCandidate" or mission.get("family_source_candidate_key") != tup.get("map_id"):
                self.error(path, loc, "air.candidate", "Explicit disposable candidate with own family source required")
            template_id = mission.get("ground_template_catalog_id")
            template = templates.get(template_id) if isinstance(template_id, str) else None
            if ground:
                if template is None or sum(isinstance(v, dict) and v.get("catalog_id") == template_id for v in ground_rows) != 1:
                    self.error(path, loc + "/ground_template_catalog_id", "air.template", "One existing Ground30 template required")
                else:
                    tt = template.get("tuple", {})
                    tt = tt if isinstance(tt, dict) else {}
                    if tt.get("map_id") != tup.get("map_id") or tt.get("start_profile") != tup.get("start_profile") or (tup.get("objective_id") != "BA" and tt.get("objective_id") != tup.get("objective_id")):
                        self.error(path, loc + "/ground_template_catalog_id", "air.template_join", "Template map/start and non-BA objective must match")
                    if template_id not in used:
                        used.add(template_id)
                        self.air_template(path, "/ground_templates/" + template_id, template)
            typed = "NotApplicableBA_ViewOnly" if tup.get("objective_id") == "BA" else "ReuseExplicitUnmeasuredObjectiveGeometry_RebindOwnIdentity"
            if mission.get("typed_areas") != typed:
                self.error(path, loc + "/typed_areas", "air.typed_areas", "Existing unmeasured typed-area reuse boundary required")
            view = mission.get("view_intent")
            if not isinstance(view, dict) or view.get("scalars") is not None or view.get("measurement") is not None or view.get("role_scope") != "AllPermittedProfileRoles_NotAnOpeningRoster":
                self.error(path, loc + "/view_intent", "air.view", "Unmeasured all-permitted-role view intent required")
            else:
                values = view.get("required_role_ids")
                if not isinstance(values, list) or not values or any(not isinstance(v, str) or not v.startswith("role.") for v in values) or len(values) != len(set(values)):
                    self.error(path, loc + "/view_intent/required_role_ids", "air.view_roles", "Unique explicit permitted role identities required")
                elif isinstance(tup.get("army_profile"), str) and tup["army_profile"] in roles and set(values) != roles[tup["army_profile"]]:
                    self.error(path, loc + "/view_intent/required_role_ids", "air.view_roles", "View scope must include the actual pinned profile's complete permitted role set")
        if len(missions) != 80 or (expected is not None and set(seen) != expected) or len(seen) != len(set(seen)):
            self.error(path, "/missions", "air.coverage", "Exact80 distinct canonical A/C rows required")
        scoped = doc.get("protection_sets", {})
        scoped = scoped.get("scoped_protected_read_only_ids") if isinstance(scoped, dict) else None
        if not isinstance(scoped, list) or any(not isinstance(v, str) for v in scoped) or len(scoped) != 3 or set(scoped) != {"S003", "S004", "S005"} or set(protected) != {"S003", "S004", "S005"}:
            self.error(path, "/protection_sets", "air.protected", "Exactly S003/S004/S005 must remain protected")
        for field, key, want in (("objective_counts", "objective_id", {k: 20 for k in ("BA", "FC", "BT", "CE")}),
                                 ("profile_counts", "army_profile", {"A": 40, "C": 40})):
            actual = Counter(m["tuple"][key] for m in missions if isinstance(m, dict) and isinstance(m.get("tuple"), dict) and isinstance(m["tuple"].get(key), str))
            if doc.get(field) != want or actual != want:
                self.error(path, "/" + field, "air.count", "Declared and actual objective/profile distribution differs")
        self.block(path, "air_native_source_fit_and_mission_acceptance", "Static metadata/references do not prove native anchor registry, routes, facilities, aircraft lifecycle, views or missions")
        self.inputs.append(dict(source=str(path), kind=doc.get("kind"), strict_contract="NotAdmission_AirCombined80DiagnosticsOnly",
                                generic_candidates=sum(isinstance(m, dict) and m.get("action") == "DisposableGenericAuthoringCandidate" for m in missions),
                                protected_records=len(protected), referenced_ground_templates=len(used)))

    def authored(self, path, canonical, catalog_path, matrix_path):
        start = len(self.errors)
        doc = self.document(path)
        if doc is None:
            return
        kind = doc.get("kind")
        if kind not in KINDS or doc.get("schema_version") != 1:
            self.error(path, "$", "input.schema", "Supported version1 Full120, GroundObjective30 or AirCombined80 authoring object required")
            return
        if kind == "UnqualifiedAirCombined80Authoring":
            self.air_authored(path, doc, canonical, catalog_path, matrix_path)
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
