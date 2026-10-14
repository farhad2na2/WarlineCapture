"""Synthetic offline parser controls only. No native, source or gameplay calls."""
import copy,hashlib,importlib.util,json,pathlib

bundle=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('campaign_m3_1356_qualifier',bundle/'run_campaign_m3_1356.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
log='[CampaignM3Journey1356] result=Passed\n[CampaignNativeSelection] result=Passed\n[CampaignScopedJourney] result=Passed mission=3\n[CampaignTutorialEntry] result=Passed\n'
mission='saga.ch01.m03.radar_warning'
stages=['CampaignLaunchObserved','InitialDragDispatched','InitialDragAccepted','NativeHoldFourAccepted','NativeVictoryRendered','CampaignReturnRendered']
rows=[dict(stage=s,entry=7,frame=10+i,mission=mission,session='native-m3',attempt=1,readOnly=True,launchOrigin='CampaignOperations',returnDestination='CampaignOperations',selected=4,held=4,group='rifle-native',convoyElements=2,convoyResolved=2,hostileTotal=7,hostileDefeated=7,actualRendered=True,outcome='Victory') for i,s in enumerate(stages)]
rows[0].update(returnDestination='None',outcome='None',phase='Engage')
for row in rows:row['locale']='fa-IR'
victory=rows[4];returned=rows[5]
returned.update(mission='',session='',attempt=0,outcome='None',returnDestination='None',launchOrigin='None',terminalSnapshot=True,shellPresent=True,shellTransition=False,shellMode='MainMenu',shellPhase='MenuReady',shellRoute='Campaign',campaignValid=True,boundTitlePresented=True,boundTitleOwned=True,renderedCampaignScreenCount=1,boundTitleOwner='nativeCampaignView:1',boundTitleText='CAMPAIGN',title='CAMPAIGN',boundTitleKey='ui.campaign.title',boundTitleSource='CAMPAIGN',boundTitlePath='Root/CampaignComposition/MenuTitlePanel/MenuTitle',boundTitleResolver='PresentedOwnedLocalizedMenuTitlePanel')
for suffix,key in [('Frame','frame'),('Mission','mission'),('Session','session'),('Attempt','attempt'),('Locale','locale'),('Outcome','outcome'),('LaunchOrigin','launchOrigin'),('ReturnDestination','returnDestination')]:returned['terminal'+suffix]=victory[key]
rows.append(dict(stage='PostResultShellCensus',entry=7,frame=returned['frame'],readOnly=True))
def sample(wall,eligible=True,visible=True,missing=0):
    return dict(entry=7,mission=mission,phase='Engage',wall=wall,eligibleDefense=eligible,legitimateOwnedSurface=not eligible,activePredicatePanel=visible,statusText='00:30' if visible else '',missingSeconds=missing)
base=[sample(10)]
cases=[]
def check(name,expect_pass,raw=None,status=None,text=log):
    errors=module.qualify(text,rows if raw is None else raw,base if status is None else status,'fa-IR')
    passed=not errors
    assert passed==expect_pass,(name,errors)
    cases.append(dict(name=name,expectedAccepted=expect_pass,actualAccepted=passed,errors=errors))
check('valid complete ordered native witnesses',True)
for index,key,value,name in [(0,'launchOrigin','FirstLaunch','reject nonCampaign public origin'),(0,'returnDestination','CampaignOperations','reject premature terminal destination at launch'),(0,'outcome','Victory','reject terminal outcome disguised as launch'),(4,'returnDestination','None','reject missing native Victory destination'),(5,'terminalReturnDestination','None','reject missing frozen return destination'),(5,'terminalOutcome','Defeat','reject nonVictory frozen return')]:
    bad=copy.deepcopy(rows);bad[index][key]=value;check(name,False,raw=bad)
for stage in ('InitialDragAccepted','NativeHoldFourAccepted','NativeVictoryRendered','CampaignReturnRendered'):
    check('reject missing '+stage,False,raw=[r for r in rows if r['stage']!=stage])
bad=copy.deepcopy(rows);bad[4]['convoyResolved']=1;check('reject missing second resolved convoy',False,raw=bad)
check('reject missing eligible visible countdown',False,status=[sample(10,eligible=False,visible=False)])
check('reject genuine eligible missing gap greater than1.5',False,status=[sample(10),sample(11,visible=False),sample(12.6,visible=False,missing=1.6),sample(12.7)])
check('allow long legitimate FullMap suppression followed by immediate eligible restoration',True,status=[sample(10),sample(11,eligible=False,visible=False),sample(14,eligible=False,visible=False,missing=3),sample(14.1,visible=False,missing=3.1),sample(14.2)])
check('reset previous missing gap on legitimate own surface',True,status=[sample(10),sample(11,visible=False),sample(12,eligible=False,visible=False,missing=1),sample(14,visible=False,missing=3),sample(14.1)])
check('reject native explicit countdown failure even after restored visibility',False,text=log+'[CampaignNativeContinuousStatus1356] result=Failed\n')
check('reject nonmonotonic status chronology',False,status=[sample(10),sample(9)])
check('reject nonfinite status time',False,status=[sample(10),sample(float('nan'))])
white=[sample(10),sample(11),sample(12.6),sample(12.7)]
white[1]['statusText']=white[2]['statusText']='   '
check('whitespace is missing and rejects true eligible gap',False,status=white)
bad=copy.deepcopy(rows);bad[2]['selected']=3;check('reject partial initial Rifle selection',False,raw=bad)
bad=copy.deepcopy(rows);bad[3]['held']=3;check('reject partial Rifle Hold',False,raw=bad)
bad=copy.deepcopy(rows);bad[4]['actualRendered']=False;check('reject unrendered native Victory',False,raw=bad)
bad=copy.deepcopy(rows);bad[5]['frame']=1;check('reject return before Victory',False,raw=bad)
for key,value in [('terminalSnapshot',False),('terminalFrame',1),('terminalMission','wrong'),('terminalSession','wrong'),('terminalAttempt',2),('terminalLocale','en'),('shellPresent',False),('shellTransition',True),('shellRoute','MainMenu'),('shellMode','Loading'),('shellPhase','Loading'),('campaignValid',False),('renderedCampaignScreenCount',2),('boundTitlePresented',False),('boundTitleOwned',False),('boundTitleOwner',''),('boundTitleText','wrong')]:
    bad=copy.deepcopy(rows);bad[5][key]=value;check('reject incorrect menu provenance '+key,False,raw=bad)
check('reject absent post-result census',False,raw=[r for r in rows if r['stage']!='PostResultShellCensus'])
bad=copy.deepcopy(rows);bad[5].update(mission=mission,session='native-m3',attempt=1,outcome='Victory',returnDestination='CampaignOperations',launchOrigin='CampaignOperations');check('accept retained runtime when same actual menu evidence exists',True,raw=bad)
# Bilingual successor requires independent complete native witnesses for both locales.
english=copy.deepcopy(rows)
for row in english:
 row['entry']=2
 if 'locale' in row:row['locale']='en'
 if row.get('stage')=='CampaignReturnRendered':row['terminalLocale']='en'
english_status=[dict(base[0],entry=2,wall=10)]
farsi=copy.deepcopy(rows)
for row in farsi:
 row['frame']+=30
 if 'terminalFrame' in row:row['terminalFrame']+=30
allrows=english+farsi;allstatus=english_status+[dict(base[0],entry=7,wall=30)]
def bilingual(name,raw,stat,positive=False):
 errors=module.qualify(log,raw,stat,'bilingual');assert bool(not errors)==positive,(name,errors)
 cases.append(dict(name=name,expectedAccepted=positive,actualAccepted=not errors,errors=errors))
bilingual('complete bilingual witnesses',allrows,allstatus,True)
bilingual('reject FA only for bilingual',rows,base)
bilingual('reject EN only for bilingual',english,english_status)
bad=copy.deepcopy(allrows);next(r for r in bad if r['entry']==2 and r.get('stage')=='NativeVictoryRendered')['convoyResolved']=1;bilingual('reject EN unresolved second convoy',bad,allstatus)
bad=copy.deepcopy(allrows);next(r for r in bad if r['entry']==7 and r.get('stage')=='NativeVictoryRendered')['convoyResolved']=1;bilingual('reject FA unresolved second convoy',bad,allstatus)
bad=copy.deepcopy(allrows);next(r for r in bad if r['entry']==2 and r.get('stage')=='CampaignReturnRendered')['terminalLocale']='fa-IR';bilingual('reject English frozen terminal wrong locale',bad,allstatus)
bad=copy.deepcopy(allrows);next(r for r in bad if r['entry']==7 and r.get('stage')=='CampaignReturnRendered')['shellRoute']='MainMenu';bilingual('reject FA wrong rendered return route',bad,allstatus)
for key,value in [('boundTitleKey','wrong.key'),('boundTitleSource',''),('boundTitlePath','Root/CampaignComposition/MissionSelectState/ScreenTitle'),('boundTitleResolver','HiddenLegacyScreenTitle')]:
    bad=copy.deepcopy(rows);bad[5][key]=value;check('reject stale/foreign/unlocalized header '+key,False,raw=bad)
for key,value in [('hostileTotal',6),('hostileDefeated',6)]:
    bad=copy.deepcopy(rows);bad[4][key]=value;check('reject incomplete native hostiles '+key,False,raw=bad)
# Locale controls for split runs; these are parser checks, not native wins.
errors=module.qualify(log,english,english_status,'en');assert not errors,errors
cases.append(dict(name='English-only complete native witness schema',expectedAccepted=True,actualAccepted=True,errors=errors))
errors=module.qualify(log,rows,base,'en');assert errors
cases.append(dict(name='English-only rejects Persian rows',expectedAccepted=False,actualAccepted=False,errors=errors))
bad=copy.deepcopy(rows)
for row in bad:
 if 'locale' in row:row['locale']='en'
 if 'terminalLocale' in row:row['terminalLocale']='en'
check('reject consistently wrong locale for Persian entry',False,raw=bad)
output=bundle/'offline-parser-controls.json' 
with output.open('x') as stream:json.dump(dict(result='Passed',controls=len(cases),scope='Synthetic flat parser controls only; no native execution or gameplay proof',runnerSha256=hashlib.sha256((bundle/'run_campaign_m3_1356.py').read_bytes()).hexdigest(),cases=cases),stream,indent=2);stream.write('\n')
print(json.dumps(dict(result='Passed',controls=len(cases),file=str(output))))
