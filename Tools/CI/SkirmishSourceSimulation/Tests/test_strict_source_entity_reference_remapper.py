import pathlib
import subprocess
import tempfile
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[4]
TESTS = pathlib.Path(__file__).resolve().parent
ADAPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S073/StrictSourceEntityReferenceRemapper.cs"
STUB = TESTS / "StrictEntityTestStub.cs"
CONTROL = TESTS / "StrictSourceEntityReferenceRemapperControl.cs"
PASS = "[StrictSourceEntityReferenceRemapperControl] result=Passed entityRemap=1 missingReferenceRejected=1 domainChecks=2 unsupportedTypeRejected=1"


class StrictSourceEntityReferenceRemapperTests(unittest.TestCase):
    def test_strict_remap_control(self):
        with tempfile.TemporaryDirectory(prefix="s073-entity-remap-") as tmp:
            project = pathlib.Path(tmp) / "control.csproj"
            project.write_text(
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>"
                "<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>"
                "<EnableDefaultCompileItems>false</EnableDefaultCompileItems>"
                "<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable>"
                "</PropertyGroup><ItemGroup>"
                f"<Compile Include=\"{ADAPTER}\"/><Compile Include=\"{STUB}\"/>"
                f"<Compile Include=\"{CONTROL}\"/>"
                "</ItemGroup></Project>",
                encoding="utf-8",
            )
            result = subprocess.run(
                ["dotnet", "run", "--project", str(project)],
                cwd=ROOT,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertEqual(result.returncode, 0, result.stdout)
            self.assertIn(PASS, result.stdout)


if __name__ == "__main__":
    unittest.main()
