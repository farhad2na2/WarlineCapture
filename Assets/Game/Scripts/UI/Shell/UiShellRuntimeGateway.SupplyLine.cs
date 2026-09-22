namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadSupplyLine(out int stored,out int reserve,out bool canAllocate)
        {
            stored=reserve=0;canAllocate=false;
            return current is Game.UI.Contracts.IUiSupplyLineGateway gateway && gateway.TryReadSupplyLine(out stored,out reserve,out canAllocate);
        }
        public static bool IsSupplyLineGuideContext()=>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId=="saga.ch02.m02.supply_line" ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId=="saga.ch02.m02.supply_line";
        public static bool TryAllocateSupplyLineReserve()=>current is Game.UI.Contracts.IUiSupplyLineGateway gateway && gateway.TryAllocateSupplyLineReserve();
    }
}
