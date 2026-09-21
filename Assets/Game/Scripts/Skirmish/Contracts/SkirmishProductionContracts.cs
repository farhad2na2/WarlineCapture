namespace Game.Skirmish.Contracts
{
    public enum SkirmishReservationPhase : byte
    {
        None = 0,
        Reserved = 1,
        Producing = 2,
        Live = 3,
        Released = 4,
        Cancelled = 5,
        Lost = 6
    }

    public struct SkirmishProductionRequest
    {
        public string RoleId;
        public SkirmishRoleKind RoleKind;
        public SkirmishProducerKind RequestedProducer;
        public int SquadCount;
        public bool GroundStagingPresent;
        public bool BarracksPresent;
        public bool EnforceStocks;
        public int MaterialsAvailable;
        public int FuelAvailable;
        public int InfantryLive;
        public int GroundLive;
        public int AirLive;
        public int SupplyLive;
        public int InfantryReserved;
        public int GroundReserved;
        public int AirReserved;
        public int SupplyReserved;
        public int InfantryCap;
        public int GroundCap;
        public int AirCap;
        public int SupplyCap;
    }

    public struct SkirmishProductionDecision
    {
        public bool Accepted;
        public SkirmishReasonCode Reason;
        public string Field;
        public int MemberCount;
        public SkirmishProducerKind Producer;
        public int SupplyCost;
        public int MaterialsCost;
        public int FuelCost;
        public int RefundedMaterials;
        public uint ReservationId;
    }
}
