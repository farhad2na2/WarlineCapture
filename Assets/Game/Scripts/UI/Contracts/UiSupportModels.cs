using UnityEngine;
namespace Game.UI.Contracts
{
    public enum UiSupportPhase : byte { Closed, Catalog, Targeting, Preview, Pending, Collecting }
    public readonly struct UiSupportAbilityModel
    {
        public readonly byte Kind; public readonly int Charges, FuelCost, Cooldown; public readonly bool Available;
        public readonly string ReasonKey;
        public UiSupportAbilityModel(byte kind,int charges,int fuelCost,int cooldown,bool available,string reasonKey)
        { Kind=kind;Charges=charges;FuelCost=fuelCost;Cooldown=cooldown;Available=available;ReasonKey=reasonKey; }
    }
    public readonly struct UiSupportModel
    {
        public readonly UiSupportPhase Phase; public readonly byte Selected; public readonly uint Version;
        public readonly string MissionId, ReasonKey,TargetName; public readonly float Fuel, Radius;
        public readonly Vector3 Target; public readonly bool Valid, HasProposal, Visible, Active;
        public readonly UiSupportAbilityModel Smoke, Strike, Paratroopers, Supply;
        public readonly byte LessonKind; public readonly string CollectionReasonKey; public readonly int SupplyStock; public readonly bool SupplyReady,SupplyClaimed,SupplyFull;
        public readonly byte ExecutionKind,ExecutionPhase; public readonly int ReservedFuel,ExecutionSeconds; public readonly string ExecutionReasonKey;
        public UiSupportModel(UiSupportPhase phase,byte selected,uint version,string mission,string reason,float fuel,float radius,
            Vector3 target,bool valid,bool proposal,UiSupportAbilityModel smoke,UiSupportAbilityModel strike,UiSupportAbilityModel paratroopers,UiSupportAbilityModel supply,bool visible=true,bool active=true,string targetName="",byte executionKind=0,byte executionPhase=0,int reservedFuel=0,int executionSeconds=0,string executionReason="",int supplyStock=0,bool supplyReady=false,bool supplyClaimed=false,bool supplyFull=false,string collectionReason="",byte lessonKind=0)
        { Phase=phase;Selected=selected;Version=version;MissionId=mission;ReasonKey=reason;Fuel=fuel;Radius=radius;Target=target;Valid=valid;HasProposal=proposal;Visible=visible;
          Smoke=smoke;Strike=strike;Paratroopers=paratroopers;Supply=supply;Active=active;TargetName=targetName;ExecutionKind=executionKind;ExecutionPhase=executionPhase;ReservedFuel=reservedFuel;ExecutionSeconds=executionSeconds;ExecutionReasonKey=executionReason;LessonKind=lessonKind;CollectionReasonKey=collectionReason;SupplyStock=supplyStock;SupplyReady=supplyReady;SupplyClaimed=supplyClaimed;SupplyFull=supplyFull; }
        public UiSupportAbilityModel Ability(byte kind) => kind switch { 1=>Smoke,2=>Strike,3=>Paratroopers,4=>Supply,_=>default };
    }
    public interface IUiSupportGateway
    {
        bool TryReadSupport(out UiSupportModel model);
        bool SelectSupport(byte kind);
        bool BeginSupportTargeting();
        bool BeginSupplyCollection();
        bool PreviewSupport(Vector3 position);
        bool PreviewSupportPointer(Vector2 screenPosition,Vector3 groundPosition);
        bool ConfirmSupport();
        void CancelSupport();
        bool ProposeSupport();
        bool ApproveSupport();
        void DeclineSupport();
        void ShowSupportTarget();
    }
}
