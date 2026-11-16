using System;
using Game.Simulation;
using Newtonsoft.Json;

internal static class GroundAirOpeningDifferentialControl
{
    static void Main()
    {
        int cases=0,waits=0,recruits=0,continued=0;
        foreach(bool air in new[]{false,true})
        foreach(int infantry in new[]{0,19,20,24})
        foreach(float now in new[]{0f,10f,50f,85f})
        foreach(float deadline in new[]{0f,50f,100f})
        foreach(byte started in new byte[]{0,1})
        foreach(bool pending in new[]{false,true})
        foreach(bool affordable in new[]{false,true})
        foreach(bool admitted in new[]{false,true})
        foreach(bool drawer in new[]{false,true})
        {
            var view=new AriaS002Observation { AirProfile=air, Infantry=infantry, Time=now, Frame=1,
                OwnDefenseTowers=2, LogisticsTruckCommitted=true,TankersCommitted=2,
                RifleRecruitPending=pending,CanAffordRifle=affordable,CatalogOpen=drawer,
                Recruit=new AriaS002ActionTarget { Admitted=admitted,Action=AriaS002Action.RecruitRifle,Id=123 },
                ReadinessEligible=true,PlayerDesignatedAlive=true,EnemyDesignatedAlive=true };
            var a=new AriaS002PlanState {AssaultStarted=started,OpeningUntil=deadline};var b=a;
            var ta=new AriaS002ContactState {Phase=AriaS002ContactPhase.Observing};var tb=ta;
            AriaS002Decision da=default,db=default;Exception ea=null,eb=null;
            try { AriaGroundBaselinePolicy.StepLegacyS002Observation(view,ref a,ref ta,out da); } catch(Exception e){ea=e;}
            try { AriaGroundBaseAssaultPolicy.StepLegacyS002Observation(view,ref b,ref tb,out db); } catch(Exception e){eb=e;}
            if ((ea==null)!=(eb==null) || ea!=null&&(ea.GetType()!=eb.GetType()||ea.Message!=eb.Message) ||
                JsonConvert.SerializeObject(a)!=JsonConvert.SerializeObject(b) ||
                JsonConvert.SerializeObject(ta)!=JsonConvert.SerializeObject(tb) ||
                JsonConvert.SerializeObject(da)!=JsonConvert.SerializeObject(db))
                throw new InvalidOperationException("Ground air-opening differential mismatch at case "+cases);
            if(air && started==0 && infantry<20 && (deadline==0 || now<deadline)) {
                if(pending && a.Intent==AriaS002Intent.ObserveBattle) waits++;
                if(!pending && affordable && a.Intent==AriaS002Intent.Recruit) recruits++;
            }
            if(air && started==0 && infantry>=20 && a.AssaultStarted==1) continued++;
            cases++;
        }
        if(waits==0 || recruits==0 || continued==0)throw new InvalidOperationException("Air-opening coverage missing");
        Console.WriteLine("[GroundAirOpeningDifferentialControl] result=Passed cases="+cases+" wait="+waits+" recruit="+recruits+" continued="+continued+" fixtureOnly=1 nativeInput=0 readiness=0");
    }
}
