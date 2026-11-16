from __future__ import annotations

import importlib.util
import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
RECIPE = Path(__file__).with_name("generate.py")
PREPARE = Path(__file__).with_name("prepare_manifest.py")


def load(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SparseAcquisitionTransformTests(unittest.TestCase):
    def test_generation_is_exact_opt_in_and_preserves_original_fallback(self):
        generator = load(RECIPE, "sparse_acquisition_generator")
        watched = [generator.KERNEL, generator.SYSTEM, generator.BRIDGE]
        before = {name: (ROOT / name).read_bytes() for name in watched}
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "generated"
            proof = generator.generate(ROOT, output)
            self.assertTrue(proof["experimental_only"])
            kernel = (output / "SparseTargetAcquisitionKernel.cs").read_text()
            self.assertIn("ISparseTargetAcquisitionContext", kernel)
            self.assertIn("if(sparse)", kernel)
            self.assertIn("else\n            {\n            for(int dz=-searchRange", kernel)
            self.assertIn("while(context.NextCell(out candidate))", kernel)
            system = (output / "SparseUnitEngagementSystem.cs").read_text()
            self.assertIn("SourceSparseKeysEnabled", system)
            self.assertIn("Job.SourceSparseKeys", system)
            self.assertIn("RecordSparseUsedRow()", system)
            self.assertIn("RecordSparseFallbackRow()", system)
            self.assertIn("RecordSparseCellKey()", system)
            bridge = (output / "SparseUnitEngagementHostBridge.cs").read_text()
            self.assertIn('WARLINE_SOURCE_SPARSE_ACQUISITION\")==\"1', bridge)
            self.assertIn("BuildSortedUniqueIntKeyIndexForSourceCandidate", bridge)
            self.assertIn("RecordSparseSnapshot(unique,job.SourceSparseKeysEnabled)", bridge)
            self.assertIn("SourceSparseSnapshotBuilds", bridge)
            self.assertIn("SourceSparseSnapshotValid", bridge)
            self.assertIn("SourceSparseSnapshotInvalid", bridge)
            self.assertIn("SourceSparseTelemetry", bridge)
            self.assertIn("SourceSparseIncrement(ref long value)", bridge)
            self.assertIn("SourceSparseAdd(ref long value,long amount)", bridge)
            workload = (output / "SparsePaidSourceOwnerStep.cs").read_text()
            self.assertIn('["sparse_acquisition"]=UnitEngagementSystem.SourceSparseTelemetry', workload)
            self.assertIn('["enabled"]=false,["path_schedule_trace"]=ground.SourcePathScheduleTrace,["sparse_acquisition"]', workload)
        self.assertEqual(before, {name: (ROOT / name).read_bytes() for name in watched})

    def test_manifest_rewrite_is_explicit_and_default_manifest_stays_untouched(self):
        prepare = load(PREPARE, "sparse_acquisition_manifest")
        source = ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json"
        original = source.read_bytes()
        with tempfile.TemporaryDirectory() as temporary:
            destination = Path(temporary) / "sparse-manifest.json"
            prepare.prepare(source, destination)
            manifest = json.loads(destination.read_text())
            rows = {row["id"]: row for row in manifest["sources"] if row["id"] in prepare.OUTPUTS}
            self.assertEqual(set(rows), set(prepare.OUTPUTS))
            for identity, output in prepare.OUTPUTS.items():
                row = rows[identity]
                self.assertEqual(row["kind"], "current_original")
                self.assertEqual(row["generator_output"], output)
                self.assertEqual(row["generator"], prepare.GENERATOR)
            workload = rows["312_PaidSourceOwnerStep.retry.cs"]
            self.assertEqual(workload["generator_output"], "SparsePaidSourceOwnerStep.cs")
            self.assertEqual(workload["generator_inputs"], prepare.INPUTS)
        self.assertEqual(original, source.read_bytes())

    def test_generator_rejects_changed_kernel_anchor(self):
        generator = load(RECIPE, "sparse_acquisition_generator_rejection")
        source = (ROOT / generator.KERNEL).read_text()
        with self.assertRaisesRegex(ValueError, "readonly-context-interface"):
            generator._transform_kernel(source.replace("IReadOnlyTargetAcquisitionContext : ITargetAcquisitionContext", "IReadOnlyTargetAcquisitionContext : IDisposable", 1))

    def test_generated_telemetry_distinguishes_opt_in_use_and_original_fallback(self):
        generator = load(RECIPE, "sparse_acquisition_telemetry")
        system = (ROOT / generator.SYSTEM).read_text()
        bridge = (ROOT / generator.BRIDGE).read_text()
        transformed_system = generator._transform_system(system)
        transformed_bridge = generator._transform_bridge(bridge)
        # Off or invalid snapshots return false from the opt-in seam, so the
        # transformed kernel runs its unchanged original rectangle sweep.
        self.assertIn("UnitEngagementSystem.RecordSparseFallbackRow();returnfalse;", transformed_system.replace(" ", ""))
        self.assertIn("else SourceSparseIncrement(ref SourceSparseSnapshotInvalid)", transformed_bridge)
        self.assertIn("SourceSparseAcquisitionOptIn", transformed_bridge)
        # Only a valid sparse iterator start increments sparse-row usage;
        # each yielded occupied cell key is counted as it is consumed.
        self.assertIn("UnitEngagementSystem.RecordSparseUsedRow()", transformed_system)
        self.assertIn("UnitEngagementSystem.RecordSparseCellKey()", transformed_system)

    def test_generated_csharp_sparse_iterator_matches_original_cell_and_candidate_order(self):
        dotnet = shutil.which("dotnet")
        if dotnet is None:
            self.skipTest("dotnet is not installed")
        generator = load(RECIPE, "sparse_acquisition_csharp_parity")
        harness = Path(__file__).with_name("SparseCellParity.cs.txt").read_text()
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project = root / "project"
            project.mkdir()
            generated = root / "generated"
            generator.generate(ROOT, generated)
            (project / "TargetAcquisitionKernel.cs").write_text(
                (generated / "SparseTargetAcquisitionKernel.cs").read_text()
            )
            (project / "SparseCellParity.cs").write_text(harness)
            (project / "Parity.csproj").write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
                '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings>'
                '<Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>'
            )
            result = subprocess.run(
                [dotnet, "run", "--project", str(project / "Parity.csproj"), "--verbosity", "quiet"],
                capture_output=True,
                text=True,
                timeout=60,
                check=False,
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("[SparseCellParity] cases=750 passed=true", result.stdout)

    def test_original_and_experimental_csharp_acquisition_command_parity(self):
        dotnet = shutil.which("dotnet")
        if dotnet is None:
            self.skipTest("dotnet is not installed")
        generator = load(RECIPE, "sparse_acquisition_owner_parity")
        harness = Path(__file__).with_name("AcquisitionParity.cs.txt").read_text()
        common = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>'
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
            generated = root / "generated"
            generator.generate(ROOT, generated)
            outputs = []
            for name, kernel, interface, field, begin, next_cell in [
                ("original", ROOT / generator.KERNEL, "IReadOnlyTargetAcquisitionContext", "", "key=-1;return false;", "key=0;return false;"),
                ("experimental", generated / "SparseTargetAcquisitionKernel.cs", "ISparseTargetAcquisitionContext", "private SourceSparseCellIterator sparse;", "return sparse.Begin(keys,keys.Length,centerX,centerZ,range,width,height,out key);", "return sparse.Next(out key);"),
            ]:
                project = root / name
                project.mkdir()
                (project / "TargetAcquisitionKernel.cs").write_text(kernel.read_text())
                source = harness.replace("__CONTEXT_INTERFACE__", interface).replace("__SPARSE_FIELD__", field)
                source = source.replace("__SPARSE_BEGIN__", begin).replace("__SPARSE_NEXT__", next_cell)
                (project / "AcquisitionParity.cs").write_text(source)
                (project / "Parity.csproj").write_text(common)
                result = subprocess.run(
                    [dotnet, "run", "--project", str(project / "Parity.csproj"), "--verbosity", "quiet"],
                    capture_output=True,
                    text=True,
                    timeout=60,
                    check=False,
                )
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                outputs.append(result.stdout.strip())
            self.assertEqual(outputs[0], outputs[1])
            lines = outputs[0].splitlines()
            by_case = {line.split("|", 1)[0]: line for line in lines}
            self.assertEqual(by_case["mobile-tie"].split("|")[2], "21")
            self.assertEqual(by_case["building-tie"].split("|")[2], "30")
            self.assertEqual(by_case["building-tie"].split("|")[6], "0,1")
            self.assertEqual(by_case["mobile-before-building"].split("|")[6], "")
            self.assertEqual(by_case["manual-cancel"].split("|")[1:5], ["0", "0", "0", "1"])
            self.assertEqual(by_case["scan-command"].split("|")[2], "50")
            self.assertEqual(by_case["attack-move-command"].split("|")[2], "60")
            self.assertEqual(by_case["recent-retaliation"].split("|")[1:4], ["1", "70", "1"])


if __name__ == "__main__":
    unittest.main()
