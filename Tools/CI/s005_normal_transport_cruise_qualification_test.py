"""Synthetic negative controls only; no native/gameplay claim."""
import copy
import json
import unittest
from pathlib import Path
from s005_paid_plane_lineage_qualifier_test import fixture as lineage_fixture
from s005_plane_camera_evidence_test import CameraEvidenceControls
from s005_normal_transport_cruise_qualification import qualify,MARKER,OPENING


class CruiseControls(unittest.TestCase):
    def setUp(self):
        self.camera_fixture=CameraEvidenceControls();self.camera_fixture.setUp();self.addCleanup(self.camera_fixture.doCleanups)
        self.lineage,self.contacts,self.log=lineage_fixture();self.log+='\n'+MARKER
        base=dict(attempt='synthetic-only',reservation=1003,actorIndex=90,actorVersion=3,stable='production.1003.0',timeScale=1,normalBudget=True,fullMissionCertified=False,
            fuelScope='FactionPooledFuel',aircraftFuelSpendAttributed=False,
            transformSource='UnparentedLocalTransform',anchor='anchor.skirmish.db.staging_player',goalX=30,goalZ=20,device=1,generation=1,completed=2,dispatched=2,
            fuel=200,fuelBefore=200,airborne=0,takeoff=0,landing=0,returning=0)
        plan=dict(base,stage='FriendlyFlightPlan',frame=210,admissionFuel=20,planningFuel=60,layoutId='layout.skirmish.db.ba',layoutContentHash='synthetic',padId='pad.skirmish.db.staging_player',padKind='GroundStaging',padFaction=1)
        plan.update(campaignSourceHash='synthetic-source',bindingSourceHash='synthetic-source')
        touch=dict(base,stage='PublicFriendlyMoveTouch',frame=220,gesture=3,dispatched=3,baseline=40,touchFrame=220)
        accepted=dict(touch,stage='NativeFriendlyMoveAccepted',frame=222,completed=3,receipt=9,request=41,receiptFrame=222,verified=True,issued=True,
            nativeResultKind='GroupedManual',resultAirUnits=1,resultRejection=0,resultActorIndex=90,resultActorVersion=3,resultGoalX=30,resultGoalZ=20)
        rolling=dict(base,stage='CruiseNativeSample',frame=223,takeoff=1,completed=3,dispatched=3)
        follow=dict(base,stage='PublicPlaneFollowTouch',frame=225,followGesture=4,completed=3,dispatched=4)
        stable=[dict(base,stage='StableNativeCruise',frame=frame,airborne=1,cruiseInitialized=1,followEnabled=1,panLocked=1,fuel=180,actorY=55,cruiseY=55,completed=4,dispatched=4) for frame in range(230,245)]
        done=dict(stable[-1],stage='CruiseCameraScopedComplete',frame=245)
        parked=dict(base,stage='ParkedWorldBaseline',frame=200,actorX=0,actorZ=0)
        for index,row in enumerate(stable):row.update(actorX=10+index,actorZ=0,parkedX=0,parkedZ=0,horizontalFromParked=10+index,horizontalStep=1,
            consumptionEnabled=1,consumptionInitialized=1,airFuelPerCell=.1,consumptionCellX=10+index,consumptionCellZ=0)
        self.raw=[parked,plan,touch,accepted,rolling,follow]+stable+[done]
        for dispatched,frame in ((3,220),(4,225)):
            for index,kind in enumerate(('Dispatched','Began','Ended')):
                self.contacts.append(dict(device=1,generation=1,dispatched=dispatched,completed=dispatched if kind=='Ended' else dispatched-1,
                    kind=kind,phase='PlayerResume',commandAuditActive=True,frame=frame+(index==2)))
        self.commands=[dict(operation='Move',receipt=9,verified=True,frame=222)]
        self.camera=copy.deepcopy(self.camera_fixture.rows)
        for row in self.camera:row['attempt']=row['measurement']['SessionId']='synthetic-only'
        self.camera[1]['frame']=self.camera[1]['measurement']['Frame']=199
        for index,phase in enumerate(OPENING):
            opening=copy.deepcopy(self.camera[0]);opening['phase']=phase;opening['frame']=opening['measurement']['Frame']=10+index
            self.camera.append(opening)
        flight=copy.deepcopy(self.camera[1]);flight.update(phase='cruising-transport',frame=244,aircraftState='Airborne',actorTransformSource='UnparentedLocalTransform',airborne=1,
            takeoffRolling=0,landingRolling=0,returningHome=0,cruiseYInitialized=1)
        flight['measurement'].update(Frame=244,ActorPosition=dict(x=24,y=55,z=0),CameraPixels=dict(x=0,y=0,width=1280,height=720),ProjectedPixels=dict(x=300,y=200,width=400,height=200))
        path=Path(self.camera_fixture.temp.name)/'synthetic-flight.png';path.write_bytes(Path(flight['png']).read_bytes());flight['png']=str(path)
        self.camera.append(flight)
        for row in stable:
            row['framing']=copy.deepcopy(flight['measurement']);row['framing']['Frame']=row['frame']
            row['framing']['ActorPosition']=dict(x=row['actorX'],y=row['actorY'],z=row['actorZ'])
        # New parked Follow/Exit lifecycle precedes the original synthetic flight.
        for row in self.raw:
            if row['stage'] not in ('ParkedWorldBaseline','FriendlyFlightPlan'):
                row['frame']+=100
                if row.get('receiptFrame'):row['receiptFrame']+=100
                if row.get('touchFrame'):row['touchFrame']+=100
                for field in ('gesture','followGesture','completed','dispatched'):
                    if row.get(field,0)>2:row[field]+=2
                if row.get('framing'):row['framing']['Frame']=row['frame']
        self.commands[0]['frame']+=100
        flight['frame']+=100;flight['measurement']['Frame']+=100
        pose=dict(cameraPosition=dict(x=0,y=44,z=0),cameraRotation=dict(x=0,y=0,z=0,w=1),cameraFieldOfView=46,
                  cameraOrthoSize=10,cameraOrthographic=False,zoomControlsObserved=True,zoomInInteractable=True,zoomOutInteractable=False)
        parked_touch=dict(base,**pose,stage='PublicParkedFollowTouch',frame=212,parkedFollowGesture=3,completed=2,dispatched=3,actorX=0,actorY=0,actorZ=0)
        entered=dict(parked_touch,stage='NativeParkedFollowEntered',frame=214,completed=3,nativeRestoreValid=1,nativeFollowEnteredFrame=214,
            followEnabled=1,panLocked=1,nativeRestorePosition=pose['cameraPosition'],nativeRestoreRotation=pose['cameraRotation'],
            nativeRestoreFov=46,nativeRestoreSize=10,nativeRestoreOrthographic=False)
        parked_stable=[]
        for frame in range(215,230):
            row=dict(entered,stage='ParkedFollowStable',frame=frame,airborne=0,takeoff=0,landing=0,returning=0,horizontalFromParked=0,cameraPosition=dict(x=0,y=100,z=0),cameraFieldOfView=60)
            row['framing']=copy.deepcopy(flight['measurement']);row['framing'].update(Frame=frame,ActorPosition=dict(x=0,y=0,z=0))
            parked_stable.append(row)
        exit_touch=dict(parked_stable[-1],stage='PublicParkedExitTouch',frame=230,parkedExitGesture=4,dispatched=4)
        restored=dict(base,**pose,stage='ParkedFollowExitedRestored',frame=232,completed=4,dispatched=4,followEnabled=0,panLocked=0)
        self.raw.extend([parked_touch,entered]+parked_stable+[exit_touch,restored]);self.raw.sort(key=lambda row:row['frame'])
        self.indices=[next(i for i,row in enumerate(self.raw) if row['stage']==name) for name in ('FriendlyFlightPlan','PublicFriendlyMoveTouch','NativeFriendlyMoveAccepted','CruiseNativeSample','PublicPlaneFollowTouch','StableNativeCruise')]
        parked_image=copy.deepcopy(flight);parked_image.update(phase='parked-follow-transport',frame=229,aircraftState='Grounded',airborne=0)
        parked_image['measurement']=copy.deepcopy(parked_stable[-1]['framing'])
        parked_path=Path(self.camera_fixture.temp.name)/'synthetic-parked-follow.png';parked_path.write_bytes(Path(flight['png']).read_bytes());parked_image['png']=str(parked_path)
        self.camera.insert(-1,parked_image)
        self.contacts=[row for row in self.contacts if row['dispatched']<=2]
        for dispatched,frame in ((3,212),(4,230),(5,320),(6,325)):
            for index,kind in enumerate(('Dispatched','Began','Ended')):
                self.contacts.append(dict(device=1,generation=1,dispatched=dispatched,completed=dispatched if kind=='Ended' else dispatched-1,
                    kind=kind,phase='PlayerResume',commandAuditActive=True,frame=frame+(index==2)))

    def result(self,**kwargs):
        values=dict(lineage=self.lineage,raw=self.raw,camera=self.camera,contacts=self.contacts,commands=self.commands,log=self.log,wrapper_exit=0,source_unchanged=True)
        values.update(kwargs);return qualify(**values)

    def test_scoped_fixture_never_claims_mission_or_camera_acceptance(self):
        result=self.result();self.assertTrue(result['qualified'],result['errors'])
        for field in ('cameraAccepted','playerReady','fullMissionCertified'):self.assertFalse(result[field])

    def test_corrupt_actual_controls_reject(self):
        mutations=[(0,'anchor','enemy'),(0,'padFaction',2),(0,'planningFuel',201),(0,'admissionFuel',0),
            (1,'gesture',999),(1,'frame',100),(2,'receipt',99),(2,'request',999),(2,'resultActorVersion',4),(2,'resultGoalX',999),
            (2,'issued',False),(2,'resultAirUnits',0),(2,'nativeResultKind','Immediate'),(3,'takeoff',0),(4,'followGesture',3),
            (5,'actorY',0),(5,'airborne',0),(5,'fuel',200),(5,'panLocked',0),(5,'frame',999),(5,'returning',1),
            (5,'transformSource','ParentLocalTransform'),(5,'timeScale',8),(5,'actorVersion',99)]
        for index,key,value in mutations:
            with self.subTest(key=key,value=value):
                raw=copy.deepcopy(self.raw);raw[self.indices[index]][key]=value
                self.assertFalse(self.result(raw=raw)['qualified'])
        for key,value in [('paidReservation',999),('frame',200),('aircraftState','Grounded'),('airborne',0),('pngSha256','0'*64),('png',self.camera[1]['png'])]:
            camera=copy.deepcopy(self.camera);camera[-1][key]=value
            self.assertFalse(self.result(camera=camera)['qualified'],key)
        camera=copy.deepcopy(self.camera);camera[-1]['measurement']['ProjectedPixels']['width']=1000
        self.assertFalse(self.result(camera=camera)['qualified'])
        camera=copy.deepcopy(self.camera);camera[-1]['measurement']['ProjectedPixels']['x']=-1000
        self.assertFalse(self.result(camera=camera)['qualified'])
        self.assertFalse(self.result(contacts=self.contacts[:-1])['qualified'])
        self.assertFalse(self.result(commands=[])['qualified'])
        self.assertFalse(self.result(source_unchanged=False)['qualified'])
        raw=copy.deepcopy(self.raw)
        for row in raw:
            if row.get('stage')=='StableNativeCruise':row.update(actorX=10,actorZ=0,horizontalFromParked=10,horizontalStep=0,consumptionCellX=10)
        self.assertFalse(self.result(raw=raw)['qualified'])
        raw=json.loads(json.dumps(self.raw))
        next(row for row in raw if row['stage']=='NativeParkedFollowEntered')['nativeRestorePosition']=dict(x=99,y=44,z=0)
        next(row for row in raw if row['stage']=='ParkedFollowExitedRestored')['cameraPosition']=dict(x=99,y=44,z=0)
        self.assertFalse(self.result(raw=raw)['qualified'],'A wrong native snapshot restoring itself must reject')

    def test_parked_follow_capture_and_exit_restoration_are_required(self):
        mutations=[('PublicParkedFollowTouch','parkedFollowGesture',99),('NativeParkedFollowEntered','nativeRestoreValid',0),
            ('NativeParkedFollowEntered','nativeFollowEnteredFrame',1),('NativeParkedFollowEntered','nativeRestorePosition',dict(x=99,y=44,z=0)),
            ('ParkedFollowExitedRestored','followEnabled',1),('ParkedFollowExitedRestored','cameraFieldOfView',47),
            ('ParkedFollowExitedRestored','cameraOrthoSize',99),('ParkedFollowExitedRestored','zoomInInteractable',False),
            ('ParkedFollowStable','airborne',1),('ParkedFollowStable','cameraPosition',dict(x=0,y=101,z=0))]
        for stage,key,value in mutations:
            with self.subTest(stage=stage,key=key):
                raw=json.loads(json.dumps(self.raw));next(row for row in raw if row['stage']==stage)[key]=value
                self.assertFalse(self.result(raw=raw)['qualified'])
        raw=[row for row in self.raw if row['stage']!='ParkedFollowStable']
        self.assertFalse(self.result(raw=raw)['qualified'])
        for key,value in [('png',self.camera[1]['png']),('frame',200),('aircraftState','Airborne'),('paidReservation',999)]:
            camera=copy.deepcopy(self.camera);next(row for row in camera if row['phase']=='parked-follow-transport')[key]=value
            self.assertFalse(self.result(camera=camera)['qualified'],key)
        raw=copy.deepcopy(self.raw);raw[self.indices[5]]['parkedX']=999
        self.assertFalse(self.result(raw=raw)['qualified'])
        raw=copy.deepcopy(self.raw);raw[self.indices[5]]['aircraftFuelSpendAttributed']=True
        self.assertFalse(self.result(raw=raw)['qualified'])
        raw=copy.deepcopy(self.raw);raw[self.indices[5]]['framing']['EntityIndex']=999
        self.assertFalse(self.result(raw=raw)['qualified'])
        raw=copy.deepcopy(self.raw);raw[self.indices[5]]['framing']['ProjectedPixels']['width']=1000
        self.assertFalse(self.result(raw=raw)['qualified'])
        raw=copy.deepcopy(self.raw);raw[self.indices[5]].pop('framing')
        self.assertFalse(self.result(raw=raw)['qualified'])

if __name__=='__main__':unittest.main()
