"""Source and retained-input controls for the S073 placement-mask load A/B."""
import hashlib
import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[4]
HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyHostHandoff.cs"
NORMAL_RUN = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs"
DOMAIN = ROOT / "Tools/CI/SkirmishSourceSimulation/Placement/SourcePlacementDomain.OriginalOwnerCapture.cs"
BUNDLE = ROOT / "Logs/SkirmishProduction/s073-shared-grid-frame-binding-20261102-r4/diagnostic-bundle/s073-current-source-diagnostic-bundle.json"


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


class S073PlacementMaskLoadModeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.helper = HELPER.read_text(encoding="utf-8")
        cls.normal_run = NORMAL_RUN.read_text(encoding="utf-8")
        cls.domain = DOMAIN.read_text(encoding="utf-8")
        cls.bundle = json.loads(BUNDLE.read_text(encoding="utf-8"))

    def test_default_is_reference_and_mode_override_fails_closed(self):
        loader = self.helper[self.helper.index("internal static SourcePlacementDomain LoadSharedPlacementSource("):]
        loader = loader[:loader.index("internal static int Queue(")]
        self.assertIn('Environment.GetEnvironmentVariable("WARLINE_S073_PLACEMENT_MASK_LOAD_VALIDATION")', loader)
        self.assertIn('if(string.IsNullOrEmpty(validationMode))validationMode="reference";', loader)
        self.assertIn('validationMode!="optimized"&&validationMode!="reference"', loader)
        self.assertIn('throw new InvalidDataException("S073 placement mask load validation mode must be optimized or reference")', loader)
        self.assertLess(loader.index('validationMode!="optimized"'), loader.index('if(validationMode=="reference")'))
        self.assertIn('[S073PlacementMaskLoadValidation] mode=', loader)

    def test_reference_and_optimized_paths_keep_freshness_and_input_contracts(self):
        loader = self.helper[self.helper.index("internal static SourcePlacementDomain LoadSharedPlacementSource("):]
        loader = loader[:loader.index("internal static int Queue(")]
        self.assertIn("SourcePlacementDomain.LoadS002SourceDiagnostic(", loader)
        self.assertIn("domain.VerifyOriginalOwnerAssetPins();", loader)
        self.assertIn("domain.UsesOriginalOwnerMaskCapture", loader)
        self.assertIn("domain.WaterDomainKnown", loader)
        self.assertIn("domain.WaterFootprintCount!=194", loader)
        self.assertIn("domain.CopyOriginalOwnerRoadMask().Length!=cells", loader)
        self.assertIn('if(validationMode=="reference")', loader)
        self.assertIn("domain.Build(2048,1024,1,0,0,0)", loader)
        self.assertIn("domain.BuildWaterMask(true,2048,1024,1,0,0,0)", loader)
        self.assertIn("masks.AuthoredBuildings.Length!=cells", loader)
        self.assertIn("masks.Sidewalks.Length!=cells", loader)
        self.assertIn("activeWater==null||activeWater.Length!=cells", loader)

        # The optimized load returns the same domain; the actual S073 occupancy
        # binding and final products remain on the caller's unchanged path.
        call = self.normal_run.index("SourceS073LegacySpawnRequestAdapter.LoadSharedPlacementSource(")
        binding = self.normal_run.index("domain.BindValidatedS073NativeAuthoredBuildingMask(", call)
        build = self.normal_run.index("var placementMasks = domain.Build(", binding)
        water = self.normal_run.index("var waterMask = domain.BuildWaterMask(", build)
        self.assertLess(call, binding)
        self.assertLess(binding, build)
        self.assertLess(build, water)
        self.assertIn("placementMasks.Sidewalks[index]", self.normal_run)
        self.assertIn("domain.CopyOriginalOwnerRoadMask()", self.normal_run)
        self.assertIn("capturedSidewalks = File.ReadAllBytes(sidewalkPath)", self.normal_run)
        self.assertIn("placementMasks.Sidewalks[i] |= capturedSidewalks[i] != 0", self.normal_run)
        self.assertIn("placementMasks.Sidewalks, waterMask", self.normal_run)

    def test_pin_and_cardinality_guards_are_backed_by_the_real_retained_inputs(self):
        self.assertEqual(self.bundle["mission"], "S073")
        expected = {
            "authored_mask": "b4c979394eb734ccd978b30896303dbb1c1ac058e7fc3eaef5da3fa0cb98c8b8",
            "campaign_physical": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
            "campaign_water": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
            "campaign_startup": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
        }
        for name, digest in expected.items():
            record = self.bundle[name]
            self.assertEqual(record["sha256"], digest)
            path = Path(record["path"])
            self.assertTrue(path.is_file(), name)
            self.assertEqual(sha256_file(path), digest, name)

        mask = Path(self.bundle["authored_mask"]["path"]).read_bytes()
        self.assertEqual(len(mask), 2048 * 1024)
        self.assertTrue(all(value in (0, 1) for value in mask))
        self.assertEqual(sum(mask), 376518)

        # The loader rejects malformed/partial captured masks before the
        # optimization can skip eager raster materialization.
        self.assertIn('const int count=2048*1024;', self.domain)
        self.assertIn('row["actualLength"]!=count', self.domain)
        self.assertIn('row["expectedLength"]!=count', self.domain)
        self.assertIn('row["byteCount"]!=count', self.domain)
        self.assertIn('bytes.Length!=count||bytes.Any(v=>v>1)', self.domain)
        self.assertIn('rows?.Count!=194', self.domain)
        self.assertIn("capturedSourcePinsVerified", self.domain)
        self.assertIn("HashFile(pin.Key,512*1024*1024)", self.domain)


if __name__ == "__main__":
    unittest.main()
