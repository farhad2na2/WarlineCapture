#if UNITY_EDITOR
using System;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using Game.UI.Shell.Contracts.Ecs;
namespace Game.Editor
{
    public static class AriaSkirmishPlanValidation
    {
        public static void RunFocusedValidation() => UnityEngine.Debug.Log(Run());

        static int RunSurvivingStructureRifleDrawerControls()
        {
            int checks = 0;
            void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks++; }
            var view = new AriaSkirmishObservation(); var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent(); var output = new AriaPlayObservationComponent();
            void Reset()
            {
                view = new AriaSkirmishObservation { Active = true, ExpandedSession = true,
                    ExpandedBaseAssault = true, Time = 100, Frame = 100, Infantry = 12,
                    PlayerHealth = 800, EnemyHealth = 800, EnemyDesignatedAlive = true,
                    ExpandedPageCount = 2, ExpandedPageIndex = 1, ExpandedTotalGroups = 7,
                    ExpandedRosterRevision = 10, CanAffordRifle = true, OwnStructureAttackUnits = 4,
                    Recruit = new AriaTouchTarget { Available = true, Id = 31,
                        Position = new UnityEngine.Vector2(30,40) },
                    CloseDrawer = new AriaTouchTarget { Available = true, Id = 63 } };
                plan = new AriaSkirmishPlanComponent { AssaultIssued = 1, StructureOrdered = 1,
                    PagesOrdered = 3, SweepRosterRevision = 10, LastProgressAt = 100,
                    Infantry = 12, PlayerHealth = 800, EnemyHealth = 800,
                    Intent = AriaSkirmishIntent.ObserveBattle };
                touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Waiting,
                    GoalId = 10000, LastProgressAt = 100, LastObjectiveProgressAt = 100,
                    ObjectiveWatchdogInitialized = 1, VisitedGoalMask = 1UL << (10000 & 63) };
            }
            void Step()
            {
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
                AriaPlayDecisionSystem.Step(output, ref touch);
            }
            Reset(); Step();
            Check(output.TargetId == 31 && touch.Phase == AriaPlayPhase.Aiming,
                "surviving structure force approaches public Rifle Build opener");
            view.Time = 101; view.Frame++; Step();
            Check(touch.Phase == AriaPlayPhase.Touching && touch.GestureRequested == 1,
                "Rifle Build opener requires ordinary aim and touch");
            // Sample a completed input acknowledgement; no production command is fabricated.
            view.DrawerOpen = true; view.Recruit = default; view.Time = 102; view.Frame = 103;
            touch.Phase = AriaPlayPhase.Verifying; touch.GestureRequested = 0; touch.Pressed = 0;
            touch.Actions = 1; touch.LastCompletedTargetId = 31;
            touch.LastCompletedFrame = 102; touch.DueAt = 103;
            Step(); Check(plan.Intent == AriaSkirmishIntent.Recruit && output.TargetId != 63 &&
                touch.Phase == AriaPlayPhase.Verifying && touch.GestureRequested == 0,
                "surviving structure Rifle catalog gap must not become Inspect CloseDrawer");
            view.Time = 103; view.Frame++; Step();
            Check(plan.Intent == AriaSkirmishIntent.Recruit && output.TargetId != 63 &&
                touch.GestureRequested == 0, "Rifle presentation gap retains owned recruitment without purchase");
            view.Time = 103.5f; view.Frame++;
            view.Recruit = new AriaTouchTarget { Available = true, Id = 61,
                Position = new UnityEngine.Vector2(50,40) };
            Step(); Check(output.TargetId == 61 && touch.Phase == AriaPlayPhase.Aiming &&
                touch.GestureRequested == 0 && touch.Actions == 1,
                "fresh eligible Rifle item receives its own normal approach");
            view.Time = 104.5f; view.Frame++; Step();
            Check(output.TargetId == 61 && touch.Phase == AriaPlayPhase.Touching &&
                touch.GestureRequested == 1 && touch.Actions == 1,
                "Rifle purchase remains a second ordinary UI gesture");
            touch.Phase = AriaPlayPhase.Verifying; touch.GestureRequested = 0;
            touch.Actions = 2; touch.LastCompletedTargetId = 61;
            touch.LastCompletedFrame = view.Frame; touch.DueAt = view.Time + 1;
            view.RifleRecruitPending = true; view.Time++; view.Frame++;
            Step(); Check(output.TargetId == 63 && touch.Actions == 2,
                "public pending Rifle production closes without duplicate purchase");
            Reset(); view.DrawerOpen = true; view.Recruit = default;
            plan.Intent = AriaSkirmishIntent.Recruit; plan.RecruitUnavailableAt = 96;
            Step(); Check(output.TargetId == 63 && plan.NextRecruitAt == 112 &&
                plan.Intent == AriaSkirmishIntent.Inspect,
                "persistent Rifle catalog refusal retains bounded close and cooldown");
            Reset(); view.DrawerOpen = true; plan.Intent = AriaSkirmishIntent.Recruit;
            view.CanAffordRifle = false; Step();
            Check(output.TargetId != 31 && touch.GestureRequested == 0,
                "Rifle public affordability refusal cannot be overridden by drawer recovery");
            Reset(); view.DrawerOpen = true; plan.Intent = AriaSkirmishIntent.Recruit;
            view.RifleRecruitPending = true; Step();
            Check(output.TargetId != 31 && touch.GestureRequested == 0,
                "existing pending Rifle prevents another purchase");
            Reset(); view.DrawerOpen = true; view.Recruit = default;
            plan.Intent = AriaSkirmishIntent.Recruit;
            touch.Phase = AriaPlayPhase.Aiming; touch.TargetId = 77; Step();
            Check(output.TargetId != 31 && touch.GestureRequested == 0,
                "Rifle catalog recovery cannot replace unrelated active aim");
            return checks;
        }

        static int RunExpandedAssaultRecruitmentControls()
        {
            int checks = 0;
            void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks++; }
            var view = new AriaSkirmishObservation(); var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent(); var output = new AriaPlayObservationComponent();
            void Reset()
            {
                view = new AriaSkirmishObservation { Active = true, ExpandedSession = true,
                    ExpandedBaseAssault = true, Time = 100, Frame = 100, Infantry = 10,
                    PlayerHealth = 800, EnemyHealth = 800, EnemyDesignatedAlive = true,
                    ExpandedPageCount = 2, ExpandedPageIndex = 1, ExpandedTotalGroups = 7,
                    ExpandedRosterRevision = 10, AssaultDistanceVisible = true,
                    CanAffordRifle = true, OwnStructureAttackUnits = 3,
                    Recruit = new AriaTouchTarget { Available = true, Id = 61,
                        Position = new UnityEngine.Vector2(30, 40) } };
                plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, AssaultIssued = 1, StructureOrdered = 1,
                    PagesOrdered = 3, SweepRosterRevision = 10, LastProgressAt = 100,
                    Infantry = 10, PlayerHealth = 800, EnemyHealth = 800,
                    Intent = AriaSkirmishIntent.ObserveBattle };
                touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Waiting,
                    GoalId = 10000, LastProgressAt = 100, LastObjectiveProgressAt = 100,
                    ObjectiveWatchdogInitialized = 1, VisitedGoalMask = 1UL << (10000 & 63) };
            }
            void Step()
            {
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
                AriaPlayDecisionSystem.Step(output, ref touch);
            }
            void NoRecruit(string name)
            {
                Step(); Check(output.TargetId != 61 && touch.GestureRequested == 0, name);
            }
            Reset(); Step();
            Check(output.Kind == AriaPlayObservationKind.Control && output.TargetId == 61 &&
                plan.Intent == AriaSkirmishIntent.Recruit && touch.Phase == AriaPlayPhase.Aiming &&
                touch.GestureRequested == 0 && touch.Actions == 0,
                "expanded accepted assault offers one normal replacement recruitment control");
            view.Time = 101; view.Frame++; Step();
            Check(output.TargetId == 61 && touch.Phase == AriaPlayPhase.Touching &&
                touch.GestureRequested == 1 && touch.Attempts == 1 && touch.Actions == 0,
                "replacement uses normal Decision aim delay and requests one gesture without synthetic purchase");
            Check(plan.AssaultIssued == 1 && plan.StructureOrdered == 1 && plan.PagesOrdered == 3 &&
                touch.LastObjectiveProgressAt == 100,
                "recruitment preserves accepted assault orders and grants no objective progress");
            Reset(); view.CanAffordRifle = false; NoRecruit("public stock/readiness/cap eligibility refusal preserves page observation");
            Reset(); view.Recruit.Available = false; NoRecruit("unavailable production control is not bypassed");
            Reset(); view.RifleRecruitPending = true; NoRecruit("pending replacement never queues another purchase");
            Reset(); view.Infantry = 16; NoRecruit("viable force keeps existing assault observation");
            Reset(); view.AirProfile = true; NoRecruit("air strategy retains its existing production priority");
            Reset(); plan.NextRecruitAt = 101; NoRecruit("existing recruitment cooldown remains effective");
            Reset(); touch.Phase = AriaPlayPhase.Aiming; touch.TargetId = 77;
            NoRecruit("another control aim is not replaced by recruitment");
            Reset(); touch.Phase = AriaPlayPhase.Touching; touch.GestureRequested = 1; touch.Pressed = 1;
            Step(); Check(output.TargetId != 61 && touch.TargetId == 0 && touch.GestureRequested == 1,
                "active gesture is left to the existing input lifecycle");
            Reset(); touch.Phase = AriaPlayPhase.Verifying; touch.TargetId = 77; touch.Actions = 1;
            NoRecruit("incomplete verification is not bypassed");
            Reset(); touch.Phase = AriaPlayPhase.Verifying; touch.TargetId = 77; touch.Actions = 1;
            touch.LastCompletedTargetId = 78; touch.LastCompletedFrame = 99;
            NoRecruit("wrong completed target refuses recruitment transition");
            Reset(); touch.Phase = AriaPlayPhase.Verifying; touch.TargetId = 77; touch.Actions = 1;
            touch.LastCompletedTargetId = 77; touch.LastCompletedFrame = 100;
            NoRecruit("same-frame completion cannot start recruitment");
            Reset(); touch.Phase = AriaPlayPhase.Verifying; touch.TargetId = 77; touch.Actions = 1;
            touch.LastCompletedTargetId = 77; touch.LastCompletedFrame = 99; touch.DueAt = 101;
            NoRecruit("not-due completed verification cannot start recruitment");
            Reset(); touch.Phase = AriaPlayPhase.Verifying; touch.TargetId = 77; touch.Actions = 1;
            touch.LastCompletedTargetId = 77; touch.LastCompletedFrame = 99;
            Step(); Check(output.TargetId == 61 && touch.Phase == AriaPlayPhase.Aiming &&
                touch.GestureRequested == 0, "fresh completed verification can approach the public Recruit control");
            void MissingStructure()
            {
                Reset(); view.OwnStructureAttackUnits = 0; view.Infantry = 0;
                view.ExpandedTotalGroups = 2;
                view.CanAffordRocketeer = true;
                view.RecruitRocketeer = new AriaTouchTarget { Available = true, Id = 62,
                    Position = new UnityEngine.Vector2(50, 40) };
                view.NextSquadPage = new AriaTouchTarget { Available = true, Id = 71 };
                plan.AssaultIssued = 0; plan.StructureOrdered = 0; plan.PagesOrdered = 1;
                plan.PagedToAssault = 1; view.ExpandedPageIndex = 0;
            }
            void NoStructurePurchase(string name)
            {
                Step(); Check(output.TargetId != 61 && output.TargetId != 62 &&
                    touch.GestureRequested == 0, name);
            }
            MissingStructure(); Step();
            Check(output.TargetId == 62 && plan.Intent == AriaSkirmishIntent.Recruit &&
                touch.Phase == AriaPlayPhase.Aiming && touch.GestureRequested == 0,
                "lost structure force requests eligible public Rocketeer before empty-page traversal");
            view.Time = 101; view.Frame++; Step();
            Check(output.TargetId == 62 && touch.Phase == AriaPlayPhase.Touching &&
                touch.GestureRequested == 1 && touch.Actions == 0,
                "structure replacement uses actual normal Decision gesture without production injection");
            MissingStructure(); view.Infantry = 24; Step();
            Check(output.TargetId == 62 && touch.Phase == AriaPlayPhase.Aiming,
                "public-eligible structure replacement is not blocked by legacy Combined Arms infantry threshold");
            MissingStructure(); view.ExpandedPageCount = 0; view.ExpandedTotalGroups = 0;
            view.NextSquadPage = default; view.CanAffordRocketeer = false;
            NoStructurePurchase("absent-page sentinel with ineligible structure production cannot buy rifles instead");
            MissingStructure(); view.ExpandedPageCount = 0; view.ExpandedTotalGroups = 0;
            view.NextSquadPage = default; view.RecruitRocketeer.Available = false;
            NoStructurePurchase("absent-page sentinel with unavailable structure control cannot buy rifles instead");
            MissingStructure(); view.ExpandedPageCount = 0; view.ExpandedTotalGroups = 0;
            view.NextSquadPage = default; view.CanAffordRocketeer = false;
            view.DrawerOpen = true; plan.Intent = AriaSkirmishIntent.Recruit;
            NoStructurePurchase("own open drawer cannot substitute rifles for unavailable structure production");
            MissingStructure(); view.CanAffordRocketeer = false;
            NoStructurePurchase("structure affordability/cap refusal cannot fall back to rifles for Barracks");
            MissingStructure(); view.RecruitRocketeer.Available = false;
            NoStructurePurchase("unavailable structure production preserves ordinary page behavior");
            MissingStructure(); view.RocketeerRecruitPending = true;
            NoStructurePurchase("pending structure replacement waits without a duplicate or rifle purchase");
            MissingStructure(); plan.NextRecruitAt = 101;
            NoStructurePurchase("lost structure force still honors cooldown");
            MissingStructure(); touch.Phase = AriaPlayPhase.Aiming; touch.TargetId = 77;
            NoStructurePurchase("structure recovery cannot replace an unrelated aim");
            MissingStructure(); plan.TargetPending = 1;
            NoStructurePurchase("structure recovery preserves pending assault input");
            MissingStructure(); view.DrawerOpen = true; plan.Intent = AriaSkirmishIntent.Recruit;
            Step(); Check(output.TargetId == 62 && touch.Phase == AriaPlayPhase.Aiming,
                "own normal recruitment drawer continues the eligible structure purchase");
            Reset(); plan.TargetPending = 1; NoRecruit("pending assault target retains priority");
            Reset(); plan.MapNavigationStage = 1; NoRecruit("active page navigation retains priority");
            // Public UI sequence: Build opener -> completed input -> catalog -> role purchase.
            // Acknowledgements below are sampled input-driver state, not production commands.
            MissingStructure();
            view.RecruitRocketeer.Id = 31;
            view.CloseDrawer = new AriaTouchTarget { Available = true, Id = 63 };
            Step(); Check(output.TargetId == 31 && touch.Phase == AriaPlayPhase.Aiming,
                "structure recruitment approaches the actual public Build opener");
            view.Time = 101; view.Frame++; Step();
            Check(touch.Phase == AriaPlayPhase.Touching && touch.GestureRequested == 1,
                "drawer opening requires the ordinary Decision gesture");
            view.DrawerOpen = true; view.RecruitRocketeer = default;
            view.Time = 102; view.Frame = 103;
            touch.Phase = AriaPlayPhase.Verifying; touch.GestureRequested = 0; touch.Pressed = 0;
            touch.Actions = 1; touch.LastCompletedTargetId = 31;
            touch.LastCompletedFrame = 102; touch.DueAt = 103;
            Step(); Check(plan.Intent == AriaSkirmishIntent.Recruit && output.TargetId != 63 &&
                touch.Phase == AriaPlayPhase.Verifying && touch.GestureRequested == 0,
                "completed Build opener not yet due must not become Inspect CloseDrawer");
            view.Time = 103; view.Frame++; Step();
            Check(plan.Intent == AriaSkirmishIntent.Recruit && output.TargetId != 63 &&
                touch.GestureRequested == 0,
                "own recruitment catalog presentation gap waits without a purchase or close");
            view.Time = 103.5f; view.Frame++;
            view.RecruitRocketeer = new AriaTouchTarget { Available = true, Id = 62,
                Position = new UnityEngine.Vector2(50,40) };
            Step(); Check(output.TargetId == 62 && touch.Phase == AriaPlayPhase.Aiming &&
                touch.GestureRequested == 0 && touch.Actions == 1,
                "fresh public role item gets its own Decision approach after the opener");
            view.Time = 104.5f; view.Frame++; Step();
            Check(output.TargetId == 62 && touch.Phase == AriaPlayPhase.Touching &&
                touch.GestureRequested == 1 && touch.Actions == 1,
                "only the presented eligible role requests the second ordinary gesture");
            touch.Phase = AriaPlayPhase.Verifying; touch.GestureRequested = 0;
            touch.Actions = 2; touch.LastCompletedTargetId = 62;
            touch.LastCompletedFrame = view.Frame; touch.DueAt = view.Time + 1;
            view.RocketeerRecruitPending = true; view.Time++; view.Frame++;
            Step(); Check(plan.Intent == AriaSkirmishIntent.ObserveBattle && output.TargetId == 63 &&
                touch.Actions == 2, "public pending production ends recruitment without duplicate purchase");
            MissingStructure(); view.DrawerOpen = true; plan.Intent = AriaSkirmishIntent.Recruit;
            view.RecruitRocketeer = default; view.CloseDrawer = new AriaTouchTarget { Available = true, Id = 63 };
            plan.RecruitUnavailableAt = 96;
            Step(); Check(output.TargetId == 63 && plan.NextRecruitAt == 112 &&
                plan.Intent == AriaSkirmishIntent.Inspect,
                "persistent unavailable role keeps bounded refusal and cooldown instead of looping forever");
            return checks;
        }


        public static void RunPublicPrecisionAttackValidation()
        {
            int checks=0;
            void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);checks++;}
            var v=new AriaSkirmishObservation();var p=new AriaSkirmishPlanComponent();
            var t=new AriaPlaySessionComponent();var o=new AriaPlayObservationComponent();
            void Reset()
            {
                v=new AriaSkirmishObservation {Active=true,ExpandedSession=true,ExpandedBaseAssault=true,Frame=100,Time=100,Infantry=20,
                    PlayerHealth=800,EnemyHealth=800,EnemyDesignatedAlive=true,CombinedArmsField=true,
                    OwnStructureAttackUnits=8,ExpandedAssaultMask=4,ExpandedStructureMask=4,ExpandedAttackOrderMask=4,
                    ExpandedGroup2=77,ExpandedPageCount=1,ExpandedTotalGroups=3,ExpandedRosterRevision=10,
                    ExpandedSelectedMask=4,ExpandedSelectedGroups=1,SelectionVisible=true,SelectionMode=true,
                    EnemyBase=new AriaTouchTarget{Available=true,Id=-20001,Position=new UnityEngine.Vector2(30,40)},
                    AdvanceGround=new AriaTouchTarget{Available=true,Id=16},
                    Attack=new AriaTouchTarget{Available=true,Id=14},Select=new AriaTouchTarget{Available=true,Id=15},
                    Squad2=new AriaTouchTarget{Available=true,Id=12},
                    SelectedSquadToToggle=new AriaTouchTarget{Available=true,Id=13}};
                p=new AriaSkirmishPlanComponent {AssaultStarted=1,AssaultIssued=1,StructureOrdered=1,
                    LastProgressAt=100,Infantry=20,PlayerHealth=800,EnemyHealth=800,SweepRosterRevision=10};
                t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing,LastProgressAt=100,LastObjectiveProgressAt=100};
            }
            void Step(){AriaSkirmishPlanSystem.Step(v,ref p,ref t,ref o);}
            void Issue(){Step();v.Frame++;v.AttackMode=true;Step();}
            void Ack(uint group=77,int contact=-20001,bool presented=true,uint revision=1)
            {
                t.Actions++;t.LastCompletedTargetId=-20001;t.LastCompletedFrame=v.Frame;
                t.LastCompletedPosition=new UnityEngine.Vector2(30,40);
                v.PrecisionAttack=new UiExpandedPrecisionAttackResult{Presented=presented,Revision=revision,GroupId=group,
                    PublicTargetId=contact,RequestId=123,Frame=v.Frame,AcceptedMembers=1,SourceMembers=4,
                    ScreenPosition=t.LastCompletedPosition};
                v.AttackMode=false;v.Frame++;Step();
            }
            Reset();v.ExpandedBaseAssault=false;Step();Check(p.PrecisionStage==0,"non BaseAssault scope never starts precision");
            Reset();v.EnemyBase=default;Step();
            Check(p.PrecisionStage==0,"hidden target cannot start precision");
            Reset();Step();Check(o.TargetId==14&&p.PrecisionStage==1,"selected structure group uses normal Attack");
            v.AttackMode=true;v.Frame++;Step();
            Check(o.TargetId==-20001&&p.PrecisionStage==4&&p.PrecisionAttempts==1,"one normal public target tap");
            t.Actions++;t.LastCompletedTargetId=-20001;t.LastCompletedFrame=v.Frame;v.AttackMode=false;Step();
            Check(p.PrecisionAcceptedGroups.Length==0&&p.PrecisionStage==4,"gesture and old attack mask are not native ack");
            Reset();Issue();Ack();
            Check(p.PrecisionAcceptedGroups.Length==1&&p.PrecisionAcceptedGroups[0]==77,
                "fresh exact partial native acceptance is acknowledged without all-member claim");
            Step();Check(o.TargetId!=-20001&&o.TargetId!=14,"accepted group is not spammed");
            Reset();Issue();v.ExpandedRosterRevision++;Ack();Check(p.PrecisionAcceptedGroups.Length==0,"roster churn cannot splice an ack");
            Reset();Issue();Ack(revision:0);Check(p.PrecisionAcceptedGroups.Length==0,"stale revision rejected");
            Reset();Issue();Ack(group:88);Check(p.PrecisionAcceptedGroups.Length==0,"wrong group rejected");
            Reset();Issue();Ack(contact:99);Check(p.PrecisionAcceptedGroups.Length==0,"wrong public target rejected");
            Reset();Issue();Ack(presented:false);Check(p.PrecisionAcceptedGroups.Length==0,"unpresented result rejected");
            Reset();v.ExpandedSelectedGroups=0;v.ExpandedSelectedMask=0;Step();
            Check(o.TargetId==12,"actual available card selected through normal input");
            Reset();v.ExpandedSelectedOffPage=1;Step();
            Check(o.TargetId==13,"offpage selection normally deselected before precision");
            Reset();Step();v.AttackMode=true;v.EnemyBase=default;Step();
            Check(p.PrecisionStage==5&&o.TargetId!=-20001&&o.TargetId!=v.AdvanceGround.Id,
                "lost precision target never falls back to terrain");
            Reset();v.ExpandedAirMask=4;Step();
            Check(p.PrecisionStage==0,"air group excluded from ground precision");
            Reset();Issue();v.AttackMode=false;v.Time=109;Step();
            Check(p.PrecisionStage==1&&p.PrecisionAttempts==1,"ack timeout rearms bounded cooldown");
            v.Time=110;Step();Check(o.TargetId==0,"cooldown has no repeated order");
            v.Time=112;Step();v.AttackMode=true;v.Frame++;Step();
            Check(p.PrecisionAttempts==2&&o.TargetId==-20001,"second target attempt permitted once");
            v.AttackMode=false;v.Time=121;Step();v.Time=122;Step();
            Check(p.PrecisionExhaustedGroups.Length==1&&p.PrecisionStage==0&&o.TargetId!=14,
                "exhausted group cannot repeat indefinitely");
            Reset();Issue();v.Time=281;Step();
            Check(t.Phase==AriaPlayPhase.Blocked&&p.LastProgressAt==100&&t.LastObjectiveProgressAt==100,
                "pending precision cannot renew180second watchdog");
            Reset();t.Phase=AriaPlayPhase.Manual;Step();
            Check(p.PrecisionStage==0,"Manual does not start precision");
            UnityEngine.Debug.Log($"[PublicPrecisionAttackControls] result=Passed checks={checks} nativeBehaviorProved=0 missionReady=0");
        }

        public static void RunPublicAssaultFocusValidation()
        {
            int checks=0;
            void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);checks++;}
            var v=new AriaSkirmishObservation();var p=new AriaSkirmishPlanComponent();
            var t=new AriaPlaySessionComponent();var o=new AriaPlayObservationComponent();
            void Reset()
            {
                v=new AriaSkirmishObservation{Active=true,ExpandedSession=true,ExpandedBaseAssault=true,Frame=100,Time=100,
                    Infantry=20,PlayerHealth=800,EnemyHealth=800,EnemyDesignatedAlive=true,CombinedArmsField=true,
                    OwnStructureAttackUnits=8,ExpandedAssaultMask=4,ExpandedStructureMask=4,ExpandedAttackOrderMask=4,
                    ExpandedGroup2=77,ExpandedPageCount=1,ExpandedTotalGroups=3,ExpandedRosterRevision=10,
                    ExpandedSelectedMask=4,ExpandedSelectedGroups=1,SelectionVisible=true,SelectionMode=true,
                    AssaultAtBase=true,AssaultDistanceVisible=true,NearestFriendlyObjectiveDistance=25,
                    AssaultMap=new AriaAssaultMapObservation{Identity=1,ObjectiveHudIdentity=2,ExpectedIdentity=true,
                        Complete=true,ProjectionValid=true,MarkerRevision=1,MarkerFrame=100,MarkerTime=100},
                    FocusEnemy=new AriaTouchTarget{Available=true,Id=55,Position=new UnityEngine.Vector2(30,40)},
                    Attack=new AriaTouchTarget{Available=true,Id=14},Squad2=new AriaTouchTarget{Available=true,Id=12}};
                p=new AriaSkirmishPlanComponent{AssaultStarted=1,AssaultIssued=1,StructureOrdered=1,SweepRosterRevision=10,
                    LastProgressAt=100,EnemyHealth=800,PlayerHealth=800,Infantry=20,
                    AssaultDistanceObserved=1,BestAssaultDistance=25,AssaultObjectiveHudIdentity=2};
                t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing,LastProgressAt=100,LastObjectiveProgressAt=100};
            }
            void Step(){AriaSkirmishPlanSystem.Step(v,ref p,ref t,ref o);}
            void NoFocus(string why){Step();Check(p.AssaultFocusStage==0,why);}
            Reset();Step();
            Check(o.Kind==AriaPlayObservationKind.Control&&o.TargetId==55&&o.Drag==0&&p.AssaultFocusStage==1,
                "existing public focus button is a normal control, never a world order");
            Check(p.AssaultIssued==1&&p.StructureOrdered==1&&p.PrecisionAcceptedGroups.Length==0&&
                p.LastProgressAt==100&&t.LastObjectiveProgressAt==100&&!v.EnemyBase.Available,
                "camera request neither accepts combat nor reveals target nor refreshes watchdog");
            Step();Check(o.TargetId==55&&p.AssaultFocusActions==0,"same pending gesture is maintained without restarting attempt");
            t.Actions=1;t.LastCompletedTargetId=55;t.LastCompletedFrame=100;Step();
            Check(p.AssaultFocusStage==2&&p.PrecisionAcceptedGroups.Length==0,"focus completion is distinct from precision acceptance");
            Step();Check(o.TargetId!=55,"completed focus cannot repeat");
            v.AssaultAtBase=false;Step();v.AssaultAtBase=true;Step();
            Check(o.TargetId!=55,"arrival jitter cannot rearm camera gesture");
            v.EnemyBase=new AriaTouchTarget{Available=true,Id=-20001,Position=new UnityEngine.Vector2(80,90)};Step();
            Check(o.TargetId==14&&p.PrecisionStage==1,"only subsequently presented real target enters unchanged precision selection");
            Reset();v.ExpandedBaseAssault=false;NoFocus("no other objective scope");
            Reset();v.AssaultAtBase=false;NoFocus("no early focus before public arrival");
            Reset();v.AssaultMap.MarkerTime=98;NoFocus("stale presented map cannot authorize arrival");
            Reset();v.AssaultMap.ExpectedIdentity=false;NoFocus("foreign presented map cannot authorize arrival");
            Reset();p.AssaultIssued=0;NoFocus("unaccepted assault cannot authorize focus");
            Reset();p.StructureOrdered=0;NoFocus("structure force order is required");
            Reset();t.Phase=AriaPlayPhase.Manual;NoFocus("Manual remains idle");
            Reset();v.AttackMode=true;NoFocus("camera cannot consume an active targeting mode");
            Reset();v.DrawerOpen=true;NoFocus("open drawer blocks focus");
            Reset();v.MapOpen=true;NoFocus("open full map blocks focus");
            Reset();v.PlacementOpen=true;NoFocus("placement blocks focus");
            Reset();v.PresentedHomeThreat=true;NoFocus("home defence has priority");
            Reset();v.FocusEnemy=default;NoFocus("unavailable focus is not bypassed");
            Reset();v.EnemyBase=new AriaTouchTarget{Available=true,Id=-20001};NoFocus("already presented precision target needs no focus");
            Reset();Step();t.Actions=1;t.LastCompletedTargetId=99;t.LastCompletedFrame=100;Step();
            Check(p.AssaultFocusStage==1,"unrelated normal gesture is not a focus completion receipt");
            v.Time=108;v.Frame=108;Step();
            Check(p.AssaultFocusStage==2&&o.TargetId!=55&&p.PrecisionAcceptedGroups.Length==0,
                "timeout consumes bounded attempt without accepting combat");
            // Exercise the unchanged actual camera request owner, not a fabricated gameplay acknowledgement.
            using(var world=new Unity.Entities.World("Public objective camera mutation boundary"))
            {
                var em=world.EntityManager;
                var camera=em.CreateEntity(typeof(Game.Components.RuntimeCameraFocusRequestComponent));
                var objective=em.CreateEntity(typeof(Unity.Transforms.LocalTransform));
                var position=new Unity.Mathematics.float3(100,0,200);
                em.SetComponentData(objective,Unity.Transforms.LocalTransform.FromPosition(position));
                var actor=em.CreateEntity(typeof(Unity.Transforms.LocalTransform),typeof(Game.Components.AttackMoveOrder));
                var actorPosition=new Unity.Mathematics.float3(20,0,30);
                em.SetComponentData(actor,Unity.Transforms.LocalTransform.FromPosition(actorPosition));
                em.SetComponentData(actor,new Game.Components.AttackMoveOrder{Destination=new Unity.Mathematics.int2(80,90)});
                var system=world.GetOrCreateSystemManaged<Game.Composition.SkirmishPresentationSystem>();
                var method=typeof(Game.Composition.SkirmishPresentationSystem).GetMethod("Focus",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                Check(method!=null,"existing camera owner is available");
                Check((bool)method.Invoke(system,new object[]{objective,false,0,true,false}),"actual public focus request accepted");
                var request=em.GetComponentData<Game.Components.RuntimeCameraFocusRequestComponent>(camera);
                Check(request.Requested==1&&request.UseExplicitPerspective==1,"actual owner only requests camera presentation");
                Check(em.GetComponentData<Game.Components.AttackMoveOrder>(actor).Destination.Equals(new Unity.Mathematics.int2(80,90))&&
                    em.GetComponentData<Unity.Transforms.LocalTransform>(actor).Position.Equals(actorPosition)&&
                    !em.HasComponent<Game.Components.EngageTarget>(actor),
                    "actual camera owner preserves troop destination/position and creates no EngageTarget");
            }
            UnityEngine.Debug.Log($"[PublicAssaultFocusControls] result=Passed checks={checks} nativeCameraProved=0 missionReady=0");
        }

        private static int RunPublicApproachProgressControls()
        {
            int checks = 0;
            void Check(bool condition, string name)
            { if (!condition) throw new InvalidOperationException(name); checks++; }
            var view = new AriaSkirmishObservation();
            var plan = new AriaSkirmishPlanComponent();
            var touch = new AriaPlaySessionComponent();
            var output = new AriaPlayObservationComponent();
            void Reset(int acceptedMask = 4, bool fullSweep = false)
            {
                view = new AriaSkirmishObservation
                {
                    Active = true, ExpandedSession = true, Time = 100, Infantry = 20,
                    PlayerHealth = 800, EnemyHealth = 800, EnemyDesignatedAlive = true,
                    CombinedArmsField = true, OwnStructureAttackUnits = 8,
                    ExpandedPageIndex = 1, ExpandedPageCount = 2, ExpandedTotalGroups = 10,
                    ExpandedAssaultMask = fullSweep ? 0 : 20,
                    ExpandedAttackOrderMask = acceptedMask, ExpandedStructureMask = 4,
                    ExpandedSelectedMask = 4, ExpandedSelectedGroups = 1, ExpandedRosterRevision = 10,
                    SelectionVisible = true, SelectedCount = 4,
                    Attack = new AriaTouchTarget { Available = true, Id = 14 },
                    AssaultDistanceVisible = true, NearestFriendlyObjectiveDistance = 130.3f
                };
                plan = new AriaSkirmishPlanComponent
                {
                    AssaultStarted = 1, AssaultIssued = (byte)(fullSweep ? 1 : 0),
                    LastProgressAt = 100, Infantry = 20, PlayerHealth = 800, EnemyHealth = 800,
                    PagedToAssault = 1, AssaultSelecting = 1, StructureOrdered = 1,
                    PagesOrdered = 1, AssaultPageSeen = 1, SweepRosterRevision = 10
                };
                touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing,
                    LastProgressAt = 100, LastObjectiveProgressAt = 100 };
                output = default;
            }
            void Step(float time, float distance)
            {
                view.Time = time; view.Frame++; view.NearestFriendlyObjectiveDistance = distance;
                view.AssaultMap = new AriaAssaultMapObservation {
                    Identity = 11, ExpectedIdentity = true, Complete = true, ProjectionValid = true,
                    FullMap = view.MapOpen, MarkerRevision = (uint)view.Frame,
                    MarkerFrame = view.Frame, MarkerTime = view.Time, ObjectiveHudIdentity = 77,
                    ObjectiveWorld = UnityEngine.Vector3.zero };
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            }
            void NoProgress(string label) => Check(plan.LastProgressAt == 100 &&
                touch.LastProgressAt == 100 && touch.LastObjectiveProgressAt == 100, label);

            Reset(); Step(100, 130.3f);
            Check(plan.AssaultDistanceObserved == 1 && plan.BestAssaultDistance == 130.3f,
                "actual accepted partial assault establishes public approach baseline");
            NoProgress("the first public sample is not progress");
            Step(110, 129.3f); NoProgress("one metre closer is below the existing threshold");
            Step(120, 127.9f);
            Check(plan.LastProgressAt == 120 && touch.LastProgressAt == 120 &&
                touch.LastObjectiveProgressAt == 120 && plan.BestAssaultDistance == 127.9f,
                "new closest public approach beyond2m counts before complete sweep");
            Step(130, 127.9f); Step(140, 140f); Step(150, 127.9f);
            Check(plan.LastProgressAt == 120 && touch.LastObjectiveProgressAt == 120 &&
                plan.BestAssaultDistance == 127.9f,
                "repeat,retreat and re-advance to the same best distance earn none");
            Check(plan.AssaultIssued == 0 && plan.PagesOrdered == 1 && plan.AssaultPageSeen == 1 &&
                plan.PagedToAssault == 1 && plan.StructureOrdered == 1,
                "public travel progress never claims the whole sweep completed");
            Check(touch.Actions == 0 && touch.GestureRequested == 0 && output.TargetId == 14 &&
                output.Kind == AriaPlayObservationKind.Control,
                "progress observation adds no gesture or order beyond the existing pending control");

            Reset(); Step(100, 130.3f);
            touch.Actions = 50; view.ExpandedPageIndex = 0;
            Step(110, 130.3f); view.ExpandedPageIndex = 1; Step(120, 130.3f);
            NoProgress("tap,reissue and page changes alone do not renew progress");

            foreach (int rejectedMask in new[] { 0, 2 })
            {
                Reset(rejectedMask); Step(100, 130.3f); Step(110, 100f);
                Check(plan.AssaultDistanceObserved == 0,
                    "absent or disjoint accepted mask establishes no assault baseline");
                NoProgress("mask outside the presented assault cohort earns no credit");
            }
            Reset(0); Step(100, 130.3f);
            view.ExpandedAttackOrderMask = 4; Step(110, 130.3f);
            NoProgress("new accepted mask alone earns no progress");

            Reset(); view.AssaultDistanceVisible = false;
            Step(100, 130.3f); Step(110, 100f);
            Check(plan.AssaultDistanceObserved == 0, "hidden public approach has no baseline");
            NoProgress("unpresented distance cannot renew progress");
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f })
            {
                Reset(); Step(110, invalid);
                Check(plan.AssaultDistanceObserved == 0, "nonfinite or negative approach refused");
                NoProgress("invalid public distance earns none");
            }

            Reset(); Step(100, 130.3f); touch.Actions = 100;
            Step(281, 130.3f);
            Check(touch.Phase == AriaPlayPhase.Blocked && touch.GestureRequested == 0 &&
                plan.LastProgressAt == 100 && touch.LastObjectiveProgressAt == 100,
                "accepted masks and repeated actions cannot bypass180second no-progress stop");

            Reset(0, fullSweep: true); Step(100, 130.3f); NoProgress("complete sweep retains first-sample rule");
            Step(110, 127.9f);
            Check(touch.LastObjectiveProgressAt == 110 && plan.AssaultIssued == 1,
                "existing completed-sweep eligibility works even on a page with no current assault mask");

            void PartialPage()
            {
                Reset(31);
                view.ExpandedPageIndex = 2;
                view.ExpandedPageCount = 5;
                view.ExpandedTotalGroups = 21;
                view.ExpandedAssaultMask = 31;
                view.ExpandedSelectedMask = 31;
                view.ExpandedSelectedGroups = 5;
                view.AssaultDistanceVisible = false;
                plan.AssaultPageSeen = 3;
                plan.PagesOrdered = 3;
                plan.StructureOrdered = 0;
            }
            void MapStep(float time, bool open, bool distanceVisible, float distance)
            {
                view.Time = time;
                view.Frame++;
                view.MapOpen = open;
                view.AssaultDistanceVisible = distanceVisible;
                view.NearestFriendlyObjectiveDistance = distance;
                view.AssaultMap = new AriaAssaultMapObservation {
                    Identity = open ? 22 : 11, ExpectedIdentity = true, Complete = true,
                    ProjectionValid = true, FullMap = open,
                    MarkerRevision = (uint)view.Frame, MarkerFrame = view.Frame,
                    MarkerTime = time, ObjectiveHudIdentity = 77,
                    ObjectiveWorld = UnityEngine.Vector3.zero,
                    OpenMap = new AriaTouchTarget { Available = !open, Id = -20010 }
                };
                view.CloseMap = new AriaTouchTarget { Available = open, Id = -20012 };
                AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            }
            PartialPage(); MapStep(110, false, false, 0);
            Check(plan.AssaultInspectStage == 1 && output.TargetId == -20010 &&
                plan.AssaultIssued == 0 && plan.PagesOrdered == 3,
                "accepted same-roster five-page partial sweep can request normal map inspection");
            NoProgress("normal map request is not objective progress");
            touch.Actions++;
            touch.TargetId = -20010;
            touch.Phase = AriaPlayPhase.Observing;
            MapStep(111, true, false, 0);
            Check(plan.AssaultInspectStage == 2 && plan.AssaultInspectMapIdentity == 22,
                "partial inspection retains owned open across later frames");
            MapStep(112, true, true, 178.4f);
            Check(plan.AssaultDistanceObserved == 1 && plan.BestAssaultDistance == 178.4f &&
                output.TargetId == -20012,
                "fresh actually presented full-map sample sets baseline and uses normal close");
            NoProgress("first full-map sample and normal close request earn no credit");
            touch.Actions++;
            touch.TargetId = -20012;
            touch.Phase = AriaPlayPhase.Observing;
            MapStep(113, false, false, 0);
            Check(plan.AssaultInspectStage == 0 && plan.AssaultInspectReadyAt == 125,
                "actual normal close clears the operation and starts existing cooldown");
            MapStep(124, false, false, 0);
            Check(plan.AssaultInspectStage == 0, "cooldown cannot reopen early");
            NoProgress("close and cooldown earn no objective progress");
            touch.Phase = AriaPlayPhase.Observing;
            MapStep(125, false, false, 0);
            Check(plan.AssaultInspectStage == 1 && output.TargetId == -20010,
                "later missing compact contacts request a second owned normal open");
            touch.Actions++;
            touch.TargetId = -20010;
            touch.Phase = AriaPlayPhase.Observing;
            MapStep(126, true, false, 0);
            Check(plan.AssaultInspectStage == 2 && plan.AssaultInspectOpenedFrame == view.Frame,
                "second actual open establishes its own new marker boundary");
            MapStep(127, true, true, 175.9f);
            Check(plan.LastProgressAt == 127 && touch.LastObjectiveProgressAt == 127 &&
                plan.AssaultIssued == 0 && plan.PagesOrdered == 3 && output.TargetId == -20012,
                "only second genuinely closer presented sample renews progress and normally closes");
            PartialPage(); MapStep(110, false, false, 0);
            touch.Actions++;
            touch.TargetId = -20010;
            touch.Phase = AriaPlayPhase.Observing;
            MapStep(111, true, false, 0);
            view.ExpandedAssaultMask = 0;
            MapStep(112, true, false, 0);
            Check(plan.AssaultInspectStage == 3 && output.TargetId == -20012 &&
                plan.AssaultDistanceObserved == 0,
                "loss of accepted masks during an owned open requests close without progress");
            NoProgress("suppressed or lost page projection earns no baseline or progress");
            PartialPage(); view.ExpandedAttackOrderMask = 15;
            MapStep(110, false, false, 0);
            Check(plan.AssaultInspectStage == 0, "partially accepted page cannot interrupt selection");
            PartialPage(); view.ExpandedRosterRevision++;
            MapStep(110, false, false, 0);
            Check(plan.AssaultInspectStage == 0, "changed roster cannot borrow accepted-page inspection");
            PartialPage(); view.AttackMode = true;
            MapStep(110, false, false, 0);
            Check(plan.AssaultInspectStage == 0, "armed command mode cannot be interrupted by inspection");
            PartialPage();
            plan.LastProgressAt = 62.142464f;
            touch.LastProgressAt = 62.142464f;
            touch.LastObjectiveProgressAt = 62.142464f;
            MapStep(239.547409f, false, false, 0);
            Check(touch.Phase != AriaPlayPhase.Blocked && plan.LastProgressAt == 62.142464f,
                "captured prior177.405seconds remains below the unchanged watchdog");
            MapStep(242.241562f, false, false, 0);
            Check(touch.Phase == AriaPlayPhase.Blocked && touch.GestureRequested == 0 &&
                plan.LastProgressAt == 62.142464f && touch.LastObjectiveProgressAt == 62.142464f,
                "exact captured FA180.099seconds boundary still blocks without real progress");
            PartialPage();
            MapStep(281, false, false, 0);
            Check(touch.Phase == AriaPlayPhase.Blocked && touch.GestureRequested == 0 &&
                plan.LastProgressAt == 100 && touch.LastObjectiveProgressAt == 100,
                "five-page acceptance and map availability do not weaken180second watchdog");

            // Actual Plan -> Decision ordering, with the completed-input receipt shape
            // sampled at EN64776 input line77. DueAt/Pressed/GestureRequested are not
            // traced: fixture values follow AriaPlayInputSystem's completed-touch API.
            void CompletedPage()
            {
                PartialPage();
                view.Frame = 445;
                plan.AssaultSelecting = 2;
                plan.SelectionResetPage = 2;
                touch.Phase = AriaPlayPhase.Verifying;
                touch.Actions = 43;
                touch.TargetId = 1073741835;
                touch.LastCompletedTargetId = 1073741835;
                touch.LastCompletedFrame = 445;
                touch.DueAt = 123.15753f; // fixture: completion122.50753 + existing .65s cooldown
                touch.Pressed = 0;
                touch.GestureRequested = 0;
                touch.GoalId = 10000;
                touch.ObjectiveWatchdogInitialized = 1;
                touch.VisitedGoalMask = 1UL << (10000 & 63);
            }
            void Pipeline(float time, bool open = false, bool visible = false, float distance = 0)
            {
                MapStep(time, open, visible, distance);
                AriaPlayDecisionSystem.Step(output, ref touch);
            }
            void RefuseCompletedBoundary(string name, Action change)
            {
                CompletedPage(); change(); Pipeline(123.3f);
                Check(plan.AssaultInspectStage == 0, name);
            }
            RefuseCompletedBoundary("aiming remains owned by the current target", () => touch.Phase = AriaPlayPhase.Aiming);
            RefuseCompletedBoundary("touching cannot be interrupted", () => touch.Phase = AriaPlayPhase.Touching);
            RefuseCompletedBoundary("pressed completed-looking receipt refuses inspection", () => touch.Pressed = 1);
            RefuseCompletedBoundary("pending gesture refuses inspection", () => touch.GestureRequested = 1);
            RefuseCompletedBoundary("no completed action refuses inspection", () => touch.Actions = 0);
            RefuseCompletedBoundary("wrong completed target refuses inspection", () => touch.LastCompletedTargetId--);
            RefuseCompletedBoundary("missing completion frame refuses inspection", () => touch.LastCompletedFrame = 0);
            RefuseCompletedBoundary("same-frame completion waits for a later planner frame", () => touch.LastCompletedFrame = 446);
            RefuseCompletedBoundary("future completion frame refuses inspection", () => touch.LastCompletedFrame = 447);
            RefuseCompletedBoundary("completed touch cooldown is preserved", () => touch.DueAt = 123.4f);
            RefuseCompletedBoundary("incomplete page acknowledgement refuses inspection", () => view.ExpandedAttackOrderMask = 15);
            RefuseCompletedBoundary("inspection cooldown still refuses early restart", () => plan.AssaultInspectReadyAt = 123.4f);
            RefuseCompletedBoundary("foreign command presentation still refuses inspection", () => view.AttackMode = true);

            CompletedPage();
            Pipeline(123.3f);
            // This is the red assertion on the unchanged R8 planner: it excludes
            // Verifying and Decision immediately resumes an ordinary card/page aim.
            Check(plan.AssaultInspectStage == 1 && output.TargetId == -20010 &&
                touch.TargetId == -20010 && touch.Phase == AriaPlayPhase.Aiming &&
                touch.GestureRequested == 0 && touch.DueAt == 123.3f + .9f,
                "completed Verifying Plan->Decision must normally aim the owned map open");
            Check(touch.LastObjectiveProgressAt == 100 && plan.LastProgressAt == 100 &&
                plan.PagesOrdered == 3 && plan.AssaultIssued == 0,
                "inspection entry neither renews objective progress nor finishes pending pages");
            Pipeline(124.3f);
            Check(touch.Phase == AriaPlayPhase.Touching && touch.GestureRequested == 1 &&
                touch.TargetId == -20010, "normal Decision delay must submit the map gesture");
            // Explicit input-owner completion fixture, not a hardware-input claim.
            void CompleteRequestedGesture(float time)
            {
                Check(touch.Phase == AriaPlayPhase.Touching && touch.GestureRequested == 1,
                    "fixture only acknowledges a real Decision gesture request");
                touch.GestureRequested = 0;
                touch.Pressed = 0;
                touch.Actions++;
                touch.LastCompletedTargetId = touch.TargetId;
                touch.LastCompletedFrame = view.Frame;
                touch.Phase = AriaPlayPhase.Verifying;
                touch.DueAt = time + .65f;
            }
            CompleteRequestedGesture(124.3f);
            Pipeline(125.1f, true);
            Check(plan.AssaultInspectStage == 2 && plan.AssaultInspectOpenedFrame == view.Frame &&
                plan.AssaultDistanceObserved == 0,
                "actual open acknowledgement waits for a later rendered marker");
            Pipeline(125.2f, true, true, 178.4f);
            Check(plan.AssaultInspectStage == 3 && plan.AssaultDistanceObserved == 1 &&
                output.TargetId == -20012 && touch.Phase == AriaPlayPhase.Aiming &&
                touch.LastObjectiveProgressAt == 100 && plan.LastProgressAt == 100,
                "fresh marker establishes baseline and normally aims close without progress credit");
            Pipeline(126.2f, true, true, 178.4f);
            Check(touch.Phase == AriaPlayPhase.Touching && touch.TargetId == -20012 &&
                touch.GestureRequested == 1, "normal Decision delay must submit close gesture");
            CompleteRequestedGesture(126.2f);
            Pipeline(126.3f);
            Check(plan.AssaultInspectStage == 0 && plan.AssaultInspectReadyAt == 126.3f + 12 &&
                touch.LastObjectiveProgressAt == 100 && plan.LastProgressAt == 100,
                "acknowledged normal close retains watchdog and existing inspection cooldown");
            view.ExpandedSelectedGroups = 0;
            view.ExpandedSelectedMask = 0;
            view.SelectedCount = 0;
            view.SelectionVisible = false;
            view.NextSquadPage = new AriaTouchTarget { Available = true, Id = -268468306 };
            Pipeline(127);
            Check(output.TargetId == -268468306 && touch.TargetId == -268468306 &&
                touch.Phase == AriaPlayPhase.Aiming && plan.PagesOrdered == 3 &&
                plan.AssaultIssued == 0 && touch.LastObjectiveProgressAt == 100,
                "after close normal pending-page input resumes without sweep or progress credit");
            return checks;
        }

        public static string Run()
        {
            int checks = 0;
            void Check(bool valid, string name) { if (!valid) throw new InvalidOperationException(name); checks++; }
            var view = new AriaSkirmishObservation { Active = true, Time = 1, Infantry = 8, AvailableSquads = 1, SelectedSlot = -1,
                Recruit = new AriaTouchTarget { Available = true, Id = 11 }, Squad0 = new AriaTouchTarget { Available = true, Id = 12 },
                FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 }, Attack = new AriaTouchTarget { Available = true, Id = 14 } };
            var plan = new AriaSkirmishPlanComponent { AssaultStarted = 1 }; var touch = new AriaPlaySessionComponent(); var output = new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Waiting, "no action before consent");
            touch.Phase = AriaPlayPhase.Observing;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 11, "visible recruitment control");
            view.Recruit = default; view.DrawerOpen = true; view.CloseDrawer = new AriaTouchTarget { Available = true, Id = 15 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            view.Time += 4;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 15 && plan.NextRecruitAt > view.Time, "unaffordable order closes drawer and backs off");
            plan.RecruitBurstRemaining = 2; view.Time += .1f;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 15, "recruitment backoff must not cancel an in-flight close-drawer gesture");
            plan.RecruitBurstRemaining = 0;
            view.DrawerOpen = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 12, "select existing squad");
            view.SelectionVisible = true; view.SelectedSlot = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 13, "offscreen target uses visible navigation");
            view.EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 14, "attack mode before world touch");
            view.AttackMode = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.WorldTarget, "only observable target can be tapped");
            touch.TargetId = -20001; touch.Actions++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Waiting && plan.Slot == 0 && plan.Cycle == 1, "wait after accepted gesture, no rapid retapping");
            view.Time += 15; view.AvailableSquads = 1; view.SelectedSlot = 0; plan.NextRecruitAt = 100;
            view.FocusThreat = new AriaTouchTarget { Available = true, Id = -20003 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20003, "navigate to the nearest observed threat instead of attacking distant defenders");
            view.MapOpen = true; view.FocusThreat.Id = -20004; view.CloseMap = new AriaTouchTarget { Available = true, Id = 16 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20004, "focus the observed threat once on the open map");
            view.FocusThreatDrag = true; view.FocusThreatDragEnd = new UnityEngine.Vector2(500, 400);
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Drag == 1 && output.DragEnd == view.FocusThreatDragEnd, "inside-viewport focus requires a real map drag");
            view.FocusThreatDrag = false;
            touch.TargetId = -20003; touch.Actions = plan.MapFocusActions + 1;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20004, "opening-map completion is not focus completion");
            touch.TargetId = -20004; touch.Actions = plan.MapFocusActions + 2;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "one focus gesture closes map even for moving contact");
            touch.TargetId = 16;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "close intent persists while hand approaches close");
            view.MapContactInView = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 16, "close tactical map before world order");
            view.MapOpen = false;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(plan.Intent == AriaSkirmishIntent.FindThreat, "camera settle cannot interrupt attack with recruitment");
            view.Time += 2;
            view.Threat = new AriaTouchTarget { Available = true, Id = -20002 };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == -20002 && plan.Intent == AriaSkirmishIntent.TargetThreat, "attack presented threat before base");
            touch.TargetId = -20002; touch.Actions++;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Waiting, "threat tap also advances and waits");
            view.Time = 30; plan.ObserveUntil = 0; plan.Intent = AriaSkirmishIntent.Attack;
            plan.NextRecruitAt = 0; view.Recruit = new AriaTouchTarget { Available = true, Id = 11 };
            view.AttackMode = false; view.SelectedSlot = 0;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.TargetId == 14, "recruitment cannot interrupt a combat sequence");
            view.Time = 200;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(touch.Phase == AriaPlayPhase.Blocked, "tap loop is not match progress");
            view.Finished = true;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Check(output.Kind == AriaPlayObservationKind.Finished, "terminal outcome is never replayed");
            // Group selection uses the same visible controls and drag input as the player.
            view = new AriaSkirmishObservation { Active = true, Time = 1, Infantry = 24,
                AvailableSquads = 1, FocusPlayer = new AriaTouchTarget { Available = true, Id = 20 },
                Select = new AriaTouchTarget { Available = true, Id = 21 },
                FocusEnemy = new AriaTouchTarget { Available = true, Id = 22 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1 }; touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 20, "focus troops before selecting a group");
            touch.Actions++; touch.TargetId = 20; view.Time = 2;
            Step(); Check(output.Kind == AriaPlayObservationKind.Waiting, "wait for camera to settle");
            view.Time = 5;
            Step(); Check(output.Kind == AriaPlayObservationKind.Waiting, "never press Select without a usable rectangle");
            view.GroupStart = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(200, 200) };
            view.GroupEnd = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(600, 500) };
            view.GroupEnd.Available = false;
            Step(); Check(output.TargetId != 21, "one clear corner is insufficient for Select");
            view.GroupEnd.Available = true;
            var end = view.GroupEnd.Position;
            view.GroupEnd.Position = view.GroupStart.Position;
            Step(); Check(output.TargetId != 21, "zero-size rectangle cannot arm Select");
            view.GroupEnd.Position = end;
            Step(); Check(output.TargetId == 21, "activate rectangle selection only with valid drag ready");
            touch.Actions++; touch.TargetId = 21; view.SelectionMode = true;
            Step(); Check(output.Drag == 1 && output.DragEnd == view.GroupEnd.Position, "selection is a drag, never a soldier tap");
            touch.Actions++; touch.TargetId = -20005; view.SelectionVisible = true; view.SelectedCount = 24;
            Step(); Check(plan.GroupStage == 3 && output.TargetId == 22 && output.Drag == 0, "completed selection advances without repeating drag");
            view.SelectedSlot = -1; view.SelectionVisible = true; view.EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 };
            plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions; touch.TargetId = -20001; touch.Actions++;
            Step(); Check(plan.GroupStage == 3 && plan.Slot == 0 && plan.Cycle == 1,
                "completed group attack preserves multi-selection instead of splitting into cards");
            plan.Cycle = 0; plan.ObserveUntil = 0;
            plan.GroupStage = 1; plan.GroupReadyAt = 1; plan.GroupAction = 0;
            view.Time = 10; view.GroupStart = default; view.GroupEnd = default;
            view.SelectedSlot = 0;
            Step(); Check(plan.GroupStage == 4, "card fallback is never treated as whole-army selection");
            plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions;
            touch.TargetId = -20001; touch.Actions++;
            Step(); Check(plan.Slot == 0 && plan.Cycle == 1, "fallback completes the cycle when other squads are empty");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 8,
                AvailableSquads = 1, Squad0 = new AriaTouchTarget { Available = true, Id = 20 } };
            plan = new AriaSkirmishPlanComponent { RecruitDrawerSeen = 1 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.AssaultStarted == 0 && output.Kind == AriaPlayObservationKind.Waiting,
                "opening preserves normal engagement while waiting for the next recruitment");
            view.ThreatNearForce = true;
            view.Threat = new AriaTouchTarget { Available = true, Id = -20002 };
            view.Attack = new AriaTouchTarget { Available = true, Id = 14 };
            Step(); Check(output.TargetId == 14, "opening arms Attack when a nearby contact is already on screen");
            view.AttackMode = true;
            Step(); Check(output.TargetId == -20002 && plan.Intent == AriaSkirmishIntent.TargetThreat,
                "opening contests the nearby contact instead of idling until the assault deadline");
            view.Infantry = 0; view.AvailableSquads = 0; view.Threat = default; view.FocusThreat =
                new AriaTouchTarget { Available = true, Id = -20003 };
            plan.MapNavigationReadyAt = 0;
            Step(); Check(plan.Intent == AriaSkirmishIntent.Recruit && output.TargetId != -20003,
                "empty opening force rebuilds instead of opening the threat map");
            view.ThreatNearForce = false; view.Threat = default; view.FocusThreat = default; view.AttackMode = false;
            view.Infantry = 8; view.AvailableSquads = 1;
            plan.Intent = AriaSkirmishIntent.ObserveBattle; plan.NextRecruitAt = 999;
            view.SelectionVisible = true; view.SelectedSlot = 0;
            view.Hold = new AriaTouchTarget { Available = true, Id = 30 };
            Step(); Check(output.TargetId != 30, "opening does not pin defenders with Hold");
            touch.TargetId = 30; touch.Actions++;
            Step(); Check(plan.Intent == AriaSkirmishIntent.ObserveBattle, "opening returns to recruitment rather than issuing more Hold orders");
            view.AvailableSquads = 5; view.Infantry = 12; view.Time = 12;
            Step(); Check(output.Kind == AriaPlayObservationKind.Waiting,
                "new arrivals retain their automatic rally instead of being stopped at delivery");
            view.AvailableSquads = 0; view.Infantry = 0; view.Time = 20;
            view.Recruit = new AriaTouchTarget { Available = true, Id = 11 };
            plan.Intent = AriaSkirmishIntent.GroupForce; plan.AssaultStarted = 1; plan.NextRecruitAt = 0;
            Step(); Check(output.TargetId == 11 && plan.Intent == AriaSkirmishIntent.Recruit,
                "empty army recruits instead of selecting building markers");
            view.AvailableSquads = 9; view.Infantry = 6; view.Time = 30;
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, Slot = 3,
                TargetPending = 1, ActionsAtTarget = touch.Actions };
            touch.TargetId = -20002; touch.Actions++;
            Step(); Check(plan.Slot == 0 && plan.Cycle == 1 && plan.Intent == AriaSkirmishIntent.ObserveBattle,
                "empty final vehicle slot still finishes the cycle and opens recruitment window");
            view.AvailableSquads = 31; view.Time = 40; view.Infantry = 3;
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, GroupStage = 4,
                TargetPending = 1, ActionsAtTarget = touch.Actions };
            touch.TargetId = -20002; touch.Actions++;
            Step(); Check(output.TargetId == 11 && plan.Slot == 1,
                "depleted army recruits after an accepted order without waiting for every squad");
            view.Infantry = 12; plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions;
            plan.AssaultStarted = 1; plan.GroupStage = 4;
            plan.Intent = AriaSkirmishIntent.Attack; plan.RecruitBurstRemaining = 0;
            touch.Actions++; Step();
            Check(plan.Intent != AriaSkirmishIntent.Recruit && plan.Slot == 2,
                "viable army finishes its squad orders before returning to production");
            plan.Slot = 1; plan.Intent = AriaSkirmishIntent.Recruit; plan.RecruitBurstRemaining = 2;
            plan.RecruitDrawerSeen = 1; view.Time = 41;
            view.FocusPlayer = new AriaTouchTarget { Available = true, Id = 22 };
            view.Squad1 = new AriaTouchTarget { Available = true, Id = 23 };
            view.SelectedSlot = -1; view.SelectionVisible = false;
            Step(); Check(plan.RecruitBurstRemaining == 1 && output.Kind == AriaPlayObservationKind.Waiting,
                "depleted force waits briefly for a second recruitment without regrouping");
            view.Time = 45;
            Step(); Check(output.TargetId == 11, "second replenishment uses visible production controls");
            plan.RecruitDrawerSeen = 1; view.Time = 46;
            Step(); Check(plan.GroupStage == 4 && output.TargetId == 23 && plan.RecruitBurstRemaining == 0,
                "bounded replenishment resumes squad rotation without moving camera home");
            plan.GroupStage = 3; plan.RegroupAt = 0; view.Time = 47;
            Step(); Check(plan.GroupStage == 4 && plan.AssaultStarted == 1,
                "lost selection with surviving army resumes squad cards instead of restarting opening");
            view = new AriaSkirmishObservation { Active = true, Time = 1, Infantry = 8,
                Recruit = new AriaTouchTarget { Id = 11, Available = true },
                DefenseBuild = new AriaTouchTarget { Id = 50, Available = true },
                FocusPlayer = new AriaTouchTarget { Id = 51, Available = true } };
            plan = default; touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 11 && plan.DefenseStage == 0, "opening recruits before spending time on construction");
            view.Infantry = 16;
            Step(); Check(output.TargetId == 51, "defensive construction first focuses our base through its button");
            touch.Actions++; view.Time = 2; Step();
            Check(output.Kind == AriaPlayObservationKind.Waiting, "construction waits for camera");
            view.Time = 5; Step(); Check(output.TargetId == 50, "construction follows visible catalog controls");
            view.PlacementOpen = true;
            view.PlacementConfirm = new AriaTouchTarget { Id = 52, Available = true };
            Step(); Check(output.TargetId == 52 && plan.DefensesPlaced == 0, "valid placement still requires actual confirmation gesture");
            touch.Actions++; view.PlacementOpen = false; view.Time = 6; Step();
            Check(plan.DefensesPlaced == 1, "closed placement after confirmation acknowledges one construction");
            view.Time = 8; view.PlacementOpen = true; view.PlacementConfirm = default;
            view.PlacementCancel = new AriaTouchTarget { Id = 53, Available = true };
            Step(); Check(output.TargetId == 53 && plan.DefensesPlaced == 1, "invalid site cancels without counting a tower");
            view.PlacementOpen = false; view.Time = 9; Step();
            Check(plan.DefenseStage == 4, "failed construction cannot trap the rest of the match");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 24, AvailableSquads = 1,
                SelectionVisible = true, SelectedSlot = 0, AdvancePreferred = true,
                Attack = new AriaTouchTarget { Available = true, Id = 14 },
                AdvanceGround = new AriaTouchTarget { Available = true, Id = -20006 },
                Threat = new AriaTouchTarget { Available = true, Id = -20002 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, DefenseStage = 4, GroupStage = 4 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 14, "advance first arms the existing Attack control");
            view.AttackMode = true;
            Step(); Check(output.TargetId == -20006 && output.Kind == AriaPlayObservationKind.WorldTarget,
                "advance uses a visible ground touch instead of chasing a moving enemy");
            view.AdvanceGround = default; view.Threat = default;
            view.EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 };
            Step(); Check(output.TargetId == -20001, "visible base remains actionable when approach is obscured");
            view.EnemyBase = default;
            view.AdvanceGround = new AriaTouchTarget { Available = true, Id = -20006 };
            view.Threat = new AriaTouchTarget { Available = true, Id = -20002 };
            view.ThreatNearForce = true;
            Step(); Check(output.TargetId == -20006, "Attack Move retains its route when nearby enemies appear");
            view.AdvancePreferred = false;
            view.ThreatGround = new AriaTouchTarget { Available = true, Id = -20006 };
            Step(); Check(output.TargetId == -20006, "nearby threat uses clear ground to preserve automatic engagement");
            view.ThreatGround = default;
            view.ThreatNearForce = false; view.AdvancePreferred = true;
            Step();
            touch.TargetId = -20006; touch.Actions++;
            Step(); Check(plan.Cycle == 1 && plan.TargetPending == 0,
                "ground gesture completes squad order without repeat tapping");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 8, PlacementOpen = true,
                Site0 = new AriaTouchTarget { Available = true, Id = -20100 },
                Site1 = new AriaTouchTarget { Available = true, Id = -20101 },
                PlacementConfirm = new AriaTouchTarget { Available = true, Id = 52 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 2, DefenseDeadline = 100 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == -20100 && output.Kind == AriaPlayObservationKind.WorldTarget,
                "defense position is an actual ground gesture before confirm");
            Step(); Check(output.TargetId == -20100, "placement target persists until gesture completes");
            touch.Actions++; view.Time = 11; Step();
            view.Time = 12; view.PlacementConfirm = default;
            Step(); Check(output.TargetId == -20101 && plan.DefensesPlaced == 0,
                "invalid site tries another visible position without forced placement");
            touch.Actions++; view.Time = 13; Step();
            view.Time = 14; view.PlacementConfirm = new AriaTouchTarget { Available = true, Id = 52 };
            Step(); Check(output.TargetId == 52 && plan.DefensesPlaced == 0,
                "green placement requires confirmation touch before counting construction");
            view = new AriaSkirmishObservation { Active = true, Time = 100, Infantry = 24, AvailableSquads = 1,
                ThreatNearForce = true, FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, NextRecruitAt = 999 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.AssaultStarted == 0, "a full army finishes the nearby defensive fight before departing");
            view.Time = 260; plan.OpeningUntil = 100; plan.LastProgressAt = 10;
            plan.EnemyHealth = view.EnemyHealth; plan.PlayerHealth = view.PlayerHealth;
            plan.ForceHealth = view.ForceHealth; plan.Infantry = view.Infantry;
            Step(); Check(touch.Phase != AriaPlayPhase.Blocked && plan.AssaultStarted == 1,
                "opening standoff waits for the assault deadline instead of timing out as a stuck loop");
            view.Infantry = 15; view.ThreatNearForce = false; view.Time = 200;
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, NextRecruitAt = 999 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.AssaultStarted == 0, "a partial army does not depart at the old ninety-second deadline");
            view.Infantry = 24;
            Step(); Check(plan.AssaultStarted == 1, "assembled army can advance after the threat clears");
            view.AdvancePreferred = false; view.ThreatNearForce = true; view.SelectionVisible = true; view.SelectedSlot = 0;
            view.FocusThreat = new AriaTouchTarget { Available = true, Id = -20003 };
            plan.GroupStage = 4; plan.Intent = AriaSkirmishIntent.Attack;
            Step(); Check(output.TargetId == -20003, "offscreen frontline threat is brought into view instead of sending troops past it");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 20, AvailableSquads = 1,
                SelectionVisible = true, SelectedCount = 16, SelectedSlot = -1,
                Recruit = new AriaTouchTarget { Available = true, Id = 11 },
                FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, GroupStage = 3, AssaultStarted = 1,
                Intent = AriaSkirmishIntent.ObserveBattle };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 13 && plan.GroupStage == 3,
                "healthy selected force continues its assault instead of opening production mid-fight");
            view.SelectedCount = 6; view.Infantry = 10;
            Step(); Check(plan.GroupStage == 4 && plan.AssaultStarted == 1,
                "partial selection recovers surviving squads without a full army rebuild");
            plan.GroupStage = 3; view.Infantry = 6;
            Step(); Check(plan.GroupStage == 0 && plan.AssaultStarted == 0 && plan.OpeningUntil > view.Time,
                "depleted assault rebuilds a force instead of feeding isolated replacements");
            view = new AriaSkirmishObservation { Active = true, Time = 100, Infantry = 16, AvailableSquads = 1,
                SelectedCount = 16, SelectionVisible = true, SelectedSlot = -1, AssaultAtBase = true,
                AttackMode = true, EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 },
                ThreatNearForce = true, Threat = new AriaTouchTarget { Available = true, Id = -20002 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, GroupStage = 3, AssaultStarted = 1 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == -20001, "gathered assault attacks the base instead of endlessly chasing recruits");
            touch.TargetId = -20001; touch.Actions++;
            Step(); Check(plan.ObserveUntil >= view.Time + 12 && plan.GroupStage == 3,
                "base assault keeps its group and gets time to fire before replanning");
            view.Time += 13; view.SelectedCount = 4; view.Infantry = 4; view.EnemyHealth = 294;
            Step(); Check(plan.GroupStage == 3 && output.TargetId == -20001,
                "survivors finish a critically damaged base instead of abandoning their attack");
            view.SelectionVisible = false; view.SelectedCount = 0; view.AssaultAtBase = false;
            view.Infantry = 5; plan.TargetPending = 0; plan.ObserveUntil = 0;
            Step(); Check(plan.GroupStage == 0 && plan.AssaultStarted == 0 && plan.OpeningUntil > view.Time,
                "entire selected group dying rebuilds before sending isolated replacement squads");
            view = new AriaSkirmishObservation { Active = true, Time = 20, Infantry = 24,
                AvailableSquads = 1, FocusPlayer = new AriaTouchTarget { Available = true, Id = 20 },
                FocusGroup = new AriaTouchTarget { Available = true, Id = -20007 },
                Select = new AriaTouchTarget { Available = true, Id = 24 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, AssaultStarted = 1, NextRecruitAt = 999 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == -20007 && plan.GroupStage == 7, "grouping navigates through the actual map control");
            touch.Phase = AriaPlayPhase.Aiming; view.Time += .5f;
            Step(); Check(output.TargetId == -20007 && plan.GroupStage == 7,
                "map opening persists during hand approach instead of becoming a squad order");
            touch.TargetId = -20007; touch.Actions++; touch.Phase = AriaPlayPhase.Verifying;
            Step(); Check(output.TargetId == -20007,
                "map opening waits for the visible popup after touch completion");
            view.MapOpen = true; view.FocusGroup.Id = -20008; view.FocusGroupDrag = true;
            view.FocusGroupDragEnd = new UnityEngine.Vector2(400, 500);
            view.CloseMap = new AriaTouchTarget { Available = true, Id = 25 };
            Step(); Check(output.TargetId == -20008 && output.Drag == 1, "group focus drags map viewport when required");
            touch.TargetId = -20008; touch.Actions++;
            Step(); Check(output.TargetId == 25, "one group focus closes the map once");
            view.MapOpen = false; view.Time = 21; Step();
            Check(plan.GroupStage == 6 && output.Kind == AriaPlayObservationKind.Waiting, "map focus settles before group selection");
            view.Time = 29;
            view.GroupStart = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(200, 200) };
            view.GroupEnd = new AriaTouchTarget { Available = true, Id = -20005, Position = new UnityEngine.Vector2(600, 500) };
            Step(); Check(output.TargetId == 24, "map-focused group arms Select with a valid box");
            view.SelectionMode = true;
            Step(); Check(output.Drag == 1 && plan.GroupStage == 2, "map grouping completes with the real selection drag");
            view = new AriaSkirmishObservation { Active = true, Time = 10, Infantry = 8, PlacementOpen = true,
                Site1 = new AriaTouchTarget { Available = true, Id = -20101 },
                PlacementConfirm = new AriaTouchTarget { Available = true, Id = 52 },
                PlacementCancel = new AriaTouchTarget { Available = true, Id = 53 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 2, DefenseDeadline = 100,
                DefenseSitePending = 1, DefenseSiteAction = 4 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Aiming, Actions = 4, TargetId = -20100 };
            Step(); Check(output.TargetId == -20101 && plan.DefenseSite == 1,
                "camera-obscured pending site changes to a visible ground target before hand timeout");
            view.Site1 = default;
            Step(); Check(output.TargetId == 52 && plan.DefenseStage == 3 && plan.DefensesPlaced == 0,
                "no visible sites uses the normal valid preview confirmation without counting unbuilt defense");
            plan = new AriaSkirmishPlanComponent { DefenseStage = 2, DefenseDeadline = 100,
                DefenseSitePending = 1, DefenseSiteAction = 4 };
            view.PlacementConfirm = default;
            Step(); Check(output.TargetId == 53 && plan.DefensesPlaced == 0,
                "obscured sites and invalid preview cancel normally instead of blocking forever");
            view = new AriaSkirmishObservation { Active = true, Time = 100, Infantry = 24,
                AvailableSquads = 15, SelectionVisible = true, SelectedCount = 12, SelectedSlot = -1,
                FocusPlayer = new AriaTouchTarget { Available = true, Id = 51 },
                Squad0 = new AriaTouchTarget { Available = true, Id = 12 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, AssaultStarted = 1, GroupStage = 2, GroupAction = 4 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Actions = 5, TargetId = -20005 };
            Step(); Check(plan.GroupStage == 4 && output.TargetId == 12,
                "partial rectangle rotates all squad cards so off-screen reserves join the assault");
            view.SelectedCount = 20; plan.GroupStage = 2;
            Step(); Check(plan.GroupStage == 3, "a rectangle covering most of the army stays grouped");
            view = new AriaSkirmishObservation { Active = true, Time = 100, Infantry = 18, AvailableSquads = 15,
                SelectedCount = 16, SelectionVisible = true, Recruit = new AriaTouchTarget { Available = true, Id = 61 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, DefenseStage = 4, GroupStage = 3,
                Intent = AriaSkirmishIntent.ObserveBattle };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == 61 && plan.GroupStage == 4 && plan.AssaultStarted == 1,
                "viable assault recruits replacements before heavy casualties without restarting opening");
            view.SelectionVisible = false; view.SelectedCount = 0; plan.GroupStage = 3;
            Step(); Check(plan.AssaultStarted == 1, "hidden selection UI is not evidence the whole army died");
            view.Infantry = 5; plan.GroupStage = 4;
            Step(); Check(plan.AssaultStarted == 0 && plan.GroupStage == 0 && output.TargetId == 61,
                "depleted card rotation also rebuilds rather than sending isolated deliveries");
            view = new AriaSkirmishObservation { Active = true, Time = 100, Infantry = 24, AvailableSquads = 1,
                SelectionVisible = true, SelectedCount = 24, SelectedSlot = -1, AdvancePreferred = true,
                FocusAdvance = new AriaTouchTarget { Available = true, Id = -20010 } };
            plan = new AriaSkirmishPlanComponent { DefenseStage = 4, AssaultStarted = 1, GroupStage = 3 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(output.TargetId == -20010 && plan.AdvanceNavigation == 1, "obscured flank opens the visible map without discarding selection");
            Step(); Check(output.TargetId == -20010 && plan.GroupStage == 3, "flank map opening stays pending until actual popup appears");
            view.MapOpen = true; view.FocusAdvance.Id = -20011;
            view.FocusAdvanceDrag = true; view.FocusAdvanceDragEnd = new UnityEngine.Vector2(600, 500);
            view.CloseMap = new AriaTouchTarget { Available = true, Id = 62 };
            Step(); Check(output.TargetId == -20011 && output.Drag == 1, "flank camera movement uses the visible map viewport drag");
            touch.TargetId = -20011; touch.Actions++; Step();
            Check(output.TargetId == 62, "completed flank focus closes the map exactly once");
            view.MapOpen = false; Step();
            Check(plan.GroupStage == 3 && plan.AdvanceNavigation == 0, "flank focus preserves the army selection and completes navigation");
            view.FocusAdvance = new AriaTouchTarget { Available = true, Id = -20010 };
            view.FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 };
            view.Time = plan.MapNavigationReadyAt - 1f;
            Step(); Check(output.TargetId == 13 && plan.MapNavigationStage == 0 && plan.AdvanceNavigation == 0,
                "advance settle uses the labelled enemy camera instead of reopening the map");
            view.Time += 10; view.FocusAdvance.Id = 63;
            Step(); Check(output.TargetId == 63 && plan.MapNavigationStage == 0,
                "final approach can use the labelled base camera button without entering a map loop");
            view = new AriaSkirmishObservation { Active = true, ExpandedSession = true, Time = 100,
                Infantry = 20, PlayerHealth = 800, EnemyHealth = 800, EnemyDesignatedAlive = true,
                CombinedArmsField = true, OwnStructureAttackUnits = 8, SelectionVisible = true,
                SelectedCount = 20, AttackMode = true, ExpandedPageCount = 1, ExpandedTotalGroups = 2,
                ExpandedAssaultMask = 3, ExpandedStructureMask = 2, ExpandedSelectedMask = 3,
                ExpandedSelectedGroups = 2, FocusEnemy = new AriaTouchTarget { Available = true, Id = 13 },
                AdvanceGround = new AriaTouchTarget { Available = true, Id = -20006 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.Intent == AriaSkirmishIntent.Advance && output.TargetId == -20006 &&
                output.Kind == AriaPlayObservationKind.WorldTarget && plan.AssaultIssued == 0,
                "expanded hidden base offers presented terrain Attack Move without pretending an order was issued");
            view.EnemyBase = new AriaTouchTarget { Available = true, Id = -20001 };
            Step(); Check(plan.Intent == AriaSkirmishIntent.TargetBase && output.TargetId == -20001 &&
                output.Kind == AriaPlayObservationKind.WorldTarget,
                "native visible base retains precision-target priority over the approach");
            view.EnemyBase = default; view.AdvanceGround = default;
            Step(); Check(plan.Intent == AriaSkirmishIntent.FindBase && output.TargetId == 13 &&
                output.Kind == AriaPlayObservationKind.Control && plan.AssaultIssued == 0,
                "unpresented terrain retains camera-only guidance without inventing a hidden contact");
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Verifying, Attempts = 3,
                TargetId = 13, GoalId = output.GoalId, LastProgressAt = view.Time,
                LastObjectiveProgressAt = view.Time, ObjectiveWatchdogInitialized = 1,
                VisitedGoalMask = 1UL << (output.GoalId & 63) };
            Step(); AriaPlayDecisionSystem.Step(output, ref touch);
            Check(touch.Phase == AriaPlayPhase.Blocked && touch.GestureRequested == 0,
                "unavailable approach cannot bypass the existing three-attempt camera bound");
            view.AdvanceGround = new AriaTouchTarget { Available = true, Id = -20006 };
            Step(); Check(output.Kind == AriaPlayObservationKind.Waiting && touch.Phase == AriaPlayPhase.Blocked,
                "new approach availability cannot silently restart blocked ARIA");
            view.AttackMode = false;
            view.Attack = new AriaTouchTarget { Available = true, Id = 14 };
            // This cohort is already assembled by the plan. Initial selection reset
            // is tested separately through presented card toggles and swipes.
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, AssaultSelecting = 1 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.Intent == AriaSkirmishIntent.Attack && output.TargetId == 14 &&
                output.Kind == AriaPlayObservationKind.Control && plan.AssaultIssued == 0,
                "expanded selected force must arm explicit Attack before any presented terrain world touch");
            // Native1158 page0 had Assault31/Attack3: the already ordered rifles must
            // remain excluded while the pending rocketeer/car/APC cohort receives its own order.
            view = new AriaSkirmishObservation { Active = true, ExpandedSession = true, Time = 100,
                Infantry = 16, PlayerHealth = 800, EnemyHealth = 800, EnemyDesignatedAlive = true,
                CombinedArmsField = true, OwnStructureAttackUnits = 8, SelectionVisible = true,
                ExpandedPageCount = 1, ExpandedTotalGroups = 5, ExpandedAssaultMask = 31,
                ExpandedStructureMask = 4, ExpandedAttackOrderMask = 3, ExpandedSelectedMask = 28,
                ExpandedSelectedGroups = 3, Attack = new AriaTouchTarget { Available = true, Id = 14 },
                SelectedSquadToToggle = new AriaTouchTarget { Available = true, Id = 99 },
                Squad0 = new AriaTouchTarget { Available = true, Id = 40 },
                Squad1 = new AriaTouchTarget { Available = true, Id = 41 },
                Squad2 = new AriaTouchTarget { Available = true, Id = 42 },
                Squad3 = new AriaTouchTarget { Available = true, Id = 43 },
                Squad4 = new AriaTouchTarget { Available = true, Id = 44 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, AssaultSelecting = 1, PagedToAssault = 1 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(); Check(plan.Intent == AriaSkirmishIntent.Attack && output.TargetId == 14 &&
                output.Kind == AriaPlayObservationKind.Control && plan.AssaultIssued == 0,
                "native partial-order page arms explicit Attack when every remaining assault group is selected");
            view.ExpandedSelectedMask = 4; view.ExpandedSelectedGroups = 1;
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, AssaultSelecting = 1, PagedToAssault = 1 };
            Step(); Check(plan.Intent == AriaSkirmishIntent.SelectSquad && output.TargetId == 43 && plan.AssaultIssued == 0,
                "partial pending cohort selects the next unordered card without reselecting accepted rifles");
            view.ExpandedAttackOrderMask = 31; view.ExpandedSelectedMask = 0; view.ExpandedSelectedGroups = 0;
            view.SelectionVisible = false;
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, AssaultSelecting = 1, PagedToAssault = 1 };
            Step(); Check(plan.AssaultIssued == 1 && plan.StructureOrdered == 1 && plan.PagesOrdered == 1 &&
                plan.AssaultPageSeen == 1 && output.Kind == AriaPlayObservationKind.Waiting,
                "fully accepted one-page assault completes inspection even when no group remains selected");
            view.ExpandedAttackOrderMask = 0; view.ExpandedSelectedMask = 31; view.ExpandedSelectedGroups = 5;
            view.SelectionVisible = true;
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, AssaultSelecting = 1, PagedToAssault = 1 };
            Step(); Check(plan.Intent == AriaSkirmishIntent.Attack && output.TargetId == 14 &&
                output.Kind == AriaPlayObservationKind.Control && plan.AssaultIssued == 0,
                "no accepted orders still require the complete assault selection before explicit Attack");
            // A source-pinned ARIA sweep can momentarily lack a presented page
            // after roster losses while its accepted ArmyAttack is still travelling.
            // The HUD Hold control remains visible, but no group is selected, so
            // TryHoldSelection cannot accept it. Waiting preserves the assault.
            view = new AriaSkirmishObservation { Active = true, ExpandedSession = true,
                Time = 190, Infantry = 15, PlayerHealth = 800, EnemyHealth = 800,
                EnemyDesignatedAlive = true, CombinedArmsField = true,
                OwnStructureAttackUnits = 4,
                Hold = new AriaTouchTarget { Available = true, Id = 77 } };
            plan = new AriaSkirmishPlanComponent { AssaultStarted = 1, LastProgressAt = 190,
                EnemyHealth = 800, PlayerHealth = 800, Infantry = 15 };
            touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing,
                LastProgressAt = 190, LastObjectiveProgressAt = 190 };
            for (int attempt = 0; attempt < 4; attempt++)
            {
                view.Time = 190 + attempt * 2;
                Step(); AriaPlayDecisionSystem.Step(output, ref touch);
                Check(plan.Intent == AriaSkirmishIntent.ObserveBattle &&
                    output.Kind == AriaPlayObservationKind.Waiting && touch.Attempts == 0 &&
                    touch.Phase != AriaPlayPhase.Blocked,
                    "unselected in-flight assault observes battle instead of exhausting Hold retries");
            }
            checks += RunExpandedAssaultRecruitmentControls();
            checks += RunSurvivingStructureRifleDrawerControls();
            checks += RunPublicApproachProgressControls();
            return $"[AriaSkirmishPlanValidation] result=Passed cases={checks}";

            void Step() => AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
        }
    }
}
#endif
