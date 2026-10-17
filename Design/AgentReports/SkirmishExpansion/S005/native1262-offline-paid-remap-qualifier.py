"""Native paid receipt/actor recovery or pre-save casualty conservation. No missing-live-actor waiver."""

def qualify(rows,snapshot,paid,recovered,binding,checkpoint):
 attempt=binding['attempt']; rid=paid['reservation']
 runtime=[r for r in rows if r['stage']=='NativeManualRuntime']
 pre=[r for r in runtime if not r['recoveredNativeComplete']]
 live=[r for r in pre if r['reservation']==rid and r['actorCount']==1 and r['actorHealth']>0 and r['actorStableId']]
 if not live:raise ValueError('Paid native actor never observed alive before Save')
 stable=live[0]['actorStableId']
 if any(r['actorStableId']!=stable for r in live):raise ValueError('Original paid stable identity changed')
 saved=[r for r in rows if r['stage']=='NativeSavedPaidReceipt']
 if len(saved)!=1:raise ValueError('Exactly one actual native decoded saved receipt required')
 saved=saved[0]
 facts={'reservation':rid,'faction':1,'role':'ApcHeavy','producer':paid['producer'],'materialsPaid':paid['materialsPaid'],'members':1,'deliveredMembers':1,'supplyCost':6,'actorStableId':stable,'attempt':attempt}
 if any(saved.get(k)!=v for k,v in facts.items()) or saved.get('paused') is not True or abs(saved['elapsed']-checkpoint['savedElapsed'])>.001:raise ValueError('Saved paid receipt identity/paused clock differs')
 states=[r for r in rows if r['stage']=='NativePaidReceiptState' and r['reservation']==rid]
 # Runtime IDs belong to the freshly reconstructed building namespace.
 # Preserve exact saved/pre IDs; only completed same-attempt native recovery may remap.
 stablefacts={k:v for k,v in facts.items() if k!='producer'}
 if not states or any(any(r.get(k)!=v for k,v in stablefacts.items()) for r in states):raise ValueError('Actual runtime paid receipt invariant facts changed')
 preStates=[r for r in states if not r['recoveredNativeComplete']]
 postStates=[r for r in states if r['recoveredNativeComplete']]
 if not preStates or any(r['producer']!=paid['producer'] for r in preStates):raise ValueError('Pre-recovery producer runtime identity changed')
 producerActors=[a for a in snapshot['Actors'] if a['OldRuntimeBuildingId']==paid['producer']]
 if len(producerActors)!=1:raise ValueError('Exactly one saved live producer identity required for remap')
 producerActor=producerActors[0]
 import base64,struct
 nativeParts={p['Type']:p for p in producerActor['Parts']}
 info=struct.unpack('<7i',base64.b64decode(nativeParts['Game.Components.RuntimeBuildingCombatInfo']['Data']))
 faction=base64.b64decode(nativeParts['Game.Components.Faction']['Data'])[0]
 hp=struct.unpack('<2i',base64.b64decode(nativeParts['Game.Components.UnitHealth']['Data']))
 if producerActor['Id']!='building.ground_staging.1' or producerActor['PrefabKey']!='Building_GroundStaging' or producerActor['IsStructure']!=1 or not producerActor['HasProduction'] or info[0]!=paid['producer'] or info[1]!=1 or faction!=1 or hp[0]<=0:raise ValueError('Saved exact own living GroundStaging producer provenance differs')
 if not postStates or recovered['attempt']!=attempt or not recovered['recoveredNativeComplete'] or any(r['frame']<recovered['frame'] or r['elapsed']<checkpoint['savedElapsed'] for r in postStates):raise ValueError('Producer transition must follow genuine same-attempt completed native recovery')
 remappedIds={r['producer'] for r in postStates}
 if len(remappedIds)!=1 or next(iter(remappedIds))<=0:raise ValueError('Recovered producer ID must be positive and remain constant')
 if any(r['frame']>=postStates[0]['frame'] for r in preStates):raise ValueError('Producer ID transition chronology overlaps pre/recovered observations')

 restored=[r for r in states if r['recoveredNativeComplete']]
 if not restored or recovered['attempt']!=attempt or recovered['runtimeReadiness']!='Established' or recovered['elapsed']<checkpoint['savedElapsed']:raise ValueError('Actual same-attempt restored paid receipt/R2 missing')
 paused=[r for r in pre if r['paused'] and abs(r['elapsed']-checkpoint['savedElapsed'])<=.05]
 if not paused:raise ValueError('Actual paused pre-save actor census missing')
 present=any(a['Id']==stable for a in snapshot['Actors'])
 actorstates=[r for r in rows if r['stage']=='NativePaidActorState' and r['reservation']==rid]
 if any(r['attempt']!=attempt or r['actorStableId']!=stable or r['role']!='ApcHeavy' for r in actorstates):raise ValueError('Tracked paid actor native authority changed')
 if saved['phase']=='Live':
  if saved['remainingMembers']!=1 or not present or any(r['actorCount']!=1 or r['actorHealth']<=0 or r['actorStableId']!=stable or r['reservation']!=rid for r in paused):raise ValueError('Live saved paid actor not actually alive/present at Pause')
  if recovered['reservation']!=rid or recovered['actorStableId']!=stable or recovered['actorCount']!=1 or recovered['actorHealth']<=0:raise ValueError('Live actor recovery must preserve exact paid stable identity')
  first=restored[0]
  if first['phase']!='Live' or first['remainingMembers']!=1:raise ValueError('Live paid receipt not restored live')
  return {'branch':'LivePaidActorRecovered','countsForLivePaidActorRecovery':True,'preSaveCasualtyConserved':False,'stableId':stable,'reservation':rid,'savedReceipt':saved,'firstRestoredReceipt':first}
 if saved['phase']!='Released' or saved['remainingMembers']!=0:raise ValueError('Unsupported paid actor lifecycle at Save')
 released=[r for r in states if not r['recoveredNativeComplete'] and r['phase']=='Released' and r['remainingMembers']==0 and r['elapsed']<checkpoint['savedElapsed']]
 if not released or present:raise ValueError('Pre-save casualty needs actual earlier released receipt and absence in checkpoint')
 loss=released[0]['elapsed']
 if loss<=live[0]['elapsed'] or any(r['actorCount']!=0 or r['actorStableId'] or r['reservation'] for r in pre if r['elapsed']>=loss):raise ValueError('Paid actor alive/identity remained after alleged pre-save casualty')
 if any(r['actorCount']!=0 or r['actorStableId'] or r['reservation'] for r in runtime if r['recoveredNativeComplete']):raise ValueError('Paid actor resurrected after casualty Continue')
 if any(r['phase']!='Released' or r['remainingMembers']!=0 for r in restored):raise ValueError('Released paid receipt resurrected or changed after Continue')
 if any(r['actorHealth']>0 or r['capacityReleased']!=1 for r in actorstates if r['elapsed']>=loss):raise ValueError('Released paid actor was alive or capacity not released')
 return {'branch':'PreSaveCasualtyConserved','countsForLivePaidActorRecovery':False,'preSaveCasualtyConserved':True,'stableId':stable,'reservation':rid,'lastObservedLiveHealth':live[-1]['actorHealth'],'lastObservedLiveElapsed':live[-1]['elapsed'],'nativeReleasedElapsed':loss,'directZeroHealthObserved':any(r['actorHealth']<=0 and r['capacityReleased']==1 for r in actorstates if r['elapsed']>=loss),'savedReceipt':saved,'firstRestoredReceipt':restored[0],'noResurrection':True}
