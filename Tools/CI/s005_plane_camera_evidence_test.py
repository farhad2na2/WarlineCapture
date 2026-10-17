import copy
import struct
import tempfile
import unittest
import zlib
import hashlib
from pathlib import Path
from s005_plane_camera_evidence import inspect


class CameraEvidenceControls(unittest.TestCase):
    def setUp(self):
        parent=Path(__file__).resolve().parents[2]/'Temp/S005PlaneCameraControls'
        parent.mkdir(parents=True,exist_ok=True)
        self.temp=tempfile.TemporaryDirectory(dir=parent)
        self.addCleanup(self.temp.cleanup)
        def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data))
        png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',1,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\0\0\0\0'))+chunk(b'IEND',b'')
        path=Path(self.temp.name)/'synthetic.png';path.write_bytes(png)
        base=dict(attempt='synthetic',png=str(path),pngSha256=hashlib.sha256(png).hexdigest(),scope='RawCameraMeasurementOnly',cameraAccepted=False,fullMissionCertified=False,failure='')
        self.rows=[dict(base,phase='opening-infantry',frame=10,infantryCount=1,paidReservation=0,
                       measurement=dict(SessionId='synthetic',Frame=10,EntityIndex=20,EntityVersion=1,StableActorId='initial.infantry.1',NativeCategory='Infantry',Valid=False,Failure='NativeVisualIsFarImpostorOrUnknown')),
                   dict(base,phase='paid-transport',frame=200,infantryCount=0,paidReservation=1003,
                       measurement=dict(SessionId='synthetic',Frame=200,EntityIndex=90,EntityVersion=3,StableActorId='production.1003.0',NativeRole='TransportPlane',Valid=True,Failure=''))]
        self.lineage=[dict(stage='PaidPlaneDeliveredAtHome',attempt='synthetic',frame=201,actorIndex=90,actorVersion=3,stable='production.1003.0',reservation=1003)]

    def test_invalid_far_is_preserved_as_calibration_never_acceptance(self):
        result=inspect(self.rows,self.lineage)
        self.assertTrue(result['evidenceCaptured'],result)
        self.assertFalse(result['cameraAccepted'])
        self.assertFalse(result['playerReady'])

    def test_corrupt_joins_reject(self):
        mutations=[(0,'attempt','foreign'),(0,'frame',11),(0,'infantryCount',2),(0,'cameraAccepted',True),
                   (1,'paidReservation',999),(1,'fullMissionCertified',True),(1,'pngSha256','0'*64),
                   (1,'png',str(Path(self.temp.name)/'missing.png'))]
        for index,key,value in mutations:
            with self.subTest(key=key):
                rows=copy.deepcopy(self.rows);rows[index][key]=value
                self.assertFalse(inspect(rows,self.lineage)['evidenceCaptured'])
        for key,value in [('EntityIndex',999),('EntityVersion',4),('StableActorId','foreign'),('SessionId','foreign'),('Frame',199),('NativeRole','Strike')]:
            rows=copy.deepcopy(self.rows);rows[1]['measurement'][key]=value
            self.assertFalse(inspect(rows,self.lineage)['evidenceCaptured'],key)
        self.assertFalse(inspect(self.rows[1:],self.lineage)['evidenceCaptured'])

    def test_grounded_v3_never_becomes_cruise_proof(self):
        rows=copy.deepcopy(self.rows)
        rows[1].update(aircraftState='Grounded',actorTransformSource='UnparentedLocalTransform',airborne=0,takeoffRolling=0,landingRolling=0,returningHome=0,
                       nativeCruiseY=55,actorY=0,cameraY=65,verticalCameraSeparation=65)
        result=inspect(rows,self.lineage)
        self.assertTrue(result['evidenceCaptured'],result)
        self.assertFalse(result['cameraAccepted'])
        rows[1]['actorTransformSource']='ParentLocalTransform'
        self.assertFalse(inspect(rows,self.lineage)['evidenceCaptured'])
        rows[1]['actorTransformSource']='UnparentedLocalTransform'
        rows[1].update(aircraftState='Airborne',airborne=1,actorY=55,verticalCameraSeparation=10)
        self.assertFalse(inspect(rows,self.lineage)['evidenceCaptured'])

if __name__=='__main__':unittest.main()
