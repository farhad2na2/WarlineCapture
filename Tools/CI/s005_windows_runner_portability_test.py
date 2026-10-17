"""Exercise the real Windows PowerShell runner in plan mode; never launch Unity."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

import skirmish_production_queue as queue


class WindowsRunnerPortability(unittest.TestCase):
    def setUp(self):
        self.repository=Path(__file__).resolve().parents[2]
        fixtures=self.repository/"Temp/S005WindowsRunnerPortability"
        fixtures.mkdir(parents=True,exist_ok=True)
        self.temp=tempfile.TemporaryDirectory(prefix="canary spaces-",dir=fixtures)
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.prepared=self.root/"prepared فارسی"
        self.prepared.mkdir()
        self.observer=self.prepared/"observer نمونه.cs.txt"
        self.observer.write_bytes(b"abc")
        self.sha="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"
        self.preparation=dict(runId="1632",candidate=self.observer.name,candidateSha256=self.sha,nativeIdentityFieldsCaptured=True)
        # Windows PowerShell must correctly decode BOM and non-ASCII file paths.
        self.write_preparation()
        (self.root/"source.txt").write_bytes(b"synthetic source")
        (self.root/"packet.md").write_text("## S005\nRegular seeds `104734`, `130368`, `155926` in both EN and FA at Standard.\n## S006\n",encoding="utf-8")
        self.job=dict(schema_version=1,job_id="S005-portability-synthetic",catalog_id="S005",definition_id="skirmish.s005",
            definition_version=1,candidate_sha256="a"*64,build_sha256="b"*64,setup_sha256="c"*64,content_sha256="d"*64,
            source_manifest=[dict(path="source.txt",sha256=queue.digest(self.root/"source.txt"))],
            packet_path="packet.md",packet_sha256=queue.digest(self.root/"packet.md"),seed=155926,locale="FA",
            size="Standard",difficulty="Regular",control_mode="manual",required_stages=sorted(queue.STAGES),timeout_seconds=2100)
        self.job_file=self.root/"job.json"
        self.job_file.write_text(json.dumps(self.job),encoding="utf-8")
        self.claim=dict(attempt_id="attempt-synthetic",token="synthetic",worker="offline-worker",job_sha256=queue.fingerprint(self.job),
                        artifact_directory=str(self.root/"never created native artifacts"))
        self.claim_file=self.root/"claim.json"
        self.claim_file.write_text(json.dumps(self.claim),encoding="utf-8-sig")

    def write_preparation(self):
        (self.prepared/"PREPARATION.json").write_text(json.dumps(self.preparation,ensure_ascii=False),encoding="utf-8-sig")

    def plan(self):
        return subprocess.run(["rtk","proxy","powershell.exe","-NoProfile","-ExecutionPolicy","Bypass","-File",
            str(self.repository/"Tools/CI/InvokeS005WindowsNorthTrial.ps1"),"-UnityExe","NOT_LAUNCHED_Unity.exe",
            "-ProjectPath",str(self.root),"-JobFile",str(self.job_file),"-ClaimFile",str(self.claim_file),
            "-PreparedDirectory",str(self.prepared),"-InstalledObserver",str(self.root/"not-installed.cs"),
            "-RunDirectory",self.claim["artifact_directory"]],cwd=self.repository,
            stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding="utf-8",errors="replace",timeout=60)

    def test_real_plan_uses_dotnet_known_sha_and_bom_unicode_paths(self):
        result=self.plan()
        self.assertEqual(result.returncode,0,result.stderr+result.stdout)
        start=result.stdout.rfind("\n{")+1
        plan=json.loads(result.stdout[start:])
        self.assertEqual(plan["result"],"PreparedPlanOnly")
        self.assertEqual(plan["candidateSha256"],self.sha)
        self.assertFalse(plan["dispatchEnabled"])
        self.assertFalse(Path(self.claim["artifact_directory"]).exists())
        self.assertFalse((self.root/"Assets").exists())

    def test_binary_file_hash_and_candidate_tamper(self):
        binary=bytes(range(256))*257+b"\x00\xff\r\n"
        self.observer.write_bytes(binary)
        self.preparation["candidateSha256"]=hashlib.sha256(binary).hexdigest()
        self.write_preparation()
        result=self.plan()
        self.assertEqual(result.returncode,0,result.stderr+result.stdout)
        self.assertIn(self.preparation["candidateSha256"],result.stdout)
        self.observer.write_bytes(binary+b"changed")
        rejected=self.plan()
        self.assertNotEqual(rejected.returncode,0)
        self.assertIn("Prepared observer bytes changed",rejected.stderr+rejected.stdout)

    def test_shared_source_preflight_still_rejects_changes(self):
        (self.root/"source.txt").write_bytes(b"changed source")
        result=self.plan()
        self.assertNotEqual(result.returncode,0)
        self.assertIn("Shared pinned-job preflight failed",result.stderr+result.stdout)

    def test_utf8_bom_job_rejected_by_existing_strict_queue(self):
        # The shared queue contract currently accepts UTF8 without BOM only.
        # Do not weaken or rewrite its job bytes silently in a platform adapter.
        self.job_file.write_text(json.dumps(self.job),encoding="utf-8-sig")
        result=self.plan()
        self.assertNotEqual(result.returncode,0)
        self.assertIn("Shared pinned-job preflight failed",result.stderr+result.stdout)


if __name__=="__main__":
    unittest.main(verbosity=2)
