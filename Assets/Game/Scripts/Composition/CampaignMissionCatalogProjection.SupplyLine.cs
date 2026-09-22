using Game.Components;
using Game.Configs;
using Unity.Collections;
namespace Game.Composition { internal static partial class CampaignMissionCatalogProjection {
 private static CampaignMissionSupplyLineDefinitionBlob ProjectSupplyLine(MissionSupplyLineDefinitionConfig s) => !s.Enabled ? default : new CampaignMissionSupplyLineDefinitionBlob { Enabled=1,OilAnchorId=new FixedString64Bytes(s.OilAnchorId),
RefineryAnchorId=new FixedString64Bytes(s.RefineryAnchorId),
StorageAnchorId=new FixedString64Bytes(s.StorageAnchorId),
AlternateLaneAnchorId=new FixedString64Bytes(s.AlternateLaneAnchorId),
OilBuildingId=new FixedString128Bytes(s.OilBuildingId),
RefineryBuildingId=new FixedString128Bytes(s.RefineryBuildingId),
StorageBuildingId=new FixedString128Bytes(s.StorageBuildingId),
ReserveBarrels=s.ReserveBarrels,
CivilianReserveBarrels=s.CivilianReserveBarrels,
HoldMilliseconds=s.HoldMilliseconds,
DeadlineMilliseconds=s.DeadlineMilliseconds };
 private static bool MatchesSupplyLine(CampaignMissionSupplyLineDefinitionBlob v,MissionSupplyLineDefinitionConfig s) => v.Enabled==Flag(s.Enabled) && (!s.Enabled || (v.AlternateLaneAnchorId.Equals(new FixedString64Bytes(s.AlternateLaneAnchorId)) && v.OilAnchorId.Equals(new FixedString64Bytes(s.OilAnchorId)) && v.RefineryAnchorId.Equals(new FixedString64Bytes(s.RefineryAnchorId)) && v.StorageAnchorId.Equals(new FixedString64Bytes(s.StorageAnchorId)) && v.OilBuildingId.Equals(new FixedString128Bytes(s.OilBuildingId)) && v.RefineryBuildingId.Equals(new FixedString128Bytes(s.RefineryBuildingId)) && v.StorageBuildingId.Equals(new FixedString128Bytes(s.StorageBuildingId)) && v.ReserveBarrels==s.ReserveBarrels && v.CivilianReserveBarrels==s.CivilianReserveBarrels && v.HoldMilliseconds==s.HoldMilliseconds && v.DeadlineMilliseconds==s.DeadlineMilliseconds));
} }
