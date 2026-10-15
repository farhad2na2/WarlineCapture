"""Compose approved dispatch-through-quitting runtime error gate, preserving parents."""
from pathlib import Path
import copy,hashlib,json
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
P=ROOT/'Tools/CI/SkirmishProductionDrafts/ProductionBaselineBatchCandidateAdmission'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(P/'manifest.json')=='57fe24a4ab181437fba16c95eddda0c9521479f82d2b265ff8b213a9098dd5bc'
m=copy.deepcopy(json.loads((P/'manifest.json').read_text()))
def once(s,a,b):
    assert s.count(a)==1,(a,s.count(a));return s.replace(a,b)
def change(target,transform):
    row=next(r for r in m['files']+m['tools'] if r['target']==target);old=ROOT/row['source'];assert sha(old)==row['sha256']
    data=transform(old.read_text(encoding='utf-8'));out=HERE/Path(target).name
    with out.open('x',encoding='utf-8',newline='\n') as stream:stream.write(data)
    row['runtime_gate_parent_source']={'path':row['source'],'sha256':row['sha256']};row.update(source=out.relative_to(ROOT).as_posix(),sha256=sha(out))
def batch(s):
    s=once(s,'        private sealed class ProductionBatchContext','''        internal sealed class ProductionRuntimeErrorGate
        {
            private int count;
            internal int Count=>System.Threading.Volatile.Read(ref count);
            internal void Observe(string condition,string stackTrace,LogType type)
            {
                if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)System.Threading.Interlocked.Increment(ref count);
            }
            internal void EnsureClean()
            { if(Count!=0)throw new InvalidOperationException("Production runtime Error/Exception/Assert observations="+Count); }
        }
        private sealed class ProductionBatchContext''')
    s=once(s,'            private int exitInvocations;','''            private int exitInvocations;
            private readonly ProductionRuntimeErrorGate runtimeErrors=new ProductionRuntimeErrorGate();
            private bool runtimeObservationArmed;
            private volatile bool quitGateEmitted;
            private int finalRequestedCode=2;
            internal bool RuntimeErrorsClean=>runtimeErrors.Count==0;
            private void ArmRuntimeObservation()
            {
                if(runtimeObservationArmed)return;
                runtimeObservationArmed=true;
                Application.logMessageReceivedThreaded+=ObserveRuntimeLog;
                EditorApplication.quitting+=RuntimeQuittingGate;
            }
            private void ObserveRuntimeLog(string condition,string stackTrace,LogType type)
            {
                runtimeErrors.Observe(condition,stackTrace,type);
                // Retain this observer until process exit: a later quitting callback
                // can invalidate an earlier clean marker. Log, not LogError, avoids recursion.
                if(quitGateEmitted&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))
                    UnityEngine.Debug.Log("[SkirmishProductionRuntimeGate] result=Failed errors="+runtimeErrors.Count+" reason=ErrorAfterQuitGate");
            }
            private void RuntimeQuittingGate()
            {
                quitGateEmitted=true;
                bool clean=RuntimeErrorsClean&&stopped&&completed==jobs.Count&&finalExitRequested&&finalRequestedCode==0&&exitInvocations==1;
                Write("RuntimeQuittingGate",new JObject{["errors"]=runtimeErrors.Count,["completed_jobs"]=completed,["passed"]=clean});
                UnityEngine.Debug.Log("[SkirmishProductionRuntimeGate] result="+(clean?"Passed":"Failed")+" errors="+runtimeErrors.Count+" jobs="+completed+" actualWrapperExitStillRequired=1");
            }''')
    s=once(s,'                if(stopped)return;\n                try\n                {\n                    if(next==jobs.Count)', '                if(stopped)return;\n                ArmRuntimeObservation();\n                try\n                {\n                    runtimeErrors.EnsureClean();\n                    if(next==jobs.Count)')
    s=once(s,'                    contract.Finish(code);','                    runtimeErrors.EnsureClean();\n                    contract.Finish(code);')
    s=once(s,'                Matrix?.Recheck();','                runtimeErrors.EnsureClean();\n                Matrix?.Recheck();')
    s=once(s,'                stopped=true;Write("BatchCompleted"','                runtimeErrors.EnsureClean();\n                stopped=true;Write("BatchCompleted"')
    s=once(s,'                finalExitRequested=true;productionBatchExitOwner=this;','                if(!RuntimeErrorsClean)code=2;\n                finalRequestedCode=code;\n                finalExitRequested=true;productionBatchExitOwner=this;')
    s=once(s,'                RequestQuitIfAsked(code);\n                RestoreEnvironment();','                RequestQuitIfAsked(RuntimeErrorsClean?code:2);\n                RestoreEnvironment();')
    return s
change('Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.ProductionBatch.cs',batch)
def main(s):
    s=once(s,'bool counted = appended && nativeProvenance &&','bool counted = appended && nativeProvenance && (productionBatch==null||productionBatch.RuntimeErrorsClean) &&')
    return once(s,'bool operational=matrixJob&&SkirmishProductionMatrixAuthority.IsOperationallyComplete','bool operational=matrixJob&&(productionBatch==null||productionBatch.RuntimeErrorsClean)&&SkirmishProductionMatrixAuthority.IsOperationallyComplete')
change('Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.cs',main)
def validation(s):
    s=once(s,'        public static void RunFocusedValidation()','''        public void RuntimeErrorsCannotBecomeCleanOperationalCompletion()
        {
            var clean=new SkirmishS002AriaRunHarness.ProductionRuntimeErrorGate();
            clean.Observe("normal","",LogType.Log);clean.Observe("warning","",LogType.Warning);clean.EnsureClean();
            foreach(var type in new[]{LogType.Error,LogType.Exception,LogType.Assert})
            {
                var gate=new SkirmishS002AriaRunHarness.ProductionRuntimeErrorGate();
                gate.Observe("actual callback input","stack",type);
                MatrixControlAssert.AreEqual(1,gate.Count);MatrixControlAssert.Throws<InvalidOperationException>(gate.EnsureClean);
                gate.Observe("later log","",LogType.Log);MatrixControlAssert.AreEqual(1,gate.Count);
            }
        }
        public static void RunFocusedValidation()''')
    s=once(s,'t.OperationalDefeatAndDrawRemainLossesAndRequireCleanNativeFacts();Debug.Log','t.OperationalDefeatAndDrawRemainLossesAndRequireCleanNativeFacts();t.RuntimeErrorsCannotBecomeCleanOperationalCompletion();Debug.Log')
    return once(s,'result=Passed methods=6 baselineSlots=720','result=Passed methods=7 baselineSlots=720')
change('Assets/Game/Scripts/Editor/Skirmish/SkirmishProductionMatrixAuthorityValidation.cs',validation)
def tests(s):return once(s,'        public static void RunFocusedValidation()', '''        [Test] public void RuntimeErrorsCannotBecomeCleanOperationalCompletion()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().RuntimeErrorsCannotBecomeCleanOperationalCompletion();
        public static void RunFocusedValidation()''')
change('Assets/Tests/Editor/SkirmishExpansion/SkirmishProductionMatrixAuthorityTests.cs',tests)
def launcher(s):
    s=once(s,'    count=len(descriptor[\'jobs\'])','''    count=len(descriptor['jobs'])
    runtime_marker='[SkirmishProductionRuntimeGate] result=Passed errors=0 jobs='+str(count)+' actualWrapperExitStillRequired=1'
    runtime=[row['facts'] for row in events if row['kind']=='RuntimeQuittingGate']
    if log.count(runtime_marker)!=1 or '[SkirmishProductionRuntimeGate] result=Failed' in log or len(runtime)!=1 or runtime[0].get('passed') is not True or type(runtime[0].get('errors')) is not int or runtime[0]['errors']!=0 or runtime[0].get('completed_jobs')!=count:
        raise ValueError('Whole-run runtime Error/Exception/Assert gate missing or failed')''')
    old='''quoted(METHOD) + ")\\nexit $global:LASTEXITCODE\\n", encoding='utf-8-sig')'''
    new='''quoted(METHOD) + ")\\nif($global:LASTEXITCODE -ne 0){exit $global:LASTEXITCODE}\\n$runtimeLog=[IO.File]::ReadAllText(" + quoted(log) + ")\\nif(-not $runtimeLog.Contains(" + quoted('[SkirmishProductionRuntimeGate] result=Passed errors=0 jobs='+str(len(descriptor['jobs']))+' actualWrapperExitStillRequired=1') + ") -or $runtimeLog.Contains('[SkirmishProductionRuntimeGate] result=Failed')){throw 'Required whole-run runtime gate absent or failed'}\\nexit 0\\n", encoding='utf-8-sig')'''
    return once(s,old,new)
change('Tools/CI/invoke_skirmish_production_batch.py',launcher)
m['runtime_gate_parent']={'path':(P/'manifest.json').relative_to(ROOT).as_posix(),'sha256':sha(P/'manifest.json')}
m['delta']='Existing S005 candidate QA admission restored plus actual scoped Unity runtime Error/Exception/Assert observation from first dispatch through final quitting. Checked launcher requires exact clean quitting marker and event; late errors invalidate it. Clean losses remain operational only, never wins.'
with (HERE/'manifest.json').open('x',encoding='utf-8') as stream:json.dump(m,stream,indent=2);stream.write('\n')
print(sha(HERE/'manifest.json'))
