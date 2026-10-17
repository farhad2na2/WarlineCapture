"""Read-only 1366 evidence gates. Never upgrades an originating mission failure."""
import math
def require(value,reason):
 if not value:raise ValueError(reason)
def one(rows,kind):
 found=[r for r in rows if r.get('kind')==kind]
 require(len(found)==1,'Unique '+kind+' required')
 return found[0]
def qualify(rows,military,paid_receipts,attempt,airport_id,faction):
 require(isinstance(attempt,str) and attempt and type(airport_id) is int and airport_id>0 and type(faction) is int and faction in (1,2),'Exact caller attempt/paid owner required')
 require(not any(r.get('kind')=='Failed' for r in rows),'Native observer failed; preserve partial census')
 start=one(rows,'Started');clear=one(rows,'MilitaryClearancePassed');done=one(rows,'CensusComplete')
 require(all(r.get('attempt')==attempt for r in rows),'Foreign attempt in native census')
 require(type(start.get('frame')) is int and start['frame']>0 and all(r.get('frame')==start['frame'] for r in rows),'Single actual read-only capture frame required')
 require(start.get('runtimeId')==clear.get('runtimeId')==airport_id and start.get('faction')==clear.get('faction')==faction,'Paid native owner binding drift')
 require(clear.get('passed') is True and done.get('passed') is True and clear.get('paidConstructionCertified') is False,'Preview cannot certify payment')
 owners=[r for r in rows if r.get('kind')=='NativeBuilding'];ids=[r.get('runtimeId') for r in owners]
 require(len(ids)==len(set(ids)) and all(type(i) is int and i>0 for i in ids),'Duplicate/invalid native building runtime identity')
 airport=[r for r in owners if r['runtimeId']==airport_id];require(len(airport)==1,'Measured native Airport owner missing')
 airport=airport[0]
 require(airport.get('producer')=='Airport' and airport.get('stableId')=='constructed.'+str(airport_id) and airport.get('faction')==faction and type(airport.get('health')) in (int,float) and math.isfinite(airport['health']) and airport['health']>0,'Functional constructed Airport identity required')
 require(military.get('result')=='Passed' and military.get('factions')==2 and type(military.get('buildings')) is int and military['buildings']>=3,'Actual both-base full-model military clearance required')
 observations=military.get('observations',[])
 measured=[x for x in observations if x.startswith('building='+str(airport_id)+' ')]
 require(len(measured)==1 and 'actualModelMin=' in measured[0] and 'actualModelMax=' in measured[0],'Completed Airport actual model bounds absent')
 require(not any(x.startswith('failure=') for x in observations),'Original geometry probe failed')
 # Pass only a real archived native paid-completion row from the owning fixture.
 # This is not a green preview/candidate and does not invent a transaction.
 paid=[p for p in paid_receipts if p.get('runtimeId')==airport_id and p.get('faction')==faction and p.get('phase') in ('PlayerNativeTouchPaid','EnemyNativeCompletionCaptured')]
 require(len(paid)==1,'Unique original native paid Airport completion row required')
 paid=paid[0]
 require(type(paid.get('frame')) is int and 0<paid['frame']<=start['frame'] and type(paid.get('materialsCost')) is int and paid['materialsCost']>0 and paid.get('materialsSpentDelta')==paid['materialsCost'] and type(paid.get('materialsSpentDelta')) is int and paid.get('fullPreviewValid') is True,'Actual exact native paid debit/full preview witness required')
 require(all(type(airport.get(k)) is int and airport.get(k)==paid.get(k) for k in ('originX','originZ','footprintX','footprintZ')) and airport['footprintX']>0 and airport['footprintZ']>0,'Actual paid completion origin/footprint versus live producer drift')
 if faction==2:require(paid.get('status')==1 and paid.get('nativeResult')==0 and paid.get('ownerFlag')==1 and paid.get('granted') is False and paid.get('allowNonbuildable') is False and paid.get('relocated') is False,'Native successful strict paid enemy construction request required')
 candidates=[r for r in rows if r.get('kind')=='AuthoredIntersectionCandidate'];require(type(done.get('candidates')) is int and 0<=done['candidates']<=5000 and done['candidates']==len(candidates),'No truncated/missing civilian candidate census')
 require(type(done.get('buildings')) is int and type(done.get('vegetation')) is int and done['buildings']>0 and done['vegetation']>0 and isinstance(done.get('databaseHash'),str) and done['databaseHash'],'Actual packed source coverage required')
 seen=set()
 for r in candidates:
  pair=(r.get('buildingPlacement'),r.get('vegetationPlacement'))
  require(all(type(i) is int and i>=0 for i in pair) and pair not in seen,'Duplicate/invalid authored source pair');seen.add(pair)
  for prefix in ('','other'):
   def field(axis,bound):return prefix+(bound[0].upper()+bound[1:] if prefix else bound)+axis
   values=[r.get(field(axis,bound)) for axis in 'XYZ' for bound in ('min','max')]
   require(all(type(v) in (int,float) and math.isfinite(v) for v in values),'Nonfinite native source bounds')
  require(all(min(r['max'+axis],r['otherMax'+axis])-max(r['min'+axis],r['otherMin'+axis])>.0001 for axis in 'XYZ'),'Nonintersecting authored bounds incorrectly reported')
  require(r.get('renderedIntersectionCertified') is False,'AABB cannot certify rendered intersection')
 return dict(result='PassedScopedMilitaryClearanceWithAuthoredCandidates' if candidates else 'PassedScopedMilitaryClearanceAndNoPackedCandidate',attempt=attempt,paidAirportRuntimeId=airport_id,faction=faction,militaryBuildings=military['buildings'],authoredCandidateCount=len(candidates),civilianVisualReviewPending=bool(candidates),civilianCoverage='Packed authored intact buildings versus standing vegetation only; legacy renderer/collider and actual live visual state not certified',originatingMissionQualified=False,playerReady=False,nativeWrapperClosureQualified=False,sourceEqualityQualified=False)
