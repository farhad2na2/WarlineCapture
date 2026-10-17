def eligible(r):
 return (r.get('playing') is True and r.get('paused') is False and abs(r.get('timeScale',0)-1)<.0001 and r.get('runInBackground') is True and r.get('running') is True and r.get('busy') is True and r.get('queued') is True and r.get('activeGesture') is True and r.get('pressed') is False and r.get('dispatched')==1 and r.get('completed')==0 and r.get('samples')==0 and r.get('unexpected')==0 and r.get('interventions')==0 and r.get('deviceAdded') is True and r.get('deviceEnabled') is True and r.get('settingsPresent') is True and r.get('updateMode')=='ProcessEventsInDynamicUpdate' and r.get('backgroundBehavior')=='IgnoreFocus' and r.get('editorInputBehavior')=='AllDeviceInputAlwaysGoesToGameView' and r.get('frameUnchanged') is True and r.get('clockUnchanged') is True and r.get('gameViewCount')==1)
def qualify(rows):
 if not rows or rows[0].get('stage')!='FirstDispatched':raise ValueError('First input health capture missing')
 initial=rows[0];previous=None;before=None;issued=0;progress=[]
 for r in rows:
  if r.get('frameUnchanged') is not (r['frame']==r['firstFrame']) or r.get('clockUnchanged') is not (abs(r['unscaledTime']-r['firstUnscaledTime'])<=.0001):raise ValueError('Claimed frame/clock flags differ raw values')
  if r.get('unexpected')!=0 or r.get('interventions')!=0:raise ValueError('Input intervention')
  if r.get('firstFrame')!=initial['firstFrame'] or r.get('firstUnscaledTime')!=initial['firstUnscaledTime'] or r.get('firstEditorTime')!=initial['firstEditorTime'] or r.get('deviceId')!=initial['deviceId']:raise ValueError('Original queued gesture identity changed')
  if previous is not None and (r['frame']<previous['frame'] or r['editorTime']<previous['editorTime'] or r['samples']<previous['samples']):raise ValueError('Input health chronology')
  if r.get('stage')=='BeforeExistingGameViewFocus':
   if before is not None or not eligible(r) or r.get('recoveryEligible') is not True or r.get('focusAttempted') is not False or r['editorTime']-r['firstEditorTime']<2:raise ValueError('Unsafe pre-sample presentation recovery')
   before=r
  if r.get('stage')=='ExistingGameViewFocusIssued':
   issued+=1
   if before is None or issued!=1 or r.get('actionIssued') is not True or r.get('focusAttempted') is not True or r.get('samples')!=0 or r.get('dispatched')!=1 or r.get('completed')!=0:raise ValueError('Retry/pump/duplicate focus not permitted')
  elif r.get('actionIssued') is not False:raise ValueError('Unexpected input action')
  if r.get('stage')=='FirstNativeSamplesObserved':
   if r['samples']<1:raise ValueError('Invented input progress')
   progress.append(r)
  if r.get('stage')=='FirstNativeSampleTimeout':raise ValueError('Actual first-input timeout retained')
  previous=r
 if len(progress)!=1:raise ValueError('Actual first native samples not observed exactly once')
 if before is not None and issued!=1:raise ValueError('Missing focus action evidence')
 return {'result':'QualifiedActualFirstNativeSampleProgress','nativeSamples':progress[0]['samples'],'existingGameViewFocusIssued':issued,'originalQueuedGestureRetained':True,'inputPumpRetrySettingsDeviceMutations':False,'notGameplayAcceptance':True}
