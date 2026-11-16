import hashlib
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[4]
ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/527_GeneratedS002DropClearance.s003.cs"
HARNESS = Path(__file__).with_name("S003ClearanceConsumerHarness.cs")
PROJECTION = ROOT / "Logs/private-candidates/s003-preflight-r6-overlay/relocation-r1/mapped-tmp/s003-static-clearance-r1.json"
SOURCE = ROOT / "Logs/private-candidates/s003-preflight-r6-overlay/relocation-r1/mapped-tmp/s003-desert-navigation-r1/reference-data.json"
PROJECTION_SHA = "56f092a791a44cf4a45f70714b1bfdc313425e66e9148777d23352620ef4bfd7"
SOURCE_SHA = "5a0993af26be09c8605a883e842ff802c67ae56477b57e9513c2b11b3827b010"
SOURCE_BYTES = 86684375


class S003ClearanceConsumerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not PROJECTION.is_file() or not SOURCE.is_file():
            raise unittest.SkipTest("Current retained S003 clearance evidence is unavailable")
        cls.temp = tempfile.TemporaryDirectory()
        project = Path(cls.temp.name)
        references = {
            "Newtonsoft": ROOT / "Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll",
            "UnityMathematics": ROOT / "Library/ScriptAssemblies/Unity.Mathematics.dll",
            "UnityCollections": ROOT / "Library/ScriptAssemblies/Unity.Collections.dll",
            "UnityEngineMathematicsModule": Path("/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.MathematicsModule.dll"),
        }
        for reference in references.values():
            if not reference.is_file():
                raise unittest.SkipTest("Local Unity compile reference unavailable: " + str(reference))
        items = "".join(f'<Reference Include="{name}"><HintPath>{path}</HintPath></Reference>' for name, path in references.items())
        (project / "Harness.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>'
            '<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>'
            f'<Compile Include="{ADAPTER}" Link="ProductionClearanceConsumer.cs" />{items}</ItemGroup></Project>',
            encoding="utf-8",
        )
        (project / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
        (project / "Program.cs").write_text(HARNESS.read_text(encoding="utf-8"), encoding="utf-8")
        build = subprocess.run(["dotnet", "build", str(project / "Harness.csproj"), "--configfile", str(project / "NuGet.Config"), "--nologo", "-v:q"],
                               text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        if build.returncode:
            raise AssertionError("Production consumer harness compile failed:\n" + build.stdout)
        cls.dll = project / "bin/Debug/net8.0/Harness.dll"

    @classmethod
    def tearDownClass(cls):
        if hasattr(cls, "temp"):
            cls.temp.cleanup()

    def invoke(self, projection=PROJECTION, source=SOURCE, source_sha=SOURCE_SHA, source_bytes=SOURCE_BYTES):
        return subprocess.run(["dotnet", str(self.dll), str(projection), PROJECTION_SHA, str(source), source_sha, str(source_bytes)],
                              text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)

    def test_current_projection_and_source_bytes_are_consumed_by_production_adapter(self):
        self.assertEqual(hashlib.sha256(PROJECTION.read_bytes()).hexdigest(), PROJECTION_SHA)
        self.assertEqual(SOURCE.stat().st_size, SOURCE_BYTES)
        result = self.invoke()
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("[S003ClearanceConsumer] accepted", result.stdout)

    def test_missing_source_is_rejected(self):
        result = self.invoke(source=SOURCE.parent / "missing-reference-data.json")
        self.assertNotEqual(result.returncode, 0)

    def test_wrong_declared_size_is_rejected(self):
        result = self.invoke(source_bytes=SOURCE_BYTES - 1)
        self.assertNotEqual(result.returncode, 0)

    def test_wrong_declared_hash_is_rejected(self):
        result = self.invoke(source_sha="0" * 64)
        self.assertNotEqual(result.returncode, 0)

    def test_same_size_tampered_source_is_rejected_by_content_hash(self):
        with tempfile.TemporaryDirectory() as temporary:
            altered = Path(temporary) / "source.json"
            with altered.open("wb") as stream:
                stream.truncate(SOURCE_BYTES)
                stream.seek(0)
                stream.write(b"!")
            result = self.invoke(source=altered)
            self.assertNotEqual(result.returncode, 0)

    def test_changed_projection_bytes_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            altered = Path(temporary) / "projection.json"
            altered.write_bytes(PROJECTION.read_bytes() + b" ")
            result = self.invoke(projection=altered)
            self.assertNotEqual(result.returncode, 0)


if __name__ == "__main__":
    unittest.main()
