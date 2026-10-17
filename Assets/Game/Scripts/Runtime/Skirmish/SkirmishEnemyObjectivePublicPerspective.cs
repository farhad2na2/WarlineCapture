using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    // Faction 2's independent legal observer. Never reads BT/CE original subject
    // arrays, collector originals, hidden hostile transforms or hostile resources.
    public static class SkirmishEnemyObjectivePublicPerspective
    {
        public static bool TryBuild(EntityManager em,Entity session,SkirmishResolvedSetup setup,
            SkirmishEnemyPerceptionBuilder.Snapshot legalContacts,
            out SkirmishPublicGoalSnapshot snapshot,out string error)
        {
            snapshot=null;error="Exact active native objective/public binding required.";
            if(setup==null||setup.EnemyFaction!=2||setup.PlayerFaction!=1||legalContacts==null||
                !em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||
                !em.HasComponent<SkirmishObjectiveStateComponent>(session)||!em.HasComponent<SkirmishObjectiveClockComponent>(session)||
                !em.HasComponent<SkirmishNativeObjectiveRecord>(session)||
                !SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var compiled))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            var owner=em.GetComponentObject<SkirmishNativeObjectiveRecord>(session);
            if(native.IsLegacy!=0||native.Phase!=SkirmishSessionPhase.Playing||native.SpawnComplete!=1||
                native.SetupHash!=setup.SetupHash||native.DefinitionId.ToString()!=setup.DefinitionId||
                native.ObjectiveKind!=setup.ObjectiveKind||objective.Kind!=setup.ObjectiveKind||objective.Terminal!=0||
                objective.Outcome!=SkirmishOutcomeKind.None||clock.Playing!=1||clock.Paused!=0||
                owner?.Areas==null||!ReferenceEquals(owner.Areas,compiled.Areas)||
                !float.IsFinite(clock.ElapsedSeconds)||clock.ElapsedSeconds<0||
                Math.Abs(clock.ElapsedSeconds-owner.Clock.LastAuthoritativeSeconds)>.00001)return false;
            long elapsed,deadline;
            var score=default(SkirmishPublicGoalScore);var nodes=new List<SkirmishPublicGoalNode>();
            SkirmishPublicGoalPerspective perspective;
            if(setup.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl)
            {
                perspective=SkirmishPublicGoalPerspective.Capture;
                elapsed=owner.Frontline.ElapsedTicks;deadline=owner.Frontline.DeadlineTicks;
                score.TicketsKnown=true;score.OwnTickets=owner.Frontline.EnemyTickets;score.OpposingTickets=owner.Frontline.PlayerTickets;
                if(!Zone(compiled.Areas,SkirmishObjectiveAreaRole.FrontlineA,owner.Frontline.ZoneA,elapsed,nodes)||
                    !Zone(compiled.Areas,SkirmishObjectiveAreaRole.FrontlineB,owner.Frontline.ZoneB,elapsed,nodes)||
                    !Zone(compiled.Areas,SkirmishObjectiveAreaRole.FrontlineC,owner.Frontline.ZoneC,elapsed,nodes))return false;
            }
            else if(setup.ObjectiveKind==SkirmishObjectiveKind.Breakthrough)
            {
                perspective=SkirmishPublicGoalPerspective.DefendExits;
                elapsed=owner.Breakthrough.ElapsedTicks;deadline=owner.Breakthrough.DeadlineTicks;
                score.ExitStateKnown=true;score.ExitOpen=owner.Breakthrough.ExitOpen;
                if(!Area(compiled.Areas,SkirmishObjectiveAreaRole.BreakthroughCorridorA,SkirmishPublicGoalNodeKind.ExitCorridor,elapsed,out var a)||
                    !Area(compiled.Areas,SkirmishObjectiveAreaRole.BreakthroughCorridorB,SkirmishPublicGoalNodeKind.ExitCorridor,elapsed,out var b)||
                    !Area(compiled.Areas,SkirmishObjectiveAreaRole.BreakthroughExit,SkirmishPublicGoalNodeKind.EvacuationExit,elapsed,out var exit))return false;
                // Corridor hold and exit state are public objective progress.
                a.WorkProgressKnown=b.WorkProgressKnown=true;
                a.WorkTicks=owner.Breakthrough.CorridorA.HoldTicks;b.WorkTicks=owner.Breakthrough.CorridorB.HoldTicks;
                a.RequiredWorkTicks=b.RequiredWorkTicks=SkirmishBreakthroughReducer.CorridorHoldTicks;
                nodes.Add(a);nodes.Add(b);nodes.Add(exit);
            }
            else if(setup.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort)
            {
                perspective=SkirmishPublicGoalPerspective.Interdict;
                elapsed=owner.Convoy.ElapsedTicks;deadline=owner.Convoy.DeadlineTicks;
                if(!Area(compiled.Areas,SkirmishObjectiveAreaRole.ConvoyDestination,SkirmishPublicGoalNodeKind.DeliveryDestination,elapsed,out var destination))return false;
                nodes.Add(destination);
            }
            else return false;
            if(elapsed<0||deadline<elapsed)return false;
            // Only currently visible contacts may be pursued. Designation remains
            // unknown until a real publicly revealed marker/role owner exists.
            // Seeing an ordinary Rifle does not expose its hidden original ID.
            int subjects=0,maximumSubjects=setup.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort?3:12;
            foreach(var contact in legalContacts.Visible)
            {
                if(nodes.Count>=32||subjects>=maximumSubjects)break;
                if(contact.Structure||!em.Exists(contact.Entity)||
                    !SkirmishFogService.IsVisibleToFaction(em,contact.Entity,2)||
                    !em.HasComponent<Faction>(contact.Entity)||em.GetComponentData<Faction>(contact.Entity).Id!=1||
                    !em.HasComponent<SkirmishAttemptOwnedComponent>(contact.Entity)||
                    !em.HasComponent<UnitHealth>(contact.Entity)||em.GetComponentData<UnitHealth>(contact.Entity).Current<=0||
                    !em.HasComponent<SkirmishSharedActorTag>(contact.Entity)||em.HasComponent<Disabled>(contact.Entity)||
                    em.HasComponent<UnitDeathAnimationComponent>(contact.Entity)||em.HasComponent<UnitSpawnTransitTag>(contact.Entity)||
                    em.HasComponent<Parent>(contact.Entity)||em.HasComponent<UnitTransportPassenger>(contact.Entity)||
                    em.HasComponent<UnitTransportCargoPassenger>(contact.Entity)||!em.HasComponent<LocalTransform>(contact.Entity))continue;
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(contact.Entity);
                if(owned.SessionId!=native.SessionId||owned.FactionId!=1||owned.IsStructure!=0||owned.StableObjectId!=contact.StableId)continue;
                var position=em.GetComponentData<LocalTransform>(contact.Entity).Position;
                if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z))continue;
                // FC exposes public zones, not an extra hostile actor node shape.
                if(setup.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl)continue;
                nodes.Add(new SkirmishPublicGoalNode{PublicId=PublicContactId(contact.StableId.ToString()),
                    Kind=SkirmishPublicGoalNodeKind.ObservedOpposingSubject,Provenance=SkirmishPublicGoalProvenance.LegallyObservedContact,
                    PositionKnown=true,CurrentlyVisible=true,WorldX=position.x,WorldZ=position.z,LastObservedTicks=elapsed,
                    OwnerKnown=true,OwnerFaction=1,RevealedSubjectKind=SkirmishPublicSubjectKind.None,
                    SubjectDisposition=SkirmishPublicSubjectDisposition.Live});
                subjects++;
            }
            return SkirmishPublicGoalSnapshot.TryCreate(setup.DefinitionId,setup.ContentVersion,2,setup.ObjectiveKind,perspective,
                elapsed,deadline-elapsed,SkirmishOutcomeKind.None,score,nodes.ToArray(),out snapshot,out error);
        }

        public static string PublicContactId(string actualObservedStableId)
        {
            if(string.IsNullOrEmpty(actualObservedStableId))throw new ArgumentException("Actual observed contact identity required.");
            using var sha=System.Security.Cryptography.SHA256.Create();
            return "seen."+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(actualObservedStableId))).Replace("-","").ToLowerInvariant().Substring(0,56);
        }

        private static bool Zone(SkirmishCompiledObjectiveAreas areas,SkirmishObjectiveAreaRole role,
            SkirmishFrontlineZoneState state,long now,List<SkirmishPublicGoalNode> nodes)
        {
            if(!Area(areas,role,SkirmishPublicGoalNodeKind.CaptureZone,now,out var node)||state.StableZoneId!=node.PublicId)return false;
            node.OwnerKnown=true;node.OwnerFaction=(int)state.Owner;node.Contested=state.Contested;
            node.CaptureProgressKnown=state.Phase!=SkirmishFrontlineCapturePhase.None;
            if(node.CaptureProgressKnown)
            {
                node.CapturingFaction=(int)state.CapturingSide;
                node.CaptureFraction=(float)((double)state.ProgressTicks/SkirmishFrontlineReducer.CaptureTicks);
                node.CapturePhase=state.Phase==SkirmishFrontlineCapturePhase.Neutralizing?SkirmishPublicCapturePhase.Neutralizing:SkirmishPublicCapturePhase.Capturing;
            }
            else node.CapturePhase=state.Owner==SkirmishFrontlineSide.Neutral?SkirmishPublicCapturePhase.None:SkirmishPublicCapturePhase.Held;
            nodes.Add(node);return true;
        }
        private static bool Area(SkirmishCompiledObjectiveAreas areas,SkirmishObjectiveAreaRole role,SkirmishPublicGoalNodeKind kind,
            long now,out SkirmishPublicGoalNode node)
        {
            node=default;if(!areas.TryRead(role,out var area))return false;
            double x=0,z=0;foreach(var p in area.Points){x+=p.WorldX;z+=p.WorldZ;}
            float px=(float)(x/area.Points.Length),pz=(float)(z/area.Points.Length);
            // Concave polygon mean can lie outside. A perimeter vertex remains
            // actual declared geometry; native Move/path validation is separate.
            if(!areas.Contains(role,px,pz)){px=area.Points[0].WorldX;pz=area.Points[0].WorldZ;}
            if(!areas.Contains(role,px,pz))return false;
            node=new SkirmishPublicGoalNode{PublicId=area.StableAreaId,Kind=kind,Provenance=SkirmishPublicGoalProvenance.PublicObjective,
                PositionKnown=true,WorldX=px,WorldZ=pz,LastObservedTicks=now};return true;
        }
    }
}
