"""Read-only operational joins. Never qualifies, publishes, launches or mutates a queue."""
import argparse
import copy
import csv
import hashlib
import io
import json
import math
import os
from pathlib import Path
import re

from invoke_skirmish_production_batch import verify_native_events, verify_mac_wrapper, METHOD
from skirmish_native_identity import verify_job, normalize_native_locale
from audit_skirmish_native_contacts import audit
from skirmish_production_queue import fingerprint
from skirmish_profile_journal_admission import validate_profile_journal
from s005_windows_source_snapshot import candidate_hash

MISSING = ("compile", "live_api", "source_fit", "paid_resources", "read_only_observers", "profile_before_restore")
REFS = {"descriptor","batch_events","wrapper_log","outer_wrapper","source_before","source_after"}
ATTEMPT = {"job","claim","immutable_claim","native_events","contacts","run_csv","run_id","trace","run_log"}
HEADER = "run_id,catalog_id,definition_version,code_hash,config_hash,size,difficulty,seed,locale,device,executor,normal_speed,started_at,result,end_reason,duration_seconds,input_violations,human_interventions,trace_path,log_path".split(",")
MAX_BYTES = 16 * 1024 * 1024
MAX_ROWS = 25000

def need(ok, why):
    if not ok: raise ValueError(why)

def strict_pairs(items):
    result={}
    for k,v in items:
        need(k not in result,"Duplicate JSON key: "+k);result[k]=v
    return result

def parse(text):
    return json.loads(text,object_pairs_hook=strict_pairs,parse_constant=lambda x: (_ for _ in ()).throw(ValueError("Nonfinite JSON")))

def _io_path(path):
    """Extended Windows spelling for IO only; public artifact identity stays canonical."""
    p=Path(path).absolute()
    if os.name!="nt":return p
    value=str(p);prefix=chr(92)*2+"?"+chr(92)
    if value.startswith(prefix):return p
    if value.startswith(chr(92)*2):return Path(prefix+"UNC"+chr(92)+value[2:])
    return Path(prefix+value)


def _resolved_path(path):
    p=_io_path(path).resolve(strict=True)
    if os.name!="nt":return p
    value=str(p);prefix=chr(92)*2+"?"+chr(92)
    if value.startswith(prefix+"UNC"+chr(92)):return Path(chr(92)*2+value[len(prefix)+4:])
    if value.startswith(prefix):return Path(value[len(prefix):])
    return p


def digest(path):
    h=hashlib.sha256()
    with _io_path(path).open("rb") as f:
        for chunk in iter(lambda:f.read(65536),b""):h.update(chunk)
    return h.hexdigest()

class Reader:
    def __init__(self, project):
        self.root=_resolved_path(project);self.pins={}
    def path(self,value):
        need(isinstance(value,(str,Path)) and str(value),"Missing artifact path")
        need(not str(value).replace("/",chr(92)).startswith((chr(92)*2+"?"+chr(92),chr(92)*2+"."+chr(92))),"Device artifact path refused")
        p=Path(value)
        if not p.is_absolute():p=self.root/p
        p=p.absolute()
        need(".." not in p.parts,"Artifact traversal refused")
        need(p.is_relative_to(self.root),"Artifact escapes project")
        cursor=p
        while cursor!=self.root:
            need(not _io_path(cursor).is_symlink() and not _io_path(cursor).is_junction(),"Artifact reparse path refused")
            cursor=cursor.parent
        p=_resolved_path(p)
        need(p.is_relative_to(self.root) and _io_path(p).is_file(),"Missing/escaped artifact")
        need(0 < _io_path(p).stat().st_size <= MAX_BYTES,"Empty/oversized bounded artifact")
        return p
    def read(self,ref):
        need(isinstance(ref,dict) and set(ref)=={"path","sha256"},"Exact artifact reference required")
        need(isinstance(ref["sha256"],str) and re.fullmatch("[0-9a-f]{64}",ref["sha256"]),"Malformed artifact hash")
        p=self.path(ref["path"]);need(digest(p)==ref["sha256"],"Artifact SHA differs: "+str(p))
        need(p not in self.pins or self.pins[p]==ref["sha256"],"Conflicting artifact aliases")
        self.pins[p]=ref["sha256"]
        return p,_io_path(p).read_text(encoding="utf-8-sig")
    def obj(self,ref):
        p,text=self.read(ref);v=parse(text);need(isinstance(v,dict),"JSON object required");return p,v
    def lines(self,ref):
        p,text=self.read(ref);lines=text.splitlines()
        need(0<len(lines)<=MAX_ROWS and all(0<len(s)<=262144 for s in lines),"Bounded nonempty JSONL required")
        values=[parse(s) for s in lines];need(all(isinstance(v,dict) for v in values),"JSONL object required")
        return p,values
    def derived(self,path):
        p=self.path(str(path));self.pins[p]=digest(p);return p
    def stable(self):
        need(all(_io_path(p).is_file() and digest(p)==h for p,h in self.pins.items()),"Artifact changed during inspection")

def one(events,kind):
    rows=[r["facts"] for r in events if r["kind"]==kind]
    need(len(rows)==1,"Exactly one "+kind+" required");return rows[0]

def validate_native_lifecycle(events):
    """Required semantic order; diagnostic events and repeated snapshots remain allowed."""
    def index(kind):
        rows=[i for i,row in enumerate(events) if row["kind"]==kind]
        message=("Actual normal Menu launch lifecycle missing/duplicate" if kind=="NormalMenuLaunchObserved"
                 else "Exactly one "+kind+" lifecycle required")
        need(len(rows)==1,message)
        return rows[0]
    captured=index("ContextCaptured");menu=index("NormalMenuLaunchObserved")
    returned=index("TerminalReturnOwnerCompleted");restoring=index("RestoreStarting")
    restored=index("ContextRestored")
    need(captured==0 and restored==len(events)-1 and captured<menu<returned<restoring<restored,
         "Native context/menu/return/restoration lifecycle order differs")
    snapshots=[(i,row["kind"],row["facts"]) for i,row in enumerate(events)
               if row["kind"] in ("NormalMenuLaunchObserved","NativeResolvedSnapshot","TerminalSnapshot")]
    need(all(captured<i<returned for i,_,_ in snapshots),"Native snapshot outside launch-to-return lifecycle")
    finished=[i for i,_,facts in snapshots if facts.get("phase")=="Finished"]
    need(finished,"Actual Finished terminal lifecycle missing")
    first_finished=min(finished)
    playing=[i for i,_,facts in snapshots if facts.get("phase")=="Playing"
             and facts.get("simulation_active") is True]
    need(menu<first_finished<returned and any(i<first_finished for i in playing),
         "Actual Menu/active Playing must precede Finished terminal")
    need(any(kind=="TerminalSnapshot" and facts.get("phase")=="Finished"
             for _,kind,facts in snapshots),"Actual Finished TerminalSnapshot owner missing")
    need(all(facts.get("phase")=="Finished" for i,_,facts in snapshots if i>=first_finished),
         "Native phase regressed after Finished terminal")
    need(all(facts.get("phase")=="Finished" for _,kind,facts in snapshots if kind=="TerminalSnapshot"),
         "TerminalSnapshot must observe Finished")
    need(all(facts.get("simulation_active") is False for _,_,facts in snapshots
             if facts.get("phase")=="Finished"),"Finished native simulation is not frozen")

def read_only_observer_receipt(job,claim,native):
    """One owned expanded snapshot in Playing and Finished; never qualification."""
    answer={"admission":"read_only_observers","result":"MissingAdmission","read_only_observers":False,
            "qualified":False,"queue_success_enabled":False}
    try:
        job_sha=fingerprint(job)
        need(all(isinstance(job.get(k),str) and re.fullmatch("[a-f0-9]{64}",job[k]) for k in ("candidate_sha256","build_sha256")),"Observer exact source/build hashes")
        need(all(isinstance(claim.get(k),str) and claim[k] for k in ("attempt_id","worker")),"Observer typed attempt/worker")
        need(claim["job_sha256"]==job_sha,"Observer canonical claim/job")
        need(isinstance(native,list) and native,"Observer native events missing")
        for i,row in enumerate(native,1):
            need(type(row["schema_version"])is int and row["schema_version"]==1 and
                 type(row["sequence"])is int and row["sequence"]==i and
                 row["job_sha256"]==job_sha and row["attempt_id"]==claim["attempt_id"] and
                 row["worker"]==claim["worker"],"Observer event ownership/order")
        validate_native_lifecycle(native)
        captured=one(native,"ContextCaptured")
        need(captured["source_sha256"]==job["candidate_sha256"] and
             captured["compiled_dll_manifest_sha256"]==job["build_sha256"],"Observer candidate/build")
        menu=one(native,"NormalMenuLaunchObserved")
        verify_job(job,menu["native_setup_identity"])
        session=menu["native_session_id"];world=menu["native_world_sequence"];config=menu["native_config_sha256"]
        need(isinstance(session,str) and session and isinstance(world,str) and len(world)<=20 and re.fullmatch("[0-9]+",world) and int(world)<=18446744073709551615 and
             isinstance(config,str) and re.fullmatch("[a-f0-9]{64}",config),"Observer native session/world/config")
        need(not any(r["kind"]=="NativeReadOnlyObserverProofUnsupported" for r in native),"Observer Unsupported evidence retained")
        proofs=[(i,r) for i,r in enumerate(native) if r["kind"]=="NativeReadOnlyObserverProof"]
        need(len(proofs)==2 and [r["facts"].get("phase") for _,r in proofs]==["Playing","Finished"],
             "Exactly one Playing then Finished observer window")
        fields=set("schema scope native_world_sequence native_session_id catalog_id phase status beforeSha256 afterSha256 capturedEntityCount queriedRelevantEntityCount coverageComplete capturedFrameBefore capturedFrameAfter worldEntityCount observerElapsedMilliseconds captureQueriesReadOnly worldUpdateBetweenSnapshots diagnosticFactsFedToAria missionReady".split())
        windows=[]
        for index,row in proofs:
            facts=row["facts"];need(isinstance(facts,dict) and set(facts)==fields,"Exact observer schema")
            need(facts["schema"]=="NativeReadOnlyObserverWindow-v1" and facts["scope"]=="ExpandedResolvedSnapshot",
                 "Known expanded observer scope")
            need(facts["native_session_id"]==session and facts["native_world_sequence"]==world and
                 facts["catalog_id"]==job["catalog_id"],"Observer subject splice")
            need(index>0,"Observer snapshot association missing")
            previous=native[index-1];snap=previous["facts"]
            need(previous["kind"] in ("NormalMenuLaunchObserved","NativeResolvedSnapshot","TerminalSnapshot") and
                 snap["phase"]==facts["phase"] and snap["native_session_id"]==session and
                 snap["native_world_sequence"]==world and snap["native_config_sha256"]==config,
                 "Observer exact preceding native snapshot")
            verify_job(job,snap["native_setup_identity"])
            need(facts["status"]=="ComparedEqual" and
                 all(isinstance(facts[k],str) and re.fullmatch("[a-f0-9]{64}",facts[k]) for k in ("beforeSha256","afterSha256")) and
                 facts["beforeSha256"]==facts["afterSha256"],"Observer complete before/after equality")
            need(all(type(facts[k])is int and 0<=facts[k]<=2147483647 for k in
                 ("capturedEntityCount","queriedRelevantEntityCount","worldEntityCount","capturedFrameBefore","capturedFrameAfter")),
                 "Observer finite typed counts/frames")
            need(0<facts["capturedEntityCount"]==facts["queriedRelevantEntityCount"]<=facts["worldEntityCount"] and
                 facts["coverageComplete"]is True,"Observer complete relevant entity coverage")
            need(type(row.get("frame"))is int and type(previous.get("frame"))is int and
                 facts["capturedFrameBefore"]==facts["capturedFrameAfter"]==row["frame"]==previous["frame"],
                 "Observer same actual frame window")
            cost=facts["observerElapsedMilliseconds"]
            need(type(cost)in (int,float) and math.isfinite(cost) and cost>=0,"Observer finite nonnegative cost")
            need(facts["captureQueriesReadOnly"]is True and facts["worldUpdateBetweenSnapshots"]is False and
                 facts["diagnosticFactsFedToAria"]is False and facts["missionReady"]is False,
                 "Observer isolation flags")
            windows.append({"sequence":row["sequence"],"frame":row["frame"],"facts":copy.deepcopy(facts)})
        answer.update(result="MeasuredLiveAndTerminalObserverWindows",read_only_observers=True,
            job_sha256=job_sha,attempt_id=claim["attempt_id"],native_session_id=session,
            native_world_sequence=world,native_config_sha256=config,candidate_sha256=job["candidate_sha256"],
            build_sha256=job["build_sha256"],windows=windows,
            scope="ExpandedResolvedSnapshot_RelevantEntitiesAndNativeProfileProjection")
    except (ValueError,KeyError,TypeError,IndexError,AttributeError,OverflowError) as error:
        answer["errors"]=[str(error)]
    return answer


def sampling_scope(job):
    """Classify existing request contracts; never upgrade development evidence."""
    schema=job.get("schema_version")
    catalog=job.get("catalog_id")
    protected=catalog in ("S001","S025","S073")
    need(type(schema) is int,"Typed production job schema required")
    if schema==3:
        need(protected,"Schema3 development counterpart requires a protected catalog")
        return "DevelopmentCounterpartZeroBaselineCredit"
    if schema==2:
        need(protected,"Schema2 legacy preset requires a protected catalog")
        return "LegacyPresetZeroBaselineCredit"
    need(schema==1 and not protected,"New normal counterpart dispatch authority requires separate review")
    return "ExpandedNormalUnqualified"


def ownership(value):
    need(value["preview_census_complete"] is True and type(value["actual_preview_count"]) is int and
        value["actual_preview_count"]==value["measured_preview_count"],"Unknown preview ownership")
    for world in value["worlds"]:
        need(all(type(world[k]) is int and world[k]==0 for k in
            ("native_sessions","owned_actors","setup_records","launch_definition_records","logical_provider_records")),"Owned native objects remain")
    # Same stable projection as native CleanupSignature, retaining all other fields.
    v=copy.deepcopy(value)
    for scene in v["scenes"]:
        if scene["preview"] is False:
            scene.pop("handle",None)
            for root in scene["roots"]:root.pop("native_entity_id",None)
            scene["roots"].sort(key=lambda r:r["name"])
    v["scenes"].sort(key=lambda s:(s["path"],s["name"]))
    return v

def inspect(project,spec,*,wrapper_platform='windows'):
    result={"schema_version":1,"result":"Incomplete","wins":0,"attempts":[],
        "admission":{"result":"MissingAdmission","missing":list(MISSING)},
        "queue_success_enabled":False,"qualified":False,"mission_ready":False,"publication_changed":False,"errors":[]}
    try:
        need(isinstance(spec,dict) and set(spec)=={"schema_version",*REFS,"attempts"} and
            type(spec["schema_version"]) is int and spec["schema_version"]==1,"Exact inspector schema required")
        read=Reader(project)
        descriptor_path,descriptor=read.obj(spec["descriptor"])
        _,events=read.lines(spec["batch_events"]);_,log=read.read(spec["wrapper_log"])
        _,outer=read.obj(spec["outer_wrapper"])
        need(outer["descriptor_sha256"]==spec["descriptor"]["sha256"] and type(outer["checked_wrapper_exit"]) is int and
            outer["checked_wrapper_exit"]==0 and outer["execute_method"]==METHOD and outer["queue_success_enabled"] is False,
            "Actual outer wrapper/descriptor/method mismatch")
        need(wrapper_platform in ('windows', 'macos'), 'Unknown wrapper platform')
        if wrapper_platform == 'macos':
            verify_mac_wrapper(descriptor, outer, project)
        else:
            need(re.findall(r"\[UnityInvoke\] ExitCode: (-?\d+)",log)==["0"],"Unique actual checked exit0 required")
        need(not re.search(r"Crash!!!|error CS\d+:|\[SkirmishProductionOperationalRun\] result=Failed",log),"Native failure retained")
        store_path=read.derived(descriptor["candidate_manifest_path"])
        need(digest(store_path)==descriptor["candidate_manifest_sha256"],"Immutable candidate store SHA differs")
        store=parse(_io_path(store_path).read_text(encoding="utf-8-sig"))
        need(candidate_hash(store["source_manifest"])==store["candidate_sha256"]==descriptor["candidate_sha256"] and
            fingerprint(store["build_manifest"])==store["build_sha256"]==descriptor["build_sha256"],"Candidate/build rows do not derive identity")
        for name,phase in (("source_before","before"),("source_after","after")):
            _,source=read.obj(spec[name])
            need(source["phase"]==phase and source["complete_manifest_equality"] is True and
                source["observed_candidate_sha256"]==descriptor["candidate_sha256"] and
                source["observed_build_sha256"]==descriptor["build_sha256"] and
                read.path(source["manifest_path"])==store_path and source["manifest_sha256"]==descriptor["candidate_manifest_sha256"],
                "Actual full source/build endpoint differs")
        # Reuse actual whole-batch/runtime/exit/fingerprint verifier; never replace by a marker-only detector.
        if wrapper_platform == 'macos':
            verify_native_events(descriptor,events,log,Path(project),mac_wrapper_receipt=outer)
        else:
            verify_native_events(descriptor,events,log,Path(project))
        need(descriptor_path==read.path(Path(descriptor["artifact_directory"])/"batch.json"),"Noncanonical descriptor path")
        declared=spec["attempts"];need(isinstance(declared,list) and len(declared)==len(descriptor["jobs"]),"Every dispatched attempt required")
        baseline=one(events,"BatchCaptured")
        baseline_ownership=ownership(baseline["baseline_ownership_census"])
        dispatch=[r["facts"] for r in events if r["kind"]=="Dispatch"]
        cleanups=[r["facts"] for r in events if r["kind"]=="CleanupOwnershipCensus"]
        measured=[r["facts"] for r in events if r["kind"]=="CleanupMeasured"]
        need(len(measured)==len(declared),"Per-job cleanup measurement absent")
        sessions=set();worlds=set();run_ids=set();profile_receipts=[];profile_errors=[];observer_receipts=[];observer_errors=[]
        for i,(item,entry) in enumerate(zip(declared,descriptor["jobs"])):
            need(isinstance(item,dict) and set(item)==ATTEMPT,"Exact attempt refs required")
            jp,job=read.obj(item["job"]);cp,claim=read.obj(item["claim"]);ip,immutable=read.obj(item["immutable_claim"])
            need(jp==read.path(entry["job_path"]) and cp==read.path(entry["claim_path"]),"Descriptor job/claim path splice")
            evidence_scope=sampling_scope(job)
            job_sha=fingerprint(job)
            need(claim==immutable and claim["job_sha256"]==job_sha,"Canonical/immutable claim differs")
            folder=Path(claim["artifact_directory"])
            need(cp==jp.parent/"claim.json" and ip==folder/"claim.json" and folder.parent==jp.parent and
                folder.name==claim["attempt_id"] and jp.parent.name==job["job_id"],"Attempt folder ownership differs")
            need(dispatch[i]["job_sha256"]==job_sha and dispatch[i]["attempt_id"]==claim["attempt_id"],"Dispatch attempt splice")
            need(job["source_manifest"]==store["source_manifest"] and job["candidate_sha256"]==descriptor["candidate_sha256"] and
                job["build_sha256"]==descriptor["build_sha256"],"Job source/build differs")
            ep,native=read.lines(item["native_events"])
            need(ep==folder/"adapter-native-events.jsonl","Native event stream not owned by attempt")
            for sequence,row in enumerate(native,1):
                need(type(row["schema_version"]) is int and row["schema_version"]==1 and type(row["sequence"]) is int and
                    row["sequence"]==sequence and row["job_sha256"]==job_sha and row["attempt_id"]==claim["attempt_id"] and
                    row["worker"]==claim["worker"],"Native event ownership/order differs")
            validate_native_lifecycle(native)
            captured=one(native,"ContextCaptured");restored=one(native,"ContextRestored");one(native,"RestoreStarting")
            need(captured["source_sha256"]==descriptor["candidate_sha256"] and captured["compiled_dll_manifest_sha256"]==descriptor["build_sha256"],
                "Captured candidate/build differs")
            need(restored["native_qualification"]=="pending_adapter_canary","Restore qualification boundary changed")
            need(native[-1]["kind"]=="ContextRestored","Actual restore is incomplete")
            need(len([r for r in native if r["kind"]=="NormalMenuLaunchObserved"])==1,"Actual normal Menu launch missing")
            snapshots=[r["facts"] for r in native if r["kind"] in ("NormalMenuLaunchObserved","NativeResolvedSnapshot","TerminalSnapshot")]
            need(snapshots,"Actual native resolved setup missing")
            session=snapshots[0]["native_session_id"];world=snapshots[0]["native_world_sequence"]
            need(isinstance(session,str) and session and session not in sessions and isinstance(world,str) and
                re.fullmatch("[0-9]+",world) and world not in worlds and world not in baseline["baseline_worlds"],"Native session/world missing or reused")
            sessions.add(session);worlds.add(world);config=snapshots[0]["native_config_sha256"]
            need(isinstance(config,str) and re.fullmatch("[a-f0-9]{64}",config),"Actual config hash missing")
            live=read.derived(snapshots[0]["native_trace_path"])
            for snap in snapshots:
                verify_job(job,snap["native_setup_identity"])
                need(snap["native_session_id"]==session and snap["native_world_sequence"]==world and
                    snap["native_config_sha256"]==config and normalize_native_locale(snap["actual_native_locale"])==job["locale"] and
                    read.path(snap["native_trace_path"])==live and Path(snap["actual_save_root"])==folder/"save-profile",
                    "Actual session/config/locale/trace/profile splice")
            terminal=[s for s in snapshots if s["phase"]=="Finished"]
            need(terminal,"Actual Finished terminal missing (Abort/Blocked/diagnostic is incomplete)")
            outcome=terminal[0]["outcome"];need(outcome in ("Victory","Defeat","Draw"),"Incomplete/unknown native outcome")
            need(all(s["outcome"]==outcome and s["result_outcome"]==outcome and s["result_session_id"]==session and
                type(s["result_frozen"]) is int and s["result_frozen"]>0 and s["normal_speed_latch"] is True for s in terminal),
                "Frozen result/session/normal-speed mismatch")
            returned=one(native,"TerminalReturnOwnerCompleted")
            need(returned["native_terminal_session_id"]==session and returned["native_terminal_outcome"]==outcome and
                returned["owner_probe_passed"] is True and returned["terminal_action"]=="MAIN MENU" and
                returned["actual_shell_route"]=="MainMenu" and returned["actual_shell_transition_running"] is False and
                returned["native_match_session_present"] is False and type(returned["native_terminal_session_count"]) is int and
                returned["native_terminal_session_count"]==0 and returned["native_outcome_matches_rendered_title"] is True,
                "Actual rendered result/normal return incomplete")
            for kind in ("rendered_result_presentation","localized_main_menu_presentation"):
                proof=returned[kind]
                need(isinstance(proof,dict) and proof["attempt"]==session and proof["rendered"] is True and proof["copyMatches"] is True and
                    proof["locale"]==("en" if job["locale"]=="EN" else "fa-IR") and type(proof["frame"]) is int and proof["frame"]>0 and
                    isinstance(proof["actualTitle"],str) and proof["actualTitle"] and isinstance(proof["expectedTitle"],str) and proof["expectedTitle"],
                    "Actual presentation proof/session/locale missing")
            need(returned["rendered_result_presentation"]["nativeOutcome"]==outcome and
                returned["localized_main_menu_presentation"]["presented"] is True and returned["localized_main_menu_presentation"]["pushHistory"] is True,
                "Result/menu presentation mismatch")
            contacts_path,contacts=read.lines(item["contacts"]);contact_audit=audit(contacts)
            need(contact_audit["strictRecordedContacts"] and contact_audit["nativeMenuContactsRecorded"] and
                contact_audit["terminalContactsRecorded"],"Actual contact audit failed")
            terminal_contacts=[c for c in contacts if c.get("phase")=="Terminal"]
            need(all(c["device"]==returned["driver_device"] and c["generation"]==returned["driver_generation"] for c in terminal_contacts) and
                returned["unexpected_samples"]==0 and returned["physical_interventions"]==0 and
                max(c["completed"] for c in terminal_contacts)==returned["completed_gestures"],"Terminal contact driver/counters splice")
            trace_path,trace=read.lines(item["trace"]);live_contacts=read.derived(str(live)+".touch.jsonl")
            need(contacts_path==Path(str(trace_path)+".touch.jsonl") and digest(contacts_path)==digest(live_contacts) and
                digest(trace_path)==digest(live),"Final trace/contact artifacts differ from owned live stream")
            need(any(t.get("phase")=="Playing" and t.get("simulationActive")==1 for t in trace),"Genuine active Playing trace missing")
            _,csv_text=read.read(item["run_csv"]);rows=csv.DictReader(io.StringIO(csv_text))
            need(rows.fieldnames==HEADER,"Actual runs.csv schema differs")
            matches=[r for r in rows if r["run_id"]==item["run_id"]];need(len(matches)==1,"Unique actual run row missing")
            run=matches[0];need(run["run_id"] not in run_ids,"Run identity reused");run_ids.add(run["run_id"])
            need(run["catalog_id"]==job["catalog_id"] and int(run["definition_version"])==job["definition_version"] and
                run["code_hash"]==job["candidate_sha256"] and run["config_hash"]==config and int(run["seed"])==job["seed"] and
                normalize_native_locale(run["locale"])==job["locale"] and run["size"]==job["size"] and run["difficulty"]==job["difficulty"] and
                run["executor"]==job["control_mode"]=="aria" and run["normal_speed"]=="1" and run["result"]==outcome and
                run["input_violations"]=="0" and run["human_interventions"]=="0","Actual terminal row differs/Abort/unclean")
            need(math.isfinite(float(run["duration_seconds"])) and float(run["duration_seconds"])>0 and run["started_at"],"Actual duration/start missing")
            need(read.path(run["trace_path"])==trace_path,"CSV trace ownership differs")
            run_log_path,run_log=read.read(item["run_log"])
            need(read.path(run["log_path"])==run_log_path and
                ("[SkirmishS002AriaRun] appended=1 countedWin="+("1" if outcome=="Victory" else "0")+" error=") in run_log and
                run["run_id"]+"," in run_log,"Actual appended/count-win run log missing")
            need(cleanups[i]["job_sha256"]==job_sha and ownership(cleanups[i]["census"])==baseline_ownership and
                measured[i]["job_sha256"]==job_sha and measured[i]["worlds"]==baseline["baseline_worlds"] and
                measured[i]["scenes"]==baseline["baseline_scenes"] and measured[i]["preview_count"]==baseline["baseline_preview_count"],
                "Actual cleanup differs from captured baseline")
            # Pure validator consumes only the already Reader-pinned owned event stream.
            profile=(validate_profile_journal(job,claim,native,path_platform='macos')
                     if wrapper_platform == 'macos' else validate_profile_journal(job,claim,native))
            expected_profile={"admission":"profile_before_restore",
                "result":"MeasuredProfilePreservedAndRootRestored","profile_restored":True,
                "job_sha256":job_sha,"attempt_id":claim["attempt_id"],
                "native_session_id":session,"native_world_sequence":world,
                "native_config_sha256":config,"candidate_sha256":job["candidate_sha256"],
                "build_sha256":job["build_sha256"],"qualified":False,"queue_success_enabled":False}
            profile_receipts.append(profile)
            if not (isinstance(profile,dict) and all(type(profile.get(k))is type(v) and profile.get(k)==v for k,v in expected_profile.items())):
                profile_errors.append({"job_sha256":job_sha,"attempt_id":claim["attempt_id"],
                    "errors":profile.get("errors",["Profile receipt does not match inspected subject"]) if isinstance(profile,dict) else ["Malformed profile receipt"]})
            observer=read_only_observer_receipt(job,claim,native)
            expected_observer={"admission":"read_only_observers","result":"MeasuredLiveAndTerminalObserverWindows",
                "read_only_observers":True,"job_sha256":job_sha,"attempt_id":claim["attempt_id"],
                "native_session_id":session,"native_world_sequence":world,"native_config_sha256":config,
                "candidate_sha256":job["candidate_sha256"],"build_sha256":job["build_sha256"],
                "qualified":False,"queue_success_enabled":False}
            if isinstance(observer,dict):observer["native_events"]=copy.deepcopy(item["native_events"])
            observer_receipts.append(observer)
            if not(isinstance(observer,dict) and all(type(observer.get(k))is type(v) and observer.get(k)==v for k,v in expected_observer.items())):
                observer_errors.append({"job_sha256":job_sha,"attempt_id":claim["attempt_id"],"errors":observer.get("errors",["Observer receipt differs from inspected subject"]) if isinstance(observer,dict) else ["Malformed observer receipt"]})
            result["attempts"].append({"job_sha256":job_sha,"attempt_id":claim["attempt_id"],"catalog_id":job["catalog_id"],
                "run_id":run["run_id"],"session":session,"native_config_sha256":config,"outcome":outcome,"result":"NormalCompletedUnqualified",
                "wins":int(outcome=="Victory" and evidence_scope=="ExpandedNormalUnqualified"),
                "actual_native_wins":int(outcome=="Victory"),"evidence_scope":evidence_scope,
                "sampling_coverage_credit":0,"input_audit":contact_audit,"qualified":False})
        read.stable()
        # Credit this one facet only after EVERY declared normal attempt and all stable reads succeed.
        result["admission"]["profile_receipts"]=profile_receipts
        if len(profile_receipts)==len(declared) and profile_receipts and not profile_errors:
            result["admission"]["missing"]=[v for v in result["admission"]["missing"] if v!="profile_before_restore"]
        else:
            result["admission"]["profile_errors"]=profile_errors
        result["admission"]["observer_receipts"]=observer_receipts
        if len(observer_receipts)==len(declared) and observer_receipts and not observer_errors:
            result["admission"]["missing"]=[v for v in result["admission"]["missing"] if v!="read_only_observers"]
        else:
            result["admission"]["observer_errors"]=observer_errors
        result["result"]="NormalCompletedUnqualified";result["wins"]=sum(a["wins"] for a in result["attempts"])
        result["proof_pins"]=[{"path":str(p),"sha256":h} for p,h in read.pins.items()]
    except (ValueError,OSError,KeyError,TypeError,IndexError,AttributeError) as error:
        result["errors"].append(str(error))
        if "events" in locals():
            result["failure_records"]=[r.get("facts",{}) for r in events if r.get("kind")=="BatchFailed"]
        # No partial/earlier successful attempt becomes batch coverage on a failed batch.
        result["wins"]=0
    return result

if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--project",type=Path,required=True);p.add_argument("--input",type=Path,required=True)
    p.add_argument("--compile-admission",action="store_true",help="Read schema3 immutable attempt receipts; base inspection remains unchanged")
    a=p.parse_args()
    try:
        spec=parse(_io_path(a.input).read_text(encoding="utf-8-sig"))
        if a.compile_admission:
            from skirmish_operational_compile_connector import inspect as inspect_compile
            answer=inspect_compile(a.project,spec)
        else: answer=inspect(a.project,spec)
    except (OSError,ValueError,KeyError,TypeError,IndexError,AttributeError,UnicodeError) as e:answer={"result":"Incomplete","errors":[str(e)],"wins":0,"queue_success_enabled":False,"qualified":False,"mission_ready":False,"publication_changed":False}
    complete=answer["result"]=="NormalCompletedUnqualified"
    compile_ok=not a.compile_admission or "compile" not in answer.get("admission",{}).get("missing",["compile"])
    print(json.dumps(answer,indent=2));raise SystemExit(0 if complete and compile_ok else 1)
