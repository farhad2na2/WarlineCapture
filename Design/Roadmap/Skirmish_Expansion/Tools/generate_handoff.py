#!/usr/bin/env python3
"""Generate/check planning artifacts only. Never loads Unity or writes Assets.

Source identity: existing SCENARIO_CATALOG.csv. Numeric inputs below mirror
MATCH_SETUP.md and the explicit September 21 implementation decisions.
Run with --write to update derived docs, --check to verify committed artifacts.
"""
from pathlib import Path
import argparse
import collections
import csv
import hashlib
import io
import re

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[2]
PROTOTYPES = {'S001': (1, 0), 'S025': (2, 1), 'S073': (3, 3)}
MAPS = {
 'DB': dict(name='Desert Base', map_id='opmap.skirmish.desert_base_01', main='highway',
            a='north ruins', b='south sweep', convoy_a='highway', convoy_b='southern service road',
            zones='central crossroads; north ruins; south supply junction',
            corridors='north gate; south checkpoint', exit='east rear exit',
            origin='west depot', destination='east evacuation pad',
            defect='Preserve shared Campaign anchors; correct berm/road joins; prove no decoration in producer exits.'),
 'CC': dict(name='City Crossroads', map_id='opmap.skirmish.city_crossroads', main='central boulevard',
            a='east service lane', b='west courtyards', convoy_a='boulevard', convoy_b='east ring road',
            zones='market crossing; east depot entrance; west civic square',
            corridors='boulevard barricade; east service checkpoint', exit='south rear exit',
            origin='north supply yard', destination='south supply yard',
            defect='Fix the reported northern-base floating ground shelf; verify height/collision/shadow together and actual vehicle corner clearance.'),
 'MP': dict(name='Mountain Pass', map_id='opmap.skirmish.mountain_pass', main='central pass',
            a='west vehicle bypass', b='east infantry trail', convoy_a='central pass', convoy_b='west bypass',
            zones='pass junction; west turnout; east lookout',
            corridors='central ridge crossing; western bypass crossing', exit='northeast valley exit',
            origin='southwest valley depot', destination='northeast valley depot',
            defect='All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.'),
 'IB': dict(name='Industrial Basin', map_id='opmap.skirmish.industrial_basin', main='freight avenue',
            a='service ring', b='warehouse lane', convoy_a='freight avenue', convoy_b='service ring',
            zones='rail junction; refinery entrance; warehouse square',
            corridors='freight gate; service-ring control point', exit='southeast rear exit',
            origin='northwest storage terminal', destination='southeast storage terminal',
            defect='Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.'),
 'AP': dict(name='Airfield Plains', map_id='opmap.skirmish.airfield_plains', main='middle logistics highway',
            a='northern cover line', b='southern armored sweep', convoy_a='logistics highway', convoy_b='southern ring road',
            zones='weather station; north depot; south landing-zone entrance',
            corridors='north radar-road; south apron corridor', exit='east perimeter exit',
            origin='west fuel depot', destination='east logistics pad',
            defect='Certify runway/taxi/return and convoy separation; retain a full ground-only winning approach; inspect aircraft visibility/selection at all camera angles.'),
}
OBJECTIVES = {
 'BA': dict(name='Base Assault', system='SkirmishBaseAssaultObjectiveSystem', ticket='SK-06',
     roles='base.player; base.enemy',
     success='Destroy the original enemy main Barracks while the original player main Barracks survives. Same-tick destruction of both draws; both alive at deadline draws.',
     failure='Original player main base destroyed while enemy survives, or accepted surrender. Other Barracks cannot replace the designated identity.',
     tests='Original vs replacement Barracks; both deaths on the same tick; deadline draw; loss of the whole field army with a surviving producer; hidden enemy health must remain last-observed.'),
 'FC': dict(name='Frontline Control', system='SkirmishFrontlineObjectiveSystem', ticket='SK-07',
     roles='base.player; base.enemy; zone.a; zone.b; zone.c',
     success='Start both sides at 500 tickets and three neutral zones. Dismounted infantry takes 8 s to neutralize then 8 s to capture; majority ownership drains enemy tickets at 1/s. Enemy tickets zero or main base destroyed wins; opposing same-tick terminals draw. Higher tickets at deadline wins, equal draws.',
     failure='Player tickets zero or designated main base destroyed without an opposing same-tick terminal, lower tickets at deadline, or surrender.',
     tests='Vehicle/passenger capture denied; contested freeze; 10 s empty grace then decay; 8+8 capture; owned-empty retention; fraction-preserving ticket drain; base destruction vs reciprocal ticket zero; checkpoint at 7.9 s.'),
 'BT': dict(name='Breakthrough', system='SkirmishBreakthroughObjectiveSystem', ticket='SK-08',
     roles='base.player; base.enemy; corridor.a; corridor.b; exit; designated.01 through designated.12',
     success='Hold either corridor with dismounted infantry for 20 continuous s to latch the exit open, then evacuate at least 8 of the 12 designated original rifle soldiers with 3 s living/dismounted exit dwell each.',
     failure='Alive plus already evacuated designated soldiers falls below 8, deadline before eight evacuations, or surrender. Main-base loss alone is not terminal.',
     tests='12 designation IDs with no extra bodies; fifth loss impossible; corridor contest reset; exit stays open after recapture; boarded flyover denied; death on final exit tick denied; replacement recruits never count; checkpoint keeps evacuated tombstones.'),
 'CE': dict(name='Convoy Escort', system='SkirmishConvoyObjectiveSystem', ticket='SK-09',
     roles='base.player; base.enemy; convoy.1; convoy.2; convoy.3; convoy.origin; convoy.destination',
     success='Deliver at least 2 of 3 original objective trucks with 5 uninterrupted living seconds in the destination. Trucks wait for orders, are Fuel-exempt, and use normal movement/damage plus the specified repair action.',
     failure='Two original trucks destroyed, deadline before two deliveries, or surrender. Main-base loss alone is not terminal. No replacement truck, sale, boarding or logistics transfer can satisfy the objective.',
     tests='Three original truck IDs vs ordinary logistics; 4.9 s dwell restore; same-tick lethal damage before arrival; two-loss failure; selectable overlap; route change after a wreck; Fuel exemption isolated; repair cost/cancel/death accounting.'),
}
ARMIES = {
 'G': dict(name='Ground Maneuver', capabilities='ground;intel;transport;advanced_ground',
     includes='five infantry roles; car; fast/armored/heavy APC; tank; radar; ground siege; transport helicopter; recon drone; logistics',
     excludes='attack helicopters, fighter/strike jets and transport plane',
     counters='rocketeer squads before tanks; rifle/gunner support for anti-armor; optional unarmed transport/recon does not add offensive air'),
 'A': dict(name='Air Mobile', capabilities='ground;intel;transport;offensive_air;advanced_air',
     includes='five infantry roles; car; fast/armored APC; radar; R1 AA; transport/attack helicopters; recon drone; fighter/strike jets; transport plane; logistics',
     excludes='tank, heavy APC and ground siege launcher',
     counters='R1 ground AA before offensive helicopters; rocketeers handle enemy light armor; affordable air return/refuel is required'),
 'C': dict(name='Combined Arms', capabilities='ground;intel;transport;offensive_air;advanced_ground;advanced_air',
     includes='all certified ground, infantry, recon, transport, offensive-air, anti-air and siege roles; logistics',
     excludes='unsupported abilities and noncombatant models advertised as combat roles',
     counters='early rocketeers and R1 AA; infantry screens tanks/launchers, air counters cannot replace objective infantry'),
}
# Tactical choices, not deterministic instructions for ARIA. Substitute map routes.
CHOICES = {
 ('BA','G','F'): ('Use initial infantry/car to scout {main}; recruit anti-armor before a tank commitment.', 'Develop tanks for {main} with infantry cover, or use APC infantry through {a} while threatening supply.'),
 ('BA','G','E'): ('Use the starting tank/APC to contest {main}, retaining one infantry squad at home.', 'Press quickly with the established ground force, or invest in siege and attack via {b} after scouting.'),
 ('BA','A','F'): ('Hold a cheap ground screen and build R2/Helipad only after the affordable counter window.', 'Escort an APC ground push on {a}, or transition to attack/transport air while defending supply.'),
 ('BA','A','E'): ('Scout with infantry and the initial transport helicopter; AA protects the staging area.', 'Lift infantry behind an observed flank near {a}, or establish offensive air with an affordable ground reserve.'),
 ('BA','C','F'): ('Choose between earlier ground armor and the R2 air facility; do not buy every unlock before defending.', 'Concentrate a tank/infantry push along {main}, or pressure {b} while air/recon supports the other approach.'),
 ('BA','C','E'): ('Separate the starting tank/AA screen from transportable infantry; keep Fuel delivery protected.', 'Make a coordinated ground-plus-lift assault, or raid exposed supply from {a} while armor holds {main}.'),
 ('FC','G','F'): ('Send dismounted rifles toward two reachable zones and use the APC as movement support only.', 'Hold the central and nearer side zone with gunner/rocketeer cover, or concede center and connect the two side zones.'),
 ('FC','G','E'): ('Use starting armor to deny approaches while separate infantry squads capture.', 'Build a two-zone ground defense with a reserve, or rotate armor along {main} and retake a weak side zone.'),
 ('FC','A','F'): ('Capture with starting infantry before investing in air; obtain R1 AA if scouting reveals an air transition.', 'Use APC rotations between ground points, or unlock transports and reposition infantry across {a}.'),
 ('FC','A','E'): ('Starting helicopter moves infantry; it never captures while passengers remain aboard.', 'Hold two nearby zones with an air reserve, or rapidly reinforce the more exposed third point when ticket pressure warrants it.'),
 ('FC','C','F'): ('Reserve funds for capture-capable infantry while selecting one vehicle/air development path.', 'Use armor to screen two linked zones, or draw defense toward center and move infantry around {b}.'),
 ('FC','C','E'): ('Assign tank and AA to separate approach protection roles; keep enough dismounted infantry for two zones.', 'Maintain a stable majority with combined cover, or threaten the enemy base only while a reserve can preserve ticket control.'),
 ('BT','G','F'): ('Protect the three designated rifle squads; buy specialist support rather than spending them as the only assault group.', 'Open corridor A with ground force and escort the eight survivors, or scout corridor B and use APCs for the longer approach.'),
 ('BT','G','E'): ('Starting tank and specialist infantry clear a corridor while designated rifles remain protected.', 'Push armor through the broad corridor, or screen one approach and evacuate via the alternate ground route.'),
 ('BT','A','F'): ('Recruit a ground screen and choose APC evacuation or a later unarmed airlift; the corridor still needs a ground hold.', 'Open the safer corridor with ordinary infantry and drive designated squads through, or unlock transport air and unload at a certified far-side pocket.'),
 ('BT','A','E'): ('Use starting transport for staging, retaining designated squad identities and an AA-protected return route.', 'Lift after a ground team opens the exit, or keep helicopters in reserve and escort designated troops along corridor B.'),
 ('BT','C','F'): ('Choose armor to force a corridor or R2 transport to shorten the final movement; protect all twelve identities initially.', 'Punch corridor A with ground support, or stage a split approach with a separate capture group and protected evacuees.'),
 ('BT','C','E'): ('Separate tank/AA cover, the corridor-capture squad and the designated evacuation group.', 'Cover a ground evacuation with armor, or combine an infantry-held corridor with legal air transport/unload near the exit.'),
 ('CE','G','F'): ('Leave trucks at origin until a scout and cheap ground escort check the first exposed segment.', 'Use gunner/rocketeer infantry on {convoy_a}, or send APCs ahead along {convoy_b} and move trucks between holding pockets.'),
 ('CE','G','E'): ('Starting tank screens the truck route while one squad protects the rear service pad.', 'Advance as a tight armored escort, or clear the long route first and move the convoy only when both holding pockets are safe.'),
 ('CE','A','F'): ('Start with a ground escort and scout both truck routes; air research competes with convoy protection funds.', 'Use an APC infantry escort on {convoy_b}, or delay departure for a limited air-support transition without exhausting the deadline.'),
 ('CE','A','E'): ('Move infantry ahead with the transport helicopter while AA and APCs remain near the trucks.', 'Leapfrog ground protection between holding pockets, or use observed air support to clear an intercept point before advancing.'),
 ('CE','C','F'): ('Choose tank escort investment or air scouting; the three objective trucks do not replace Oil/Fuel logistics.', 'Clear {convoy_a} with mixed infantry/armor, or draw enemy reserve away and take {convoy_b}.'),
 ('CE','C','E'): ('Split tank/AA escort, advance infantry and the protected convoy; do not leave truck commands implicit.', 'Move a mixed force and convoy together, or use a mobile advance group to clear the alternative route and repair at a safe service pad.'),
}
SIZE = {
 'Standard': dict(inf=48,ground=8,air=2,supply=128,support=6,built=20,barriers=40,mult=1,deadline=1080),
 'War': dict(inf=96,ground=12,air=4,supply=200,support=8,built=30,barriers=60,mult=1.5,deadline=1500),
 'LargeWar': dict(inf=144,ground=20,air=6,supply=320,support=10,built=40,barriers=80,mult=2,deadline=1800),
}

def force(size, start, army):
    # Infantry are individual bodies; R/H/K inputs below are four-person squads.
    if start=='F':
        r,h,k,car,apc={'Standard':(2,0,1,1,1),'War':(3,1,1,1,2),'LargeWar':(4,1,1,2,2)}[size]
        tank=aa=th=0
    else:
        r,h,k={'Standard':(3,1,1),'War':(4,1,2),'LargeWar':(6,2,2)}[size]
        if size=='Standard':
            car,apc,tank,aa,th={'G':(1,1,1,0,0),'A':(1,1,0,1,1),'C':(0,1,1,1,1)}[army]
        elif size=='War':
            car,apc,tank,aa,th={'G':(1,2,2,0,0),'A':(1,2,0,2,2),'C':(0,2,2,1,2)}[army]
        else:
            car,apc,tank,aa,th={'G':(2,2,3,0,0),'A':(2,2,0,3,2),'C':(0,2,3,2,2)}[army]
    return dict(rifle=4*r,gunner=4*h,rocketeer=4*k,car=car,apc=apc,tank=tank,aa=aa,transport_heli=th)

def totals(f):
    inf=f['rifle']+f['gunner']+f['rocketeer']; ground=sum(f[x] for x in ('car','apc','tank','aa'))
    air=f['transport_heli']; supply=inf+4*(f['car']+f['apc'])+6*(f['tank']+f['aa'])+8*air
    return dict(infantry=inf,ground=ground,air=air,combat=inf+ground+air,supply=supply)

def setup(row,size):
    caps=SIZE[size]; st=row['start_profile']; obj=row['objective_id']; army=row['army_profile']
    e=force(size,st,army); p=e.copy()
    if obj=='BT' and st=='F' and size=='Standard': p.update(rifle=12,rocketeer=0)
    pt,et=totals(p),totals(e)
    for t in (pt,et):
        assert t['infantry']<=caps['inf'] and t['ground']<=caps['ground'] and t['air']<=caps['air'] and t['supply']<=caps['supply']
    base=(450,120,350,800,500,800) if st=='F' else (900,240,700,1800,1000,1600)
    stock=[int(x*caps['mult']) for x in base]
    support=(2 if st=='F' else 3)+(size!='Standard')
    structures=(7 if st=='F' else 10)+int(st=='E' and size!='Standard')
    out=dict(catalog_id=row['scenario_id'],size_id=size,army_profile=army,start_profile=st,
             objective_id=obj,readiness=1 if st=='F' else 2,deadline_seconds=1200 if size=='Standard' and obj=='FC' else caps['deadline'])
    for side,f,t in [('player',p,pt),('enemy',e,et)]:
        out.update({side+'_'+k:v for k,v in f.items()})
        out.update({side+'_'+k:v for k,v in t.items()})
        out[side+'_logistics_support']=support
        out[side+'_objective_trucks']=3 if obj=='CE' and side=='player' else 0
        out[side+'_starting_structures']=structures+(2 if obj in ('BT','CE') and side=='enemy' else 0)
        out[side+'_designated_rifles']=12 if obj=='BT' and side=='player' else 0
    out.update(dict(materials_each=stock[0],oil_each=stock[1],usable_fuel_each=stock[2],materials_capacity_each=stock[3],
        oil_capacity_each=stock[4],fuel_capacity_each=stock[5],infantry_queues_each=1 if st=='F' and size=='Standard' else 2,
        vehicle_queues_each=2 if st=='E' and size!='Standard' else 1,logistics_queues_each=1,
        refinery_modules_each=2 if st=='E' and size!='Standard' else 1,
        infantry_cap_each=caps['inf'],ground_cap_each=caps['ground'],tactical_air_cap_each=caps['air'],
        supply_cap_each=caps['supply'],logistics_support_cap_each=caps['support'],delivery_carrier_cap_each=2,
        objective_support_extra_player=3 if obj=='CE' else 0,player_built_structure_cap_each=caps['built'],barrier_segment_cap_each=caps['barriers'],
        loaded_vehicle_fuel='Full certified role tanks; report actual totals separately from usable store',status='Planned'))
    assert support<=caps['support']
    assert pt['infantry']==et['infantry'] and pt['combat']==et['combat']
    return out

def csv_text(rows):
    buf=io.StringIO(newline='');w=csv.DictWriter(buf,fieldnames=list(rows[0]),lineterminator='\n');w.writeheader();w.writerows(rows);return buf.getvalue()

def formatted_force(s,side):
    labels={'rifle':'rifle','gunner':'gunner','rocketeer':'rocketeer','car':'CAR','apc':'armored APC','tank':'TANK','aa':'AA','transport_heli':'TH'}
    return ', '.join(f'{s[side+"_"+k]} {v}' for k,v in labels.items() if s[side+'_'+k])

def packet_roles(m,obj):
    if obj=='BA': return f'Player/enemy bases and all three routes: {m["main"]}, {m["a"]}, {m["b"]}; both supply expansions and service pads.'
    if obj=='FC': return f'Three ground zones in order a/b/c: {m["zones"]}; player/enemy designated bases and independent infantry approaches.'
    if obj=='BT': return f'Alternative corridors a/b: {m["corridors"]}; muster, {m["exit"]}, two defender tower slots, twelve individual designation bindings.'
    return f'Three truck bays at {m["origin"]}; destination {m["destination"]}; routes a/b: {m["convoy_a"]} / {m["convoy_b"]}; two defender tower slots, holding pockets, repair service pads.'

def build():
    rows=list(csv.DictReader((ROOT/'SCENARIO_CATALOG.csv').open(encoding='utf-8')))
    assert len(rows)==120 and [r['scenario_id'] for r in rows]==[f'S{i:03}' for i in range(1,121)]
    assert len({(r['map_id'],r['objective_id'],r['army_profile'],r['start_profile']) for r in rows})==120
    for m in MAPS: assert sum(r['map_id']==m for r in rows)==24
    for o in OBJECTIVES:
        assert sum(r['objective_id']==o for r in rows)==30
        for a in ARMIES:
            assert sum(r['objective_id']==o and r['army_profile']==a for r in rows)==10
    pending=[r['scenario_id'] for r in rows if r['scenario_id'] not in PROTOTYPES]
    ordinals={s:i for i,s in enumerate(pending,4)}|{s:v[0] for s,v in PROTOTYPES.items()}
    outputs={};manifest=[];matrix=[]
    index=['# All 120 Skirmish programming briefs','',
      '**Status: Planned.** Twenty map/objective packets each contain six individually specified army/start variants. '
      'The [handoff](../IMPLEMENTATION_HANDOFF.md) explains the distinction between work ordinals 4–120 and stable S-IDs. '
      'Read the shared architecture, objectives, roster/economy and map contracts before the assigned packet.','',
      '| Map | Base Assault | Frontline Control | Breakthrough | Convoy Escort |','|---|---|---|---|---|']
    for mid,m in MAPS.items():
        links=[]
        for obj,o in OBJECTIVES.items():
            subset=[r for r in rows if r['map_id']==mid and r['objective_id']==obj]
            file=f'Scenarios/{mid}_{obj}.md';links.append(f'[{subset[0]["scenario_id"]}–{subset[-1]["scenario_id"]}]({mid}_{obj}.md)')
            lines=[f'# {m["name"]} — {o["name"]}: implementation packet','',
              'Proposed content, 2026-09-21. **All expanded entries below remain Planned.** Existing small prototype evidence, '
              'if mentioned, is not acceptance of the expanded version. Use [technical architecture](../TECHNICAL_ARCHITECTURE.md), '
              '[objective rules](../OBJECTIVE_IMPLEMENTATION.md), [roster/economy](../ROSTER_AND_ECONOMY_IMPLEMENTATION.md), '
              '[map contract](../MAP_IMPLEMENTATION.md) and [work packages](../AGENT_WORK_PACKAGES.md).','',
              '## Shared packet implementation','',
              f'- Map: `{m["map_id"]}`; layout `layout.skirmish.{mid.lower()}.{obj.lower()}`. '+packet_roles(m,obj),
              f'- Objective owner: `{o["system"]}` with shared fact projection and `SkirmishOutcomeSystem`. Roles: `{o["roles"]}`.',
              f'- Win: {o["success"]}',f'- Loss: {o["failure"]}',
              '- All sizes preserve the objective rule; Standard/War/Large War deadlines and force values are printed per entry below. Difficulty never changes resources/stats. Default first visit: Regular/Standard.',
              '- Startup/roster/production/army/visibility/AI/UI/save use the shared types in TECHNICAL_ARCHITECTURE; no per-entry controller or ARIA solution script.',
              '- Build shared map assets once. Per entry, build `SkirmishScenario_SNNN.asset` and `ScenarioSetup_SNNN.asset` in `Assets/Game/Configs/SkirmishExpansion/Scenarios/SNNN/`; bind the shared objective/army/start/size assets and resolved initial placement arrays. Use `SkirmishDefinitionBuilder` (proposed), not hand-written Unity YAML.',
              f'- Map-specific acceptance: {m["defect"]}',
              f'- Mandatory objective fixtures for every entry: {o["tests"]}',
              '- Shared class dependencies: `SkirmishSessionInitializationSystem`, `SkirmishScenarioSpawnSystem`, `SkirmishRosterProjectionSystem`, `SkirmishCapacityReservationSystem`, `SkirmishCapacityLifecycleSystem`, `SkirmishArmyGroupSystem`, `SkirmishResearchSystem`, `SkirmishEnemyStrategySystem`, `SkirmishCheckpointSystem`, `SkirmishResultSettlementSystem`, `SkirmishSessionCleanupSystem`, UI projections and extended `AriaSkirmishPlanSystem`.',
              '- Enemy starts with the same profile/start resources and total combat roster. BA/FC are symmetric; BT/CE use two extra disclosed defender towers and A/B/reserve group placement from MAP_IMPLEMENTATION. All reinforcements are paid production, never scripted free waves.',
              '- Infantry numbers below are individual soldiers, not squad cards. CAR/APC/TANK/AA/TH are platforms; logistics, delivery carriers, objective trucks and buildings are counted separately. Full vehicle tanks are additional listed endowment from certified role data; not invented Fuel stock.',
              '']
            for r in subset:
                sid=r['scenario_id'];n=int(sid[1:]);army=r['army_profile'];start=r['start_profile'];a=ARMIES[army]
                srows=[setup(r,size) for size in SIZE];matrix.extend(srows)
                opening,choice=CHOICES[(obj,army,start)];opening=opening.format(**m);choice=choice.format(**m)
                gates='SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;'+o['ticket']+';SK-10;SK-11;SK-12;SK-13'
                capabilities=a['capabilities']+';objective_'+obj.lower()+(';vehicle_repair' if obj=='CE' else '')
                anchor=sid.lower()
                lines += [f'## {sid}','',
                  f'**{r["working_title_en"]}** — handoff work ordinal **{ordinals[sid]}**. '+('Prototype compatibility mapping exists; certify this expanded revision separately.' if sid in PROTOTYPES else 'One of the 117 remaining new catalog combinations.'),'',
                  f'**Bind:** catalog `{sid}`; definition `skirmish.{sid.lower()}`; scenario `scenario.skirmish.{sid.lower()}`; map `{m["map_id"]}`; objective `{obj}`; army `{army}`; start `{start}`. '
                  f'Prerequisite tickets: `{gates}`. Required capability tags: `{capabilities}`. Recommended later size `{r["recommended_size"]}` is gated; first visit remains Standard.','',
                  f'**Opening and decisions:** {opening} Two intended approaches: {choice} These describe tactical options, not a mandatory click sequence or AI script.','',
                  f'**Roster:** {a["includes"]}. Exclude {a["excludes"]}. Counter contract: {a["counters"]}. '
                  +('Field begins R1: buy R2 facilities/readiness through normal costs.' if start=='F' else 'Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.'),'',
                  '| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |',
                  '|---|---|---|---|---|---|---|---|']
                for s in srows:
                    lines.append(f'| {s["size_id"]} | {formatted_force(s,"player")} | {formatted_force(s,"enemy")} | {s["materials_each"]}/{s["oil_each"]}/{s["usable_fuel_each"]} | {s["player_logistics_support"]}; {s["player_objective_trucks"]}/{s["enemy_objective_trucks"]} | {s["player_starting_structures"]}/{s["enemy_starting_structures"]} | {s["infantry_queues_each"]}/{s["vehicle_queues_each"]}/{s["logistics_queues_each"]} | {s["deadline_seconds"]} s |')
                lines += ['',
                  '**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. '
                  '(2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. '
                  f'(3) Install `{o["system"]}` and its typed state; preserve the exact terminal/clock semantics. '
                  '(4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.'+sid.lower()+'.{title,brief,objective,warning.*,result.*}` in EN/FA. '
                  '(5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. '
                  '(6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.','',
                  '**Entry-specific checks:** '+('At Standard Field, replace K with the third designated rifle squad for the player only; enemy remains 2R+K. ' if obj=='BT' and start=='F' else '')+
                  ('Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. ' if start=='F' else 'Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. ')+
                  ('Reject tank/heavy APC/siege queues in A while keeping R1 AA available. ' if army=='A' else 'Reject offensive-air queues in G while preserving transport/recon. ' if army=='G' else 'Verify simultaneous armor/AA/air queues share category and Supply reservations correctly. ')+
                  ('Prove both ground-only and transport-assisted evacuation without replacement designated soldiers. ' if obj=='BT' else 'Prove both valid convoy routes and the normal service-pad repair command. ' if obj=='CE' else 'Prove split infantry control and majority-ticket UI through real group selection. ' if obj=='FC' else 'Prove direct and flank base assaults; a supply raid by itself never awards Victory. ')+
                  f'{m["defect"]}','',
                  f'**ARIA must play and win:** Regular seeds `{104729+n}`, `{130363+n}`, `{155921+n}` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. '
                  f'Additional exposed difficulty wins at Standard/EN use Recruit `{196613+n}`, Veteran `{262147+n}`, Commander `{327673+n}`. '
                  f'Additional exposed sizes need a full Regular EN and FA win at War seed `{393241+n}` and Large War seed `{458879+n}`, plus their device/recovery gates. '
                  'These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.','',
                  '**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. '
                  'Store evidence under `Design/AgentReports/SkirmishExpansion/'+sid+'/` and update only this publication row after every required gate. '
                  'Authoring a valid asset does not mark it Accepted.','']
                manifest.append(dict(catalog_id=sid,handoff_ordinal=ordinals[sid],remaining_after_three='no' if sid in PROTOTYPES else 'yes',
                  legacy_runtime_index=PROTOTYPES[sid][1] if sid in PROTOTYPES else '',
                  map_id=mid,operation_map_id=m['map_id'],objective_id=obj,army_profile=army,start_profile=start,
                  definition_id='skirmish.'+sid.lower(),scenario_setup_id='scenario.skirmish.'+sid.lower(),
                  layout_id=f'layout.skirmish.{mid.lower()}.{obj.lower()}',packet=file+'#'+anchor,
                  objective_system=o['system'],required_tickets=gates,required_capabilities=capabilities,
                  asset_folder='Assets/Game/Configs/SkirmishExpansion/Scenarios/'+sid,
                  first_visit_size='Standard',recommended_size=r['recommended_size'],default_difficulty='Regular',
                  regular_seeds=';'.join(str(x+n) for x in (104729,130363,155921)),
                  recruit_standard_en_seed=196613+n,veteran_standard_en_seed=262147+n,commander_standard_en_seed=327673+n,
                  war_regular_en_fa_seed=393241+n,large_war_regular_en_fa_seed=458879+n,
                  implementation_status='Planned',manual_win='Pending',aria_win='Pending',checkpoint='Pending',device='Pending',evidence_path=''))
            outputs[file]='\n'.join(lines)
        index.append('| '+m['name']+' | '+' | '.join(links)+' |')
    index+=['','Each packet is a bounded authoring assignment once its shared dependency tickets pass. '
            '[117 remaining work items](../WORK_QUEUE_004_120.csv) preserve S-IDs; [all 120 definitions](../IMPLEMENTATION_MANIFEST.csv) '
            'include prototype expansion recertification. [360 setup rows](../INITIAL_SETUP_MATRIX.csv) are deterministic planning vectors, not runtime acceptance.','']
    outputs['Scenarios/README.md']='\n'.join(index)
    manifest.sort(key=lambda r:r['catalog_id']);matrix.sort(key=lambda r:(r['catalog_id'],list(SIZE).index(r['size_id'])))
    work=sorted((r for r in manifest if r['remaining_after_three']=='yes'),key=lambda r:r['handoff_ordinal'])
    assert len(work)==117 and [r['handoff_ordinal'] for r in work]==list(range(4,121))
    assert {r['catalog_id'] for r in work}=={r['scenario_id'] for r in rows}-set(PROTOTYPES)
    outputs['IMPLEMENTATION_MANIFEST.csv']=csv_text(manifest)
    outputs['WORK_QUEUE_004_120.csv']=csv_text(work)
    outputs['INITIAL_SETUP_MATRIX.csv']=csv_text(matrix)
    outputs['ROSTER_SOURCE_AUDIT.csv']=csv_text(roster_audit())
    return outputs

def roster_audit():
    canonical={
      'Chr_Soldier_Male_02_Alt_04':'role.rifle','Chr_Soldier_Male_01':'role.gunner',
      'Chr_Soldier_Female_01':'role.marksman','Chr_Soldier_Female_02_Alt_02':'role.breacher',
      'Chr_Ghillie_Male_01':'role.rocketeer','Veh_Light_Armored_Car':'role.car',
      'Veh_APC_Fast':'role.apc_fast','Veh_APC_Slow':'role.apc_armored','Veh_APC_Heavy':'role.apc_heavy',
      'Veh_Tank_USA':'role.tank','Veh_Missle_Launcher_Air':'role.aa','Veh_Radar_Tank':'role.radar',
      'Veh_Missle_Launcher_Ground':'role.siege','Veh_Helicopter_Transport':'role.transport_heli',
      'Veh_Helicopter_Attack_Small':'role.attack_heli_light','Veh_Helicopter_Attack':'role.attack_heli',
      'Veh_Drone':'role.drone','Veh_Jet_02':'role.fighter','Veh_Jet_01':'role.strike',
      'Veh_Plane_Transport':'role.transport_plane','Veh_Truck_Tray':'role.logistics_truck;role.convoy_objective',
      'Veh_Truck_Tanker':'role.tanker'}
    structures={'Building_Barrack','Helipad','Airport','Building_Satelite_Dish','OilPump','OilRefinery','OilRefinery_Big','Fuel_Bladder','Ammunition_Depot','GuardTower','GuardTower_Big','Road_Barrier','Wall_Dirt_Straight','Wall_Fence_Straight'}
    result=[]
    for kind,pattern,prefix in [('Unit','Prefab_UnitGrid*.asset','Prefab_UnitGrid_'),('Building','Prefab_BuildingDefinition*.asset','Prefab_BuildingDefinition_')]:
        paths=sorted((REPO/'Assets/Game/Configs/Prefabs').glob(pattern))
        assert len(paths)==(51 if kind=='Unit' else 23),(kind,len(paths))
        for p in paths:
            raw=p.read_bytes();s=raw.decode('utf-8');name=re.search(r'^  displayName: (.*)',s,re.M)
            key=p.stem.removeprefix(prefix).removesuffix('_Config')
            if kind=='Building': role='';disposition='Supported facility or defensive variant' if key in structures else 'Scenario/Sandbox or visual source only'
            elif key in canonical: role=canonical[key];disposition='Canonical role candidate; capability certification required'
            elif any(x in key for x in ('Civilian','Pilot','Leader','Bombsuit')): role='';disposition='Scenario/Sandbox only; no invented special combat ability'
            elif 'Insurgent' in key or 'Contractor' in key: role='';disposition='Alternate-faction appearance after explicit equivalent-role certification'
            elif key=='Veh_Truck_Canopy': role='';disposition='Transport/logistics variant after seat/role/cap accounting certification'
            elif name and ('Rifleman' in name.group(1) or 'Marksman' in name.group(1)): role='';disposition='Role appearance variant; preserve role equivalence and shared cap'
            else: role='';disposition='Scenario/Sandbox candidate; distinct sidearm/special role not in core five-role scope'
            result.append(dict(asset_kind=kind,source_config=str(p.relative_to(REPO)),observed_display_name=name.group(1) if name else '',
              source_sha256=hashlib.sha256(raw).hexdigest(),proposed_role=role,proposed_disposition=disposition,
              evidence='SourceReadOnly',runtime_certification='Pending',
              planned_use_contexts=('S023 CE DB Combined; S095 CE IB Combined' if 'convoy_objective' in role else 'S005 BA DB Combined; S095 CE IB Combined') if role else 'Record accepted variant/facility/scenario context before claiming coverage'))
    return result

def verify_links(outputs):
    # Resolve before existence checks: Scenarios may not exist on first generation.
    documents={str(p.relative_to(ROOT)):p.read_text(encoding='utf-8') for p in ROOT.rglob('*.md')}
    documents.update(outputs)
    for name,text in documents.items():
        if name.endswith('.md'):
            for dest in re.findall(r'\]\(([^)]+)\)',text):
                plain=dest.split('#')[0]
                if not plain or '://' in plain: continue
                target=((ROOT/name).parent/plain).resolve()
                assert target.exists() or str(target.relative_to(ROOT)) in outputs,(name,dest)
    for row in csv.DictReader(io.StringIO(outputs['IMPLEMENTATION_MANIFEST.csv'])):
        name,anchor=row['packet'].split('#')
        assert '\n## '+anchor.upper()+'\n' in outputs[name]

def main():
    ap=argparse.ArgumentParser();g=ap.add_mutually_exclusive_group(required=True);g.add_argument('--write',action='store_true');g.add_argument('--check',action='store_true');args=ap.parse_args()
    outputs=build();verify_links(outputs)
    if args.write:
        for name,text in outputs.items():
            p=ROOT/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text,encoding='utf-8')
    else:
        drift=[name for name,text in outputs.items() if not (ROOT/name).exists() or (ROOT/name).read_text()!=text]
        if drift: raise SystemExit('Planning artifact drift; review inputs then regenerate: '+', '.join(drift))
    print('[SkirmishHandoffValidation] result=Passed scenarios=120 remainingWorkItems=117 packets=20 setupRows=360 sourceConfigs=74 mode='+('write' if args.write else 'check'))
    print('Documentation generation/consistency only; no Unity, gameplay, ARIA or device acceptance was executed.')

if __name__=='__main__': main()
