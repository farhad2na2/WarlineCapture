import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph'))
from source_s004_terminal_gate import activation_admitted


class TerminalGateTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.out=Path(self.tmp.name)
        self.event={'scope':'SourceOwnerSubset_NotNativeOrFullScheduler','reason':'Terminal','tick':31,'phase':'Finished',
                    'terminal':True,'match_outcome':'Victory','objective_outcome':'Victory','native_input_claimed':False,'mission_ready':False}
        self.production={'requested_ticks':40,'completed_ticks':20,'source_tick_after':31,'terminal_after':True,
                         'objective_outcome_after':'Victory','scope':'SourceOwnerSubset_NoNativeInputOrReadiness',
                         'progress_path':str(self.out/'source-progress.jsonl'),'progress_rows':1}
        self.data={'source_simulation_active':0,'mission_ready':False,'source_gameplay_diagnostic':{'native_readiness_claimed':False,
                   'source_bank_diagnostic':{'source_experimental_tick':{'tick':11},'source_production_ticks':self.production}}}
        self.write()
    def tearDown(self): self.tmp.cleanup()
    def write(self): (self.out/'source-progress.jsonl').write_text(json.dumps(self.event)+'\n')
    def admitted(self,mode=True): return activation_admitted(self.data,mode,self.out,40)
    def test_original_actual_terminal_outcomes(self):
        for outcome in ('Victory','Defeat','Draw'):
            self.production['objective_outcome_after']=outcome;self.event['objective_outcome']=outcome;self.event['match_outcome']=outcome;self.write();self.assertTrue(self.admitted())
    def test_terminal_requires_explicit_mode_and_inactive(self):
        self.assertFalse(self.admitted(False));self.data['source_simulation_active']=1;self.assertFalse(self.admitted())
    def test_nonterminal_requires_active(self):
        self.production['terminal_after']=False;self.assertFalse(self.admitted());self.data['source_simulation_active']=1;self.assertTrue(self.admitted(False))
    def test_result_disagreement_rejected(self):
        for field,value in (('match_outcome','Defeat'),('objective_outcome','Draw'),('phase','Playing'),('terminal',False),('tick',32),('native_input_claimed',True),('mission_ready',True)):
            old=self.event[field];self.event[field]=value;self.write();self.assertFalse(self.admitted(),field);self.event[field]=old
    def test_production_or_requested_count_drift_rejected(self):
        for field,value in (('source_tick_after',32),('completed_ticks',21),('requested_ticks',41),('objective_outcome_after','None'),('progress_rows',2)):
            original=self.production[field];self.production[field]=value;self.assertFalse(self.admitted(),field);self.production[field]=original
    def test_foreign_or_missing_progress_rejected(self):
        self.production['progress_path']=str(self.out.parent/'source-progress.jsonl');self.assertFalse(self.admitted())
        self.production['progress_path']=str(self.out/'source-progress.jsonl');(self.out/'source-progress.jsonl').unlink();self.assertFalse(self.admitted())
    def test_readiness_or_forged_outcome_rejected(self):
        self.data['mission_ready']=True;self.assertFalse(self.admitted());self.data['mission_ready']=False
        self.data['source_gameplay_diagnostic']['native_readiness_claimed']=True;self.assertFalse(self.admitted())

if __name__=='__main__': unittest.main()
