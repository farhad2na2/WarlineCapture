using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiLastCorridorModel
    {
        public readonly int Stage,DeliveredCategories,HoldSeconds,RemainingSeconds,HostilesDefeated,RouteStep;
        public readonly bool LinkRecovered,MedicineDelivered,FuelDelivered,ReinforcementsDelivered,EngineerAboard,EngineerDelivered,KeysDelivered;
        public readonly Vector3 RepairGate,MedicineGate,FuelGate,ReinforcementGate,KeyReceiver;
        public UiLastCorridorModel(int stage,int delivered,int hold,int remaining,int defeated,int route,bool repaired,bool medicine,bool fuel,bool reinforcements,bool aboard,bool engineer,bool keys,Vector3 repair,Vector3 medical,Vector3 receivingFuel,Vector3 troops,Vector3 keyReceiver)
        {Stage=stage;HostilesDefeated=defeated;RouteStep=route;DeliveredCategories=delivered;HoldSeconds=hold;RemainingSeconds=remaining;LinkRecovered=repaired;MedicineDelivered=medicine;FuelDelivered=fuel;ReinforcementsDelivered=reinforcements;EngineerAboard=aboard;EngineerDelivered=engineer;KeysDelivered=keys;RepairGate=repair;MedicineGate=medical;FuelGate=receivingFuel;ReinforcementGate=troops;KeyReceiver=keyReceiver;}
    }
    public readonly struct UiLastCorridorResultModel
    {
        public readonly bool LinkRecovered,SuppliesDelivered,KeysDelivered;
        public UiLastCorridorResultModel(bool link,bool supplies,bool keys){LinkRecovered=link;SuppliesDelivered=supplies;KeysDelivered=keys;}
    }
    public interface IUiLastCorridorGateway {bool TryReadLastCorridor(out UiLastCorridorModel model);}
    public interface IUiLastCorridorResultGateway {bool TryReadLastCorridorResult(out UiLastCorridorResultModel model);}
    public static class UiLastCorridorProgress
    {
        // ARIA's bounded watchdog records GoalId modulo 64. Each real milestone
        // needs a distinct slot; multiplying a stage by 64 aliases every stage.
        public static int WatchGoal(int stage, int hostiles, int route, bool aboard) => 7100 + (stage switch
        {
            1 => System.Math.Clamp(hostiles, 0, 7), 2 => 8,
            3 => 9 + System.Math.Clamp(route, 0, 3), 4 => 13 + System.Math.Clamp(route, 0, 3),
            5 => 17, 6 => 18, 7 => 19 + (aboard ? 1 : 0),
            8 => 21 + System.Math.Clamp(route, 0, 3), _ => 25
        });
    }
}
