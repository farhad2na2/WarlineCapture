"""Exercise the actual focused wrapper with a fresh owned EXE; never launch Unity."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile


CHILD = r'''
using System;
using System.IO;
class Probe {
 static string Arg(string[] args,string key) {
  for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];return "";
 }
 static int Main(string[] args) {
  File.WriteAllLines(Environment.GetEnvironmentVariable("WARLINE_WRAPPER_ARG_PROBE"),args);
  string method=Arg(args,"-executeMethod");
  File.AppendAllText(Arg(args,"-logFile"),method=="Probe.Missing"?"owned missing marker\n":
   method=="Probe.Failed"?"[OwnedRefreshProbe] result=Failed\n":"[OwnedRefreshProbe] result=Passed\n");
  return method=="Probe.Nonzero"?37:0;
 }
}
'''


def quote(value):
    return "'" + str(value).replace("'", "''") + "'"


def main():
    project = Path(__file__).resolve().parents[2]
    parent = project / "Logs/SkirmishProduction/Offline/RefreshImportModeControls"
    parent.mkdir(parents=True, exist_ok=True)
    output = Path(tempfile.mkdtemp(prefix="controls-", dir=parent))
    source = output / "OwnedRefreshProbe.cs"
    source.write_text(CHILD, encoding="utf-8")
    executable = output / "OwnedRefreshProbe.exe"
    compiler = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework64/v4.0.30319/csc.exe"
    build = subprocess.run(["rtk", "proxy", str(compiler), "/nologo", "/target:exe", "/out:" + str(executable), str(source)], capture_output=True, text=True)
    (output / "compiler.log").write_text(build.stdout + build.stderr, encoding="utf-8")
    if build.returncode:
        raise RuntimeError(build.stdout + build.stderr)
    cases = [
        ("default-gui", None, True, "Probe.Pass", True),
        ("explicit-empty", "", True, "Probe.Pass", True),
        ("in-process", "InProcess", True, "Probe.Pass", True),
        ("per-queue", "OutOfProcessPerQueue", True, "Probe.Pass", True),
        ("out-of-process", "OutOfProcess", True, "Probe.Pass", True),
        ("default-command-line", None, False, "Probe.Pass", True),
        ("invalid-mode", "Other", True, "Probe.Pass", False),
        ("argument-injection", "InProcess -quit", True, "Probe.Pass", False),
        ("missing-marker", "InProcess", True, "Probe.Missing", False),
        ("nonzero", "InProcess", True, "Probe.Nonzero", False),
        ("failed-marker", "InProcess", True, "Probe.Failed", False),
    ]
    results = []
    for name, mode, gui, method, success in cases:
        log = output / (name + ".log")
        args_path = output / (name + ".args.txt")
        command = "& " + quote(project / "Tools/CI/InvokeUnityExecuteMethodValidation.ps1")
        command += " -UnityExe " + quote(executable) + " -ProjectPath " + quote(output)
        command += " -LogFile " + quote(log) + " -ExecuteMethod " + quote(method)
        command += " -RequiredPassMarker '[OwnedRefreshProbe] result=Passed' -BuildTarget StandaloneWindows64 -TimeoutSeconds 10"
        if gui:
            command += " -GuiLicensing"
        if mode is not None:
            command += " -RefreshImportMode " + quote(mode)
        script = output / (name + ".ps1")
        script.write_text(command + "\n", encoding="utf-8-sig")
        env = dict(os.environ, WARLINE_WRAPPER_ARG_PROBE=str(args_path))
        # Invoke the actual wrapper as the PowerShell entry file, matching the
        # documented caller. A nested '& script' under Continue can report a
        # nonterminating parameter-binding error yet return outer process zero.
        invocation = ["rtk", "proxy", "powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
                      str(project / "Tools/CI/InvokeUnityExecuteMethodValidation.ps1"),
                      "-UnityExe", str(executable), "-ProjectPath", str(output), "-LogFile", str(log),
                      "-ExecuteMethod", method, "-RequiredPassMarker", "[OwnedRefreshProbe] result=Passed",
                      "-BuildTarget", "StandaloneWindows64", "-TimeoutSeconds", "10"]
        if gui:
            invocation.append("-GuiLicensing")
        if mode is not None:
            invocation.extend(["-RefreshImportMode", mode])
        (output / (name + ".invocation.json")).write_text(json.dumps(invocation), encoding="utf-8")
        run = subprocess.run(invocation, cwd=project, env=env, capture_output=True, text=True, timeout=30)
        console = run.stdout + run.stderr
        (output / (name + ".console.log")).write_text(console, encoding="utf-8")
        args = args_path.read_text(encoding="utf-8-sig").splitlines() if args_path.exists() else []
        valid_mode = mode in (None, "", "InProcess", "OutOfProcessPerQueue", "OutOfProcess")
        passed = (run.returncode == 0) == success
        if valid_mode:
            expected = [] if not mode else ["-refreshImportMode", mode]
            actual = args[args.index("-refreshImportMode"):args.index("-refreshImportMode") + 2] if "-refreshImportMode" in args else []
            passed &= actual == expected and ("-batchmode" in args) == (not gui)
            passed &= "-quit" in args and args[args.index("-executeMethod") + 1] == method
            passed &= args[args.index("-buildTarget") + 1] == "StandaloneWindows64"
            passed &= args[args.index("-projectPath") + 1] == str(output)
            passed &= args[args.index("-logFile") + 1] == str(log)
            text = log.read_text(encoding="utf-8-sig")
            passed &= "[UnityInvoke] ExitCode: " + ("37" if method == "Probe.Nonzero" else "0") in text
            if name == "missing-marker":
                passed &= "missing required pass marker" in console
        else:
            passed &= not args_path.exists() and "ValidateSet" in console
        results.append(dict(case=name, passed=bool(passed), exit=run.returncode, launched_owned_probe=args_path.exists()))
        print(json.dumps(results[-1]), flush=True)
    record = dict(result="PassedOwnedRefreshImportModeControls" if all(r["passed"] for r in results) else "FailedOwnedRefreshImportModeControls", controls=len(results), cases=results, output=str(output), unity_launched=False, assets_changed=False,
                  focused_wrapper_sha256=hashlib.sha256((project / "Tools/CI/InvokeUnityExecuteMethodValidation.ps1").read_bytes()).hexdigest(),
                  lower_wrapper_sha256=hashlib.sha256((project / "Tools/CI/InvokeUnity.ps1").read_bytes()).hexdigest())
    (output / "result.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(record, indent=2))
    return 0 if all(r["passed"] for r in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
