using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        static S005GuardMoveReceipt guardMovePending;
        static int guardMoveFailures,guardLastRequest;
        static uint guardLastVerifiedReceipt;
        static void StopS005GuardObservation(){AriaMoveExecutionEvidence.Recorded-=ObserveS005GuardNative;AriaCommandEvidence.Recorded-=ObserveS005GuardCommand;guardMovePending=null;}
        static void ResetS005GuardObservation(){StopS005GuardObservation();guardMoveFailures=guardLastRequest=0;guardLastVerifiedReceipt=0;}
        static void ObserveS005GuardCommand(string operation,uint receipt,bool verified)
        {if(guardMovePending!=null&&(operation=="Move"||operation=="AttackMove")&&verified)guardLastVerifiedReceipt=receipt;}
        static void ObserveS005GuardNative(AriaMoveExecutionEvidence.Row row)
        {
            if(guardMovePending==null)return;
            File.AppendAllText(LiveTracePath()+".guard-move-native.jsonl",JsonUtility.ToJson(row)+"\n");
            if(row.requestFrame>=guardMovePending.DispatchFrame&&Vector2.Distance(row.screen,guardMovePending.Screen)<=2)guardMovePending.Actual=row;
        }
        static int NativeGuardConstant(string name)
        {
            var field=typeof(UnitMoveOrderSystem).GetField(name,BindingFlags.NonPublic|BindingFlags.Static);
            if(field==null||!field.IsLiteral||field.FieldType!=typeof(int))throw new InvalidOperationException("Native formation constant unavailable: "+name);
            return (int)field.GetRawConstantValue();
        }
        static void PrepareS005GuardMove(uint renderedGroup,Vector3 site,Vector2 screen,double now)
        {
            var world=World.DefaultGameObjectInjectionWorld;
            if(renderedGroup==0||world==null||!world.IsCreated||!SkirmishLaunchProjection.TryGet(world.EntityManager,out var session,out _))
                throw new InvalidOperationException("Actual presented defense group/session unavailable");
            var em=world.EntityManager;var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(!em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session))throw new InvalidOperationException("Actual defense source binding absent");
            var launch=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);var resolved=em.GetComponentObject<SkirmishResolvedSetupRecord>(session);
            if(launch==null||resolved==null||!launch.Matches(resolved.Setup,attempt)||launch.Definition?.MapLayout==null||!launch.Definition.MapLayout.HasWorldBinding)
                throw new InvalidOperationException("Actual defense source/layout binding differs");
            using var grids=em.CreateEntityQuery(typeof(GridConfig));if(grids.CalculateEntityCount()!=1)throw new InvalidOperationException("Unique defense native grid required");
            var grid=grids.GetSingleton<GridConfig>();if(grid.CellSize<=0)throw new InvalidOperationException("Native defense grid invalid");
            var members=new List<AriaMoveExecutionEvidence.Member>();float spreadX=0,spreadZ=0;
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth),typeof(Faction));
            using var actors=query.ToEntityArray(Allocator.Temp);
            int groupSize=0;
            foreach(var actor in actors)
            {var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);if(own.SessionId==attempt.SessionId&&own.FactionId==1&&own.IsStructure==0&&own.CapacityReleased==0&&
                em.GetComponentData<UnitHealth>(actor).Current>0&&em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId==renderedGroup)groupSize++;}
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=attempt.SessionId||own.FactionId!=1||own.IsStructure!=0||own.CapacityReleased!=0||em.GetComponentData<UnitHealth>(actor).Current<=0)continue;
                uint group=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                bool selected=em.HasComponent<SelectedUnitTag>(actor);
                if(group!=renderedGroup){if(selected)throw new InvalidOperationException("Another own group is selected");continue;}
                if(!selected||em.GetComponentData<Faction>(actor).Id!=1||own.StableObjectId.IsEmpty)throw new InvalidOperationException("Presented guard is not the complete selected living group");
                int2 footprint=em.HasComponent<UnitFootprint>(actor)?em.GetComponentData<UnitFootprint>(actor).Size:new int2(1,1);
                bool vehicle=UnitVehicleMovementUtility.IsVehicle(new UnitFootprint{Size=footprint},em.HasComponent<UnitMovementBehavior>(actor)?em.GetComponentData<UnitMovementBehavior>(actor):default);
                int padding=NativeGuardConstant(vehicle?"ManualMoveGoalPaddingVehicle":"ManualMoveGoalPaddingInfantry");
                int radius=NativeGuardConstant(vehicle?"ManualMoveGoalSearchRadiusVehicle":"ManualMoveGoalSearchRadiusInfantry");
                // Every selected member may occupy any native formation slot;
                // take the union before issuing the unchanged guard target.
                float maxOffset=0;
                for(int slot=0;slot<groupSize;slot++)
                {var offset=new UnitMoveOrderSystem().GetManualMoveFormationOffset(slot,footprint,padding);maxOffset=Mathf.Max(maxOffset,Mathf.Abs(offset.x),Mathf.Abs(offset.y));}
                float reach=(maxOffset+radius+.5f)*grid.CellSize;
                spreadX=Mathf.Max(spreadX,reach);spreadZ=Mathf.Max(spreadZ,reach);
                members.Add(new AriaMoveExecutionEvidence.Member{entityIndex=actor.Index,entityVersion=actor.Version,stableId=own.StableObjectId.ToString()});
            }
            if(members.Count==0)throw new InvalidOperationException("No living selected defense members");
            // Explicit QA producer-relative envelope, NOT an authored Defense pad.
            // It reports the existing target plus native formation/search bounds.
            var envelope=Rect.MinMaxRect(Mathf.Max(grid.Origin.x,site.x-spreadX),Mathf.Max(grid.Origin.z,site.z-spreadZ),
                Mathf.Min(grid.Origin.x+grid.Width*grid.CellSize,site.x+spreadX),Mathf.Min(grid.Origin.z+grid.Height*grid.CellSize,site.z+spreadZ));
            guardMovePending=new S005GuardMoveReceipt{Attempt=attempt.SessionId.ToString(),Group=renderedGroup,Members=members.ToArray(),Screen=screen,
                DispatchFrame=Time.frameCount,CompletedBefore=helipadProbeTouch.CompletedGestures,LastRequest=guardLastRequest,Deadline=now+8,QaEnvelope=envelope};
            AriaMoveExecutionEvidence.Recorded-=ObserveS005GuardNative;AriaMoveExecutionEvidence.Recorded+=ObserveS005GuardNative;
            AriaCommandEvidence.Recorded-=ObserveS005GuardCommand;AriaCommandEvidence.Recorded+=ObserveS005GuardCommand;
            Debug.Log("[S005GuardMove] stage=Prepared group="+renderedGroup+" members="+members.Count+" envelopeKind=QaProducerRelativeNativeFormation xMin="+envelope.xMin+" zMin="+envelope.yMin+" xMax="+envelope.xMax+" zMax="+envelope.yMax);
        }
        static S005GuardReceiptState ObserveS005GuardMove(double now)
        {
            if(guardMovePending==null)throw new InvalidOperationException("Owned guard receipt missing");
            guardMovePending.VerifiedReceipt=guardLastVerifiedReceipt;
            var state=guardMovePending.Evaluate(helipadProbeTouch.CompletedGestures,helipadProbeTouch.IsBusy,now);
            if(state==S005GuardReceiptState.Waiting)return state;
            var actual=guardMovePending.Actual;
            if(actual!=null)guardLastRequest=Math.Max(guardLastRequest,actual.requestId);
            Debug.Log("[S005GuardMove] result="+state+" group="+guardMovePending.Group+" receipt="+(actual?.receipt??0)+" request="+(actual?.requestId??0)+" reason="+guardMovePending.Failure);
            guardMovePending=null;
            if(state==S005GuardReceiptState.Retry&&++guardMoveFailures>=3)throw new InvalidOperationException("Opening defense full-member Move failed three attempts; see preserved guard receipts");
            if(state==S005GuardReceiptState.Accepted)guardMoveFailures=0;
            return state;
        }
    }
}
