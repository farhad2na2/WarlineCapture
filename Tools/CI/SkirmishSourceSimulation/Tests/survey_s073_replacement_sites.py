"""Conservative candidate filter, not original placement validation or source-fit certification."""
import argparse
import hashlib
import json
from pathlib import Path
import re

import numpy as np


def read(path):
    data = Path(path).read_bytes()
    return data, hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    for key in ("census", "census-receipt", "road-receipt", "surface", "sidewalk", "authored", "physical", "config", "map", "output"):
        parser.add_argument("--" + key, required=True)
    args = parser.parse_args()
    cb, ch = read(args.census); census = json.loads(cb); cr = json.loads(Path(args.census_receipt).read_text())
    road_receipt = json.loads(Path(args.road_receipt).read_text()); surface, sh = read(args.surface); sidewalk, wh = read(args.sidewalk)
    authored, ah = read(args.authored); physical, ph = read(args.physical); config, configh = read(args.config)
    map_bytes, map_hash = read(args.map)
    if (cr.get("validation_status") != "Passed" or cr.get("wrapper_exit_code") != 0 or cr.get("census_sha256") != ch
            or road_receipt.get("validation_status") != "Passed" or road_receipt.get("wrapper_exit_code") != 0
            or road_receipt.get("surface_mask_sha256") != sh or road_receipt.get("sidewalk_mask_sha256") != wh
            or ah != "b4c979394eb734ccd978b30896303dbb1c1ac058e7fc3eaef5da3fa0cb98c8b8"
            or ph != "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"
            or configh != "b9151b3fce0c55e32e2ffba289f00b6c661e5954272fbca06bba19a7ca4c1031"
            or map_hash != "b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a"):
        raise ValueError("Exact qualified S073 diagnostic inputs required")
    bounds = []
    for field in ("playableMin", "playableMax"):
        match = re.search(r"(?m)^    " + field + r": \{x: ([0-9]+), y: [^,]+, z: ([0-9]+)\}$", map_bytes.decode())
        if not match: raise ValueError("Integral source playable bounds required")
        bounds.append(tuple(map(int, match.groups())))
    post = census["post_stream"]; domain = post["complete_static_blocker_observation"]
    if post["entity_count"] != 4931 or domain["histories_bound_to_grid"] != domain["static_domain_count"]:
        raise ValueError("Complete native history census required")
    masks = [np.frombuffer(v, dtype=np.uint8) for v in (surface, sidewalk, authored)]
    water_record = json.loads(physical)["headlessPlacementInputs"]["water"]
    if water_record.get("present") is not False:
        raise ValueError("Reassess the inactive water evidence before this prefilter")
    if any(m.size != 2048 * 1024 or np.any(m > 1) for m in masks): raise ValueError("Exact bool grid masks required")
    blocked = np.logical_or.reduce(masks).reshape((1024, 2048)).copy()
    for row in post["entities"]:
        h = row["static_history"]
        if h is None or h["bound_to_grid_epoch"] is not True: raise ValueError("Unbound source history")
        x0, y0 = h["min"]; x1, y1 = h["max"]
        blocked[max(0,y0):min(1024,y1), max(0,x0):min(2048,x1)] = True
    for h in domain["other_blockers"]:
        if h["history_bound"] is not True: raise ValueError("Unbound other blocker")
        x0, y0 = h["min"]; x1, y1 = h["max"]
        blocked[max(0,y0):min(1024,y1), max(0,x0):min(2048,x1)] = True
    inputs = json.loads(config)["initial_spawn"]
    for faction in inputs["Factions"]:
        for unit in faction["Units"]:
            x = faction["SpawnX"] + unit["OffsetX"]; y = faction["SpawnY"] + unit["OffsetY"]
            blocked[max(0,y-6):min(1024,y+7), max(0,x-6):min(2048,x+7)] = True
    integral = np.pad(blocked.astype(np.int32), ((1,0),(1,0))).cumsum(axis=0).cumsum(axis=1)
    def candidates(origin, size):
        ox, oy = origin; w, h = size; out = []
        for y in range(bounds[0][1], bounds[1][1]-h+1):
            for x in range(bounds[0][0], bounds[1][0]-w+1):
                if integral[y+h,x+w] - integral[y,x+w] - integral[y+h,x] + integral[y,x] == 0:
                    out.append(((x-ox)**2+(y-oy)**2, (x+w/2-930)**2+(y+h/2-470)**2, x, y))
        out.sort()
        def rows(values):
            return [{"origin": [x,y], "distance_from_original_cells": round(original**.5, 3),
                     "center_distance_from_spawn_cells": round(spawn**.5, 3)} for original, spawn, x, y in values]
        return {"closest_to_original": rows(out[:25]), "closest_to_spawn": rows(sorted(out, key=lambda v: (v[1],v[0],v[3],v[2]))[:25]),
                "candidate_count": len(out)}
    result = {"kind": "S073ReplacementSitePrefilter", "native_census_sha256": ch, "native_surface_sha256": sh,
              "native_sidewalk_sha256": wh, "source_map_sha256": map_hash, "playable_bounds": bounds, "active_water_checked": False,
              "candidates": {"barrack": candidates((950,438),(28,15)), "ammunition_depot": candidates((887,444),(19,16))},
              "scope": "Conservative rectangle prefilter only; captured water may be incomplete, no live placement owner or native visual validation",
              "source_fit_certified": False, "mission_ready": False}
    Path(args.output).write_text(json.dumps(result,indent=2)+"\n")
    print(json.dumps({k:{"candidate_count":v["candidate_count"],"closest_to_spawn":v["closest_to_spawn"][:3]} for k,v in result["candidates"].items()}))


if __name__ == "__main__": main()
