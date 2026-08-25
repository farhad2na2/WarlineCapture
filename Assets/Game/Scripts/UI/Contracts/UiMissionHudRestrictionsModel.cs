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
            bool airDisabled,
            bool cinematicInteractionLocked = false,
            bool hideLogisticsResources = false,
            bool hideUnrelatedControls = false,
            bool showMissionCredits = false)
        {
            IsActive = true;
            MissionId = missionId ?? string.Empty;
            BuildingDisabled = buildingDisabled;
            ProductionDisabled = productionDisabled;
            EconomyDisabled = economyDisabled;
            TransportDisabled = transportDisabled;
            AirDisabled = airDisabled;
            CinematicInteractionLocked = cinematicInteractionLocked;
            HideLogisticsResources = hideLogisticsResources;
            HideUnrelatedControls = hideUnrelatedControls;
            ShowMissionCredits = showMissionCredits;
        }

        public bool IsActive { get; }
        public string MissionId { get; }
        public bool BuildingDisabled { get; }
        public bool ProductionDisabled { get; }
        public bool EconomyDisabled { get; }
        public bool TransportDisabled { get; }
        public bool AirDisabled { get; }
        public bool CinematicInteractionLocked { get; }
        public bool HideLogisticsResources { get; }
        public bool HideUnrelatedControls { get; }
        public bool ShowMissionCredits { get; }
    }

    public interface IUiMissionHudRestrictionsGateway
    {
        bool TryReadMissionHudRestrictions(out UiMissionHudRestrictionsModel restrictions);
    }
}
