"""Static controls for the separately pinned, one-mission source diagnostic."""
import hashlib
import json
from pathlib import Path
import re
import sys
import unittest

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/CI'))
import skirmish_ground_objective30_contract as contract

CS=ROOT/'Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.GroundObjective30.MemoryDiagnostic.cs'
CONTROL=ROOT/'Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.All120RefinementControls.cs'

class CurrentGridMissionDiagnosticControls(unittest.TestCase):
    def test_current_grid_envelope_and_pair_are_exact_and_unqualified(self):
        controls=CONTROL.read_text()
        envelope=re.search(r'CurrentGridBytesGround30="([^"]+)"',controls).group(1)
        expected=re.search(r'CurrentGridBytesGround30Sha="([0-9a-f]{64})"',controls).group(1)
        data=ROOT/envelope
        self.assertEqual(expected,hashlib.sha256(data.read_bytes()).hexdigest())
        document=json.loads(data.read_text())
        result=contract.validate(document,ROOT)
        self.assertEqual((30,90,4),(result['missions'],result['canonical_size_vectors'],result['source_worlds_planned']))
        self.assertFalse(result['source_fit_certified'])
        self.assertFalse(result['publication_enabled'])
        self.assertIn('CurrentGridBytesPhysicalGridSha',controls)
        self.assertIn('sharedCityRows.All',controls)
        grid_path=re.search(r'CurrentGridBytesPhysicalGrid="([^"]+)"',controls).group(1)
        grid_sha=re.search(r'CurrentGridBytesPhysicalGridSha="([0-9a-f]{64})"',controls).group(1)
        self.assertEqual(grid_sha,hashlib.sha256((ROOT/grid_path).read_bytes()).hexdigest())
        self.assertNotEqual(expected,hashlib.sha256(data.read_bytes()+b'changed').hexdigest())

    def test_entrypoint_is_separate_full_envelope_validated_single_mission_and_unqualified(self):
        source=CS.read_text()
        method=source.split('public static void RunGroundObjective30CurrentGridMissionSourceDiagnostic()',1)[1].split('\n        // Explicit owned diagnostic only.',1)[0]
        for required in ('RunCurrentGridBytesAuthoringControls("ground30-one-mission-source-only"',
                         'ReadGround30Envelope()', 'Single(m=>(string)m["catalog_id"]==id)',
                         'CollectGround30Source(new[]{mission},sourceRow)', 'rows.Length==1',
                         'row["error"]!=null', 'size["error"]!=null', 'collectionErrors==0',
                         '"sourceFitCertified"]=false', '"playerReady"]=false',
                         '"normalMissionReadiness"]="Pending"', '"sizes"]).Count==3', 'Ground30SceneOwnership()',
                         'CaptureMaterializationWorldCensus()', 'FileMode.CreateNew',
                         'CollectedUnqualified'):
            self.assertIn(required,method)
        self.assertNotIn('RunGroundObjective30QualificationCollection()',method)
        self.assertNotIn('normalCompilerPassed',method)
        self.assertLess(method.index('Ground30Ids.Contains(id)'),method.index('RunCurrentGridBytesAuthoringControls('))
        self.assertLess(method.index('CollectGround30Source('),method.index('report["result"]="CollectedUnqualified"'))
        self.assertLess(method.index('});'),method.index('result=CollectedUnqualified mission='))
        self.assertLess(method.index('Require(restored'),method.index('result=CollectedUnqualified mission='))
        reset=CONTROL.read_text().split('static void RunCurrentGridBytesAuthoringControls',1)[1].split('public static void RunAll120RefinementAuthoringControls()',1)[0]
        self.assertIn('Hash(SelectedGroundObjective30Inputs)==SelectedGroundObjective30InputsSha',reset)
        self.assertIn('finally { authoringInputRevisionOverride=null; }',reset)
        full30=(ROOT/'Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.GroundObjective30.cs').read_text()
        self.assertIn('rows.Length==30',full30)
        self.assertIn('Count)==90',full30)
        self.assertIn('result=Collected missions=30 sizeRows=90 physicalSources=4',full30)

if __name__=='__main__':unittest.main()
