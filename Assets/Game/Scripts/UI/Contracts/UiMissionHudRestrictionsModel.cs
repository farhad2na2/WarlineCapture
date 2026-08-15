namespace Game.UI.Contracts
{
    public readonly struct UiMissionHudRestrictionsModel
    {
        public static readonly UiMissionHudRestrictionsModel Inactive = default;

        public UiMissionHudRestrictionsModel(
            string missionId,
            bool buildingDisabled,
            bool productionDisabled,
            bool economyDisabled,
            bool transportDisabled,
            bool airDisabled)
        {
            IsActive = true;
            MissionId = missionId ?? string.Empty;
            BuildingDisabled = buildingDisabled;
            ProductionDisabled = productionDisabled;
            EconomyDisabled = economyDisabled;
            TransportDisabled = transportDisabled;
            AirDisabled = airDisabled;
        }

        public bool IsActive { get; }
        public string MissionId { get; }
        public bool BuildingDisabled { get; }
        public bool ProductionDisabled { get; }
        public bool EconomyDisabled { get; }
        public bool TransportDisabled { get; }
        public bool AirDisabled { get; }
    }

    public interface IUiMissionHudRestrictionsGateway
    {
        bool TryReadMissionHudRestrictions(out UiMissionHudRestrictionsModel restrictions);
    }
}
