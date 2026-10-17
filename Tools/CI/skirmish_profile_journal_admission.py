"""One measured profile-preservation admission only; never qualifies a job."""
import ntpath
import re
from skirmish_native_identity import verify_job
from skirmish_production_queue import fingerprint

def need(ok, message):
    if not ok: raise ValueError(message)

def normal(path):
    need(isinstance(path, str) and ntpath.isabs(path) and ".." not in path.replace("\\", "/").split("/"),
         "Absolute nontraversing native profile root required")
    need(bool(ntpath.splitdrive(path)[0]), "Fully qualified native Windows root required")
    return ntpath.normcase(ntpath.normpath(path))

def snapshot(value, catalog, scope=None):
    keys={"root", "directory_exists", "directory_entries", "enumeration_limit", "files", "observed_bytes"}
    if scope is not None:keys|={"namespace_scope","excluded_non_save_entries","total_directory_entries"}
    need(isinstance(value, dict) and set(value) == keys,
         "Exact profile snapshot schema")
    normal(value["root"]); need(type(value["directory_exists"]) is bool, "Actual directory existence")
    names = [name + suffix for name in ("profile.json", "settings.json", "quickgame.json",
        "skirmish-" + catalog.lower() + "-native.json") for suffix in ("", ".tmp", ".bak", ".lock")]
    rows = value["files"]
    need(isinstance(rows, list) and len(rows) == 16 and [r.get("name") for r in rows] == names,
         "Complete ordered known profile files required")
    total = 0
    for row in rows:
        need(set(row) == {"name", "exists", "length", "sha256"} and type(row["exists"]) is bool
             and type(row["length"]) is int and 0 <= row["length"] <= 8388608, "Malformed measured profile file")
        if row["exists"]:
            need(value["directory_exists"] and isinstance(row["sha256"], str)
                 and re.fullmatch("[a-f0-9]{64}", row["sha256"]), "Actual existing profile SHA required")
            total += row["length"]
        else:
            need(row["length"] == 0 and row["sha256"] is None, "Missing file cannot carry bytes/hash")
    need(type(value["enumeration_limit"]) is int and value["enumeration_limit"] == 32
         and isinstance(value["directory_entries"], list)
         and value["directory_entries"] == sorted(r["name"] for r in rows if r["exists"])
         and len(value["directory_entries"]) <= 32, "Complete bounded directory enumeration required")
    need(type(value["observed_bytes"]) is int and value["observed_bytes"] == total <= 16777216,
         "Actual bounded byte total")
    if scope is not None:
        need(scope in ("original","owned"),"Exact journal namespace role")
        expected="OriginalCurrentCatalogSaveFiles_KnownEngineSiblingsExcluded" if scope=="original" else "OwnedCurrentCatalogSaveFiles_Exclusive"
        need(value["namespace_scope"]==expected,"Native original/owned namespace scope")
        excluded=value["excluded_non_save_entries"]
        need(isinstance(excluded,list) and all(isinstance(e,dict) and set(e)=={"name","kind"} for e in excluded),"Exact typed non-save exclusions")
        allowed={"TestResults.xml":"File","Unity":"Directory"} if scope=="original" else {}
        need(all(isinstance(e["name"],str) and isinstance(e["kind"],str) and e["name"] in allowed and allowed[e["name"]]==e["kind"] for e in excluded) and
             len({e["name"] for e in excluded})==len(excluded) and excluded==sorted(excluded,key=lambda e:e["name"]),"Only original known engine/test siblings may be excluded")
        total=value["total_directory_entries"]
        need(isinstance(total,list) and all(isinstance(n,str) for n in total) and total==sorted(value["directory_entries"]+[e["name"] for e in excluded]) and
             len(set(total))==len(total)<=32 and (value["directory_exists"] or not total),"Complete bounded save+excluded entry census")
    return value

def save_projection(value):
    return {k:v for k,v in value.items() if k not in {"namespace_scope","excluded_non_save_entries","total_directory_entries"}}

def validate_profile_journal(job, claim, events):
    try:
        need(all(isinstance(job.get(k), str) and re.fullmatch("[a-f0-9]{64}", job[k])
                 for k in ("candidate_sha256", "build_sha256")), "Actual source/build SHA required")
        h = fingerprint(job)
        need(claim["job_sha256"] == h, "Claim/job mismatch")
        need(isinstance(events, list) and events, "Actual event stream absent")
        for i, row in enumerate(events, 1):
            need(type(row["schema_version"]) is int and row["schema_version"] == 1 and type(row["sequence"]) is int and row["sequence"] == i
                 and row["job_sha256"] == h and row["attempt_id"] == claim["attempt_id"]
                 and row["worker"] == claim["worker"], "Native envelope ownership/order")
        def one(kind):
            rows = [(i, row["facts"]) for i, row in enumerate(events) if row["kind"] == kind]
            need(len(rows) == 1, "Exactly one " + kind + " required")
            return rows[0]
        need(not any(row["kind"] == "ProfileJournalFailure" for row in events), "Partial/failed native journal")
        context_i, context = one("ContextCaptured")
        need(context["source_sha256"] == job["candidate_sha256"]
             and context["compiled_dll_manifest_sha256"] == job["build_sha256"], "Actual candidate/build mismatch")
        begin_i, begin = one("ProfileBeforeDispatch")
        menu_i, menu = one("NormalMenuLaunchObserved")
        verify_job(job, menu["native_setup_identity"])
        need(isinstance(menu["native_session_id"], str) and menu["native_session_id"] and re.fullmatch("[0-9]+", menu["native_world_sequence"])
             and re.fullmatch("[a-f0-9]{64}", menu["native_config_sha256"]), "Actual native session/config missing")
        pre_i, pre = one("ProfileBeforeRestore")
        restoring_i, _ = one("RestoreStarting")
        after_i, after = one("ProfileAfterRestore")
        restored_i, restored = one("ContextRestored")
        need(context_i < begin_i < menu_i < pre_i < restoring_i < after_i < restored_i == len(events)-1,
             "Before/after journal must span actual redirect/restoration")
        originals = []
        owned = []
        versions=[stage.get("journal_version") for stage in (begin,pre,after)]
        need(all(type(v) is int and v in (1,2) for v in versions) and len(set(versions))==1,"One consistent journal schema version")
        version=versions[0]
        for stage in (begin, pre, after):
            need(set(stage) == {"journal_version", "catalog_id", "original_environment", "actual_environment",
                 "actual_effective_root", "original", "owned"} and type(stage["journal_version"]) is int
                 and stage["journal_version"] == version and stage["catalog_id"] == job["catalog_id"],
                 "Actual journal schema/catalog")
            need(stage["original_environment"] is None or isinstance(stage["original_environment"], str),
                 "Original environment type")
            need(stage["actual_environment"] is None or isinstance(stage["actual_environment"], str),
                 "Actual environment type")
            originals.append(snapshot(stage["original"], job["catalog_id"],"original" if version==2 else None))
            owned.append(snapshot(stage["owned"], job["catalog_id"],"owned" if version==2 else None))
        expected_owned = normal(ntpath.join(claim["artifact_directory"], "save-profile"))
        need(normal(menu["actual_save_root"]) == expected_owned, "Actual launch profile root splice")
        need("actual_save_root" in restored and restored["actual_save_root"] == after["actual_environment"],
             "Actual restored context root missing/different")
        original_root = normal(originals[0]["root"])
        original_environment = begin["original_environment"]
        if isinstance(original_environment, str) and original_environment.strip():
            need(normal(original_environment) == original_root, "Original environment/effective-root mismatch")
        need(original_root != expected_owned and not original_root.startswith(expected_owned + "\\")
             and not expected_owned.startswith(original_root + "\\"), "Profile roots overlap")
        need(all(normal(o["root"]) == expected_owned for o in owned), "Owned profile path splice")
        need(save_projection(originals[0]) == save_projection(originals[1]) == save_projection(originals[2]), "Original profile bytes/existence changed")
        need(not owned[0]["directory_exists"] and not any(r["exists"] for r in owned[0]["files"]),
             "Preexisting isolated profile is not a fresh owned lease")
        need(owned[1] == owned[2] and owned[1]["directory_exists"]
             and next(r for r in owned[1]["files"] if r["name"] == "profile.json")["exists"],
             "Isolated profile missing or changed during restoration")
        need(normal(begin["actual_effective_root"]) == original_root
             and normal(pre["actual_effective_root"]) == expected_owned
             and normal(after["actual_effective_root"]) == original_root, "Actual effective profile root not restored")
        need(begin["actual_environment"] == begin["original_environment"]
             == pre["original_environment"] == after["original_environment"] == after["actual_environment"]
             and normal(pre["actual_environment"]) == expected_owned, "Actual save-root environment mismatch")
        return {"admission": "profile_before_restore", "result": "MeasuredProfilePreservedAndRootRestored",
                "profile_restored": True, "job_sha256": h, "attempt_id": claim["attempt_id"],
                "native_session_id": menu["native_session_id"], "native_world_sequence": menu["native_world_sequence"],
                "native_config_sha256": menu["native_config_sha256"], "candidate_sha256": job["candidate_sha256"],
                "build_sha256": job["build_sha256"], "qualified": False, "queue_success_enabled": False}
    except (ValueError, KeyError, TypeError, IndexError, AttributeError) as error:
        return {"admission": "profile_before_restore", "result": "MissingOrFailedAdmission",
                "profile_restored": False, "errors": [str(error)], "qualified": False, "queue_success_enabled": False}
