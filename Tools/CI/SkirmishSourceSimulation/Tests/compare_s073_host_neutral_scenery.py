"""Compare the host's policy-filtered neutral scenery with the native census."""
import argparse
import hashlib
import json
from pathlib import Path


def load(path):
    data=Path(path).read_bytes()
    return json.loads(data),hashlib.sha256(data).hexdigest()


def compare(native, native_receipt, identity, host, host_receipt, scenery):
    n,nh=native; h,hh=host; s,sh=scenery
    if (native_receipt.get("validation_status")!="Passed" or native_receipt.get("wrapper_exit_code")!=0
            or native_receipt.get("required_marker_present") is not True or native_receipt.get("census_sha256")!=nh
            or identity.get("status")!="IdentitySetsMatch" or identity.get("native_sha256")!=nh):
        raise ValueError("Checked same-session native source identity required")
    desc=h.get("experimental_physical_blocker_projection",{}).get("neutral_scenery_readback",{})
    if (host_receipt.get("worker_result",{}).get("sha256")!=hh or host_receipt.get("process_exit")!=2
            or h.get("status")!="PreparingPhysicalSourceRequired" or h.get("mission_ready") is not False
            or desc.get("sha256")!=sh or desc.get("count")!=4931
            or s.get("kind")!="S073SourceNeutralSceneryReadback" or s.get("source_capture_sha256")!=identity.get("source_sha256")
            or s.get("campaign_runtime_state_copied") is not False or s.get("complete_gameplay_payload") is not False
            or s.get("mission_ready") is not False or s.get("normalization_owner")!="SkirmishWorldSetup.NormalizeScenery"):
        raise ValueError("Exact policy-filtered source-host scenery lineage required")
    key=lambda r:(r["stable_id"],r["source_global_object_id"],r["placement_index"])
    left=s["entities"];right=n["post_stream"]["entities"]
    a={key(row):row for row in left};b={key(row):row for row in right}
    if len(a)!=len(left) or len(b)!=len(right) or len(a)!=4931 or set(a)!=set(b):
        raise ValueError("Unique matching source building identities required")
    fields=("faction","ai","combat_owner","health_current")
    differences={field:sum(a[k][field]!=b[k][field] for k in a) for field in fields}
    return {"kind":"S073HostNeutralSceneryComparison","status":"ObservedFieldsMatch" if not any(differences.values()) else "ObservedFieldsDiffer",
            "native_sha256":nh,"host_result_sha256":hh,"scenery_sha256":sh,"building_count":len(a),"field_difference_counts":differences,
            "scope":"Four observed fields in policy-filtered host and separate native session; storage, production, presentation and full payload not compared",
            "full_payload_parity":False,"source_fit_certified":False,"scheduler_admitted":False,"mission_ready":False}


def main():
    p=argparse.ArgumentParser()
    for k in ("native","native-receipt","identity","host","host-receipt","scenery","output"):p.add_argument("--"+k,required=True)
    a=p.parse_args();n=load(a.native);nr,_=load(a.native_receipt);i,_=load(a.identity);h=load(a.host);hr,_=load(a.host_receipt);s=load(a.scenery)
    result=compare(n,nr,i,h,hr,s)
    with Path(a.output).open("x") as f:json.dump(result,f,indent=2);f.write("\n")
    print(json.dumps(result));return 0 if result["status"]=="ObservedFieldsMatch" else 2


if __name__=="__main__":raise SystemExit(main())
