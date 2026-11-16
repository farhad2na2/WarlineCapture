using System;
using Game.Simulation;
using Game.Skirmish.Contracts;

internal static class S004LogicalContextCandidateControl
{
    static GroundMissionContext Context(string catalog,string definition,int scenario,SkirmishArmyProfileId army,
        SkirmishStartPackageId start,SkirmishReadinessStage readiness) =>
        new GroundMissionContext(catalog,definition,2,123,"fixture-session",scenario,SkirmishObjectiveKind.BaseAssault,
            army,start,readiness,AriaGroundApproachKind.Ordinary,"fixture-source","DB",scenario,scenario,"fixture-preset",AriaS002Scope.LogicalPublicData);
    static void Check(GroundMissionContext context,int scenario,bool air,bool expected)
    {
        var view=new AriaS002Observation { Scope=AriaS002Scope.LogicalPublicData, SourceScenarioIndex=scenario,
            ExpandedSession=true,ExpandedBaseAssault=true,AirProfile=air };
        bool accepted=true;
        try { context.RequireCompatibleObservation(view); } catch(NotSupportedException) { accepted=false; } catch(ArgumentException) { accepted=false; }
        if(accepted!=expected) throw new InvalidOperationException("Unexpected identity admission "+context.CatalogId+"/"+scenario);
    }
    static void Main()
    {
        var s004=Context("S004","skirmish.s004",6,SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.EstablishedBase,SkirmishReadinessStage.Established);
        Check(s004,6,true,true); Check(s004,5,true,false); Check(s004,6,false,false);
        Check(Context("S004","skirmish.s004",5,SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.EstablishedBase,SkirmishReadinessStage.Established),5,true,false);
        Check(Context("S004","skirmish.s004",6,SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.FieldBase,SkirmishReadinessStage.Field),6,true,false);
        Check(Context("S003","skirmish.s003",5,SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.FieldBase,SkirmishReadinessStage.Field),5,true,true);
        Check(Context("S002","skirmish.s002",4,SkirmishArmyProfileId.GroundManeuver,SkirmishStartPackageId.FieldBase,SkirmishReadinessStage.Field),4,false,true);
        Console.WriteLine("[S004LogicalContextCandidateControl] result=Passed cases=7 fixtureOnly=1 runtimeAdmission=0 readiness=0");
    }
}
