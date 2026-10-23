using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Runtime;
using Game.Components;
using Game.Runtime.Combat;
using Game.Skirmish.Contracts;

// Bounded stationary host. Runtime-specific decisions stay in shared production kernels.
internal static class WorldFrameReplay
{
    internal sealed class Weapon
    {
        public StandardAttackState State;
        public float Range=0, CooldownSeconds=0, TraceVisibleSeconds=0, AttackAnimationSeconds=0;
        public int Damage=0, TracerEveryNthShot=0;
    }
    internal sealed class Actor
    {
        public string Id, Kind, Target, Recent = "";
        public int Index, Version, Health, CellX, CellZ;
        public byte Faction;
        public float X, Y, Z, Radius;
        public bool HasEngage, CoverEligible, ProtectedRecords;
        public ushort CoverPermille;
        public Weapon Weapon;
        public JsonElement Acquisition;
        public bool MovingProfile;
        public byte CanAttack=1,EngageIsCommanded=0;
        public GridCoordinateFrame Coordinates;
        public float EngageX,EngageY,EngageZ;
        public int EngageCellX,EngageCellZ;
        public bool Mobile => Kind == "infantry";
    }
    internal static void Need(bool condition,string message) => WorldJson.Need(condition,message);
    static int Int(JsonElement j,string name,int min,int max)
    { int n=j.GetProperty(name).GetInt32();Need(n>=min&&n<=max,name+" outside range");return n; }
    static float Float(JsonElement j,string name,float min,float max)
    { float n=j.GetProperty(name).GetSingle();Need(float.IsFinite(n)&&n>=min&&n<=max,name+" outside range");return n; }
    static string Text(JsonElement j,string name)
    { string s=j.GetProperty(name).GetString();Need(!string.IsNullOrWhiteSpace(s),name+" required");return s; }
    internal static Actor ReadActor(JsonElement row,bool moving=false)
    {
        WorldJson.Shape(row,new[]{"id","kind","index","version","health","faction","x","y","z","cell_x","cell_z","radius",
            "has_unit_grid","combat_suppressed","has_animation_order","has_engage","target","cover_eligible","cover_permille","protected_records","weapon","acquisition"});
        var a=new Actor { MovingProfile=moving, Id=Text(row,"id"),Kind=Text(row,"kind"),Index=Int(row,"index",1,int.MaxValue),Version=Int(row,"version",1,int.MaxValue),
            Health=Int(row,"health",0,int.MaxValue),Faction=(byte)Int(row,"faction",1,255),X=Float(row,"x",-1000000,1000000),
            Y=Float(row,"y",-1000000,1000000),Z=Float(row,"z",-1000000,1000000),CellX=Int(row,"cell_x",-1000000,1000000),CellZ=Int(row,"cell_z",-1000000,1000000),
            Radius=Float(row,"radius",0,10000),HasEngage=row.GetProperty("has_engage").GetBoolean(),Target=row.GetProperty("target").GetString(),
            CoverEligible=row.GetProperty("cover_eligible").GetBoolean(),ProtectedRecords=row.GetProperty("protected_records").GetBoolean(),CoverPermille=(ushort)Int(row,"cover_permille",0,65535),Acquisition=row.GetProperty("acquisition") };
        Need(!row.GetProperty("combat_suppressed").GetBoolean(),"Campaign-suppressed actors outside bounded profile");
        Need(!row.GetProperty("has_animation_order").GetBoolean(),"Animation-order actors require native animation ticking");
        Need(row.GetProperty("has_unit_grid").GetBoolean()==a.Mobile,"Infantry must have UnitGrid; target-only bases must not");
        Need(a.Radius==0,"Only single-cell infantry and bases without footprint radius supported");
        Need(a.Kind=="infantry"||a.Kind=="base","Only stationary infantry and target-only runtime buildings supported");
        Need(a.Target!=null&& (a.HasEngage||a.Target==""),"Target requires EngageTarget component");
        if(a.Mobile)
        {
            Need(a.Health>0,"Initial dead mobile actor needs native teardown");
            WorldJson.FactsShape(row.GetProperty("weapon"),typeof(Weapon));
            // Classes require exact shape explicitly; FactsShape validates nested value fields.
            WorldJson.Shape(row.GetProperty("weapon"),new[]{"State","Range","CooldownSeconds","TraceVisibleSeconds","AttackAnimationSeconds","Damage","TracerEveryNthShot"});
            WorldJson.FactsShape(row.GetProperty("weapon").GetProperty("State"),typeof(StandardAttackState));
            a.Weapon=row.GetProperty("weapon").Deserialize<Weapon>(WorldJson.Json);WorldJson.FiniteFields(a.Weapon);
            WorldAcquisition.ValidateActor(a);
            Need(a.Weapon.State.AnimationTimeRemaining==0,"Initial animation-dependent state unsupported");
            Need(a.Weapon.Range>0&&a.Weapon.CooldownSeconds>=0&&a.Weapon.Damage>=0,"Invalid standard weapon values");
        }
        else Need(a.Acquisition.ValueKind==JsonValueKind.Null&&row.GetProperty("weapon").ValueKind==JsonValueKind.Null&&!a.HasEngage&&!a.CoverEligible,"Base actors must be target-only buildings without grid cover");
        return a;
    }
    static List<string> Order(JsonElement value,IEnumerable<string> expected,string name)
    {
        Need(value.ValueKind==JsonValueKind.Array,name+" must be an explicit ordered array");
        var list=value.EnumerateArray().Select(x=>x.GetString()).ToList();
        Need(list.All(x=>x!=null)&&list.Count==list.Distinct(StringComparer.Ordinal).Count(),name+" has duplicate/invalid IDs");
        Need(list.ToHashSet(StringComparer.Ordinal).SetEquals(expected),name+" must name exactly the current eligible actor set");return list;
    }
    static object Snapshot(Actor a) => new { id=a.Id,health=a.Health,has_engage=a.HasEngage,target=a.Target,recent=a.Recent,attack=a.Weapon?.State };

    internal static object Run(JsonElement scenario)
    {
        WorldJson.Shape(scenario,new[]{"id","kind","profile","profile_sha256","cell_size","actors","player_base","enemy_base","deadline_seconds","policy_profile","frames","expect"});
        Need(WorldSchedule.Enabled&&Text(scenario,"profile")==WorldSchedule.Profile,"Verified native schedule receipt required for world cases");
        Need(Text(scenario,"profile_sha256")==WorldSchedule.Hash,"Scenario schedule hash differs from verified build profile");
        Need(Text(scenario,"policy_profile")=="unrestricted_visible_no_fog","Only static unrestricted visible policies without fog supported in world profile");
        string provenance=WorldSchedule.Hash;
        float cellSize=Float(scenario,"cell_size",.001f,10000);
        var actorRows=scenario.GetProperty("actors");Need(actorRows.ValueKind==JsonValueKind.Array&&actorRows.GetArrayLength()>=2&&actorRows.GetArrayLength()<=128,"2..128 actors required");
        var actors=actorRows.EnumerateArray().Select(row=>ReadActor(row)).ToList();
        Need(actors.Select(a=>a.Id).Distinct().Count()==actors.Count&&actors.Select(a=>(a.Index,a.Version)).Distinct().Count()==actors.Count,"Duplicate actor identity");
        var byId=actors.ToDictionary(a=>a.Id,StringComparer.Ordinal);
        Need(actors.All(a=>a.Recent==""||byId.ContainsKey(a.Recent)),"Unknown initial recent-attacker identity");
        string playerBase=Text(scenario,"player_base"),enemyBase=Text(scenario,"enemy_base");
        Need(playerBase!=enemyBase&&byId.ContainsKey(playerBase)&&byId.ContainsKey(enemyBase)&&byId[playerBase].Kind=="base"&&byId[enemyBase].Kind=="base","Distinct target-only designated bases required");
        int deadline=Int(scenario,"deadline_seconds",0,int.MaxValue);
        var frames=scenario.GetProperty("frames");Need(frames.ValueKind==JsonValueKind.Array&&frames.GetArrayLength()>=1&&frames.GetArrayLength()<=2048,"1..2048 frames required");
        Need((long)actors.Count*actors.Count*frames.GetArrayLength()<=2000000,"World work budget exceeded");
        double nextAcquisition=0,lastNow=-1;
        float elapsed=0;byte playing=0,clockPaused=0;
        var phase=SkirmishSessionPhase.Playing;var outcome=SkirmishOutcomeKind.None;var reason=SkirmishEndReasonKind.None;
        bool frozen=false;int freezeCount=0;
        WorldAcquisition.ValidateWorld(actors,byId,cellSize);
        var history=new List<object>();string boundary="none";
        foreach(var frame in frames.EnumerateArray())
        {
            WorldJson.Shape(frame,new[]{"elapsed_time","delta_time","paused","surrender","has_gameplay_state","simulation_active","acquisition_order","candidate_orders","attack_order","damage_commit_order"});
            double now=frame.GetProperty("elapsed_time").GetDouble();Need(double.IsFinite(now)&&now>=0&&now>=lastNow,"Invalid/regressing world time");lastNow=now;
            float dt=Float(frame,"delta_time",0,1000);bool paused=frame.GetProperty("paused").GetBoolean(),surrender=frame.GetProperty("surrender").GetBoolean();
            var acquireOrder=Order(frame.GetProperty("acquisition_order"),actors.Where(a=>a.Mobile&&!a.HasEngage).Select(a=>a.Id),"acquisition_order");
            var attackOrder=Order(frame.GetProperty("attack_order"),actors.Where(a=>a.Mobile&&a.HasEngage).Select(a=>a.Id),"attack_order");
            var pending=new List<Action>();var acquisitionEvents=new List<object>();
            bool acquisitionAdmitted=false,attackAdmitted=false,terminalObserved=false;
            SkirmishBaseAssaultFacts observedFacts=default;
            void AcquisitionStage()
            {
                acquisitionAdmitted=WorldSchedulingKernel.TryAdvanceUnitAcquisition(now,ref nextAcquisition);
                if(acquisitionAdmitted)WorldAcquisition.Acquire(actors,byId,acquireOrder,frame.GetProperty("candidate_orders"),cellSize,pending,acquisitionEvents);
                else WorldAcquisition.ValidateOrders(actors,byId,acquireOrder,frame.GetProperty("candidate_orders"));
            }
            var decisions=new List<object>();var effects=new Dictionary<string,DirectDamageAggregate>();var predicted=new Dictionary<string,int>();
            void AttackStage()
            {
            attackAdmitted=WorldSchedulingKernel.ShouldRunAttack(true,frozen);
            if(attackAdmitted)
            {
                foreach(var idle in actors.Where(a=>a.Mobile&&!a.HasEngage))idle.Weapon.State=DirectDamageSequencer.RecoverUnengaged(idle.Weapon.State,dt);
                // Plans all read the same pre-damage actor snapshot. Commit order is explicit.
                var plans=new Dictionary<string,StandardAttackResult>();
                foreach(string id in attackOrder)
                {
                    Actor a=byId[id];byId.TryGetValue(a.Target,out Actor target);
                    var w=a.Weapon;
                    var input=new StandardAttackInput {State=w.State,SelfHealth=a.Health,TargetHealth=target?.Health??0,HasTarget=(byte)(a.Target!=""?1:0),
                        TargetExists=(byte)(target!=null?1:0),TargetHasHealth=(byte)(target!=null?1:0),TargetHasTransform=(byte)(target!=null?1:0),
                        AttackerX=a.X,AttackerZ=a.Z,TargetX=target?.X??0,TargetZ=target?.Z??0,SelfCombatRadius=a.Radius,TargetCombatRadius=target?.Radius??0,
                        Range=w.Range,CooldownSeconds=w.CooldownSeconds,TraceVisibleSeconds=w.TraceVisibleSeconds,AttackAnimationSeconds=w.AttackAnimationSeconds,
                        Damage=w.Damage,TracerEveryNthShot=w.TracerEveryNthShot};
                    plans[id]=StandardAttackPlanner.Plan(input,dt,cellSize);
                }
                foreach(string id in attackOrder)
                {
                    Actor a=byId[id];var plan=plans[id];byId.TryGetValue(a.Target,out Actor target);
                    int remaining=target==null?0:predicted.TryGetValue(a.Target,out int hp)?hp:target.Health;
                    var decision=DirectDamageSequencer.Decide(plan.Kind,true,remaining);
                    a.Weapon.State=DirectDamageSequencer.CommittedState(a.Weapon.State,plan,decision);
                    decisions.Add(new {actor=id,target=a.Target,decision,plan});
                    if(decision==StandardAttackCommit.ClearTarget){a.Target="";boundary="engagement_clear_requires_native_cleanup";continue;}
                    if(decision!=StandardAttackCommit.Shot||plan.Damage<=0)continue;
                    int damage=DirectDamageSequencer.ApplyToTarget(plan.Damage,target.CoverEligible,target.CoverPermille);
                    bool has=effects.TryGetValue(target.Id,out var aggregate);
                    var source=new DirectDamageSource {Index=a.Index,Version=a.Version,CellX=a.CellX,CellZ=a.CellZ,X=a.X,Y=a.Y,Z=a.Z};
                    predicted[target.Id]=DirectDamageSequencer.Accumulate(ref aggregate,has,remaining,damage,source,plan.ShowTracer);effects[target.Id]=aggregate;
                }
                var attackPending=new List<Action>();
                var damageOrder=Order(frame.GetProperty("damage_commit_order"),effects.Keys,"damage_commit_order");
                foreach(string id in damageOrder)
                {
                    Actor target=byId[id];int health=DirectDamageSequencer.RemainingHealth(target.Health,effects[id].TotalDamage);
                    if(target.Mobile&&health>0)
                    {
                        Need(target.HasEngage&&target.Target!=""&&byId.TryGetValue(target.Target,out var retained)&&retained.Health>0&&
                            (!predicted.TryGetValue(retained.Id,out int future)||future>0),"Nonlethal hit could trigger unsupported native counter-engagement");
                    }
                    target.Health=health;
                    var source=effects[id].LastSource;string recentSource=actors.Single(a=>a.Index==source.Index&&a.Version==source.Version).Id;
                    if(target.Recent!="")target.Recent=recentSource;else attackPending.Add(()=>target.Recent=recentSource);
                    if(target.Mobile&&health<=0)boundary="mobile_death_requires_native_teardown";
                }
                foreach(var command in attackPending)command();
            }
            else Order(frame.GetProperty("damage_commit_order"),Array.Empty<string>(),"damage_commit_order");
            }
            foreach(string stage in WorldSchedule.Stages)
            {
                if(stage=="acquisition")AcquisitionStage();
                else if(stage=="stationary_engagement"&&WorldSchedulingKernel.ShouldRunGridMovement(dt,frame.GetProperty("has_gameplay_state").GetBoolean(),(byte)Int(frame,"simulation_active",0,1)))WorldAcquisition.MaintainStationaryEngagement(actors,byId,cellSize);
                else if(stage=="attack")AttackStage();
                else if(stage=="projection")ProjectObjective();
                else if(stage=="evaluation"&&!terminalObserved)
                { observedFacts.Surrender=surrender;terminalObserved=SkirmishMissionOrchestrationKernel.TryEvaluateBaseAssault(observedFacts,out outcome,out reason); }
                else if(stage=="outcome"&&terminalObserved)
                {
                    var freeze=SkirmishMissionOrchestrationKernel.DecideFreeze(false,true,frozen);
                    if(freeze==SkirmishResultFreezeDecision.CreateFrozen){frozen=true;freezeCount++;phase=SkirmishSessionPhase.Finished;}
                }
                else if(stage=="end")foreach(var command in pending)command();
            }
            history.Add(new {elapsed_time=now,delta_time=dt,acquisition_admitted=acquisitionAdmitted,attack_admitted=attackAdmitted,
                next_acquisition=nextAcquisition,observed_ba_facts=observedFacts,stages=WorldSchedule.Stages,acquisition=acquisitionEvents,attacks=decisions,damage=effects,
                actors=actors.Select(Snapshot).ToArray(),elapsed,phase,outcome,reason,frozen,freeze_count=freezeCount,boundary});
            if(boundary!="none"||frozen)break;
            void ProjectObjective()
            {
                SkirmishMissionOrchestrationKernel.ProjectClock(false,phase,paused,dt,deadline,ref elapsed,ref deadline,ref playing,ref clockPaused);
                observedFacts=new SkirmishBaseAssaultFacts {Playing=playing!=0,Paused=clockPaused!=0,Surrender=false,
                    PlayerDesignatedAlive=byId[playerBase].Health>0,EnemyDesignatedAlive=byId[enemyBase].Health>0,
                    ElapsedSeconds=elapsed,DeadlineSeconds=deadline,ReplacementBarracksPresent=false,FieldArmyWiped=false};
                terminalObserved=SkirmishMissionOrchestrationKernel.TryEvaluateBaseAssault(observedFacts,out outcome,out reason);

            }
        }
        var actual=JsonSerializer.SerializeToElement(new {profile=WorldSchedule.Profile,profile_sha256=provenance,frames=history,consumed_frames=history.Count,
            remaining_frames=frames.GetArrayLength()-history.Count,actors=actors.Select(Snapshot).ToArray(),elapsed,phase,outcome,reason,frozen,freeze_count=freezeCount,boundary},WorldJson.Json);
        WorldJson.Expect(scenario.GetProperty("expect"),actual,"world");return actual;
    }
}
