using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiPublicBoardingGateway
    {
        bool IUiPublicBoardingGateway.TryReadPublicBoarding(out UiPublicBoardingModel model)
        {
            model=null;if(!TrySkirmish(out var em,out var session,out _))return false;
            if(!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag));using var selectedActors=selected.ToEntityArray(Allocator.Temp);
            SkirmishPublicBoardingPresentation row=null;int pending=0,mounted=0;
            if(em.HasComponent<SkirmishPublicBoardingPresentation>(session))
            {var captured=em.GetComponentObject<SkirmishPublicBoardingPresentation>(session);if(SkirmishPublicBoardingPresentation.TryReadProgress(em,session,captured,out pending,out mounted))row=captured;}
            bool existing=false;
            bool capturedCarrierSelected=row!=null&&selectedActors.Length==1&&selectedActors[0]==row.Carrier;
            if(!capturedCarrierSelected&&SkirmishPublicBoardingPresentation.TryReadExistingMounted(em,session,out var publicMounted))
            {
                bool mountedCarrierSelected=selectedActors.Length==1&&selectedActors[0]==publicMounted.Carrier;
                if(row==null||mountedCarrierSelected){row=publicMounted;existing=true;SkirmishPublicBoardingPresentation.TryReadProgress(em,session,row,out pending,out mounted);}
            }
            bool carrierSelected=row!=null&&selectedActors.Length==1&&selectedActors[0]==row.Carrier;
            bool groupSelected=row!=null&&selectedActors.Length>0;
            if(groupSelected)foreach(var actor in selectedActors)
                if(!em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor)||em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=row.GroupId){groupSelected=false;break;}
            bool haveCandidate=SkirmishPublicBoardingPresentation.TryCandidate(em,session,out var candidate);
            if(!haveCandidate&&row==null)return false;
            Entity carrier=haveCandidate?candidate.Carrier:row.Carrier;
            model=new UiPublicBoardingModel{Visible=true,Eligible=haveCandidate,Attempt=attempt.SessionId.ToString(),World=em.WorldUnmanaged.SequenceNumber,
                SetupHash=attempt.SetupHash,Carrier=new UiEntityHandle(carrier.Index,carrier.Version),CarrierId=haveCandidate?candidate.CarrierId:row.CarrierId.ToString(),
                GroupId=haveCandidate?candidate.GroupId:row.GroupId,CarrierGroupId=haveCandidate?candidate.CarrierGroupId:row.CarrierGroupId,
                Count=haveCandidate?candidate.Actors.Length:row.Actors.Length,Seats=haveCandidate?candidate.Seats:0,CarrierWorld=em.GetComponentData<LocalTransform>(carrier).Position,
                CarrierSelected=carrierSelected,AlreadyMounted=existing,MoveActive=em.HasComponent<ManualMoveOrderTag>(carrier)};
            if(row!=null&&row.Carrier==carrier&&row.GroupId==model.GroupId)
            {model.Accepted=row.Accepted;model.Presented=row.Presented;model.Pending=pending;model.Mounted=mounted;model.AllMounted=mounted==row.Actors.Length;
                model.RequestId=row.RequestId;model.Frame=row.Frame;model.Screen=row.Screen;}
            if(TryGetBoundary(out var uiEm,out var boundary)&&uiEm.HasComponent<AriaPlaySessionComponent>(boundary))
            {var touch=uiEm.GetComponentData<AriaPlaySessionComponent>(boundary);model.CompletedTarget=touch.LastCompletedTargetId;model.CompletedFrame=touch.LastCompletedFrame;model.CompletedScreen=touch.LastCompletedPosition;}
            using var grids=em.CreateEntityQuery(typeof(GridConfig));
            if(grids.CalculateEntityCount()==1&&em.HasComponent<UnitGrid>(carrier)&&em.HasComponent<UnitFootprint>(carrier))
            {
                var grid=grids.GetSingleton<GridConfig>();var size=em.GetComponentData<UnitFootprint>(carrier).Size;
                var cell=em.GetComponentData<UnitGrid>(carrier).Cell;
                UnityEngine.Vector2 World(int2 p)=>new UnityEngine.Vector2(grid.Origin.x+p.x*grid.CellSize,grid.Origin.z+p.y*grid.CellSize);
                if(SkirmishBoardingFootprintProjection.TryRead(em,carrier,grid,out var footprint))model.CarrierFootprint=footprint;
                if(em.HasComponent<UnitTarget>(carrier))
                {int2 goal=em.HasComponent<UnitLongDistanceMove>(carrier)?em.GetComponentData<UnitLongDistanceMove>(carrier).FinalGoal:em.GetComponentData<UnitTarget>(carrier).Cell;if(SkirmishBoardingFootprintProjection.TryReadAt(em,carrier,grid,goal,out var goals))model.MoveGoals=goals;}
            }
            if(row!=null&&carrierSelected&&em.HasComponent<SkirmishPublicOrderFeedbackComponent>(session))
            {
                var move=em.GetComponentData<SkirmishPublicOrderFeedbackComponent>(session);
                if(move.GroupId==model.CarrierGroupId&&move.Kind==RtsSelectionCommandIntentKind.Move)
                {model.MovePresented=move.Presented!=0;model.MoveAccepted=move.Accepted!=0&&move.HasWorldPosition!=0;model.MoveRevision=move.Revision;model.MoveFrame=move.Frame;model.MoveScreen=move.ScreenPosition;model.MoveWorld=move.WorldPosition;}
            }
            bool fa=GameLocalization.IsRightToLeft;
            model.Status=model.AllMounted?(fa?"سوار شدن انجام شد · ":"BOARDED · ")+model.Mounted+" / "+model.Count:
                model.Accepted?(fa?"در حال سوار شدن · ":"BOARDING · ")+model.Mounted+" / "+model.Count:
                (fa?"سوار شدن · صندلی خالی ":"BOARD · AVAILABLE SEATS ")+model.Seats;
            return true;
        }
    }
    public static class SkirmishBoardingFootprintProjection
    {
        public static bool TryRead(EntityManager em,Entity carrier,GridConfig grid,out UnityEngine.Vector2[] corners)
        {
            corners=null;
            return em.Exists(carrier)&&em.HasComponent<UnitGrid>(carrier)&&
                TryReadAt(em,carrier,grid,em.GetComponentData<UnitGrid>(carrier).Cell,out corners);
        }
        public static bool TryReadAt(EntityManager em,Entity carrier,GridConfig grid,int2 center,out UnityEngine.Vector2[] corners)
        {
            corners=null;
            if(!em.Exists(carrier)||!em.HasComponent<UnitGrid>(carrier)||!em.HasComponent<UnitFootprint>(carrier)||
                !math.isfinite(grid.CellSize)||grid.CellSize<=0||!math.all(math.isfinite(grid.Origin)))return false;
            var size=em.GetComponentData<UnitFootprint>(carrier).Size;
            if(math.any(size<=0))return false;
            var min=UnitFootprintUtility.GetMinCell(center,size);
            UnityEngine.Vector2 World(int2 p)=>new UnityEngine.Vector2(grid.Origin.x+p.x*grid.CellSize,grid.Origin.z+p.y*grid.CellSize);
            corners=new[]{World(min),World(min+new int2(size.x,0)),World(min+size),World(min+new int2(0,size.y))};
            foreach(var point in corners)if(!float.IsFinite(point.x)||!float.IsFinite(point.y)){corners=null;return false;}
            return true;
        }
    }

}
