import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[4]
GENERATOR_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/availability/regenerate_availability.py"
SPEC = importlib.util.spec_from_file_location("s073_availability_generator", GENERATOR_PATH)
GENERATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GENERATOR)


class S073LegacyPopulationPolicyExtractionTests(unittest.TestCase):
    def test_native_source_producer_selector_is_extracted_without_a_second_rule(self):
        repo = ROOT
        recipe_dir = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/availability"
        manifest = json.loads((recipe_dir / "recipes.json").read_text(encoding="utf-8"))
        members = next(file["members"] for file in manifest if file["output"] == "AvailabilityKernel.original.cs")
        candidate_spec = next(spec for spec in members if spec["member"] == "UnitProducerCandidate")
        selector_spec = next(spec for spec in members if spec["member"] == "SelectUnitProducerCandidate")
        source = (repo / selector_spec["source"]).read_text(encoding="utf-8")
        native_selector = GENERATOR.member(source, selector_spec["member"], selector_spec.get("signature"))

        with tempfile.TemporaryDirectory(prefix="unit-producer-policy-extraction-") as temp:
            generated = GENERATOR.generate(repo, Path(temp))
            output = (Path(temp) / "AvailabilityKernel.original.cs").read_text(encoding="utf-8")
            source_selector = GENERATOR.member(output, selector_spec["member"], selector_spec.get("signature"))
            candidate_source = (repo / candidate_spec["source"]).read_text(encoding="utf-8")
            native_candidate = GENERATOR.declaration(candidate_source, "struct", candidate_spec["member"])
            source_candidate = GENERATOR.declaration(output, "struct", candidate_spec["member"])

        self.assertEqual(source_selector, native_selector)
        self.assertEqual(source_candidate, native_candidate)
        self.assertTrue(any(row["member"] == "SelectUnitProducerCandidate" for row in generated["members"]))

    def test_legacy_population_tail_matches_native_source_with_only_declared_bindings(self):
        repo = ROOT
        recipe_dir = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/availability"
        manifest = json.loads((recipe_dir / "recipes.json").read_text(encoding="utf-8"))
        target = next(
            spec
            for output in manifest
            for spec in output["members"]
            if spec["id"] == "member26"
        )
        self.assertTrue(target["preserve_source_tail"])

        source = (repo / target["source"]).read_text(encoding="utf-8")
        native_member = GENERATOR.member(source, target["member"], target.get("signature"))
        expected = GENERATOR.apply(native_member, target)

        with tempfile.TemporaryDirectory(prefix="s073-population-extraction-") as temp:
            generated = GENERATOR.generate(repo, Path(temp))
            output_path = Path(temp) / "PopulationAvailability.SourcePrefix.cs"
            output = output_path.read_text(encoding="utf-8")
            extracted_member = GENERATOR.member(
                output, "CanQueue", "public static"
            )

        self.assertEqual(extracted_member, expected)
        self.assertIn("SourceCampCatalogPolicy.Allows(em,prefab,false)", extracted_member)
        self.assertIn("SourceCampNativeProduction.CanQueue(em, expanded, producer, prefab)", extracted_member)
        self.assertIn("int limit=key.Contains(\"soldier\")?SkirmishPresetConfig.InfantryLimitPerFaction", extracted_member)
        self.assertIn("if(limit==0)return false;", extracted_member)
        self.assertIn("return count+Mathf.Max(1,quantity)<=limit;", extracted_member)
        self.assertTrue(any(
            item["member"] == "CanQueue"
            and item["source_prefix"] is True
            and item["bindings"] == target["replace"]
            for item in generated["members"]
        ))


if __name__ == "__main__":
    unittest.main()
