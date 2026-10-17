using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum VehicleRepairAction : byte { Start=1,Cancel=2 }
    public enum VehicleRepairPhase : byte { None=0,Working=1,Completed=2,Cancelled=3,Destroyed=4,AccountingFailure=5 }
    public struct VehicleRepairRequestComponent : IComponentData
    {
        public Entity Session;
        public byte Faction;
        public uint RequestId;
        public VehicleRepairAction Action;
        // Only normal displayed-price Start uses these pins. Pending requests are
        // not checkpointed and are still refused by native capture.
        public byte HasExpectedPlan;
        public FixedString64Bytes QuotedAttemptId,QuotedActorStableId;
        public uint QuotedSetupHash;
        public long ExpectedFractionNumerator,ExpectedFractionDenominator,ExpectedDurationTicks;
        public int ExpectedMaterials;
    }
    public struct VehicleRepairStateComponent : IComponentData
    {
        public Entity Session,ServiceFacility,MaterialsBank;
        public FixedString64Bytes AttemptId,ActorStableId,PadId,ServiceFacilityStableId;
        public byte Faction;
        public byte IncomingDamage,NewMovement;
        public VehicleRepairPhase Phase;
        public uint RequestId,SetupHash;
        public float3 AcceptedPosition,PadPosition,ServiceFacilityPosition;
        public long FractionNumerator,FractionDenominator,DurationTicks,RemainingTicks;
        public double LastClockSeconds,FractionalTicks;
        public int PaidMaterials,RefundDue,RefundedMaterials,LastHealth,LastMaximum;
    }
}
