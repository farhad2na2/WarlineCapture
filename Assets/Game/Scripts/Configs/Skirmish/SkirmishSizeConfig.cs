using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Size")]
    public sealed class SkirmishSizeConfig : ScriptableObject
    {
        [SerializeField] private string sizeId = "Standard";
        [SerializeField] private SkirmishSizeId kind = SkirmishSizeId.Standard;
        [SerializeField] private int infantryCap = 48;
        [SerializeField] private int groundCap = 8;
        [SerializeField] private int tacticalAirCap = 2;
        [SerializeField] private int supplyCap = 128;
        [SerializeField] private int logisticsSupportCap = 6;
        [SerializeField] private int deliveryCarrierCap = 2;
        [SerializeField] private int playerBuiltStructureCap = 20;
        [SerializeField] private int barrierSegmentCap = 40;
        [SerializeField] private int contentVersion = 1;

        public string SizeId => sizeId;
        public SkirmishSizeId Kind => kind;
        public int InfantryCap => infantryCap;
        public int GroundCap => groundCap;
        public int TacticalAirCap => tacticalAirCap;
        public int SupplyCap => supplyCap;
        public int LogisticsSupportCap => logisticsSupportCap;
        public int DeliveryCarrierCap => deliveryCarrierCap;
        public int PlayerBuiltStructureCap => playerBuiltStructureCap;
        public int BarrierSegmentCap => barrierSegmentCap;
        public int ContentVersion => contentVersion;

        public void Configure(SkirmishSizeId configured)
        {
            kind = configured;
            switch (configured)
            {
                case SkirmishSizeId.War:
                    sizeId = "War";
                    infantryCap = 96;
                    groundCap = 12;
                    tacticalAirCap = 4;
                    supplyCap = 200;
                    logisticsSupportCap = 8;
                    deliveryCarrierCap = 2;
                    playerBuiltStructureCap = 30;
                    barrierSegmentCap = 60;
                    break;
                case SkirmishSizeId.LargeWar:
                    sizeId = "LargeWar";
                    infantryCap = 144;
                    groundCap = 20;
                    tacticalAirCap = 6;
                    supplyCap = 320;
                    logisticsSupportCap = 10;
                    deliveryCarrierCap = 2;
                    playerBuiltStructureCap = 40;
                    barrierSegmentCap = 80;
                    break;
                default:
                    sizeId = "Standard";
                    kind = SkirmishSizeId.Standard;
                    infantryCap = 48;
                    groundCap = 8;
                    tacticalAirCap = 2;
                    supplyCap = 128;
                    logisticsSupportCap = 6;
                    deliveryCarrierCap = 2;
                    playerBuiltStructureCap = 20;
                    barrierSegmentCap = 40;
                    break;
            }

            contentVersion = 1;
        }
    }
}
