"""Bounded S050 review calculation over exact existing Campaign source assets.

This does not run Unity or establish ordinary-factory, runtime occupancy,
source binding, independent tactical approaches, or mission readiness.
"""
from pathlib import Path
import argparse, collections, gzip, hashlib, json, math, re, struct, subprocess

BASE = '97df692e737f03699550ad0eea72d8e19eb36cc0'
MAP = 'Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/'
PINS = {
    'Definition.asset': '6dc011093e5298c07985d264c5483db6875e2593cdec0ff96bd2a05d6838ecfb',
    'Grid.asset': '7aa1a9744215a444064f6a9e09fb7e6aacbdc0155b4f5cf38b19be9a5d98df1a',
    'Surface.asset': '048fe898cea5c3a1158470d5c299222715d8a911ecde6c1155aa30bc75e347ce',
}
AUTHORITY = 'Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json'
ROWS = [
    [('building.refinery',25,15),('building.refinery.module',25,15),('building.barracks',28,15)],
    [('building.fabrication_depot',19,16),('building.ground_staging',20,14),('building.fuel_bladder',14,10),('building.helipad',16,15)],
    [('building.oil_pump',15,4),('building.satellite_dish',10,10),('building.watchtower',3,4),('building.watchtower.approach',3,4)],
]

def need(value, message):
    if not value: raise ValueError(message)

def git_source(project, path):
    return subprocess.check_output(['rtk','proxy','git','show',BASE+':'+path],cwd=project)

def authority_row(value):
    if isinstance(value,dict):
        if value.get('catalog_id')=='S050' and 'canonical_size_rows' in value:return value
        for child in value.values():
            result=authority_row(child)
            if result:return result
    elif isinstance(value,list):
        for child in value:
            result=authority_row(child)
            if result:return result

def rect_cells(rect):
    return ((x,y) for y in range(rect[1],rect[3]) for x in range(rect[0],rect[2]))

def overlap(a,b):
    return a[0]<b[2] and b[0]<a[2] and a[1]<b[3] and b[1]<a[3]

def calculate(project):
    raw={name:git_source(project,MAP+name) for name in PINS}
    for name,pin in PINS.items():need(hashlib.sha256(raw[name]).hexdigest()==pin,'Source differs: '+name)
    grid=raw['Grid.asset'].decode();surface=raw['Surface.asset'].decode();definition=raw['Definition.asset'].decode()
    width=int(re.search(r'^  width: (\d+)$',grid,re.M)[1]);height=int(re.search(r'^  height: (\d+)$',grid,re.M)[1])
    need((width,height)==(440,220),'Exact existing grid dimensions required')
    origin=[float(v) for v in re.search(r'^  origin: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}$',grid,re.M).groups()]
    need(origin==[860,0,300],'Exact existing grid origin required')
    section=grid.split('  blockedCells:\n',1)[1].split('\n  drawGrid:',1)[0]
    blocked=[tuple(map(int,p)) for p in re.findall(r'^  - \{x: (\d+), y: (\d+)\}$',section,re.M)]
    need(len(blocked)==16392 and len(set(blocked))==len(blocked),'Exact blocked-cell census required')
    compressed=bytes.fromhex(re.search(r'^  compressedSurfacePayload: ([0-9a-f]+)$',surface,re.M)[1])
    payload=gzip.decompress(compressed)
    version,encoding,cells,samples,connections,w,h,minimum,step=struct.unpack_from('<iB5i2f',payload)
    need((version,encoding,cells,samples,connections,w,h)==(3,1,96800,96800,0,width,height),'Exact compact surface schema required')
    need(len(payload)==33+12*samples,'Exact compact surface length required')
    values=list(struct.iter_unpack('<HbbbhBHH',payload[33:]));blocked=set(blocked)
    ground=bytearray(cells);facility=bytearray(cells)
    heights=[minimum+v[0]*step for v in values]
    for i,(_,nx,ny,nz,layer,kind,movement,flags) in enumerate(values):
        y,x=divmod(i,width);inside=8<=x<432 and 8<=y<212
        clear=inside and (x,y) not in blocked
        ground[i]=bool(clear and movement&7==7)
        slope=math.degrees(math.atan2(math.hypot(nx,nz),ny)) if ny>0 else 180
        facility[i]=bool(clear and movement&16 and not(flags&1) and heights[i]<=.75 and slope<=8)
    def index(x,y):return y*width+x
    def inside(x,y):return 0<=x<width and 0<=y<height
    def footprint_free(center,size,mask):
        x,y=center;left=x-size//2;top=y-size//2
        return all(inside(xx,yy) and mask[index(xx,yy)] for yy in range(top,top+size) for xx in range(left,left+size))
    authority=authority_row(json.loads(git_source(project,AUTHORITY)))
    need(authority is not None and authority['canonical_size_rows_sha256']=='f36d473ba02b0f7847d6ec589f47ad352698aa890ea2415f682be1281e09305a','Exact S050 authority required')
    role_fields=['rifle','gunner','rocketeer','car','apc','tank']
    result=[]
    for budget in authority['canonical_size_rows']:
        size=budget['size_id'];facilities=[];spawns=[];compounds=[]
        for faction,offset in [(1,0),(2,189)]:
            compound=[133+offset,47,243+offset,137];compounds.append(compound)
            y=52
            for row in ROWS:
                x=139+offset
                for structure,fw,fh in row:
                    if not(size=='Standard' and structure=='building.refinery.module'):
                        rect=[x,y,x+fw,y+fh];positions=list(rect_cells(rect))
                        need(all(facility[index(xx,yy)] for xx,yy in positions),'Conservative source predicate failed: '+structure)
                        low=min(heights[index(xx,yy)] for xx,yy in positions);high=max(heights[index(xx,yy)] for xx,yy in positions)
                        need(high-low<=.5,'Per-footprint source height span failed')
                        facilities.append(dict(faction=faction,structure=structure,unrotated_configured_reserved_cells=rect,
                                               proposed_world_xz=[origin[0]+x+fw*.5,origin[2]+y+fh*.5],height_min=low,height_max=high,
                                               configured_height_span=high-low,conservative_serialized_cell_screen_pass=True,
                                               actual_model_origin_and_ordinary_placement_certified=False))
                    x+=fw+5
                y+=max(v[2] for v in row)+5
            owned=[v for v in facilities if v['faction']==faction]
            need(len(owned)==int(budget['player_starting_structures']),'Exact fixed facility count required')
            need(all(compound[0]<=v['unrotated_configured_reserved_cells'][0] and v['unrotated_configured_reserved_cells'][2]<=compound[2] and
                     compound[1]<=v['unrotated_configured_reserved_cells'][1] and v['unrotated_configured_reserved_cells'][3]<=compound[3] for v in owned),
                 'Facility outside proposed compound')
            infantry=[];vehicles=[]
            for role in role_fields:
                count=int(budget['player_'+role])
                (infantry if role in role_fields[:3] else vehicles).extend([role]*count)
            vehicles += ['tanker'] + ['logistics_truck']*(int(budget['player_logistics_support'])-1)
            need(len(infantry)==int(budget['player_infantry']) and len(vehicles)==int(budget['player_ground'])+int(budget['player_logistics_support']),'Full fixed roster required')
            for j,role in enumerate(infantry):
                spawns.append(dict(faction=faction,canonical_role=role,cell=[140+offset+(j%10)*2,116+(j//10)*2],footprint=[1,1]))
            for j,role in enumerate(vehicles):
                spawns.append(dict(faction=faction,canonical_role=role,cell=[174+offset+(j%6)*5,118+(j//6)*6],footprint=[3,3]))
        for i,a in enumerate(facilities):
            need(not any(overlap(a['unrotated_configured_reserved_cells'],b['unrotated_configured_reserved_cells']) for b in facilities[i+1:]),'Facility overlap')
        source_clear=bytearray(ground)
        for row in facilities:
            for x,y in rect_cells(row['unrotated_configured_reserved_cells']):source_clear[index(x,y)]=0
        occupied=set()
        for row in spawns:
            center=row['cell'];fp=row['footprint'][0];need(footprint_free(center,fp,source_clear),'Spawn source/structure collision')
            x,y=center;rect=[x-fp//2,y-fp//2,x-fp//2+fp,y-fp//2+fp]
            need(not occupied.intersection(rect_cells(rect)),'Spawn overlap');occupied.update(rect_cells(rect))
            row['proposed_world_xz']=[origin[0]+x+.5,origin[2]+y+.5]
        passable=bytearray(cells)
        # The complete 3x3 footprint stays inside the proposed logical sector.
        for y in range(48,160):
            for x in range(123,431):passable[index(x,y)]=footprint_free((x,y),3,source_clear)
        start=(235,128);need(passable[index(*start)],'Source graph root unavailable')
        q=collections.deque([start]);seen={start}
        while q:
            x,y=q.popleft()
            for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if inside(xx,yy) and (xx,yy) not in seen and passable[index(xx,yy)]:seen.add((xx,yy));q.append((xx,yy))
        routes=[]
        for lane,y in enumerate((55,91,139)):
            route=[(x,y) for x in range(240,325)];need(all(p in seen for p in route),'3x3 source route disconnected')
            route_heights=[heights[index(*p)] for p in route]
            routes.append(dict(route_id='proposal.approach.'+str(lane),source_cell_polyline=[[240,y],[324,y]],
                               world_xz_polyline=[[1100.5,300+y+.5],[1184.5,300+y+.5]],cardinal_length_cells=84,
                               max_center_height_step=max(abs(a-b) for a,b in zip(route_heights,route_heights[1:])),
                               all_3x3_footprints_source_connected=True,native_route_or_tactical_independence_certified=False))
        approaches=[]
        for row in facilities:
            a,b,c,d=row['unrotated_configured_reserved_cells'];candidates=[((a+c)//2,d+2),((a+c)//2,b-3),(a-3,(b+d)//2),(c+2,(b+d)//2)]
            target=next((p for p in candidates if p in seen),None);need(target is not None,'No connected 3x3 conservative facility approach')
            approaches.append(dict(faction=row['faction'],structure=row['structure'],conservative_edge_cell=list(target),
                                   source_graph_connected=True,native_service_goal_certified=False))
        for row in spawns:
            need(tuple(row['cell']) in seen,'Spawn center cannot reach source route graph')
        minimum_enemy_edge=min(math.hypot(a['cell'][0]-b['cell'][0],a['cell'][1]-b['cell'][1]) for a in spawns if a['faction']==1 for b in spawns if b['faction']==2)
        result.append(dict(size=size,canonical_size_row=budget,compounds_cells_half_open=compounds,
                           compound_dimensions=[110,90],compound_horizontal_gap_cells=79,facility_shelf_widths=[88,84,46],
                           row1_width_88_with_gaps_fits_110=True,facility_count_each=int(budget['player_starting_structures']),
                           facilities=facilities,spawns=spawns,spawn_count_each=len(spawns)//2,minimum_opposing_spawn_center_distance=minimum_enemy_edge,
                           conservative_unit_graph_cells=len(seen),connected_facility_approaches=approaches,approaches=routes,
                           runtime_occupancy_included=False,source_geometry_cache_included=False,source_fit_certified=False))
    return dict(base=BASE,source_paths_and_sha256={MAP+k:v for k,v in PINS.items()},
                coordinate_frame=dict(origin=origin,cell_size=1,grid_dimensions=[width,height],cell_bounds='half-open'),
                logical_sector_cells_half_open=[122,47,432,161],logical_sector_world_xz_half_open=[982,347,1292,461],
                scope='ConstructedSourceCellPackingAndConnectivityProposalOnly',sizes=result,
                physical_geometry_modified=False,canonical_budget_modified=False,new_native_run=False,source_fit_certified=False,
                y_policy='XZ proposal only; actual original grounding/factory buildPlaneY remains authoritative, not zeroed or reconstructed here')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--project',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    report=calculate(args.project)
    with args.output.open('x') as stream:json.dump(report,stream,indent=2,allow_nan=False);stream.write('\n')
    print(json.dumps(dict(result='SourceCellProposalCalculated',sizes=len(report['sizes']),native_fit_certified=False)))
