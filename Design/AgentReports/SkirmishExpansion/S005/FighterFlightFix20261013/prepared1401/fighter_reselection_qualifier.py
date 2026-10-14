"""Exact scoped public map camera loss followed by native paid-card reselection. Offline only."""
import math
STAGES=('PublicMapSelectionScopeBegin','PublicMapSelectionLost','PublicPaidFighterReselectTouch','PublicPaidFighterReselected','PublicMapSelectionScopeClosed')
def qualify(rows):
 ops=[r for r in rows if r['stage'] in STAGES]
 if not ops:raise ValueError('Actual owned public-map selection scopes missing')
 grouped={1:[],21:[]};identity=None;paid=None;last=-1
 for r in ops:
  for k in ('cameraScope','selectionCount','mapStep','mapAttempts','paidReservation','selectionGestureExpected','mapCompleted','actorIndex','actorVersion','attemptId','frame','dispatched','completed','selected','hold','airborne','acceptedNow','acceptedBefore','popupOpen','mode','editorTime'):
   if k not in r:raise ValueError('Actual flat map/reselection schema missing '+k)
  key=(r['attemptId'],r['actorIndex'],r['actorVersion'])
  if not key[0] or key[1]<=0 or key[2]<0 or identity is not None and key!=identity:raise ValueError('Paid native actor/attempt changed')
  identity=key
  if type(r['paidReservation']) is not int or r['paidReservation']<=0 or paid is not None and paid!=r['paidReservation']:raise ValueError('Exact paid reservation missing/changed')
  paid=r['paidReservation']
  if r['cameraScope'] not in grouped or type(r['frame']) is not int or r['frame']<last:raise ValueError('Unexpected camera scope or chronology')
  last=r['frame']
  if not r['hold'] or not r['airborne'] or r['acceptedNow']!=r['acceptedBefore'] or r['mode']!='Select':raise ValueError('Map/reselection changed flight/Hold/order state or Select mode')
  if not isinstance(r['editorTime'],(int,float)) or isinstance(r['editorTime'],bool) or not math.isfinite(r['editorTime']):raise ValueError('Invalid actual map time')
  for k in ('completed','dispatched','selectionGestureExpected','selectionCount','mapStep','mapAttempts'):
   if type(r[k]) is not int or r[k]<0:raise ValueError('Invalid actual paired/map counter')
  if r['completed']>r['dispatched'] or r['selectionCount']!=(1 if r['selected'] else 0):raise ValueError('Paired counters or exclusive own-selection state contradict')
  grouped[r['cameraScope']].append(r)
 ranges=[]
 for scope,seq in grouped.items():
  names=[r['stage'] for r in seq]
  if names==['PublicMapSelectionScopeBegin','PublicMapSelectionScopeClosed']:
   begin,end=seq
   if not begin['selected'] or not end['selected'] or end['completed']<=begin['completed']:raise ValueError('No-loss camera scope did not retain exact selection/paired input')
  elif names==['PublicMapSelectionScopeBegin','PublicMapSelectionLost','PublicPaidFighterReselectTouch','PublicPaidFighterReselected']:
   begin,lost,tap,end=seq
   if not begin['selected'] or lost['selected'] or tap['selected'] or not end['selected']:raise ValueError('Exact loss/native-reselection chronology missing')
   if lost['completed']<=begin['completed'] or not begin['frame']<=lost['frame']<=tap['frame']<end['frame']:raise ValueError('Loss not after actual paired map input or restore before Touch')
   if tap['selectionGestureExpected']!=tap['dispatched'] or tap['completed']+1!=tap['selectionGestureExpected'] or end['selectionGestureExpected']!=tap['selectionGestureExpected'] or end['completed']!=end['selectionGestureExpected'] or end['dispatched']!=end['completed']:raise ValueError('Exact paid-card gesture not completed before restored selection')
   if not 0<=end['editorTime']-tap['editorTime']<=10:raise ValueError('Native paid-card restoration exceeded exact10s completion bound')
   if not tap['mapCompleted'] or tap['popupOpen'] or tap['mapStep']!=0 or tap['mapAttempts']<=0:raise ValueError('Paid-card selection before genuine map closure')
   ranges.append(dict(identity=list(identity),lossFrame=lost['frame'],restoredFrame=end['frame'],stages=[scope,11 if scope==1 else 23]))
  else:raise ValueError('Incomplete or duplicate public map/reselection scope')
  if not end['mapCompleted'] or end['popupOpen'] or end['mapStep']!=0 or end['mapAttempts']<=0:raise ValueError('Map scope ended without genuine completed map closure')
 if ops[0]['cameraScope']!=1 or grouped[1][-1]['frame']>grouped[21][0]['frame']:raise ValueError('Two actual camera scopes out of order')
 return dict(scope='Isolated public camera input; exact native paid-card reselection only',identity=list(identity),paidReservation=paid,temporarySelectionLossRanges=ranges,scopes=2,rows=ops)
