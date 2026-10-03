using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionDelayedWaveSystem
    {
        private static readonly FixedString64Bytes CitywideDelayedMissionId = Game.Missions.Contracts.CampaignMissionSequence.CitywideAlert;
        private void AdvanceDefenseElements(ref SystemState state, Entity root,
            in CampaignMissionRuntimeComponent runtime, ref CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition)
        {
            EntityManager em = state.EntityManager;
            if (runtime.Phase != MissionPhaseKind.Engage ||
                !em.HasBuffer<CampaignMissionConvoyElementState>(root) ||
                !em.HasBuffer<CampaignMissionDefenseMember>(root)) return;
            DynamicBuffer<CampaignMissionConvoyElementState> elements = em.GetBuffer<CampaignMissionConvoyElementState>(root);
            DynamicBuffer<CampaignMissionDefenseMember> members = em.GetBuffer<CampaignMissionDefenseMember>(root, true);
            if (elements.Length != definition.Defense.Elements.Length) return;
            EntityCommandBuffer commands = new(Allocator.Temp);
            bool allActivated = true;
            for (int i = 0; i < elements.Length; i++)
            {
                CampaignMissionConvoyElementState element = elements[i];
                ref CampaignMissionConvoyElementBlob authored = ref definition.Defense.Elements[i];
                if (element.WarningIssued == 0 && facts.ElapsedMilliseconds >= authored.WarningAtMilliseconds)
                {
                    em.GetBuffer<ThreatWarningObservation>(root).Add(new ThreatWarningObservation
                    {
                        SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal,
                        SourceVersion = runtime.SourceVersion, ElementIndex = i,
                        Source = ThreatWarningSourceKind.ScoutReport, KnownVehicleCount = -1,
                        ObservedAtMilliseconds = facts.ElapsedMilliseconds
                    });
                    element.WarningIssued = 1;
                    facts.DefenseWaveWarningIssued = 1;
                }
                if (element.Activated == 0 && element.WarningIssued != 0 &&
                    facts.ElapsedMilliseconds >= authored.ActivationAtMilliseconds)
                {
                    bool ready = true;
                    if(runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak)) &&
                        authored.UnitGroupId.Equals(new FixedString64Bytes("group.ch04.m05.hostile_armor")))
                        ready=em.HasComponent<CampaignMissionArmorBreakState>(root) &&
                            em.GetComponentData<CampaignMissionArmorBreakState>(root).BatteryDisabled!=0 &&
                            em.GetComponentData<CampaignMissionArmorBreakState>(root).LauncherShots>0;
                    if(runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak)))
                    {
                        if(!em.HasComponent<CampaignMissionArmorBreakState>(root))ready=false;
                        else
                        {
                            var armorBreak=em.GetComponentData<CampaignMissionArmorBreakState>(root);
                            if(authored.UnitGroupId.Equals(new FixedString64Bytes("group.ch04.m05.air")))ready&=armorBreak.CoverageReady!=0;
                            if(authored.UnitGroupId.Equals(new FixedString64Bytes("group.ch04.m05.battery")))ready&=armorBreak.AirCleared!=0;
                            if(authored.UnitGroupId.Equals(new FixedString64Bytes("group.ch04.m05.command")))ready&=armorBreak.ArmorApproached!=0;
                        }
                    }
                    if (runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.CommandNode)))
                    {
                        ready &= em.HasComponent<CampaignMissionCommandNodeState>(root);
                        if (ready)
                        {
                            var command = em.GetComponentData<CampaignMissionCommandNodeState>(root);
                            ready = CampaignMissionCommandNodeRuleUtility.Matches(in command, in runtime) && command.Ready != 0;
                            if (authored.UnitGroupId.Equals(new FixedString64Bytes("group.ch05.m05.node"))) ready &= command.ClinicIsolated != 0 && command.UtilityIsolated != 0;
                            if (authored.UnitGroupId.Equals(new FixedString64Bytes("group.ch05.m05.core"))) ready &= command.Breached != 0;
                        }
                    }
                    if (runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.LastCorridor)))
                        ready &= em.HasComponent<CampaignMissionLastCorridorState>(root) && em.GetComponentData<CampaignMissionLastCorridorState>(root).Ready != 0;
                    if (runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.NetworkCollapse)))
                        ready &= em.HasComponent<CampaignMissionNetworkCollapseState>(root) && em.GetComponentData<CampaignMissionNetworkCollapseState>(root).Ready != 0;
                    if (runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.TrustUnderFire)))
                        ready &= em.HasComponent<CampaignMissionTrustUnderFireState>(root) && em.GetComponentData<CampaignMissionTrustUnderFireState>(root).Ready != 0;
                    if (runtime.MissionId.Equals(CitywideDelayedMissionId))
                    {
                        ready &= em.HasComponent<CampaignMissionCitywideAlertState>(root);
                        if (ready)
                        {
                            var citywide=em.GetComponentData<CampaignMissionCitywideAlertState>(root);
                            ready=CampaignMissionCitywideAlertRuleUtility.Matches(in citywide,in runtime) && citywide.Ready!=0 &&
                                citywide.CoverageReady!=0 && citywide.ReinforcementProduced!=0 &&
                                facts.ElapsedMilliseconds >= authored.WarningAtMilliseconds + CampaignMissionCitywideAlertRuleUtility.WarningLeadMilliseconds;
                        }
                    }
                    for (int j = 0; j < members.Length; j++)
                        if (members[j].ElementIndex == i &&
                            (!em.Exists(members[j].Entity) || !em.HasComponent<UnitHealth>(members[j].Entity) ||
                             em.GetComponentData<UnitHealth>(members[j].Entity).Max <= 0)) ready = false;
                    if (ready)
                    {
                        for (int j = 0; j < members.Length; j++)
                        {
                            if (members[j].ElementIndex != i) continue;
                            Entity entity = members[j].Entity;
                            if (em.HasComponent<UnitCombat>(entity))
                            {
                                UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                                combat.AutoEngage = combat.CanAttack;
                                commands.SetComponent(entity, combat);
                            }
                            commands.RemoveComponent<CampaignMissionCombatSuppressedTag>(entity);
                            bool fixedBattery = runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.CommandNode)) || runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.LastCorridor)) || runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.NetworkCollapse)) || runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.TrustUnderFire)) || em.HasComponent<CampaignMissionUnitRoleComponent>(entity) &&
                                (runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.SplitFront)) &&
                                em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(new FixedString64Bytes("role.hostile.battery")) ||
                                runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak)) &&
                                (em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(new FixedString64Bytes("role.hostile.battery")) ||
                                 em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(new FixedString64Bytes("role.hostile.command")) ||
                                 em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(new FixedString64Bytes("role.hostile.armor")) &&
                                 em.HasComponent<CampaignMissionArmorBreakState>(root) && em.GetComponentData<CampaignMissionArmorBreakState>(root).ArmoredThreatsCleared==0));
                            if (!fixedBattery) commands.RemoveComponent<CampaignMissionStationaryUnitTag>(entity);
                        }
                        element.Activated = 1;
                    }
                }
                bool defeated = element.Activated != 0;
                for (int j = 0; j < members.Length; j++)
                    if (members[j].ElementIndex == i && members[j].Defeated == 0) defeated = false;
                element.Resolved = defeated ? (byte)1 : (byte)0;
                elements[i] = element;
                allActivated &= element.Activated != 0;
            }
            facts.DefenseWaveActivated = allActivated ? (byte)1 : (byte)0;
            em.SetComponentData(root, facts);
            commands.Playback(em);
            commands.Dispose();
        }
    }
}
