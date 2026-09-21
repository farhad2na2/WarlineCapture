namespace Game.Skirmish.Contracts
{
    public struct SkirmishProductionRequest
    {
        public string RoleId;
        public SkirmishRoleKind RoleKind;
        public SkirmishProducerKind RequestedProducer;
        public int SquadCount;
        public bool GroundStagingPresent;
        public bool BarracksPresent;
    }

    public struct SkirmishProductionDecision
    {
        public bool Accepted;
        public SkirmishReasonCode Reason;
        public string Field;
        public int MemberCount;
        public SkirmishProducerKind Producer;
        public int SupplyCost;
    }
}
