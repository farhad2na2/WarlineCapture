using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Mathematics;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static (int Stage,int Recovery,int Fuel) cachedArmorBreakStatusStamp;
        private static (int Stage,int Recovery,int Fuel) ReadArmorBreakStatusStamp()
        {
            if(!TryArmorBreak(out var em,out var root,out var mission))return (-1,-1,-1);
            float fuel=0;
            if(em.Exists(mission.FuelReserve) && em.HasComponent<BuildingResourceStorageComponent>(mission.FuelReserve))
            {var s=em.GetComponentData<BuildingResourceStorageComponent>(mission.FuelReserve);fuel=math.max(0,s.StoredFuelBarrels-s.ReservedFuelOutboundBarrels-s.CivilianFuelReserveBarrels);}
            return (CampaignMissionArmorBreakRuleUtility.Stage(in mission),math.clamp(mission.RecoveryMilliseconds/1000,0,6),(int)math.floor(fuel));
        }
        private static string AppendArmorBreakStatus(string text)
        {
            var status=ReadArmorBreakStatusStamp();if(status.Stage<0)return text;
            if(status.Stage==7)text+="\n"+GameText.Format("mission.armor_break.hud.recovery","Authority recovery: {0}/6 s",status.Recovery);
            return text+"\n"+GameText.Format("mission.armor_break.hud.fuel","Military Fuel: {0} · civilian reserve protected",status.Fuel);
        }
    }
}
