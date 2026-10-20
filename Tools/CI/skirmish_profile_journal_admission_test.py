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
import skirmish_profile_journal_admission as profile_admission
assert Path(profile_admission.__file__).resolve()==(HERE/"skirmish_profile_journal_admission.py").resolve(), "Adjacent production/proposed consumer required"
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

class ScopedControls(unittest.TestCase):
 def setUp(self):
  self.old=ProfileControls();self.old.setUp()
  for stage in ['ProfileBeforeDispatch','ProfileBeforeRestore','ProfileAfterRestore']:
   value=self.old.facts(stage);value['journal_version']=2
   for scope in ['original','owned']:
    snap=value[scope]
    snap['namespace_scope']='OriginalCurrentCatalogSaveFiles_KnownEngineSiblingsExcluded' if scope=='original' else 'OwnedCurrentCatalogSaveFiles_Exclusive'
    snap['excluded_non_save_entries']=[{'name':'TestResults.xml','kind':'File'},{'name':'Unity','kind':'Directory'}] if scope=='original' else []
    snap['total_directory_entries']=sorted(snap['directory_entries']+[v['name'] for v in snap['excluded_non_save_entries']])
 def check(self,passed=False):
  actual=profile_admission.validate_profile_journal(self.old.job,self.old.claim,self.old.events)
  self.assertEqual(passed,actual['profile_restored'],actual);self.assertFalse(actual['qualified']);self.assertFalse(actual['queue_success_enabled'])
 def snap(self,stage='ProfileAfterRestore',scope='original'):return self.old.facts(stage)[scope]
 def test_original_engine_siblings_all_three_stages(self):self.check(True)
 def test_engine_only_presence_changes_not_save_bytes(self):
  value=self.snap();value['excluded_non_save_entries']=[];value['total_directory_entries']=value['directory_entries'][:];self.check(True)
 def test_owned_engine_entries_refused(self):
  value=self.snap(scope='owned');value['excluded_non_save_entries']=[{'name':'Unity','kind':'Directory'}];value['total_directory_entries']=sorted(value['directory_entries']+['Unity']);self.check()
 def test_wrong_type_refused(self):self.snap()['excluded_non_save_entries'][0]['kind']='Directory';self.check()
 def test_unity_file_type_refused(self):self.snap()['excluded_non_save_entries'][1]['kind']='File';self.check()
 def test_other_unknown_save_refused(self):self.snap()['directory_entries'].append('unknown.json');self.snap()['total_directory_entries'].append('unknown.json');self.check()
 def test_unknown_excluded_namespace_refused(self):
  value=self.snap();value['excluded_non_save_entries'].append({'name':'unknown.json','kind':'File'});value['total_directory_entries'].append('unknown.json');self.check()
 def test_reparse_metadata_cannot_be_waived(self):self.snap()['excluded_non_save_entries'][0]['reparse']=True;self.check()
 def test_save_hash_still_preserved(self):self.snap()['files'][0]['sha256']='f'*64;self.check()
 def test_mixed_version_refused(self):self.old.facts('ProfileBeforeRestore')['journal_version']=1;self.check()
 def test_namespace_ownership_refused(self):self.snap()['namespace_scope']='OwnedCurrentCatalogSaveFiles_Exclusive';self.check()
 def test_duplicate_exclusion_refused(self):
  self.snap()['excluded_non_save_entries'].append({'name':'Unity','kind':'Directory'});self.check()
 def test_null_exclusion_direct_snapshot_refused(self):
  value=copy.deepcopy(self.snap())
  value['directory_entries']=[];value['observed_bytes']=0
  for row in value['files']:row.update(exists=False,length=0,sha256=None)
  value['excluded_non_save_entries']=[{'name':None,'kind':None}];value['total_directory_entries']=[None]
  with self.assertRaises(ValueError):profile_admission.snapshot(value,'S005','original')
 def test_failed_attempt_never_rehabilitated(self):self.old.events[3]['kind']='ProfileJournalFailure';self.check()

class V3(unittest.TestCase):
 def setUp(self):
  self.fixture=ScopedControls();self.fixture.setUp()
  for kind in ['ProfileBeforeDispatch','ProfileBeforeRestore','ProfileAfterRestore']:
   stage=self.fixture.old.facts(kind);stage['journal_version']=3
   stage['original']['namespace_scope']='OriginalCanonicalCatalogSaveFiles_KnownEngineSiblingsExcluded'
 def snap(self,stage='ProfileAfterRestore',scope='original'):return self.fixture.old.facts(stage)[scope]
 def check(self,passed=False):
  result=profile_admission.validate_profile_journal(self.fixture.old.job,self.fixture.old.claim,self.fixture.old.events)
  self.assertEqual(passed,result['profile_restored'],result)
  self.assertFalse(result['qualified']);self.assertFalse(result['queue_success_enabled'])
 def add(self,stage='ProfileAfterRestore',scope='original',id='s002',suffix='',hash='b'*64):
  snap=self.snap(stage,scope);stem='skirmish-'+id+'-native.json'
  snap['files'] += [dict(name=stem+s,exists=s==suffix,length=4 if s==suffix else 0,sha256=hash if s==suffix else None) for s in ('','.tmp','.bak','.lock')]
  snap['directory_entries']=sorted(snap['directory_entries']+[stem+suffix]);snap['total_directory_entries']=sorted(snap['total_directory_entries']+[stem+suffix]);snap['observed_bytes']+=4
 def all_foreign(self,id='s002',suffix=''):
  for stage in ['ProfileBeforeDispatch','ProfileBeforeRestore','ProfileAfterRestore']:self.add(stage,id=id,suffix=suffix)
 def test_v3_no_foreign(self):self.check(True)
 def test_all_three_foreign_preserved(self):self.all_foreign();self.check(True)
 def test_foreign_tmp_only_measures_sibling_absence(self):self.all_foreign(suffix='.tmp');self.check(True)
 def test_same_size_foreign_mutation(self):
  self.all_foreign();self.snap()['files'][-4]['sha256']='c'*64;self.check()
 def test_foreign_added(self):self.add();self.check()
 def test_foreign_removed(self):
  self.all_foreign();self.snap()['files']=self.snap()['files'][:-4];self.check()
 def test_missing_absence_group(self):
  self.all_foreign();self.snap()['files'].pop();self.check()
 def test_foreign_in_owned_is_still_refused(self):
  self.add(scope='owned');self.check()
 def test_noncanonical_ids_case_and_suffix(self):
  for name in ['skirmish-s000-native.json','skirmish-s121-native.json','skirmish-S002-native.json','skirmish-s002-native.json.extra','other.json']:
   with self.subTest(name=name):
    self.setUp()
    for stage in ['ProfileBeforeDispatch','ProfileBeforeRestore','ProfileAfterRestore']:
     self.add(stage);snap=self.snap(stage);snap['files'][-4]['name']=name
     snap['directory_entries']=sorted(['profile.json',name]);snap['total_directory_entries']=sorted(['TestResults.xml','Unity','profile.json',name])
    self.check()
 def test_all120_catalog_ids_are_finite_save_grammar(self):
  for i in range(1,121):
   if i==5:continue
   with self.subTest(id=i):
    self.setUp();self.all_foreign('s'+str(i).zfill(3));self.check(True)
 def test_forged_caller_catalog_allowlist_refused(self):
  self.all_foreign();self.snap()['allowed_catalogs']=['S999'];self.check()
 def test_cross_namespace_scope_on_owned_refused(self):
  self.snap(scope='owned')['namespace_scope']='OriginalCanonicalCatalogSaveFiles_KnownEngineSiblingsExcluded';self.check()
 def test_original_scope_version_mismatch_refused(self):
  self.fixture.old.facts('ProfileAfterRestore')['journal_version']=2;self.check()
 def test_foreign_zero_hash_on_absence_refused(self):
  self.all_foreign();self.snap()['files'][-1]['sha256']='a'*64;self.check()
 def test_complete_entry_census_still_required(self):
  self.all_foreign();self.snap()['total_directory_entries'].remove('skirmish-s002-native.json');self.check()
 def test_byte_and_entry_quotas_unchanged(self):
  self.all_foreign();self.snap()['files'][-4]['length']=8388609;self.snap()['observed_bytes']=8388619;self.check()
 def test_root_existence_still_matters(self):
  self.all_foreign();self.snap()['directory_exists']=False;self.check()
 def test_actual_stage_catalog_cannot_be_changed(self):
  self.all_foreign();self.fixture.old.facts('ProfileAfterRestore')['catalog_id']='S002';self.check()

 def test_entry_limit_still32(self):
  self.all_foreign()
  self.snap()['directory_entries']=['skirmish-s'+str(i).zfill(3)+'-native.json' for i in range(1,34)]
  self.check()

if __name__=="__main__":unittest.main(verbosity=2)
