"""Independent occupancy oracle, route controls and replay integrity checks."""
import argparse
import copy
import json
import random
from pathlib import Path
import shutil
import sys
import unittest
sys.dont_write_bytecode = True
import skirmish_navigation_replay as driver


def actor(kind, entity, x=0, z=0, sx=1, sz=1):
    return dict(kind=kind, entity=entity, cell_x=x, cell_z=z, size_x=sx, size_z=sz)


def occupancy_case(name, width, height, steps):
    # Independent full recomputation, deliberately no incremental rectangle algorithm.
    members, snapshots = {}, []
    def state():
        counts = [0] * (width * height)
        for item in members.values():
            sx, sz = max(1, item['size_x']), max(1, item['size_z'])
            left, bottom = item['cell_x'] - (sx-1)//2, item['cell_z'] - (sz-1)//2
            for z in range(height):
                for x in range(width):
                    if left <= x < left+sx and bottom <= z < bottom+sz:
                        counts[z*width+x] += 1
        return dict(counts=counts, occupied=[x > 0 for x in counts], entities=len(members))
    for item in steps:
        if item['kind'] in ('add', 'update'): members[item['entity']] = item
        elif item['kind'] == 'remove': del members[item['entity']]
        elif item['kind'] == 'snapshot': snapshots.append(state())
    return dict(id=name, width=width, height=height, steps=steps, expect=dict(state(), snapshots=snapshots))


def fixtures():
    snap = dict(kind='snapshot')
    rows = [occupancy_case('overlap-removal', 3, 2, [actor('add','a',1,0),actor('add','b',1,0),snap,
              dict(kind='remove',entity='a'),snap,dict(kind='remove',entity='b')]),
            occupancy_case('partial-overlap-move', 5, 5, [actor('add','a',1,1,3,3),snap,actor('update','a',2,2,3,3),snap]),
            occupancy_case('edge-clipping-and-outside',3,3,[actor('add','a',0,0,3,3),snap,
                actor('update','a',-20,20,2,2),snap,actor('update','a',2,2,2,2),snap]),
            occupancy_case('size-change-and-no-op',4,4,[actor('add','a',1,1,2,2),snap,
                actor('update','a',1,1,2,2),snap,actor('update','a',1,1,1,1),snap])]
    rng=random.Random(7331)
    for case in range(100):
        steps=[]; living=[]
        for j in range(40):
            choice=rng.randrange(3) if living else 0
            if choice == 0:
                entity=str(j);living.append(entity);kind='add'
            else: entity=rng.choice(living);kind='update' if choice==1 else 'remove'
            if kind == 'remove': living.remove(entity);steps.append(dict(kind=kind,entity=entity))
            else: steps.append(actor(kind,entity,rng.randrange(-2,10),rng.randrange(-2,10),rng.randrange(1,6),rng.randrange(1,6)))
            steps.append(snap)
        rows.append(occupancy_case('seeded-'+str(case),8,8,steps))
    return dict(schema_version=1,cases=rows)


def map_facts(w,h):
    return dict(walkable=[1]*(w*h),dynamic_blocked=[0]*(w*h),friendly_pass=[0]*(w*h))


def path_query(w,h,x,z,**patch):
    result=dict(kind='path',entity='self',target_x=x,target_z=z,vehicle=False,manual_move=False,faction=1,
        search_padding=8,max_expansions=450,heuristic_weight=1,
        surface=dict(provenance='synthetic regression fixture',cell_pathable=[1]*(w*h),footprint_pathable=[1]*(w*h),
                     cardinal_cost=[10]*(w*h),diagonal_cost=[14]*(w*h)))
    result.update(patch);return result


def route_fixtures():
    rows=[]
    def add(name,w,h,steps,expected,grid=None):
        rows.append(dict(id=name,width=w,height=h,map=grid or map_facts(w,h),steps=steps,expect=dict(queries=expected)))
    def success(cells=None,**patch):
        obj=dict(kind='path',result=dict(Found=True,FailureCode=0))
        if cells is not None:obj['cells']=[dict(X=x,Z=z) for x,z in cells]
        obj.update(patch);return obj
    def failure(code):return dict(kind='path',result=dict(Found=False,FailureCode=code),cells=[])
    add('direct',5,3,[actor('add','self',0,1),path_query(5,3,4,1)],[success([(0,1),(4,1)],result=dict(Found=True,ExpansionCount=0))])
    add('same-cell',3,3,[actor('add','self',1,1),path_query(3,3,1,1)],[success([(1,1)])])
    add('adjacent-diagonal-production-shortcut',3,3,[actor('add','self',0,0),path_query(3,3,1,1)],[success([(0,0),(1,1)])])
    grid=map_facts(5,3);grid['dynamic_blocked'][7]=1
    steps=[actor('add','self',0,1),path_query(5,3,4,1),path_query(5,3,4,1)]
    cells=[(0,1),(1,0),(2,0),(3,0),(4,1)]
    add('astar-detour-repeat-epoch',5,3,steps,[success(cells,epoch=1),success(cells,epoch=2)],grid)
    preferred=path_query(5,3,4,1)
    preferred['surface']['cardinal_cost'][:5]=[18]*5;preferred['surface']['diagonal_cost'][:5]=[25]*5
    preferred['surface']['cardinal_cost'][10:]=[6]*5;preferred['surface']['diagonal_cost'][10:]=[8]*5
    add('surface-cost-selects-lower-detour',5,3,[actor('add','self',0,1),preferred],
        [success([(0,1),(1,2),(2,2),(3,2),(4,1)])],grid)
    vehicle_grid=map_facts(7,5);vehicle_grid['dynamic_blocked'][17]=1
    add('vehicle-turnpoint-compression',7,5,[actor('add','self',1,2),path_query(7,5,5,2,vehicle=True)],
        [success([(1,2),(2,1),(4,1),(5,2)])],vehicle_grid)
    q=path_query(5,3,4,1,max_expansions=0)
    add('expansion-budget-fails',5,3,[actor('add','self',0,1),q],[failure(9)],grid)
    q=path_query(5,3,4,1,search_padding=0)
    add('search-bounds-fails',5,3,[actor('add','self',0,1),q],[failure(10)],grid)
    blocked=map_facts(3,3)
    for i in (1,3,4):blocked['dynamic_blocked'][i]=1
    add('astar-corner-cut-rejected',3,3,[actor('add','self',0,0),path_query(3,3,2,2)],[failure(10)],blocked)
    add('occupied-goal-then-remove',5,3,[actor('add','self',0,1),actor('add','other',4,1),path_query(5,3,4,1),
        dict(kind='remove',entity='other'),path_query(5,3,4,1)],[failure(7),success([(0,1),(4,1)])])
    add('ignored-live-unit',5,3,[actor('add','self',0,1),actor('add','other',4,1),path_query(5,3,4,1,ignored_entity='other')],[success([(0,1),(4,1)])])
    grouped=actor('add','other',4,1);grouped['manual_group']=1
    add('manual-group-occupancy',5,3,[actor('add','self',0,1),grouped,path_query(5,3,4,1,manual_move=True)],[success([(0,1),(4,1)])])
    add('vehicle-soft-blocker',7,5,[actor('add','self',1,2),actor('add','other',5,2),path_query(7,5,5,2,vehicle=True)],[success([(1,2),(5,2)])])
    add('vehicle-hard-blocker',7,5,[actor('add','self',1,2),actor('add','other',4,2,2,2),path_query(7,5,5,2,vehicle=True)],[failure(7)])
    add('footprint-grid-boundary',5,4,[actor('add','self',0,1,2,2),path_query(5,4,4,1,vehicle=True)],[failure(7)])
    for name,index,field,code in [('start-unwalkable',5,'walkable',2),('goal-unwalkable',9,'walkable',5)]:
        m=map_facts(5,3);m[field][index]=0
        add(name,5,3,[actor('add','self',0,1),path_query(5,3,4,1)],[failure(code)],m)
    for name,index,field,code in [('start-surface',5,'cell_pathable',3),('goal-surface',9,'cell_pathable',6),('goal-footprint-surface',9,'footprint_pathable',8)]:
        q=path_query(5,3,4,1);q['surface'][field][index]=0
        add(name,5,3,[actor('add','self',0,1),q],[failure(code)])
    add('start-outside',5,3,[actor('add','self',-1,1),path_query(5,3,4,1)],[failure(1)])
    add('goal-outside',5,3,[actor('add','self',0,1),path_query(5,3,5,1)],[failure(4)])
    m=map_facts(5,3);m['dynamic_blocked'][9]=1;m['friendly_pass'][9]=1
    add('friendly-gate',5,3,[actor('add','self',0,1),path_query(5,3,4,1),path_query(5,3,4,1,faction=2)],[success([(0,1),(4,1)]),failure(7)],m)
    placement=dict(kind='placement',entity='self',target_x=4,target_z=1,vehicle=False,manual_move=False,faction=1)
    add('placement-query',5,3,[actor('add','self',0,1),placement],[dict(kind='placement',allowed=True)])
    return dict(schema_version=1,cases=rows)


class NavigationReplayTests(unittest.TestCase):
    def replay(self,data,tag):
        path=OUTPUT/(tag+'.json');path.write_text(data if isinstance(data,str) else json.dumps(data))
        return driver.run(BUILD,path,OUTPUT/tag)

    def test_independent_recomputed_occupancy(self):
        report=self.replay(fixtures(),'occupancy')
        self.assertEqual(report['failed'],0,report)

    def test_composed_path_and_placement_controls(self):
        report=self.replay(route_fixtures(),'routes')
        self.assertEqual(report['failed'],0,report)
        repeat=self.replay(route_fixtures(),'routes-repeat')
        self.assertEqual(report['cases'],repeat['cases'])

    def test_bulk_repeatability(self):
        rows=[dict(copy.deepcopy(route_fixtures()['cases'][i % len(route_fixtures()['cases'])]),id='bulk-'+str(i)) for i in range(720)]
        data=dict(schema_version=1,cases=rows)
        a=self.replay(data,'bulk-a');b=self.replay(data,'bulk-b')
        self.assertEqual(a['failed'],0,[(x['id'],x.get('error')) for x in a.get('cases',[]) if x['status']=='failed'])
        self.assertEqual(a['cases'],b['cases'])

    def test_navigation_missing_or_invalid_facts_rejected(self):
        rows=[]
        for i in range(7):
            row=copy.deepcopy(route_fixtures()['cases'][0]);row['id']=str(i);rows.append(row)
        del rows[0]['map']
        del rows[1]['steps'][1]['surface']
        rows[2]['steps'][1]['surface']['provenance']=''
        rows[3]['steps'][1]['surface']['cardinal_cost'][0]=0
        rows[4]['map']['walkable']=[]
        rows[5]['steps'][1]['ignored_entity']='unknown'
        rows[6]['steps'][1]['max_expansions']=-1
        report=self.replay(dict(schema_version=1,cases=rows),'invalid-routes')
        self.assertEqual(report['failed'],7,report)

    def test_repeatability(self):
        a=self.replay(fixtures(),'repeat-a');b=self.replay(fixtures(),'repeat-b')
        self.assertEqual(a['failed'],0,a);self.assertEqual(a['cases'],b['cases'])

    def test_reject_invalid_lifecycle_and_schema(self):
        rows=[]
        for i in range(9):
            row=copy.deepcopy(fixtures()['cases'][0]);row['id']=str(i);rows.append(row)
        rows[0]['steps'].insert(1,copy.deepcopy(rows[0]['steps'][0]))
        rows[1]['steps'][0]['kind']='update'
        rows[2]['steps'][0]=dict(kind='remove',entity='missing')
        rows[3]['width']=0
        rows[4]['steps'][0]['size_x']=0
        rows[5]['expect']={}
        rows[6]['expect']={'missing':1}
        rows[7]['steps'][0]['unknown']=1
        rows[8]['steps'][0]['kind']='teleport'
        report=self.replay(dict(schema_version=1,cases=rows),'invalid')
        self.assertEqual(report['failed'],9,report);self.assertEqual(report['process_exit'],1)

    def test_duplicate_keys_ids_empty_and_budget(self):
        data=fixtures();data['cases'][1]['id']=data['cases'][0]['id']
        self.assertEqual(self.replay(data,'duplicate-id')['failed'],1)
        for name,data in [('duplicate-key','{"schema_version":1,"schema_version":1,"cases":[]}'),('empty','{"schema_version":1,"cases":[]}')]:
            self.assertEqual(self.replay(data,name)['result'],'failed')
        row=dict(id='budget',width=512,height=512,steps=[dict(kind='snapshot')]*100,expect={'entities':0})
        self.assertEqual(self.replay(dict(schema_version=1,cases=[row]),'budget')['failed'],1)

    def test_stale_source_binary_and_manifest(self):
        clone=OUTPUT/'clone';shutil.copytree(BUILD,clone)
        manifest=json.loads((clone/'build.json').read_text());original=Path(manifest['project'])
        source=OUTPUT/'source';source.mkdir()
        for rel in manifest['source_pins']:
            dst=source/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(original/rel,dst)
        manifest['project']=str(source);(clone/'build.json').write_text(json.dumps(manifest))
        data=OUTPUT/'stale.json';data.write_text(json.dumps(fixtures()))
        kernel=source/driver.OCCUPANCY;kernel.write_text(kernel.read_text()+'// changed\n')
        with self.assertRaisesRegex(ValueError,'Production source changed'):driver.run(clone,data,OUTPUT/'stale-source')
        shutil.copyfile(original/driver.OCCUPANCY,kernel)
        dll=clone/'bin/NavigationReplay.dll';dll.write_bytes(dll.read_bytes()+b'changed')
        with self.assertRaisesRegex(ValueError,'Built artifact changed'):driver.run(clone,data,OUTPUT/'stale-bin')
        manifest['build_files']['bin/extra.dll']='0'*64;(clone/'build.json').write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError,'build manifest'):driver.run(clone,data,OUTPUT/'stale-manifest')

    def test_output_isolation(self):
        project=Path(json.loads((BUILD/'build.json').read_text())['project'])
        for folder in ('Assets','Packages','ProjectSettings','.git'):
            with self.assertRaises(ValueError):driver.new_output(project,project/folder/'forbidden-navigation')
        with self.assertRaises(FileExistsError):driver.new_output(project,OUTPUT)


if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--build',required=True,type=Path);p.add_argument('--output',required=True,type=Path)
    args=p.parse_args();BUILD=args.build.resolve();OUTPUT=args.output.resolve();OUTPUT.mkdir(parents=True,exist_ok=False)
    (OUTPUT/'examples.json').write_text(json.dumps(fixtures(),indent=2)+'\n')
    (OUTPUT/'navigation-examples.json').write_text(json.dumps(route_fixtures(),indent=2)+'\n')
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(NavigationReplayTests))
    print(json.dumps(dict(result='passed' if result.wasSuccessful() else 'failed',tests=result.testsRun,failures=len(result.failures),errors=len(result.errors),output=str(OUTPUT))))
    raise SystemExit(0 if result.wasSuccessful() else 1)
