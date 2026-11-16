using System;
using Game.Simulation;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Newtonsoft.Json;

// Synthetic differential only. It invokes the exact generic native Step for
// scenario 6 and the existing shared source policy on the native adapter's
// projection; it never dispatches a command, touch, or mission outcome.
internal static class S004OriginalVsLogicalPolicyControl
{
    static AriaTouchTarget Control(int id, bool available) => new AriaTouchTarget { Id=id, Available=available };
    static void Main()
    {
        int cases=0, decisionMismatch=0, stateMismatch=0, exceptionMismatch=0, samples=0;
        int waiting=0, logisticsCount=0, tankerCount=0, airCount=0, other=0;
        foreach(bool logistics in new[]{false,true})
        foreach(int tankers in new[]{0,1,2})
        foreach(bool logisticsAvailable in new[]{false,true})
        foreach(bool tankerAvailable in new[]{false,true})
        foreach(bool ready in new[]{false,true})
        foreach(bool pad in new[]{false,true})
        foreach(bool airOffered in new[]{false,true})
        foreach(bool airAvailable in new[]{false,true})
        foreach(int airLive in new[]{0,1})
        {
            var view=new AriaSkirmishObservation {
                SourceScenarioIndex=6,Active=true,ExpandedSession=true,ExpandedBaseAssault=true,AirProfile=true,
                Frame=11,Time=20f,Infantry=20,OwnDefenseTowers=2,PlayerDesignatedAlive=true,EnemyDesignatedAlive=true,
                LogisticsTruckCommitted=logistics,CanAffordLogisticsTruck=true,TankersCommitted=tankers,CanAffordTanker=true,
                RecruitLogisticsTruck=Control(101,logisticsAvailable),RecruitTanker=Control(102,tankerAvailable),
                ReadinessEligible=ready,PadPresent=pad,AirQueueOffered=airOffered,RecruitAir=Control(103,airAvailable),
                OwnAttackAirLive=airLive
            };
            if(AriaS002NativeAdapter.CanHandle(view)) throw new InvalidOperationException("Original scenario 6 adapter route changed");
            var nativePlan=default(AriaSkirmishPlanComponent);
            var nativeTouch=new AriaPlaySessionComponent {Phase=AriaPlayPhase.Observing};
            var nativeOutput=default(AriaPlayObservationComponent);
            var sourceView=AriaS002NativeAdapter.Observation(view,nativeTouch);
            var sourcePlan=AriaS002NativeAdapter.Plan(nativePlan);
            var sourceTouch=AriaS002NativeAdapter.Contact(nativeTouch);
            var sourceOutput=default(AriaS002Decision);
            Exception ne=null,se=null;
            try { AriaSkirmishPlanSystem.Step(view,ref nativePlan,ref nativeTouch,ref nativeOutput); } catch(Exception e){ne=e;}
            try { AriaGroundBaseAssaultPolicy.StepLegacyS002Observation(sourceView,ref sourcePlan,ref sourceTouch,out sourceOutput); } catch(Exception e){se=e;}
            bool ex=(ne==null)!=(se==null) || ne!=null && se!=null && (ne.GetType()!=se.GetType() || ne.Message!=se.Message);
            bool decision=!ex && ne==null && ((int)nativeOutput.Kind!=(int)sourceOutput.Kind || nativeOutput.TargetId!=sourceOutput.TargetId ||
                (int)nativePlan.Intent!=(int)sourcePlan.Intent);
            bool state=!ex && ne==null && (JsonConvert.SerializeObject(AriaS002NativeAdapter.Plan(nativePlan))!=JsonConvert.SerializeObject(sourcePlan) ||
                JsonConvert.SerializeObject(AriaS002NativeAdapter.Contact(nativeTouch))!=JsonConvert.SerializeObject(sourceTouch));
            if(ex) exceptionMismatch++;
            if(decision) decisionMismatch++;
            if(state) stateMismatch++;
            if(ne==null) {
                if(nativeOutput.Kind==AriaPlayObservationKind.Waiting) waiting++;
                else if(nativeOutput.TargetId==101) logisticsCount++;
                else if(nativeOutput.TargetId==102) tankerCount++;
                else if(nativeOutput.TargetId==103) airCount++;
                else other++;
            }
            if((ex||decision||state) && samples++<8)
                Console.WriteLine("mismatch case="+cases+" logistics="+logistics+" tankers="+tankers+" logisticsTarget="+logisticsAvailable+
                    " tankerTarget="+tankerAvailable+" readiness="+ready+" pad="+pad+" airOffer="+airOffered+" airTarget="+airAvailable+" airLive="+airLive+
                    " native="+nativeOutput.Kind+"/"+nativeOutput.TargetId+"/"+nativePlan.Intent+" shared="+sourceOutput.Kind+"/"+sourceOutput.TargetId+"/"+sourcePlan.Intent+
                    " nativeException="+(ne?.GetType().Name??"none")+" sharedException="+(se?.GetType().Name??"none")+" stateDiff="+state);
            cases++;
        }
        if(cases!=768) throw new InvalidOperationException("Synthetic case domain changed: "+cases);
        Console.WriteLine("[S004OriginalVsLogicalPolicy] result=Collected cases="+cases+" decisionMismatch="+decisionMismatch+
            " stateMismatch="+stateMismatch+" exceptionMismatch="+exceptionMismatch+" waiting="+waiting+
            " logistics="+logisticsCount+" tanker="+tankerCount+" air="+airCount+" other="+other+" fixtureOnly=1 nativeInput=0 readiness=0");
    }
}
