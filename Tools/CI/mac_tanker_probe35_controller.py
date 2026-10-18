"""DISABLED by default: outer controller for the exact Mac synthetic35 projection.

Default invocation prints a plan and launches nothing. --execute additionally
requires a separately reviewed, hash-bound authorization JSON. Only the checked
GUI wrapper can launch Unity. No C# changes, cache copy, Editor reuse, process
termination, global ENV mutation or normal mission/device qualification.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import plistlib
import re
import shlex
import shutil
import subprocess
import sys
import time
import uuid
from skirmish_construction_ledger_admission import need, exact, sha, parse
from s005_windows_source_snapshot import snapshot, candidate_hash, build_snapshot, _file_row

ROOT='/Volumes/T7/Projects/WarlineCapture-mac-native-s003'
COMMIT='a9eb0b4814ebb9c6c0d81ebb851df0917b970973'
PARENT='9f75b2f224cde04ea87f25b8c9fc097765889bcf'
BRANCH='codex/mac-native-tanker-probe35'
PINS={'Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.AircraftCycleProbe.cs': '4d89e081aa4ec84fc3a0fd7d8f84d5433200e4fb6d07936c3fcb3b9eb9020f88', 'Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.HelipadProbe.cs': '9ecd272926f905afcfd92dfaf4d4115fb0c90cf08042d1887d89eae29fda5175', 'Assets/Tests/Editor/SkirmishExpansion/SkirmishAircraftProbeTankerOwnershipTests.cs': 'b3fd454e7ec8192a6a7b4039bc3a389d777a4cc44014277e22dfe3a55489ea94', 'Assets/Tests/Editor/SkirmishExpansion/SkirmishAircraftProbeTankerOwnershipTests.cs.meta': 'f93bc63f9311f746cb0b96f90016541c92b490af17da1b6752a22a188ca0bec7'}
WRAPPER='Tools/CI/invoke_unity_macos.sh'
WRAPPER_SHA='53983409143d2a93b0b4b5369649a175323c5b08ec08a60571fdb5cd5127c608'
ENTRY='Game.Tests.Editor.SkirmishAircraftProbeTankerOwnershipTests.RunFocusedValidation'
MARKER='[SkirmishAircraftProbeTankerOwnership] result=Passed fixture=SyntheticWorldAndOwnedEditorDispatch normalMissionReady=0'
NEGATIVES=['[SkirmishS003AircraftCycleProbe] result=Failed reason=tankerOwnerCaptureRejected',
           '[SkirmishS003AircraftCycleProbe] result=Failed reason=tankerOwnerContextChanged']
DEPENDENCY_PINS={'skirmish_construction_ledger_admission.py': 'fb0306e02c2342d5606a0fa3cb25d8ed9fd5611fb6774049e925599e3d528a36', 's005_windows_source_snapshot.py': '60a6d7deab7da48ed5a61687bdc750002400160eccf3e62c8effd906e4622bba', 'skirmish_native_identity.py': 'e85cb0558fa845af215860031dbcd79ac7f2a019a2a238101f25a171802003c5', 'skirmish_production_queue.py': '95a53a42392769e20b4b7ab8ba94179b64fef0a28fe1689b34daf42ef94759fd'}
TIMEOUT=3600  # Cold import + Android compile; timeout still fails closed.


def run(argv,timeout=30,cwd=None):
    p=subprocess.run(argv,cwd=cwd,capture_output=True,text=True,timeout=timeout,
                     env=dict(os.environ,LC_ALL='C'))
    need(p.returncode==0,'Read-only command failed: '+repr(argv)+' '+p.stderr[-1000:])
    return p.stdout


def digest(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        while True:
            b=f.read(65536)
            if not b:break
            h.update(b)
    return h.hexdigest()


def checked_file(path,alias_root=None):
    p=Path(path);need(p.is_file(),'Required file missing: '+str(p))
    if p.is_symlink():need(alias_root is not None and Path(alias_root).resolve() in p.resolve().parents,'Unapproved file alias')
    return {'path':str(p),'resolved_path':str(p.resolve()),'sha256':digest(p),'size':p.stat().st_size}



def authorization(v,controller_sha):
    exact(v,{'schema_version','launch_authorized','project','commit','controller_sha256','review_receipt_sha256','scope','timeout_seconds'})
    need(type(v['schema_version'])is int and v['schema_version']==1 and v['launch_authorized'] is True,'Launch remains disabled')
    need(v['project']==ROOT and v['commit']==COMMIT and v['controller_sha256']==controller_sha,'Authorization projection/controller mismatch')
    sha(v['review_receipt_sha256'])
    need(v['scope']=='Synthetic35Only_NoMissionCredit' and type(v['timeout_seconds'])is int
         and 1800<=v['timeout_seconds']<=7200,'Scoped cold-import timeout authorization required')


def git_preflight(root):
    def git(*args):return run(['git',*args],cwd=root).strip()
    need(git('rev-parse','HEAD')==COMMIT and git('rev-parse','HEAD^')==PARENT,'Exact local projection/parent changed')
    need(git('branch','--show-current')==BRANCH and not git('status','--porcelain','--untracked-files=normal'),'Projection branch dirty/changed')
    need(set(git('diff-tree','--no-commit-id','--name-only','-r','HEAD').splitlines())==set(PINS),'Projection four-path scope changed')
    for p,h in PINS.items():need(digest(root/p)==h,'Frozen proposal pin changed: '+p)
    need(digest(root/WRAPPER)==WRAPPER_SHA,'Checked GUI wrapper changed')
    version=(root/'ProjectSettings/ProjectVersion.txt').read_text()
    need(re.search(r'^m_EditorVersion: 6000[.]5[.]8f1$',version,re.M),'Editor version changed')
    return {'commit':COMMIT,'parent':PARENT,'branch':BRANCH,'pins':PINS,'wrapper_sha256':WRAPPER_SHA}


def redact_command(command):
    # Worker command lines can contain ephemeral licensing credentials. They are
    # irrelevant to process/project ownership and must never enter evidence logs.
    return re.sub(r'(?i)(-{1,2}(?:cbmEditorLicense|cbmMachineId|accessToken|refreshToken|password|serial|token)(?:=|\s+))[^\s]+',r'\1<redacted>',command)


def processes():
    rows=[]
    for line in run(['ps','-axo','pid=,ppid=,lstart=,%cpu=,rss=,command=']).splitlines():
        fields=line.strip().split(None,9)
        if len(fields)!=10:continue
        pid,ppid,*_=fields;command=fields[9]
        if 'Unity' not in command and WRAPPER not in command and 'bee_backend' not in command:continue
        rows.append(dict(pid=int(pid),ppid=int(ppid),started=' '.join(fields[2:7]),cpu=float(fields[7]),rss_kib=int(fields[8]),command=redact_command(command)))
    return rows


def editor(row):return '/Unity.app/Contents/MacOS/Unity' in row['command']


def project_of(row):
    argv=shlex.split(row['command'])
    return str(Path(argv[argv.index('-projectPath')+1]).resolve()) if '-projectPath' in argv else None


def capacity(root):
    samples=[];log_sizes={}
    for n in range(3):
        rows=processes();samples.append(rows)
        need(any('/Unity Hub.app/Contents/MacOS/Unity Hub' in r['command'] for r in rows),'Hub must remain open')
        editor_ids={r['pid'] for r in rows if editor(r)}
        native_descendants=set(editor_ids)
        for _ in rows:
            added={r['pid'] for r in rows if r['ppid'] in native_descendants}
            if added<=native_descendants:break
            native_descendants|=added
        need(sum(r['cpu'] for r in rows if r['pid'] in native_descendants)<10,'Editor/worker CPU workload active; preserve it')
        for r in rows:
            if not editor(r):continue
            need(project_of(r)!=str(root.resolve()),'Existing Editor owns test project')
            need(r['cpu']<10,'Other Editor has active CPU workload; preserve it')
            argv=shlex.split(r['command'])
            if '-logFile' in argv:
                p=Path(argv[argv.index('-logFile')+1])
                if p.is_file():
                    size=p.stat().st_size
                    need(str(p) not in log_sizes or log_sizes[str(p)]==size,'Other Editor log changing; capacity not established')
                    log_sizes[str(p)]=size
        if n<2:time.sleep(2)
    free=shutil.disk_usage(root).free;need(free>=25*1024**3,'Insufficient 25GiB cold-import disk headroom')
    vm=run(['vm_stat']);pressure=run(['memory_pressure','-Q'])
    m=re.search(r'System-wide memory free percentage: (\d+)%',pressure)
    need(m and int(m.group(1))>=15,'Memory capacity not established')
    return dict(samples=samples,free_bytes=free,vm_stat=vm,memory_pressure=pressure)


def tools_state(root):
    auth=parse(run(['unity','auth','status','--format','json']))
    need(auth.get('loggedIn') is True,'Unity CLI auth not confirmed signed in')
    base=Path('/Applications/Unity/Hub/Editor/6000.5.8f1')
    android=base/'Unity.app/Contents/PlaybackEngines/AndroidPlayer'
    required=[base/'Unity.app/Contents/Info.plist',Path('/Applications/Unity Hub.app/Contents/Info.plist'),
        android/'NDK/source.properties',android/'NDK/toolchains/llvm/prebuilt/darwin-x86_64/bin/clang',
        android/'OpenJDK/bin/java',android/'OpenJDK/bin/javac',android/'SDK/platform-tools/adb']
    for pattern in ('platforms/*/android.jar','build-tools/*/aapt2','build-tools/*/apksigner','cmdline-tools/*/bin/sdkmanager'):
        found=sorted((android/'SDK').glob(pattern));need(found,'Missing actual Android SDK component '+pattern);required.extend(found)
    required.extend(root/p for p in ('Packages/manifest.json','Packages/packages-lock.json'))
    manifest=parse((root/'Packages/manifest.json').read_text());need('com.unity.pipeline' in manifest['dependencies'],'Pipeline package absent')
    return dict(auth_logged_in=True,versions={str(p):plistlib.loads(p.read_bytes()).get('CFBundleShortVersionString') for p in required if p.suffix=='.plist'},
                actual_tool_pins=[checked_file(p,android if android in p.parents else None) for p in required],android_build_performed=False)


def tree_rows(root,folder):
    p=root/folder
    return sorted((_file_row(root,f) for f in p.rglob('*') if f.is_file() and not f.is_symlink()),key=lambda r:r['path']) if p.is_dir() else []


def state_snapshot(root):
    for folder in ('Assets','Packages','ProjectSettings'):
        need(not any(p.is_symlink() for p in (root/folder).rglob('*')),'Source symlink refused')
    source=snapshot(root)
    # Source assets must be actual LFS bytes; unresolved pointers cannot compile the intended candidate.
    for row in source:
        p=root/row['path']
        if p.stat().st_size<=1024:
            need(not p.read_bytes().startswith(b'version https://git-lfs.github.com/spec/v1'),'Unresolved LFS source '+row['path'])
    try:build,buildsha=build_snapshot(root)
    except ValueError:build,buildsha=[],None
    rsp=[]
    bee=root/'Library/Bee'
    if bee.is_dir():
        rsp=[checked_file(p) for p in sorted(bee.rglob('*.rsp')) if p.is_file() and not p.is_symlink()]
    package_pins=[]
    cache=root/'Library/PackageCache'
    if cache.is_dir():
        package_pins=[checked_file(p) for p in sorted(cache.glob('*/package.json'))]
    return dict(source_manifest=source,candidate_sha256=candidate_hash(source),build_manifest=build,build_sha256=buildsha,
                project_settings=tree_rows(root,'ProjectSettings'),user_settings=tree_rows(root,'UserSettings'),
                response_file_pins=rsp,compiler_bee_dll_pins=[checked_file(p) for p in sorted(bee.rglob('*.dll')) if p.is_file() and not p.is_symlink()] if bee.is_dir() else [],package_cache_manifest_pins=package_pins)


def command(root,log,timeout):
    return ['rtk','proxy',str(root/WRAPPER),'--project',str(root),'--timeout',str(timeout),'--log',str(log),
            '--','-buildTarget','Android','-quit','-executeMethod',ENTRY]


def log_checks(text,returncode):
    need(returncode==0,'Wrapper nonzero/timeout')
    lines=text.splitlines();need(lines.count(MARKER)==1,'Exact unique final35 marker missing')
    for negative in NEGATIVES:need(lines.count(negative)==1,'Expected strict negative fixture count differs')
    for line in lines:
        if 'result=Failed' in line:need(line in NEGATIVES,'Unexpected failure record')
        need(not re.search(r'error CS\d+|Unhandled Exception|AssertionException|Unexpected log|Test failed|executeMethod.*threw|Aborting.*failure|Compilation failed|Failed to import|^.*Exception:|^Error:|\[Error\]',line,re.I),'Unexpected compile/test error')
    need(lines.index(MARKER)>max(lines.index(n) for n in NEGATIVES),'Final marker precedes strict fixture evaluation')


def assess(before,after,text,exitcode,remaining,parent_unchanged,hub_live):
    errors=[]
    try:log_checks(text,exitcode)
    except ValueError as e:errors.append(str(e))
    for ok,why in [(before['source_manifest']==after['source_manifest'],'Source changed'),
                   (before['project_settings']==after['project_settings'],'Project settings changed'),
                   (before['user_settings']==after['user_settings'],'User settings changed'),
                   (bool(after['build_manifest']),'Actual compiled DLLs missing'),
                   (not remaining,'Owned Editor shutdown incomplete'),(parent_unchanged,'Parent environment changed'),(hub_live,'Hub closed')]:
        if not ok:errors.append(why)
    android_rsp=[]
    for ref in after['response_file_pins']:
        p=Path(ref['path'])
        if p.is_file() and digest(p)==ref['sha256'] and re.search(r'(?:^|[;:\s])UNITY_ANDROID(?:[;\s]|$)',p.read_text(errors='replace')):android_rsp.append(ref)
    return dict(operationalControlPassed=not errors,errors=errors,settings_restored=before['project_settings']==after['project_settings'] and before['user_settings']==after['user_settings'],
        proofCompleteness=dict(actual_build_manifest=bool(after['build_manifest']),actual_android_define_response_files=android_rsp,
            active_target='InvocationAndroid_ObservedRSPOnly' if android_rsp else 'Pending_NoNativeTargetWitness',
            native_compiler_input_consumption_closure='Pending',native_in_process_environment_witness='Pending',callback_world_input_restoration='FixtureAssertionsOnly_NoIndependentPostprocessWitness',
            actual_native_domain_reload_player_frame='Pending',normal_UPM='RecordedActualPackageLockAndCache_NotIndependentNativeWitness'),
        readiness=dict(normal_mission=False,paid_resources_qualified=False,queue_success_enabled=False,Android_build=False,player_device=False))


def execute(auth_path):
    root=Path(ROOT);auth=parse(Path(auth_path).read_text());authorization(auth,digest(__file__))
    need(sys.platform=='darwin','Mac GUI controller only')
    for name,h in DEPENDENCY_PINS.items():need(digest(Path(__file__).parent/name)==h,'Controller helper dependency changed: '+name)
    parent=dict(os.environ)
    folder=root/'Logs/SkirmishProduction/MacProbe35'/('run-'+uuid.uuid4().hex);folder.mkdir(parents=True,exist_ok=False)
    result=dict(schema_version=1,launch_authorized=True,authorization=auth,
                operationalControlPassed=False,launch_performed=False)
    path=folder/'controller-result.json';child=None;before=None;owned={}
    try:
        result['preflight']=git_preflight(root)
        result['capacity']=capacity(root);result['tooling']=tools_state(root)
        before=state_snapshot(root);result['before']=before
        result['fresh_capacity']=capacity(root);git_preflight(root)
        beforep=processes();result['processes_before']=beforep
        log=folder/'unity.log';need(not log.exists(),'Fresh log required')
        argv=command(root,log,auth['timeout_seconds']);result['argv']=argv
        env=dict(parent)
        for k in list(env):
            if k.startswith('WARLINE_'):del env[k]
        env['WARLINE_REPAIR_RESOURCE_LEDGER']='0'
        # Parent environment is never mutated; fixture temporarily changes only its process ENV.
        result['child_warline_environment']={k:v for k,v in env.items() if k.startswith('WARLINE_')}
        with (folder/'wrapper.log').open('x') as output:
            child=subprocess.Popen(argv,cwd=root,env=env,stdout=output,stderr=subprocess.STDOUT)
            result['launch_performed']=True;result['wrapper_pid']=child.pid
            while child.poll() is None:
                rows=processes()
                parents={child.pid}|set(owned)
                for _ in rows:
                    added={r['pid'] for r in rows if r['ppid'] in parents}
                    if added<=parents:break
                    parents|=added
                for r in rows:
                    if editor(r) and project_of(r)==str(root.resolve()) and r['pid'] not in {p['pid'] for p in beforep}:
                        need(r['ppid'] in parents,'Test project process is not wrapper-owned')
                        owned[r['pid']]=r
                time.sleep(1)
            result['wrapper_exit']=child.returncode
        result['observed_owned_processes']=list(owned.values());need(bool(owned),'No owned Editor lifecycle observed')
        after=state_snapshot(root);result['after']=after
        finalp=processes();result['processes_after']=finalp
        remaining=[r for r in finalp if editor(r) and project_of(r)==str(root.resolve())]
        text=log.read_text(errors='replace') if log.exists() else ''
        result.update(assess(before,after,text,child.returncode,remaining,dict(os.environ)==parent,
            any('/Unity Hub.app/Contents/MacOS/Unity Hub' in r['command'] for r in finalp)))
        result['logs']=[checked_file(p) for p in (log,folder/'wrapper.log') if p.is_file()]
    except BaseException as error:
        result['controller_error']=type(error).__name__+': '+str(error)
        result['owned_shutdown']='Pending' if child is not None and child.poll() is None else 'WrapperExited_NoControllerTermination'
        result['operationalControlPassed']=False
        # Never terminate a borrowed/unknown Editor, Hub, licensing client or even a still-owned wrapper here.
        # The mandatory wrapper owns its explicit timeout and its own Editor process tree.
    finally:
        result['parent_environment_unchanged']=dict(os.environ)==parent
        result.setdefault('readiness',dict(normal_mission=False,paid_resources_qualified=False,queue_success_enabled=False,Android_build=False,player_device=False))
        result.setdefault('proofCompleteness',dict(native_compiler_input_consumption_closure='Pending',native_in_process_environment_witness='Pending'))
        with path.open('x') as f:json.dump(result,f,indent=2,allow_nan=False);f.write('\n')
    print(json.dumps({'result_path':str(path),'operationalControlPassed':result['operationalControlPassed']}))
    return 0 if result['operationalControlPassed'] else 1


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--execute',action='store_true');p.add_argument('--authorization',type=Path)
    a=p.parse_args()
    if not a.execute:
        print(json.dumps(dict(enabled=False,launch_performed=False,project=ROOT,commit=COMMIT,source_pins=PINS,
            argv_template=command(Path(ROOT),Path('<new-owned-log>'),TIMEOUT),cold_timeout_seconds=TIMEOUT,
            required_authorization_schema=dict(schema_version=1,launch_authorized=False,project=ROOT,commit=COMMIT,
                controller_sha256=digest(__file__),review_receipt_sha256='<reviewed-receipt-sha256>',scope='Synthetic35Only_NoMissionCredit',timeout_seconds=TIMEOUT),
            native_compiler_input_consumption_closure='Pending',normal_mission_ready=False),indent=2));return 0
    need(a.authorization is not None,'Separate explicit reviewed launch authorization required')
    return execute(a.authorization)


if __name__=='__main__':
    try:raise SystemExit(main())
    except (ValueError,OSError,subprocess.SubprocessError) as e:
        print(json.dumps({'enabled':False,'operationalControlPassed':False,'error':str(e)}));raise SystemExit(1)
