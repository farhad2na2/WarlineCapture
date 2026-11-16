import sys
import tempfile
import unittest
from pathlib import Path


RECIPE_DIR = Path(__file__).resolve().parents[1] / "Recipes"
sys.path.insert(0, str(RECIPE_DIR))
import regenerate_construction as recipe


class RegenerateConstructionTests(unittest.TestCase):
    def test_factory_cleanup_survives_source_extraction(self):
        repo = Path(__file__).resolve().parents[4]
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp)
            recipe.generate(repo, output)
            generated = (output / "BuildingRuntimeEntityFactory.source.cs").read_text()

        self.assertEqual(generated.count("DestroyEntityAfterFactoryFailure(em, entity);"), 4)
        self.assertEqual(generated.count("cleanup = () => DestroyEntityAfterFactoryFailure(em, entity);"), 2)
        self.assertIn("private static void DestroyEntityAfterFactoryFailure(EntityManager em, Entity entity)", generated)
        self.assertIn("if (em.Exists(entity))\n                    em.DestroyEntity(entity);", generated)
        self.assertIn("catch\n            {\n                // Cleanup must not replace the factory's original exception.\n            }", generated)
        self.assertEqual(generated.count("catch\n            {\n                DestroyEntityAfterFactoryFailure(em, entity);\n                throw;\n            }"), 2)


if __name__ == "__main__":
    unittest.main()
