"""Synthetic emitted-schema controls only; no Unity/native collection is executed."""
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import qualify_skirmish_ground_objective30_collection as q
import invoke_skirmish_ground_objective30_collection as launch


def fixture():
    inputs=q.load(q.ROOT/q.INPUT);manifest=q.load(q.ROOT/q.MAP)
    source=[dict(path=f['target'],sha256=f['sha256']) for f in manifest['files']]
    dll=[dict(path='Library/ScriptAssemblies/Fixture.dll',sha256='a'*64)]
    snapshot=dict(source_manifest=source,candidate_sha256=q.candidate_hash(source),build_manifest=dll,
                  build_sha256=hashlib.sha256(json.dumps(dll,sort_keys=True,separators=(',',':')).encode()).hexdigest())
    groups={}
    for mission in inputs['missions']:
        pin=mission['source_candidate'];path=pin['sourcePath']
        group=groups.setdefault(path,dict(sourcePath=path,sourcePins=copy.deepcopy(pin),missions=[]))
        sizes=[]
        for size in ('Standard','War','LargeWar'):
            sizes.append(dict(size=size,status='TypedStaticBudgetAndSourcePackingCollected_NormalCompilationAndLiveFitIndependent',
                sourceFitCertified=False,normalCompilerPassed=False,normalCompileReasons=[dict(code='MissingReference',field='missionLogicalBinding',detail='Fixture missing proof')],
                staticBudgetCanonicalQuantitiesExact=True,staticBudgetErrors=[],
                nativeForcePackingSourceOnly=[dict(forceIndex=0,faction=1,roleId='FixtureOnly',requestedCount=12,status='StaticSourcePackingFailedOrOutsidePad')],
                allSizeFacilities=[dict(faction=1,structureId='FixtureOnly',status='ExactFacilityMetadataUnavailable')],
                completeCanonicalFacilityConfigurationMeasured=False,normalFacilityRelocationProposals=[],
                nativeForcePackingWithCompleteFacilities=[],nativeRouteQueriesWithCompleteFacilities=dict(status='UnavailableCompleteCanonicalFacilityConfiguration')))
        group['missions'].append(dict(catalogId=mission['catalog_id'],canonicalRowsSha256=mission['canonical_size_rows_sha256'],sourceFitCertified=False,sizes=sizes))
    raw=dict(schema=1,scope='StaticAuthoringSourceQueries_NotCompiledMissionQualification',inputSha256=q.INPUT_SHA,
             sourceFitCertified=False,playerReady=False,sources=list(groups.values()),sourceWorldsDisposed=4,
             collectionErrors=0,normalCompilerRejectedRows=90,result='CollectedUnqualified')
    receipt=dict(scope='GroundObjective30StaticCollection',method=q.METHOD,wrapper_exit=0,input_sha256=q.INPUT_SHA,
                 import_map_sha256=q.MAP_SHA,preparation_checked_zero=True,tool_bytes_unchanged=True)
    path=q.ROOT/'Logs/SkirmishProduction/Offline/Ground30Synthetic/raw.json'
    log=q.MARKER+str(path)+'\n[UnityInvoke] ExitCode: 0\n'
    return raw,inputs,manifest,snapshot,copy.deepcopy(snapshot),receipt,log,path


class Ground30CollectionControls(unittest.TestCase):
    def test_complete_synthetic_rows_keep_all_failed_fit_facts_unqualified(self):
        result=q.qualify(*fixture())
        self.assertTrue(result['collection_complete'],result['errors'])
        self.assertEqual((30,90,90),(result['mission_rows'],result['size_rows'],result['normal_compiler_rejected_rows']))
        self.assertFalse(result['qualified']);self.assertFalse(result['source_fit_certified'])
        details=[row for mission in result['summaries'] for row in mission['size_rows']]
        self.assertEqual(90,sum(row['source_only_force_statuses'].get('StaticSourcePackingFailedOrOutsidePad',0) for row in details))
        self.assertEqual(90,sum(row['facility_metadata_statuses'].get('ExactFacilityMetadataUnavailable',0) for row in details))

    def test_missing_duplicate_spliced_rows_budgets_sizes_and_partial_facility_promotions_reject(self):
        mutations=[
            lambda r:r['sources'][0]['missions'].pop(),
            lambda r:r['sources'][0]['missions'].append(copy.deepcopy(r['sources'][0]['missions'][0])),
            lambda r:r['sources'][0]['missions'][0]['sizes'].pop(),
            lambda r:r['sources'][0]['missions'][0]['sizes'].__setitem__(1,copy.deepcopy(r['sources'][0]['missions'][0]['sizes'][0])),
            lambda r:r['sources'][0]['missions'][0].update(canonicalRowsSha256='0'*64),
            lambda r:r['sources'][0]['sourcePins'].update(sourceIdentityHash='foreign'),
            lambda r:r['sources'][0]['missions'][0].update(catalogId='S999'),
            lambda r:r['sources'][0]['missions'][0]['sizes'][0].update(completeCanonicalFacilityConfigurationMeasured=True),
            lambda r:r['sources'][0]['missions'][0]['sizes'][0].update(nativeForcePackingWithCompleteFacilities=[dict(status='claimed')]),
            lambda r:r['sources'][0]['missions'][0]['sizes'][0].update(nativeRouteQueriesWithCompleteFacilities=dict(queries=[dict(status='NativeQueryFoundExactGoal')])),
            lambda r:r.update(normalCompilerRejectedRows=0),
            lambda r:r.update(sourceFitCertified=True),
            lambda r:r['sources'][0]['missions'][0]['sizes'][0].update(error='Native source failure')]
        for mutation in mutations:
            args=list(copy.deepcopy(fixture()));mutation(args[0]);result=q.qualify(*args)
            self.assertFalse(result['collection_complete'],mutation)

    def test_wrapper_endpoint_native_marker_tool_and_cleanup_failures_reject(self):
        for slot,mutation in (
            (5,lambda r:r.update(wrapper_exit=1)),(5,lambda r:r.update(wrapper_exit=False)),
            (5,lambda r:r.update(tool_bytes_unchanged=False)),(5,lambda r:r.update(launcher_error='wrapper failure')),
            (5,lambda r:r.update(import_map_sha256='0'*64)),(4,lambda r:r.update(build_sha256='0'*64)),
            (0,lambda r:r.update(cleanupError='Changed preview census')),(0,lambda r:r.update(sourceWorldsDisposed=3)),
            (0,lambda r:r.update(collectionErrors=1)),(0,lambda r:r['sources'][0].update(sourceError='load failed'))):
            args=list(copy.deepcopy(fixture()));mutation(args[slot]);self.assertFalse(q.qualify(*args)['collection_complete'])
        for text in ('','[UnityInvoke] ExitCode: 0\n',fixture()[6]+'\nCrash!!!',fixture()[6]+fixture()[6],fixture()[6].replace('ExitCode: 0','ExitCode: 124')):
            args=list(fixture());args[6]=text;self.assertFalse(q.qualify(*args)['collection_complete'])

    def test_plan_is_read_only_and_lane_checks_refuse_ownership_without_killing(self):
        with tempfile.TemporaryDirectory(dir=q.ROOT/'Logs/SkirmishProduction/Offline') as folder:
            run=Path(folder)/'not-created'
            result=subprocess.run([sys.executable,str(q.ROOT/'Tools/CI/invoke_skirmish_ground_objective30_collection.py'),'--run-directory',str(run)],capture_output=True,text=True)
            self.assertEqual(0,result.returncode,result.stderr);self.assertFalse(run.exists())
            self.assertEqual(q.METHOD,json.loads(result.stdout)['method'])
        hub=dict(Name='Unity Hub.exe',ProcessId=1,CommandLine='Hub')
        launch.check_lane([hub,dict(Name='Unity.exe',ProcessId=2,CommandLine='Unity -projectPath D:/OtherProject')])
        for rows in ([],[hub,dict(Name='Unity.exe',CommandLine=None)],
                     [hub,dict(Name='Unity.exe',CommandLine='Unity -projectPath '+str(q.ROOT).replace('\\','/'))]):
            with self.assertRaises(ValueError):launch.check_lane(rows)

    def test_actual_installed_map_and_clean_preparation_refusal_without_native_launch(self):
        plan=launch.check_installed();self.assertEqual(30,plan['missions'])
        with tempfile.TemporaryDirectory(dir=q.ROOT/'Logs/SkirmishProduction/Offline') as folder:
            path=Path(folder)/'fixture.log';path.write_text(q.PREPARATION+'\n[UnityInvoke] ExitCode: 0\n')
            self.assertEqual(q.sha(path),launch.check_preparation(path))
            for suffix in ('\nsuiteFailed=fixture','\nCrash!!!','\n[UnityInvoke] ExitCode: 0'):
                path.write_text(q.PREPARATION+'\n[UnityInvoke] ExitCode: 0'+suffix)
                with self.assertRaises(ValueError):launch.check_preparation(path)

    def test_checked_child_uses_only_required_wrapper_and_actual_inherited_arguments(self):
        self.assertIn('InvokeUnityExecuteMethodValidation.ps1',launch.CHILD)
        self.assertIn('-GuiLicensing -TimeoutSeconds $TimeoutSeconds',launch.CHILD)
        self.assertNotIn('Stop-Process',launch.CHILD);self.assertNotIn('Start-Process',launch.CHILD)
        self.assertNotIn('Unity.exe',launch.CHILD)
        with tempfile.TemporaryDirectory(dir=q.ROOT/'Logs/SkirmishProduction/Offline') as folder:
            folder=Path(folder);child=folder/'child.ps1';child.write_text(launch.CHILD,encoding='utf-8-sig')
            ast=folder/'ast.ps1';ast.write_text("param($Path)\n$tokens=$null;$errors=$null\n[void][Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors)\nif($errors.Count){$errors|Out-String|Write-Error;exit 1}\nWrite-Output 'ChildAstPassed'\n")
            result=launch.powershell(['-File',str(ast),str(child)],capture_output=True,text=True)
            self.assertEqual(0,result.returncode,result.stderr);self.assertIn('ChildAstPassed',result.stdout)

    def test_emitted_child_executes_only_inert_checked_wrapper_and_preserves_failure(self):
        with tempfile.TemporaryDirectory(dir=q.ROOT/'Logs/SkirmishProduction/Offline') as folder:
            folder=Path(folder);project=folder/'inert project';ci=project/'Tools/CI';ci.mkdir(parents=True)
            stub=ci/'InvokeUnityExecuteMethodValidation.ps1'
            stub.write_text("""param($UnityExe,$ProjectPath,$ExecuteMethod,$LogFile,$RequiredPassMarker,[switch]$GuiLicensing,$TimeoutSeconds)
@{method=$ExecuteMethod;marker=$RequiredPassMarker;gui=[bool]$GuiLicensing;timeout=[int]$TimeoutSeconds;raw=$env:WARLINE_GROUND_OBJECTIVE30_OUTPUT;editor=$UnityExe}|ConvertTo-Json|Out-File -LiteralPath ($LogFile+'.stub.json') -Encoding utf8
$global:LASTEXITCODE=[int]$env:GROUND30_INERT_EXIT
""")
            child=folder/'child.ps1';child.write_text(launch.CHILD,encoding='utf-8-sig')
            for native_exit,expected in ((0,0),(37,1)):
                run=folder/str(native_exit);run.mkdir();log=run/'unity.log';raw=run/'raw.json'
                env=dict(os.environ,GROUND30_INERT_EXIT=str(native_exit))
                result=launch.powershell(['-File',str(child),'-ProjectPath',str(project),'-UnityExe','InertOnly_NotAnExecutable',
                    '-LogFile',str(log),'-RawOutput',str(raw),'-TimeoutSeconds','1800'],capture_output=True,text=True,env=env)
                self.assertEqual(expected,result.returncode,result.stderr)
                self.assertEqual(str(expected),(run/'checked-child-exit.txt').read_text())
                received=q.load(Path(str(log)+'.stub.json'))
                self.assertEqual(q.METHOD,received['method']);self.assertEqual(q.MARKER,received['marker'])
                self.assertEqual(str(raw),received['raw']);self.assertTrue(received['gui'])
                self.assertEqual(1800,received['timeout']);self.assertEqual('InertOnly_NotAnExecutable',received['editor'])

    def test_real_cli_joins_artifact_hashes_and_preserves_existing_output(self):
        with tempfile.TemporaryDirectory(dir=q.ROOT/'Logs/SkirmishProduction/Offline') as folder:
            folder=Path(folder);raw,inputs,manifest,before,after,receipt,log,_=fixture()
            paths={name:folder/(name+'.json') for name in ('raw','before','after','receipt','output')}
            paths['log']=folder/'unity.log';paths['preparation-log']=folder/'preparation.log'
            launch.write(paths['raw'],raw);launch.write(paths['before'],before);launch.write(paths['after'],after)
            paths['log'].write_text(q.MARKER+str(paths['raw'])+'\n[UnityInvoke] ExitCode: 0\n')
            paths['preparation-log'].write_text(q.PREPARATION+'\n[UnityInvoke] ExitCode: 0\n')
            child=folder/'checked-child-exit.txt';child.write_text('0')
            receipt.update(preparation_sha256=q.sha(paths['preparation-log']),checked_child_exit_sha256=q.sha(child),
                tool_pins=[dict(path=p,sha256=q.sha(q.ROOT/p)) for p in q.WORKFLOW_TOOLS])
            for field,path in (('raw_sha256',paths['raw']),('log_sha256',paths['log']),('before_sha256',paths['before']),('after_sha256',paths['after'])):
                receipt[field]=q.sha(path)
            launch.write(paths['receipt'],receipt)
            command=[sys.executable,str(q.ROOT/'Tools/CI/qualify_skirmish_ground_objective30_collection.py')]
            for name,path in paths.items():command+=['--'+name,str(path)]
            result=subprocess.run(command,capture_output=True,text=True)
            self.assertEqual(0,result.returncode,result.stderr);self.assertTrue(q.load(paths['output'])['collection_complete'])
            saved=q.sha(paths['output']);repeat=subprocess.run(command,capture_output=True,text=True)
            self.assertNotEqual(0,repeat.returncode);self.assertEqual(saved,q.sha(paths['output']))
            original_raw=paths['raw'].read_bytes();paths['raw'].write_bytes(original_raw+b' ')
            # Replace the explicit output value without changing any receipt hash.
            command[command.index('--output')+1]=str(folder/'tampered-result.json')
            result=subprocess.run(command,capture_output=True,text=True)
            self.assertNotEqual(0,result.returncode)
            self.assertFalse(q.load(folder/'tampered-result.json')['collection_complete'])
            paths['raw'].write_bytes(original_raw)
            receipt['tool_pins']=[row for row in receipt['tool_pins'] if row['path']!='Tools/CI/build_skirmish_authoring_batches.py']
            paths['receipt'].write_text(json.dumps(receipt))
            command[command.index('--output')+1]=str(folder/'missing-helper-result.json')
            result=subprocess.run(command,capture_output=True,text=True)
            self.assertNotEqual(0,result.returncode)
            failed=q.load(folder/'missing-helper-result.json');self.assertFalse(failed['collection_complete'])
            self.assertIn('Workflow tools',failed['errors'][0])


if __name__=='__main__':unittest.main(verbosity=2)
