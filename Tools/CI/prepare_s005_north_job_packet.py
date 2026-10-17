"""Build a pinned S005 job from a native census and complete source/build capture.

This does not collect native evidence, launch Unity, invent hash padding, mark a
job qualified, or claim a mission win. Run only when the native owner permits a
source-byte preflight; every pinned source/DLL is checked against the filesystem.
"""
import argparse
import hashlib
import json
from pathlib import Path

import skirmish_production_queue as queue
from skirmish_native_identity import validate_setup_identity,normalize_native_locale,sha256,content_identity
from s005_windows_source_snapshot import candidate_hash,EXTENSIONS


def load(path):return json.loads(path.read_text(encoding="utf-8-sig"))
def file_hash(path):
    with path.open("rb") as stream:return hashlib.file_digest(stream,"sha256").hexdigest()


def prepare(project,census_file,snapshot_file,governing_packet,preparation_file,installed_observer,collector_source,census_log,pass_marker,job_id,output):
    if output.exists():raise ValueError("Preserve existing packet; choose a fresh output directory")
    census=load(census_file)
    keys={"schema_version","collector_kind","native_setup_identity","actual_native_locale","native_config_sha256"}
    if set(census)!=keys or type(census["schema_version"]) is not int or census["schema_version"]!=1 or census["collector_kind"]!="SkirmishNativeCompiledSetupCensus":
        raise ValueError("Expected exact shared native compiled-setup census envelope")
    identity=validate_setup_identity(census["native_setup_identity"])
    if normalize_native_locale(census["actual_native_locale"])!=identity["locale"] or not queue.sha(census["native_config_sha256"]):
        raise ValueError("Actual native census locale/config hash differs")
    expected=dict(catalog_id="S005",definition_id="skirmish.s005",seed=155926,size="Standard",difficulty="Regular",locale="FA")
    if any(identity[k]!=v or type(identity[k]) is not type(v) for k,v in expected.items()):
        raise ValueError("Native census differs from the exact reviewed north configuration")
    log=census_log.read_text(encoding="utf-8-sig",errors="replace")
    if not pass_marker or pass_marker not in log or "[UnityInvoke] ExitCode: 0" not in log or "result=Failed" in log:
        raise ValueError("Checked census wrapper/pass evidence absent or failed")
    snapshot=load(snapshot_file)
    manifest=snapshot["source_manifest"]
    if not manifest or candidate_hash(manifest)!=snapshot["candidate_sha256"]:
        raise ValueError("Complete Windows native candidate hash differs from capture")
    supplied={row["path"] for row in manifest}
    actual={p.relative_to(project).as_posix() for directory in ("Assets","Packages","ProjectSettings")
            for p in (project/directory).rglob("*") if p.is_file() and p.suffix.lower() in EXTENSIONS}
    if supplied!=actual or len(supplied)!=len(manifest):raise ValueError("Source capture is incomplete/duplicate/stale")
    builds=snapshot["build_manifest"]
    if not builds or sha256(builds)!=snapshot["build_sha256"]:raise ValueError("Compiled DLL manifest hash differs")
    actual_builds={p.relative_to(project).as_posix() for p in (project/"Library/ScriptAssemblies").glob("*.dll")}
    if {row["path"] for row in builds}!=actual_builds:raise ValueError("Compiled DLL file set differs")
    for row in builds:
        if file_hash(queue.relative_file(project,row["path"]))!=row["sha256"]:raise ValueError("Compiled DLL bytes changed")
    preparation=load(preparation_file)
    if preparation.get("nativeIdentityFieldsCaptured") is not True or str(preparation.get("runId"))!="1632":
        raise ValueError("Reviewed identity-aware north1632 observer required")
    draft=preparation_file.parent/preparation["candidate"]
    if file_hash(draft)!=preparation["candidateSha256"] or file_hash(installed_observer)!=preparation["candidateSha256"]:
        raise ValueError("Reviewed draft/installed observer bytes differ")
    for source in (installed_observer,collector_source):
        relative=source.relative_to(project).as_posix()
        if not any(row["path"]==relative and row["sha256"]==file_hash(source) for row in manifest):
            raise ValueError("Actual installed observer/collector omitted from source capture")
    relative_packet=governing_packet.relative_to(project).as_posix()
    job=dict(schema_version=1,job_id=job_id,**{k:identity[k] for k in queue_job_config_keys()},
        candidate_sha256=snapshot["candidate_sha256"],build_sha256=snapshot["build_sha256"],
        setup_sha256=sha256(identity),content_sha256=sha256(content_identity(identity)),source_manifest=manifest,
        packet_path=relative_packet,packet_sha256=file_hash(governing_packet),control_mode="manual",
        required_stages=sorted(queue.STAGES),timeout_seconds=2100)
    receipt=queue.preflight(job,project) # Rehashes the supplied complete source bytes; no success assertion.
    provenance=dict(result="PreparedPinnedNorthJob_NativeQualificationPending",native_qualification="pending",
        native_census_sha256=file_hash(census_file),native_census_log_sha256=file_hash(census_log),
        census_required_marker=pass_marker,native_config_sha256=census["native_config_sha256"],
        source_snapshot_sha256=file_hash(snapshot_file),observer_sha256=preparation["candidateSha256"],
        collector_source=collector_source.relative_to(project).as_posix(),collector_source_sha256=file_hash(collector_source),
        job_sha256=queue.fingerprint(job),preflight=receipt,gameplay_started=False,playerReady=False)
    output.mkdir(parents=True)
    for name,value in (("job.json",job),("native_setup_identity.json",identity),("native_content_identity.json",content_identity(identity)),("INPUT_PROVENANCE.json",provenance)):
        with (output/name).open("x",encoding="utf-8") as stream:json.dump(value,stream,indent=2);stream.write("\n")
    return provenance


def queue_job_config_keys():return ("catalog_id","definition_id","definition_version","difficulty","size","seed","locale")


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project",type=Path,default=Path(__file__).resolve().parents[2])
    for name in ("census","source-snapshot","governing-packet","preparation","installed-observer","collector-source","census-log","output"):
        parser.add_argument("--"+name,type=Path,required=True)
    parser.add_argument("--census-pass-marker",required=True)
    parser.add_argument("--job-id",required=True)
    args=parser.parse_args()
    result=prepare(args.project.resolve(),args.census.resolve(),args.source_snapshot.resolve(),args.governing_packet.resolve(),args.preparation.resolve(),
        args.installed_observer.resolve(),args.collector_source.resolve(),args.census_log.resolve(),args.census_pass_marker,args.job_id,args.output.resolve())
    print(json.dumps(result,indent=2))
