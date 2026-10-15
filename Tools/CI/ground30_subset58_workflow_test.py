"""Execute only explicit subset draft guards; never import assets or launch Unity."""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import unittest

ROOT=Path(__file__).resolve().parents[2]
PACKAGE=ROOT/'Tools/CI/SkirmishProductionDrafts/Ground30Subset58'

class SubsetWorkflowTests(unittest.TestCase):
    def qualifier(self):
        path=PACKAGE/'Workflow/Tools/CI/qualify_skirmish_ground_objective30_collection.py'
        spec=importlib.util.spec_from_file_location('explicit_subset_qualifier',path)
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        return module

    def test_actual_draft_marker_guard_accepts_only_exact_checked_preparation(self):
        module=self.qualifier()
        marker=module.PROFILE_FREE_PREPARATION
        module.preparation_scope(marker+'\n[UnityInvoke] ExitCode: 0\n')
        for text in (marker+'\n[UnityInvoke] ExitCode: 1',marker+'\n'+marker+'\n[UnityInvoke] ExitCode: 0',marker+'\n'+module.PREPARATION+'\n[UnityInvoke] ExitCode: 0',marker+'\nCrash!!!\n[UnityInvoke] ExitCode: 0'):
            with self.subTest(text=text),self.assertRaises(ValueError):module.preparation_scope(text)

    def test_exact_source_map_and_all_artifacts_are_pinned(self):
        module=self.qualifier();path=ROOT/module.MAP
        self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),module.MAP_SHA)
        data=json.loads(path.read_text());self.assertEqual(len(data['files']),12)
        self.assertEqual(len({r['target'] for r in data['files']}),12)
        for row in data['files']:
            self.assertEqual(hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),row['sha256'])
            self.assertNotIn('Camera',row['target']);self.assertNotIn('MissionViewProfile',row['target'])
        for row in data['artifacts']:
            self.assertEqual(hashlib.sha256((ROOT/row['path']).read_bytes()).hexdigest(),row['sha256'])
        self.assertIn('ProjectSettings/ProjectVersion.txt',{r['path'] for r in data['artifacts']})
        self.assertIn('Packages/packages-lock.json',{r['path'] for r in data['artifacts']})

    def test_queued_compiler_and_workflow_parse_without_execution(self):
        for path in (ROOT/'Tools/CI/skirmish_head278_ground30_58_offline_compile.py',*list((PACKAGE/'Workflow/Tools/CI').glob('*.py'))):
            compile(path.read_text(),str(path),'exec')

if __name__=='__main__':unittest.main()
