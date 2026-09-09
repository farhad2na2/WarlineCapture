using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionDelayedWaveSystem
    {
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
                            commands.RemoveComponent<CampaignMissionStationaryUnitTag>(entity);
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
