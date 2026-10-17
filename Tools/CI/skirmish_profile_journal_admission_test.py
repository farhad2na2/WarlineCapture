"""Pure profile journal controls. No actual files or profiles are read."""
import copy
from pathlib import Path
import sys
import unittest
HERE=Path(__file__).resolve().parent
ROOT=HERE
while not (ROOT/"Tools/CI/skirmish_production_queue.py").is_file():ROOT=ROOT.parent
sys.path[:0]=[str(HERE),str(ROOT/"Tools/CI")]
from skirmish_native_identity import sha256,content_identity
from skirmish_production_queue import fingerprint
from skirmish_profile_journal_admission import validate_profile_journal

class ProfileControls(unittest.TestCase):
    def setUp(self):
        identity=dict(schema_version=1,identity_kind="SkirmishNativeSetupIdentity",catalog_id="S005",
            definition_id="skirmish.s005",definition_version=1,size="Standard",difficulty="Regular",seed=155926,
            locale="EN",native_setup_hash_uint32=123,native_content_identity="synthetic-content")
        self.job={k:identity[k] for k in ("catalog_id","definition_id","definition_version","size","difficulty","seed","locale")}
        self.job.update(candidate_sha256="c"*64,build_sha256="d"*64,setup_sha256=sha256(identity),
                        content_sha256=sha256(content_identity(identity)))
        self.claim=dict(job_sha256=fingerprint(self.job),attempt_id="attempt-synthetic",worker="worker",
                        artifact_directory="D:/fixture/Logs/Q/job/attempt-synthetic")
        def snap(root,exists):
            rows=[]
            for name in ("profile.json","settings.json","quickgame.json","skirmish-s005-native.json"):
                for suffix in ("",".tmp",".bak",".lock"):
                    present=exists and name=="profile.json" and suffix==""
                    rows.append(dict(name=name+suffix,exists=present,length=10 if present else 0,
                                     sha256="a"*64 if present else None))
            return dict(root=root,directory_exists=exists,directory_entries=["profile.json"] if exists else [],
                        enumeration_limit=32,files=rows,observed_bytes=10 if exists else 0)
        original="C:/user/persistent"
        owned=self.claim["artifact_directory"]+"/save-profile"
        begin=dict(journal_version=1,catalog_id="S005",original_environment=None,actual_environment=None,
                   actual_effective_root=original,original=snap(original,True),owned=snap(owned,False))
        pre=copy.deepcopy(begin);pre.update(actual_environment=owned,actual_effective_root=owned,owned=snap(owned,True))
        after=copy.deepcopy(pre);after.update(actual_environment=None,actual_effective_root=original)
        data=[("ContextCaptured",dict(source_sha256="c"*64,compiled_dll_manifest_sha256="d"*64)),
              ("ProfileBeforeDispatch",begin),
              ("NormalMenuLaunchObserved",dict(native_setup_identity=identity,native_session_id="native-session",
                 native_world_sequence="0",native_config_sha256="e"*64,actual_save_root=owned)),
              ("ProfileBeforeRestore",pre),("RestoreStarting",{}),("ProfileAfterRestore",after),
              ("ContextRestored",dict(actual_save_root=None))]
        self.events=[dict(schema_version=1,sequence=i,job_sha256=self.claim["job_sha256"],
                    attempt_id=self.claim["attempt_id"],worker=self.claim["worker"],kind=k,facts=v)
                    for i,(k,v) in enumerate(data,1)]
    def facts(self,kind):return next(e["facts"] for e in self.events if e["kind"]==kind)
    def check(self,passed=False):
        result=validate_profile_journal(self.job,self.claim,self.events)
        self.assertEqual(passed,result["profile_restored"],result)
        self.assertFalse(result["qualified"]);self.assertFalse(result["queue_success_enabled"])
        return result
    def test_measured_profile_preserved_actual_zero_world(self):self.check(True)
    def test_original_bytes_changed_even_with_same_root(self):
        self.facts("ProfileAfterRestore")["original"]["files"][0]["sha256"]="f"*64;self.check()
    def test_original_missing_file_created(self):
        row=self.facts("ProfileAfterRestore")["original"]["files"][1]
        row.update(exists=True,length=0,sha256="f"*64);self.check()
    def test_boolean_event_schema_refused(self):
        self.events[3]["schema_version"]=True;self.check()
    def test_unknown_profile_entry_refused(self):
        self.facts("ProfileAfterRestore")["original"]["directory_entries"].append("unknown.json");self.check()
    def test_omitted_directory_enumeration_refused(self):
        self.facts("ProfileAfterRestore")["owned"].pop("directory_entries");self.check()
    def test_actual_launch_root_splice(self):
        self.facts("NormalMenuLaunchObserved")["actual_save_root"]="C:/foreign";self.check()
    def test_context_restore_root_missing(self):
        self.facts("ContextRestored").pop("actual_save_root");self.check()
    def test_actual_root_not_restored(self):
        self.facts("ProfileAfterRestore")["actual_effective_root"]="C:/other";self.check()
    def test_isolated_lease_preexisted(self):
        self.facts("ProfileBeforeDispatch")["owned"]["directory_exists"]=True;self.check()
    def test_isolated_bytes_changed_during_restore(self):
        self.facts("ProfileAfterRestore")["owned"]["files"][0]["sha256"]="f"*64;self.check()
    def test_missing_pre_restore_journal(self):
        self.events=[e for e in self.events if e["kind"]!="ProfileBeforeRestore"]
        for i,e in enumerate(self.events,1):e["sequence"]=i
        self.check()
    def test_partial_native_failure_retained(self):
        self.events[3]["kind"]="ProfileJournalFailure";self.check()
    def test_foreign_attempt_refused(self):
        self.events[3]["attempt_id"]="foreign";self.check()
    def test_native_build_splice_refused(self):
        self.facts("ContextCaptured")["compiled_dll_manifest_sha256"]="0"*64;self.check()
    def test_duplicate_native_session_observation_refused(self):
        self.events[3]["kind"]="NormalMenuLaunchObserved";self.check()
    def test_caller_restored_truth_flag_refused(self):
        self.facts("ProfileAfterRestore")["profile_restored"]=True;self.check()
    def test_missing_file_cannot_claim_hash(self):
        self.facts("ProfileAfterRestore")["owned"]["files"][1]["sha256"]="f"*64;self.check()
    def test_file_omission_refused(self):
        self.facts("ProfileAfterRestore")["original"]["files"].pop();self.check()

    def set_original_root_state(self,exists,stages=("ProfileBeforeDispatch","ProfileBeforeRestore","ProfileAfterRestore")):
        for stage in stages:
            value=self.facts(stage)["original"];value["directory_exists"]=exists
            value["directory_entries"]=[];value["observed_bytes"]=0
            for row in value["files"]:row.update(exists=False,length=0,sha256=None)
    def test_absent_original_root_preserved(self):
        self.set_original_root_state(False);self.check(True)
    def test_empty_original_directory_preserved(self):
        self.set_original_root_state(True);self.check(True)
    def test_absent_original_root_created_empty_refused(self):
        self.set_original_root_state(False)
        self.set_original_root_state(True,("ProfileAfterRestore",));self.check()
    def test_empty_original_root_removed_refused(self):
        self.set_original_root_state(True)
        self.set_original_root_state(False,("ProfileAfterRestore",));self.check()


    def original_environment(self,value):
        for stage in ("ProfileBeforeDispatch","ProfileBeforeRestore","ProfileAfterRestore"):
            self.facts(stage)["original_environment"]=value
        self.facts("ProfileBeforeDispatch")["actual_environment"]=value
        self.facts("ProfileAfterRestore")["actual_environment"]=value
        self.facts("ContextRestored")["actual_save_root"]=value
    def test_absolute_original_environment_bound_to_effective_root(self):
        self.original_environment("C:/user/persistent");self.check(True)
    def test_original_environment_cannot_name_another_profile(self):
        self.original_environment("C:/another-profile");self.check()
    def test_drive_relative_original_root_refused(self):
        for stage in ("ProfileBeforeDispatch","ProfileBeforeRestore","ProfileAfterRestore"):
            self.facts(stage)["original"]["root"]="\\persistent"
        self.facts("ProfileBeforeDispatch")["actual_effective_root"]="\\persistent"
        self.facts("ProfileAfterRestore")["actual_effective_root"]="\\persistent"
        self.check()

if __name__=="__main__":unittest.main(verbosity=2)
