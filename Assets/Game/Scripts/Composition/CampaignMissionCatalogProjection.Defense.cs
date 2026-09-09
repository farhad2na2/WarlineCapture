using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static bool MatchesDefense(ref CampaignMissionDefenseDefinitionBlob projected, ScenarioSetupConfig scenario)
        {
            MissionDefenseDefinitionConfig source = scenario.Defense;
            if (projected.Enabled != Flag(source.Enabled)) return false;
            if (!source.Enabled) return true;
            if (!MatchesCameraTour(projected.CameraTour,source.CameraTour)) return false;
            if (projected.VehiclesSelfSupplied != Flag(source.VehiclesSelfSupplied)) return false;
            if (projected.AuthoredMapDefensesDormant != Flag(source.AuthoredMapDefensesDormant)) return false;
            if (!projected.ForwardPostStableId.Equals(new FixedString128Bytes(source.ForwardPostStableId)) ||
                !Matches(projected.InnerCoreAnchorId, source.InnerCoreAnchorId) ||
                projected.InnerCoreRadius != source.InnerCoreRadius ||
                !Matches(projected.SensorMissionRoleId, source.SensorMissionRoleId) ||
                !Matches(projected.InitialProducerAnchorId, source.InitialProducerAnchorId) ||
                !projected.InitialProducerConfigId.Equals(new FixedString128Bytes(scenario.MissionRuntime.RequiredProducerConfigId)) ||
                projected.RadarPingCharges != source.RadarPingCharges ||
                projected.RadarPingCooldownMilliseconds != source.RadarPingCooldownMilliseconds ||
                projected.Elements.Length != source.ConvoyElements.Length || projected.GuidanceSteps.Length != source.GuidanceSteps.Length) return false;
            for(int i=0;i<projected.GuidanceSteps.Length;i++)
            {
                ref var value=ref projected.GuidanceSteps[i]; var item=source.GuidanceSteps[i];
                if(!Matches(value.StepId,item.StepId) || !Matches(value.TitleKey,item.TitleKey) || !Matches(value.BodyKey,item.BodyKey) ||
                    value.Action!=item.Action || value.Completion!=item.Completion || value.Optional!=Flag(item.Optional)) return false;
            }
            for (int i = 0; i < projected.Elements.Length; i++)
            {
                ref CampaignMissionConvoyElementBlob value = ref projected.Elements[i];
                MissionConvoyElementConfig item = source.ConvoyElements[i];
                if (!Matches(value.ElementId, item.ElementId) || !Matches(value.UnitGroupId, item.UnitGroupId) ||
                    !Matches(value.RouteId, item.RouteId) || !Matches(value.ContactAnchorId, item.ContactAnchorId) ||
                    value.WarningAtMilliseconds != item.WarningAtMilliseconds ||
                    value.ActivationAtMilliseconds != item.ActivationAtMilliseconds ||
                    value.ContactAtMilliseconds != item.ContactAtMilliseconds) return false;
            }
            return true;
        }

        private static void ProjectDefense(ref BlobBuilder builder, ref CampaignMissionDefinitionBlob definition,
            ScenarioSetupConfig scenario)
        {
            MissionDefenseDefinitionConfig source = scenario.Defense;
            definition.Defense.Enabled = source.Enabled ? (byte)1 : (byte)0;
            if (!source.Enabled) return;
            definition.Defense.CameraTour=ProjectCameraTour(source.CameraTour);
            definition.Defense.VehiclesSelfSupplied = Flag(source.VehiclesSelfSupplied);
            definition.Defense.AuthoredMapDefensesDormant = Flag(source.AuthoredMapDefensesDormant);
            definition.Defense.ForwardPostStableId = new FixedString128Bytes(source.ForwardPostStableId);
            definition.Defense.InnerCoreAnchorId = new FixedString64Bytes(source.InnerCoreAnchorId);
            definition.Defense.InnerCoreRadius = source.InnerCoreRadius;
            definition.Defense.SensorMissionRoleId = new FixedString64Bytes(source.SensorMissionRoleId);
            definition.Defense.InitialProducerAnchorId = new FixedString64Bytes(source.InitialProducerAnchorId);
            definition.Defense.InitialProducerConfigId = new FixedString128Bytes(scenario.MissionRuntime.RequiredProducerConfigId);
            definition.Defense.RadarPingCharges = source.RadarPingCharges;
            definition.Defense.RadarPingCooldownMilliseconds = source.RadarPingCooldownMilliseconds;
            var steps=builder.Allocate(ref definition.Defense.GuidanceSteps,source.GuidanceSteps.Length);
            for(int i=0;i<steps.Length;i++)
            {
                var item=source.GuidanceSteps[i];
                steps[i]=new CampaignMissionGuidanceStepBlob {StepId=new FixedString64Bytes(item.StepId),
                    TitleKey=new FixedString64Bytes(item.TitleKey),BodyKey=new FixedString64Bytes(item.BodyKey),
                    Action=item.Action,Completion=item.Completion,Optional=Flag(item.Optional)};
            }
            BlobBuilderArray<CampaignMissionConvoyElementBlob> elements =
                builder.Allocate(ref definition.Defense.Elements, source.ConvoyElements.Length);
            for (int i = 0; i < elements.Length; i++)
            {
                MissionConvoyElementConfig item = source.ConvoyElements[i];
                elements[i] = new CampaignMissionConvoyElementBlob
                {
                    ElementId = new FixedString64Bytes(item.ElementId),
                    UnitGroupId = new FixedString64Bytes(item.UnitGroupId),
                    RouteId = new FixedString64Bytes(item.RouteId),
                    ContactAnchorId = new FixedString64Bytes(item.ContactAnchorId),
                    WarningAtMilliseconds = item.WarningAtMilliseconds,
                    ActivationAtMilliseconds = item.ActivationAtMilliseconds,
                    ContactAtMilliseconds = item.ContactAtMilliseconds
                };
            }
        }
        private static MissionCameraTourBlob ProjectCameraTour(MissionCameraTourConfig source)=>new()
        {
            StartHoldMilliseconds=source.StartHoldMilliseconds,PostHoldMilliseconds=source.PostHoldMilliseconds,
            ApproachHoldMilliseconds=source.ApproachHoldMilliseconds,ReturnHoldMilliseconds=source.ReturnHoldMilliseconds,
            SmoothTimeSeconds=source.SmoothTimeSeconds,PostPerspective=source.PostPerspective,ApproachPerspective=source.ApproachPerspective
        };
        private static bool MatchesCameraTour(MissionCameraTourBlob value,MissionCameraTourConfig source)=>
            value.StartHoldMilliseconds==source.StartHoldMilliseconds && value.PostHoldMilliseconds==source.PostHoldMilliseconds &&
            value.ApproachHoldMilliseconds==source.ApproachHoldMilliseconds && value.ReturnHoldMilliseconds==source.ReturnHoldMilliseconds &&
            value.SmoothTimeSeconds==source.SmoothTimeSeconds && value.PostPerspective.Equals((Unity.Mathematics.float4)source.PostPerspective) &&
            value.ApproachPerspective.Equals((Unity.Mathematics.float4)source.ApproachPerspective);
    }
}
