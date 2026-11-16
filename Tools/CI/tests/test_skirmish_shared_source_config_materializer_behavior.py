from pathlib import Path
import shutil
import subprocess
import tempfile
import uuid
import unittest


ROOT = Path(__file__).resolve().parents[3]
CORE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/AuthoredSourceConfigGraphMaterializer.cs"
HARNESS = Path(__file__).with_name("SharedSourceConfigMaterializerBehaviorHarness.cs")
NEWTONSOFT = ROOT / "Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/Newtonsoft.Json.dll"


class SharedSourceConfigMaterializerBehaviorTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not NEWTONSOFT.is_file():
            raise unittest.SkipTest("Local pinned Newtonsoft.Json assembly unavailable")
        cls.temp = tempfile.TemporaryDirectory()
        project = Path(cls.temp.name) / "project"
        project.mkdir()
        (project / "Harness.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
            '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings>'
            '<Nullable>disable</Nullable></PropertyGroup><ItemGroup>'
            f'<Compile Include="{CORE}" Link="AuthoredSourceConfigGraphMaterializer.cs" />'
            f'<Reference Include="Newtonsoft.Json"><HintPath>{NEWTONSOFT}</HintPath></Reference>'
            '</ItemGroup></Project>',
            encoding="utf-8",
        )
        (project / "NuGet.Config").write_text(
            '<configuration><packageSources><clear /></packageSources></configuration>',
            encoding="utf-8",
        )
        (project / "Program.cs").write_text(HARNESS.read_text(encoding="utf-8"), encoding="utf-8")
        build = subprocess.run(
            ["dotnet", "build", str(project / "Harness.csproj"), "--configfile", str(project / "NuGet.Config"), "--nologo", "-v:q"],
            text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        )
        if build.returncode:
            raise AssertionError("Shared materializer behavioral harness compile failed:\n" + build.stdout)
        cls.dll = project / "bin/Debug/net8.0/Harness.dll"

    @classmethod
    def tearDownClass(cls):
        if hasattr(cls, "temp"):
            cls.temp.cleanup()

    def test_graph_reference_and_pin_controls(self):
        scratch = ROOT / "Tools/CI/tests" / (".materializer-behavior-" + uuid.uuid4().hex)
        try:
            result = subprocess.run(["dotnet", str(self.dll), str(scratch)], text=True,
                                    stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        finally:
            if scratch.exists():
                shutil.rmtree(scratch)
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("[SharedSourceConfigMaterializerBehavior] Passed", result.stdout)


if __name__ == "__main__":
    unittest.main()
