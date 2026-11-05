"""Existing owned synthetic wrapper regression, adapted for cached-root-only cleanup; never Unity."""
import argparse
import datetime
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile
import time


def quote(value):
    return "'" + str(value).replace("'", "''") + "'"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--project-root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--wrapper', type=Path)
    args = parser.parse_args()
    root = args.project_root.resolve()
    wrapper = (args.wrapper or root / 'Tools/CI/InvokeUnity.ps1').resolve()
    baseline_path = root / 'Tools/CI/invoke_unity_process_exit_test.py'
    spec = importlib.util.spec_from_file_location('existing_owned_exit_controls', baseline_path)
    baseline = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(baseline)
    out = Path(tempfile.mkdtemp(prefix='cached-root-', dir=root / 'Logs/SkirmishProduction/Offline'))
    original_line = '   string log=Arg(args,"-logFile","");'
    child = baseline.CHILD.replace(original_line, r'''   if(args.Length==1 && args[0]=="--sentinel") { Thread.Sleep(12000); return 0; }
   string log=Arg(args,"-logFile","");
   var sentinel=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,"--sentinel") { UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true, RedirectStandardInput=true });
   File.WriteAllText(log+".sentinel-pid",sentinel.Id.ToString());
   File.WriteAllText(log+".sentinel-start",sentinel.StartTime.ToUniversalTime().Ticks.ToString());
   File.WriteAllText(log+".owned-pid",System.Diagnostics.Process.GetCurrentProcess().Id.ToString());''')
    assert child != baseline.CHILD
    shim = r'''using System; class RefusedTaskkillProbe { static int Main(string[] args) { Console.WriteLine("UNEXPECTED_TASKKILL_CALLED "+string.Join(" ",args)); return 128; } }'''
    compiler = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    fake = out / 'fake-system-root'
    (fake / 'System32').mkdir(parents=True)
    executable = out / 'OwnedExitProbe.exe'
    for name, source, target in [('OwnedExitProbe', child, executable), ('TaskkillProbe', shim, fake / 'System32/taskkill.exe')]:
        path = out / (name + '.cs')
        path.write_text(source, encoding='utf8')
        build = subprocess.run(['rtk', 'proxy', str(compiler), '/nologo', '/target:exe', '/out:' + str(target), str(path)], capture_output=True, text=True)
        (out / (name + '.compiler.log')).write_text(build.stdout + build.stderr, encoding='utf8')
        assert build.returncode == 0, build.stdout + build.stderr
    results = []
    for name, fatal, sleep, expected, focused in [('timeout-owned-root', 0, 20000, 124, False), ('fatal-owned-root', 1, 20000, 1, False), ('focused-missing-pass-marker', 0, 0, 1, True)]:
        log = out / (name + '.log')
        script = out / (name + '.ps1')
        command = '$env:SystemRoot=' + quote(fake) + '\n'
        if focused:
            command += '& ' + quote(wrapper.with_name('InvokeUnityExecuteMethodValidation.ps1')) + ' -UnityExe ' + quote(executable) + ' -ProjectPath ' + quote(out) + ' -ExecuteMethod OwnedSyntheticValidation -LogFile ' + quote(log) + ' -RequiredPassMarker ' + quote('[OwnedSyntheticRequired] result=Passed') + ' -GuiLicensing -TimeoutSeconds 10\n'
        else:
            command += '& ' + quote(wrapper) + ' -UnityExe ' + quote(executable) + ' -ProjectPath ' + quote(out) + ' -LogFile ' + quote(log) + ' -GuiLicensing -TimeoutSeconds ' + str(10 if fatal else 1) + " -UnityArguments @('--sleep'," + quote(sleep) + ",'--fatal'," + quote(fatal) + ')\nexit $global:LASTEXITCODE\n'
        script.write_text(command, encoding='utf-8-sig')
        console_out = out / (name + '.console.stdout.log')
        console_err = out / (name + '.console.stderr.log')
        sentinel = None
        observed_while_wrapper_alive = False
        query_errors = []
        owned_pid = None
        expected_marker = '[UnityInvoke] ExitCode: ' + str(0 if focused else expected)
        with console_out.open('xb') as stdout, console_err.open('xb') as stderr:
            process = subprocess.Popen(['rtk', 'proxy', 'powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(script)], cwd=root, stdout=stdout, stderr=stderr, stdin=subprocess.DEVNULL)
            deadline = time.monotonic() + 35
            while True:
                try:
                    current_log = log.read_text(encoding='utf-8-sig', errors='replace') if log.exists() else ''
                except PermissionError:
                    assert time.monotonic() < deadline, 'Synthetic log sharing violation exceeded bounded test deadline'
                    time.sleep(.1)
                    continue
                pid_file = Path(str(log) + '.sentinel-pid')
                if sentinel is None and expected_marker in current_log and pid_file.exists():
                    pid = int(pid_file.read_text())
                    birth = int(Path(str(log) + '.sentinel-start').read_text())
                    owned_pid = int(Path(str(log) + '.owned-pid').read_text())
                    query = '$p=Get-Process -Id ' + str(pid) + ' -ErrorAction Stop; $row=Get-CimInstance Win32_Process -Filter "ProcessId = ' + str(pid) + '"; $ownedRoot=Get-Process -Id ' + str(owned_pid) + ' -ErrorAction SilentlyContinue; [pscustomobject]@{Id=$p.Id;Path=$p.Path;StartTicks=$p.StartTime.ToUniversalTime().Ticks;HasExited=$p.HasExited;ParentProcessId=$row.ParentProcessId;CommandLine=$row.CommandLine;OwnedRootAbsent=($null -eq $ownedRoot)}|ConvertTo-Json -Compress'
                    observed = subprocess.run(['rtk', 'proxy', 'powershell.exe', '-NoProfile', '-Command', query], capture_output=True, text=True, timeout=10)
                    witness = json.loads(observed.stdout) if observed.returncode == 0 else None
                    if witness is not None and witness['Id'] == pid and Path(witness['Path']).samefile(executable) and witness['StartTicks'] == birth and witness['HasExited'] is False and witness['ParentProcessId'] == owned_pid and '--sentinel' in witness['CommandLine'] and witness['OwnedRootAbsent'] is True:
                        sentinel = dict(witness, observedAtUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), actualCleanupMarker=expected_marker)
                        observed_while_wrapper_alive = process.poll() is None
                        (out / (name + '.cleanup-boundary-witness.json')).write_text(json.dumps(sentinel, indent=2) + '\n', encoding='utf8')
                    else:
                        query_errors.append(dict(returncode=observed.returncode, stdout=observed.stdout, stderr=observed.stderr, observation=witness))
                if process.poll() is not None:
                    break
                assert time.monotonic() < deadline, 'Synthetic wrapper exceeded bounded test deadline'
                time.sleep(.1)
            process.wait(timeout=5)
        console = console_out.read_text(errors='replace') + console_err.read_text(errors='replace')
        (out / (name + '.console.log')).write_text(console, encoding='utf8')
        text = log.read_text(encoding='utf-8-sig', errors='replace')
        survived = sentinel is not None
        passed = process.returncode == expected and 'UNEXPECTED_TASKKILL_CALLED' not in console + text and survived
        if focused:
            passed = passed and 'missing required pass marker' in console and '[UnityInvoke] ExitCode: 0' in text
        else:
            passed = passed and '[UnityInvoke] ExitCode: ' + str(expected) in text and 'cached Process handle; descendant shutdown not claimed' in text
        record = dict(case=name, passed=passed, actualExit=process.returncode, expectedExit=expected, exactSentinelSurvived=survived, sentinelObservation=sentinel, ownedRootPid=owned_pid, cleanupObservedWhileWrapperAlive=observed_while_wrapper_alive, queryErrors=query_errors, taskkillCalled='UNEXPECTED_TASKKILL_CALLED' in console + text, descendantShutdownClaimed=False)
        results.append(record)
        print(json.dumps(record), flush=True)
        time.sleep(12)
    result = dict(result='PassedCachedOwnedRootOnlyRegression' if all(row['passed'] for row in results) else 'FailedCachedOwnedRootOnlyRegression', cases=results, wrapper=dict(path=str(wrapper), sha256=hashlib.sha256(wrapper.read_bytes()).hexdigest()), existingBaseline=dict(path=str(baseline_path), sha256=hashlib.sha256(baseline_path.read_bytes()).hexdigest()), unityLaunched=False, sourceInstalled=False, testCleanupKilledProcesses=False, output=str(out))
    (out / 'RESULT.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf8')
    print(json.dumps(result), flush=True)
    return 0 if all(row['passed'] for row in results) else 1


if __name__ == '__main__':
    raise SystemExit(main())
