import shutil
import tempfile
import unittest
from pathlib import Path
import audit_s004_airself_authored_transform_chain as audit


class ChainControl(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        for path in audit.PATHS:
            for suffix in ("", ".meta"):
                target = self.root / (path + suffix)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(audit.ROOT / (path + suffix), target)

    def tearDown(self):
        self.tmp.cleanup()

    def replace(self, path, old, new):
        target = self.root / path
        data = target.read_text()
        self.assertIn(old, data)
        target.write_text(data.replace(old, new, 1))

    def test_actual_serialized_chain(self):
        report = audit.inventory(self.root)
        self.assertEqual(report["added_rotor_names"], ["Blades_Back_X", "Blades_Main_Y"])
        self.assertFalse(report["runtime_template_admitted"])

    def test_wrong_asset_guid(self):
        self.replace(audit.PATHS[2] + ".meta", audit.GUIDS[2], "0" * 32)
        with self.assertRaises(ValueError): audit.inventory(self.root)

    def test_wrong_inherited_prefab(self):
        self.replace(audit.PATHS[1], "m_SourcePrefab: {fileID: 100100000, guid: " + audit.GUIDS[0],
                     "m_SourcePrefab: {fileID: 100100000, guid: " + "0" * 32)
        with self.assertRaises(ValueError): audit.inventory(self.root)

    def test_missing_rotor(self):
        self.replace(audit.PATHS[2], "m_Name: Blades_Main_Y", "m_Name: Blades_Main")
        with self.assertRaises(ValueError): audit.inventory(self.root)

    def test_missing_model_parent(self):
        self.replace(audit.PATHS[2], "m_Father: {fileID: 5976052629555373990}", "m_Father: {fileID: 99}")
        with self.assertRaises(ValueError): audit.inventory(self.root)


if __name__ == "__main__": unittest.main()
