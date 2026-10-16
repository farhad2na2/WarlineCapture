"""1369 extends the unchanged paid-combat qualifier with public preparation/real engagement prerequisites."""
import importlib.util,pathlib
P=pathlib.Path(__file__).parent
def _load(name):
 s=importlib.util.spec_from_file_location(name,P/(name+'.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def qualify(text,inputs):
 result=_load('combat_qualifier').qualify(text,inputs)
 result['fighterPublicPreparation']=_load('fighter_preparation_qualifier').qualify(inputs['preparation'],inputs['readiness'],result['queues'][2],result['queues'][3],result['effects'][1])
 result['publicInterceptHeading']=_load('intercept_qualifier').qualify(inputs['intercept'],result['queues'][2],inputs['touch'],result['airportReceipts'],inputs['preframe'],text.splitlines())
 result['publicGroundPatrolGoal']=_load('patrol_goal_qualifier').qualify(text,result['patrol'],result['queues'][2])
 return result
