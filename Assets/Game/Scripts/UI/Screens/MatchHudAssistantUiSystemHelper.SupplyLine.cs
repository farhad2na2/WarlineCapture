using UnityEngine;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private void ShowSupplyLineNextAction()
        {
            if(UiShellRuntimeGateway.TryReadSupplyLine(out _,out _,out bool canAllocate) && canAllocate)
            {
                var hud=Object.FindAnyObjectByType<MissionDefenseHudView>();
                if(hud!=null && hud.SupplyReserveButton!=null && hud.SupplyReserveButton.isActiveAndEnabled)
                {Cue(hud.SupplyReserveButton,"mission.supply_line.reserve.allocate");return;}
            }
            ShowGridlockNextAction();
        }
    }
}
