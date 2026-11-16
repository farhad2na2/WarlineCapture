import copy
import unittest

from compare_s073_host_neutral_scenery import compare


class HostNeutralSceneryTests(unittest.TestCase):
    def setUp(self):
        rows=[{"stable_id":str(i),"source_global_object_id":str(i),"placement_index":i,"faction":0,"ai":False,"combat_owner":0,"health_current":100} for i in range(4931)]
        self.native=({"post_stream":{"entities":rows}},"native")
        self.nr={"validation_status":"Passed","wrapper_exit_code":0,"required_marker_present":True,"census_sha256":"native"}
        self.identity={"status":"IdentitySetsMatch","native_sha256":"native","source_sha256":"source"}
        self.host=({"status":"PreparingPhysicalSourceRequired","mission_ready":False,"experimental_physical_blocker_projection":{"neutral_scenery_readback":{"sha256":"scenery","count":4931}}},"host")
        self.hr={"worker_result":{"sha256":"host"},"process_exit":2}
        self.scenery=({"kind":"S073SourceNeutralSceneryReadback","source_capture_sha256":"source","normalization_owner":"SkirmishWorldSetup.NormalizeScenery","campaign_runtime_state_copied":False,"complete_gameplay_payload":False,"mission_ready":False,"entities":copy.deepcopy(rows)},"scenery")

    def run_comparison(self): return compare(self.native,self.nr,self.identity,self.host,self.hr,self.scenery)

    def test_matching_and_health_difference(self):
        self.assertEqual(self.run_comparison()["status"],"ObservedFieldsMatch")
        self.scenery[0]["entities"][7]["health_current"]=99
        self.assertEqual(self.run_comparison()["field_difference_counts"]["health_current"],1)

    def test_rejects_state_import_or_stale_receipt(self):
        self.scenery[0]["campaign_runtime_state_copied"]=True
        with self.assertRaises(ValueError):self.run_comparison()
        self.scenery[0]["campaign_runtime_state_copied"]=False;self.nr["census_sha256"]="wrong"
        with self.assertRaises(ValueError):self.run_comparison()


if __name__=="__main__":unittest.main()
