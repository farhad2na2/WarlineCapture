import hashlib
import unittest

from finalize_s073_current_road_capture import validate


class CurrentRoadCaptureTests(unittest.TestCase):
    def setUp(self):
        self.surface = bytes(2048 * 1024); self.sidewalk = bytes(2048 * 1024)
        digest = hashlib.sha256(self.surface).hexdigest()
        rows = []
        for name,x,y,w,h in (("building_barrack",828,462,28,15),("building_ammunition_depot",867,462,19,16)):
            rows.append(f"[S073CurrentRoadMasks] faction=2 building={name} origin=int2({x}, {y}) rect=(x:{x}, y:{y}, width:{w}, height:{h}) cells=2097152 surfaceCells=0 sidewalkCells=0 roadOverlap=False waterOverlap=False surfaceSha={digest} sidewalkSha={digest} scope=OriginalPreSpawnPhysicalInputs_NotReadiness")
        self.log = "\n".join(rows + ["[S073PostStreamCensus] result=Passed ecsLoaded=1 ownedGrid=1 entities=4931"])
        self.census = {"status":"Passed","mission_ready":False,"post_stream":{"phase":"Playing","startup_failure":"None",
            "physical_source_validated":1,"entity_count":4931,
            "source_identity_hash":"2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900",
            "source_content_hash":"2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c"}}

    def test_complete_capture_and_nonzero_exit(self):
        self.assertEqual(validate(self.log,self.census,self.surface,self.sidewalk,0)["phase"],"Playing")
        with self.assertRaises(ValueError): validate(self.log,self.census,self.surface,self.sidewalk,1)

    def test_changed_mask_and_missing_epoch_rejected(self):
        changed = bytearray(self.surface); changed[462*2048+828]=1
        with self.assertRaises(ValueError): validate(self.log,self.census,changed,self.sidewalk,0)
        self.census["post_stream"]["startup_failure"] = "Timeout"
        with self.assertRaises(ValueError): validate(self.log,self.census,self.surface,self.sidewalk,0)


if __name__ == "__main__": unittest.main()
