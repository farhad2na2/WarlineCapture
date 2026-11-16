import importlib.util
from pathlib import Path
import unittest

P=Path(__file__).with_name("audit_s004_resource_cadence.py")
spec=importlib.util.spec_from_file_location("cadence_audit",P)
audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)

class CadenceControl(unittest.TestCase):
    @classmethod
    def setUpClass(cls): cls.source={k:(audit.ROOT/v).read_text() for k,v in audit.FILES.items()}
    def altered(self,key,old,new):
        values=dict(self.source);self.assertIn(old,values[key]);values[key]=values[key].replace(old,new);return values
    def test_real_authored_chain_is_conditional(self):
        result=audit.audit(self.source)
        self.assertEqual(result["conditional_seconds_per_day"],600)
        self.assertFalse(result["runtime_cadence_admitted"])
        self.assertFalse(result["managed_owner_initialized"])
    def test_wrong_config_reference_rejected(self):
        values=self.altered("scene","dayNightConfig: {fileID: 11400000, guid: b2010000000000000000000000000009, type: 2}","dayNightConfig: {fileID: 0}")
        with self.assertRaises(ValueError): audit.audit(values)
    def test_nonfinite_duration_rejected(self):
        with self.assertRaises(ValueError): audit.audit(self.altered("config","fullDayDurationMinutes: 10","fullDayDurationMinutes: nan"))
    def test_changed_original_owner_rejected(self):
        with self.assertRaises(ValueError): audit.audit(self.altered("resource_owner","context.DayNightSystem.FullDayDurationMinutes * 60f","1f"))
    def test_changed_valid_duration_is_read_from_source(self):
        result=audit.audit(self.altered("config","fullDayDurationMinutes: 10","fullDayDurationMinutes: 12"))
        self.assertEqual(result["conditional_seconds_per_day"],720)
if __name__=="__main__":unittest.main()
