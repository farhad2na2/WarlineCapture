"""Compile/run actual FC reducer + actual 13 NUnit tests outside Unity/Assets.

The execution-only UnityEngine shim delegates JSON to real installed Newtonsoft.
A separate compilation uses matching real Editor refs without that shim. Neither
check is native gameplay, occupancy, actual Unity JSON or checkpoint acceptance.
"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from skirmish_offline_evidence import new_evidence_directory


class FrontlineOfflineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.project = Path(__file__).resolve().parents[2]
        cls.drafts = cls.project / "Tools/CI/SkirmishProductionDrafts"
        cls.evidence = new_evidence_directory(cls.project, "frontline")
        cls.temp_root = cls.project / "Logs/SkirmishProduction/Offline/FrontlineTests"
        cls.temp_root.mkdir(parents=True, exist_ok=True)
        cls.temp = tempfile.TemporaryDirectory(prefix="compiled-", dir=cls.temp_root)
        cls.output = Path(cls.temp.name).resolve()

        def cleanup():
            if cls.output.parent != cls.temp_root.resolve() or not cls.output.name.startswith("compiled-"):
                raise RuntimeError("Refusing cleanup outside exact FC temporary root")
            cls.temp.cleanup()
        cls.addClassCleanup(cleanup)
        cls.dotnet = shutil.which("dotnet")
        if os.name != "nt" or not cls.dotnet:
            raise RuntimeError("Existing Windows .NET SDK required; no downloads")
        sdks = subprocess.run(["rtk", "proxy", cls.dotnet, "--list-sdks"], capture_output=True, text=True, check=True).stdout
        version, location = sdks.splitlines()[0].split(" ", 1)
        cls.compiler = Path(location.strip("[]")) / version / "Roslyn/bincore/csc.dll"
        cls.framework = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework64/v4.0.30319"
        cls.nunit = cls.single("com.unity.ext.nunit@*/net472/unity-custom/nunit.framework.dll")
        cls.newtonsoft = cls.single("com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll")
        cls.sources = [cls.project / "Assets/Game/Scripts/Skirmish/Contracts" / name for name in
                       ("SkirmishScenarioContracts.cs", "SkirmishObjectiveContracts.cs", "SkirmishReasonCodes.cs")]
        cls.sources += [cls.drafts / "SkirmishFrontlineReducer.cs.txt", cls.drafts / "SkirmishFrontlineReducerTests.cs.txt"]
        cls.pins = {str(path.relative_to(cls.project)): hashlib.sha256(path.read_bytes()).hexdigest()
                    for path in cls.sources + [cls.drafts / "SkirmishFrontlineOfflineProgram.cs.txt", cls.nunit, cls.newtonsoft]}
        cls.manifest = dict(scope="Actual FC reducer/NUnit offline; no Unity runtime claims", compiler=str(cls.compiler),
                            sources=cls.pins, execution_json_backend="Newtonsoft (offline shim only)")
        (cls.evidence / "source-and-tool-manifest.json").write_text(json.dumps(cls.manifest, indent=2), encoding="utf-8")

    @classmethod
    def single(cls, pattern):
        matches = list((cls.project / "Library/PackageCache").glob(pattern))
        if len(matches) != 1:
            raise RuntimeError("Expected exactly one installed dependency for " + pattern)
        return matches[0]

    @classmethod
    def compile_command(cls, target, references):
        return ["rtk", "proxy", cls.dotnet, str(cls.compiler), "/nologo", "/langversion:9", "/nostdlib+",
                "/target:" + ("exe" if target.suffix == ".exe" else "library"), "/out:" + str(target)] + \
            ["/reference:" + str(cls.framework / dll) for dll in ("mscorlib.dll", "System.dll", "System.Core.dll", "netstandard.dll")] + \
            ["/reference:" + str(path) for path in references] + [str(path) for path in cls.sources]

    def run_preserved(self, command, name):
        result = subprocess.run(command, capture_output=True, text=True)
        (self.evidence / name).write_text("COMMAND " + subprocess.list2cmdline(command) + "\nEXIT " + str(result.returncode) +
                                         "\n" + result.stdout + result.stderr, encoding="utf-8")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        return result.stdout

    def test_actual_thirteen_nunit_rules_and_checkpoint_value_roundtrip(self):
        shutil.copyfile(self.nunit, self.output / "nunit.framework.dll")
        shutil.copyfile(self.newtonsoft, self.output / "Newtonsoft.Json.dll")
        target = self.output / "FrontlineRules.exe"
        command = self.compile_command(target, [self.nunit, self.newtonsoft]) + [str(self.drafts / "SkirmishFrontlineOfflineProgram.cs.txt")]
        self.run_preserved(command, "offline-compile.log")
        output = self.run_preserved(["rtk", "proxy", str(target)], "offline-13-rules.log")
        self.assertEqual(13, output.count("[FrontlineOfflineRule] result=Passed"))
        self.assertIn("[SkirmishFrontlineOffline] result=Passed tests=13 passed=13", output)
        print(output, end="")

    def test_drafts_compile_against_actual_matching_editor_and_nunit_references(self):
        version = (self.project / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8").splitlines()[0].split(":", 1)[1].replace("\r", "").strip()
        engine = Path("D:/Program Files/Unity") / version / "Editor/Data/Managed/UnityEngine"
        refs = [engine / dll for dll in ("UnityEngine.CoreModule.dll", "UnityEngine.JSONSerializeModule.dll")]
        self.assertTrue(all(path.is_file() for path in refs), "Version-matched installed Editor refs required")
        command = self.compile_command(self.output / "FrontlineFocusedEditorTests.dll", [self.nunit] + refs)
        self.run_preserved(command, "actual-editor-reference-compile.log")
        print("[SkirmishFrontlineEditorReferenceCompile] result=Passed editor=" + version + " executedInUnity=0")

    def test_pinned_actual_reducer_contracts_and_test_sources_unchanged(self):
        for relative, expected in self.pins.items():
            self.assertEqual(expected, hashlib.sha256((self.project / relative).read_bytes()).hexdigest(), relative)
        print("[SkirmishFrontlineOfflineSourcePins] result=Passed sources=" + str(len(self.pins)))


if __name__ == "__main__":
    unittest.main(verbosity=2)
