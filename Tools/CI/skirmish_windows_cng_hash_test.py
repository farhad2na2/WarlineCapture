"""Compile unimported BCrypt SHA256 branch and compare native/managed/Python output."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

from s005_windows_source_snapshot import snapshot, candidate_hash


class WindowsCngHashTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if os.name != "nt":
            raise RuntimeError("This native BCrypt test requires Windows")
        cls.project = Path(__file__).resolve().parents[2]
        cls.drafts = cls.project / "Tools/CI/SkirmishProductionDrafts"
        root = cls.project / "Temp/SkirmishWindowsCngOfflineTests"
        root.mkdir(parents=True, exist_ok=True)
        cls.temp = tempfile.TemporaryDirectory(prefix="compiled-", dir=root)
        cls.addClassCleanup(cls.temp.cleanup)
        cls.output = Path(cls.temp.name)
        original = (cls.project / "Assets/Game/Scripts/Editor/Skirmish/SkirmishCandidateSourceHash.cs").read_text(encoding="utf-8")
        marker = "#else\n            private readonly SHA256 hash = SHA256.Create();"
        if original.count(marker) != 1:
            raise RuntimeError("Expected unchanged original platform fallback seam")
        branch = (cls.drafts / "SkirmishCandidateSourceHash.WindowsCng.branch.cs.txt").read_text(encoding="utf-8")
        exact_branch = branch.rstrip() + "\n"
        if original.count(exact_branch) == 1:
            generated = original
            baseline = original.replace(exact_branch, "", 1)
            cls.source_mode = "ActualAlreadyIntegratedSource"
        elif "#elif UNITY_EDITOR_WIN" in original:
            raise RuntimeError("Existing Windows branch differs from reviewed draft; re-review before testing")
        else:
            generated = original.replace(marker, exact_branch + marker)
            baseline = original
            cls.source_mode = "UnimportedPreIntegrationInsertion"
        cls.source = cls.output / "SkirmishCandidateSourceHash.cs"
        cls.source.write_text(generated, encoding="utf-8")
        # Exact surrounding-source identity: removing the one branch restores every
        # original character. This is not a source freeze/cache or imported edit.
        if generated.replace(exact_branch, "", 1) != baseline:
            raise RuntimeError("Draft changed a pre-existing branch or manifest code")
        dotnet = shutil.which("dotnet")
        sdk = subprocess.run(["rtk", "proxy", dotnet, "--list-sdks"], capture_output=True, text=True, check=True).stdout.splitlines()[0]
        version, location = sdk.split(" ", 1)
        compiler = Path(location.strip("[]")) / version / "Roslyn/bincore/csc.dll"
        framework = Path(os.environ["WINDIR"]) / "Microsoft.NET/Framework64/v4.0.30319"
        cls.dotnet, cls.compiler, cls.framework = dotnet, compiler, framework
        cls.programs = {}
        for name, symbols in (("native", "UNITY_EDITOR,UNITY_EDITOR_WIN"), ("managed", "UNITY_EDITOR")):
            target = cls.output / (name + ".exe")
            command = ["rtk", "proxy", dotnet, str(compiler), "/nologo", "/langversion:9", "/nostdlib+", "/target:exe", "/define:" + symbols, "/out:" + str(target)]
            command += ["/reference:" + str(framework / dll) for dll in ("mscorlib.dll", "System.dll", "System.Core.dll")]
            command += [str(cls.source), str(cls.drafts / "WindowsCngHashOfflineProgram.cs.txt")]
            result = subprocess.run(command, capture_output=True, text=True)
            if result.returncode:
                raise RuntimeError("C# compile failed: " + result.stdout + result.stderr)
            cls.programs[name] = target

    def execute(self, name, *args):
        result = subprocess.run(["rtk", "proxy", str(self.programs[name]), *map(str, args)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result.stdout.strip()

    def test_native_standard_binary_short_read_and_disposal_vectors(self):
        output = self.execute("native")
        self.assertIn("[WindowsCngOffline] result=Passed", output)
        self.assertIn("[WindowsCngLifecycle] result=Passed", output)
        print("[WindowsCngSourceMode] mode=" + self.source_mode)
        print(output)

    def test_focused_unity_draft_compiles_against_matching_editor_reference_assemblies(self):
        version = (self.project / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(":", 1)[1].replace("\r", "").strip()
        engine = Path("D:/Program Files/Unity") / version / "Editor/Data/Managed/UnityEngine"
        for name in ("UnityEngine.CoreModule.dll", "UnityEngine.JSONSerializeModule.dll"):
            self.assertTrue((engine / name).is_file(), "Matching installed Editor reference assembly required: " + name)
        command = ["rtk", "proxy", self.dotnet, str(self.compiler), "/nologo", "/langversion:9", "/nostdlib+", "/target:library", "/define:UNITY_EDITOR,UNITY_EDITOR_WIN", "/out:" + str(self.output / "FocusedValidation.dll")]
        command += ["/reference:" + str(self.framework / dll) for dll in ("mscorlib.dll", "System.dll", "System.Core.dll", "netstandard.dll")]
        command += ["/reference:" + str(engine / dll) for dll in ("UnityEngine.CoreModule.dll", "UnityEngine.JSONSerializeModule.dll")]
        command += [str(self.source), str(self.drafts / "SkirmishCandidateSourceHashWindowsValidation.cs.txt")]
        result = subprocess.run(command, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        print("[WindowsCngFocusedDraftCompile] result=Passed editorReferences=" + version + " executedInUnity=0")

    def test_full_multifile_manifest_matches_managed_and_independent_python(self):
        with tempfile.TemporaryDirectory(prefix="manifest-", dir=self.output) as directory:
            root = Path(directory)
            for folder in ("Assets", "Packages", "ProjectSettings"):
                (root / folder).mkdir()
            fixtures = {"Assets/A/Z.cs": b"binary\x00\x00\xff", "Assets/AB.cs": b"ordinal separator counterexample", "Assets/empty.meta": b"", "Packages/manifest.json": b'{"fixture":true}', "ProjectSettings/ProjectVersion.txt": b"excluded by existing rules", "Assets/long/" + "x" * 140 + ".asset": bytes(range(256)) * 5001, "Assets/\u0641\u0627\u0631\u0633\u06cc.cs": b"utf8-path-binarywriter", "Assets/excluded.txt": b"must remain excluded"}
            for name, data in fixtures.items():
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
            native = self.execute("native", "manifest", root)
            managed = self.execute("managed", "manifest", root)
            oracle = candidate_hash(snapshot(root))
            self.assertEqual(native, managed)
            self.assertEqual(native, oracle)
            (root / "Assets/AB.cs").write_bytes(b"changed source byte")
            self.assertNotEqual(self.execute("native", "manifest", root), native)
            print("[WindowsCngManifest] result=Passed native=managed=python sha256=" + native)


if __name__ == "__main__":
    unittest.main(verbosity=2)
