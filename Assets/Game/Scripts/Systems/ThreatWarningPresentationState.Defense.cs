using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal sealed partial class ThreatWarningPresentationState
    {
        private WorldScopedComponentQueryCache<CampaignMissionRootComponent> _defenseQuery = new(readOnly: true);
        private bool _defenseVisible;
        private string _defenseText;
        private FixedString64Bytes _defenseSession;
        private int _defenseAttempt;

        private bool TryPresentDefense(EntityManager em, IMatchRuntimeUi matchUi, float now)
        {
            EntityQuery query = _defenseQuery.Get(em);
            if (query.CalculateEntityCount() != 1 || matchUi == null) return false;
            Entity root = query.GetSingletonEntity();
            if (!em.HasComponent<ThreatWarningLedgerState>(root)) return false;
            var ledger = em.GetComponentData<ThreatWarningLedgerState>(root);
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (!ThreatWarningResolveSystem.Matches(in ledger, in runtime)) return false;
            if (runtime.Outcome != MissionOutcomeKind.None)
            {
                if (_defenseVisible) matchUi.TryShowMatchHudThreatWarning(_defenseText, now);
                _defenseVisible = false;
                return true;
            }
            var catalog = em.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (!CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int index)) return false;
            ref var definition = ref catalog.Blob.Value.Missions[index];
            if (definition.Defense.Enabled == 0) return false;
            var defense = em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            var elements = em.GetBuffer<CampaignMissionConvoyElementState>(root, true);
            var members = em.GetBuffer<CampaignMissionDefenseMember>(root, true);
            bool preparing = (defense.AcknowledgedGuidanceMask & 0x1FFu) != 0x1FFu;
            bool splitFront=runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.SplitFront));
            bool steelPush=runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.SteelPush));
            bool airCorridor=runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.AirCorridor));
            bool armorBreak=runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak));
            string text;
            if(runtime.MissionId.Equals(CampaignMissionSequence.LastCorridor)&&em.HasComponent<CampaignMissionLastCorridorState>(root))
            {
                var corridor=em.GetComponentData<CampaignMissionLastCorridorState>(root);
                int stage=CampaignMissionLastCorridorRuleUtility.Stage(in corridor);
                int delivered=(corridor.MedicineDelivered!=0?1:0)+(corridor.FuelDelivered!=0?1:0)+(corridor.ReinforcementsDelivered!=0?1:0)+(corridor.EngineerDelivered!=0?1:0)+(corridor.KeysDelivered!=0?1:0);
                text=GameText.Get("mission.last_corridor.hud.stage."+stage)+"\n"+GameText.Format("mission.last_corridor.hud.delivered","Delivered: {0}/5",delivered);
                int hold=stage switch{2=>corridor.RepairMilliseconds,3=>corridor.MedicineMilliseconds,4=>corridor.FuelMilliseconds,5=>corridor.ReinforcementMilliseconds,8=>corridor.KeyMilliseconds,_=>0};
                if(stage is 2 or 3 or 4 or 5 or 8)text+="\n"+GameText.Format("mission.last_corridor.hud.hold","Hold position: {0}/6 s",hold/1000);
            }
            else if(runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse)&&em.HasComponent<CampaignMissionNetworkCollapseState>(root))
            {
                var network=em.GetComponentData<CampaignMissionNetworkCollapseState>(root);int stage=CampaignMissionNetworkCollapseRuleUtility.Stage(in network);text=GameText.Get("mission.network_collapse.hud.stage."+stage);
                if(stage is 1 or 3 or 5)text+="\n"+GameText.Format("mission.network_collapse.hud.recon_hold","Verify node {0}: {1}/6 s",(stage+1)/2,network.VerificationMilliseconds/1000);
                if(stage==7)text+="\n"+GameText.Format("mission.network_collapse.hud.audit_hold","Recover audit: {0}/6 s",network.RecoveryMilliseconds/1000);
                if(stage==8)text+="\n"+GameText.Format("mission.network_collapse.hud.extraction_hold","Evidence custody: {0}/6 s",network.ExtractionMilliseconds/1000);
                int remaining=math.max(0,(900000-network.ElapsedMilliseconds+999)/1000);text+="\n"+GameText.Format("mission.network_collapse.hud.time","Evidence window: {0}m {1}s",remaining/60,remaining%60);
            }
            else if(runtime.MissionId.Equals(CampaignMissionSequence.TrustUnderFire) && em.HasComponent<CampaignMissionTrustUnderFireState>(root))
            {
                var trust = em.GetComponentData<CampaignMissionTrustUnderFireState>(root);
                int stage = CampaignMissionTrustUnderFireRuleUtility.Stage(in trust);
                text = GameText.Get("mission.trust_under_fire.hud.stage." + stage);
                if (stage == 2) text += "\n" + GameText.Format("mission.trust_under_fire.hud.north_hold", "North shelter: {0}/6 s", trust.NorthHoldMilliseconds / 1000);
                if (stage == 4) text += "\n" + GameText.Format("mission.trust_under_fire.hud.south_hold", "South shelter: {0}/6 s", trust.SouthHoldMilliseconds / 1000);
                if (stage == 7) text += "\n" + GameText.Format("mission.trust_under_fire.hud.verification", "Verify broadcast: {0}/6 s", trust.VerificationMilliseconds / 1000);
                if (stage == 8) text += "\n" + GameText.Format("mission.trust_under_fire.hud.stable", "Protected routes: {0}/6 s", trust.StableMilliseconds / 1000);
                int seconds = math.max(0, (CampaignMissionTrustUnderFireRuleUtility.DeadlineMilliseconds - trust.ElapsedMilliseconds + 999) / 1000);
                text += "\n" + GameText.Format("mission.trust_under_fire.hud.time", "Evacuation window: {0}m {1}s", seconds / 60, seconds % 60);
            }
            else if(runtime.MissionId.Equals(CampaignMissionSequence.CitywideAlert) && em.HasComponent<CampaignMissionCitywideAlertState>(root))
            {
                var city=em.GetComponentData<CampaignMissionCitywideAlertState>(root);
                int stage=CampaignMissionCitywideAlertRuleUtility.Stage(in city);
                text=GameText.Get("mission.citywide_alert.hud.stage."+stage);
                if(city.ClinicObstructed!=0)text+="\n"+GameText.Format("mission.citywide_alert.hud.clinic_outage","Clinic service blocked · recover within {0}s",math.max(0,(60000-city.ClinicObstructionMilliseconds+999)/1000));
                if(city.UtilityObstructed!=0)text+="\n"+GameText.Format("mission.citywide_alert.hud.utility_outage","Utility service blocked · recover within {0}s",math.max(0,(60000-city.UtilityObstructionMilliseconds+999)/1000));
                int seconds=math.max(0,(CampaignMissionCitywideAlertRuleUtility.DeadlineMilliseconds-city.ElapsedMilliseconds+999)/1000);
                text+="\n"+GameText.Format("mission.citywide_alert.hud.time","Operation window: {0}",$"{seconds/60:00}:{seconds%60:00}");
            }
            else if (armorBreak && em.HasComponent<CampaignMissionArmorBreakState>(root))
            {
                var armor=em.GetComponentData<CampaignMissionArmorBreakState>(root);
                int stage=CampaignMissionArmorBreakRuleUtility.Stage(in armor);
                text=GameText.Get("mission.armor_break.hud.stage."+stage);
                int remaining=math.max(0,(CampaignMissionArmorBreakRuleUtility.DeadlineMilliseconds-armor.ElapsedMilliseconds+999)/1000);
                string window=$"{remaining/60:00}:{remaining%60:00}";
                if(stage==7)text+="\n"+GameText.Format("mission.armor_break.hud.hold_time","Hold: {0}/6 s · Time: {1}",armor.RecoveryMilliseconds/1000,window);
                else
                {
                    text+="\n"+GameText.Format("mission.armor_break.hud.time","Operation window: {0}",window);
                }
            }
            else if (preparing)
                text = splitFront?GameText.Get("mission.split_front.warning.prepare"):steelPush?GameText.Get("mission.steel_push.warning.prepare"):airCorridor?GameText.Get("mission.air_corridor.warning.prepare"):GameText.Get("mission.m03.prepare.short") + "\n" + GameText.Get("mission.m03.prepare.progress");
            else
            {
                int next = 0;
                while (next < elements.Length && elements[next].Resolved != 0) next++;
                if (next >= elements.Length)
                    text = GameText.Get(splitFront?"mission.split_front.defense.complete":steelPush?"mission.steel_push.defense.complete":airCorridor?"mission.air_corridor.defense.complete":"mission.m03.defense.complete");
                else
                {
                    int seconds = math.max(0, (definition.Defense.Elements[next].ContactAtMilliseconds - facts.ElapsedMilliseconds + 999) / 1000);
                    string phase = GameText.Format(splitFront?"mission.split_front.defense.wave":steelPush?"mission.steel_push.defense.wave":airCorridor?"mission.air_corridor.defense.wave":"mission.m03.defense.wave", "Wave {0}/{1}", next + 1, elements.Length);
                    if (seconds > 0)
                        text = phase + "\n" + GameText.Format(splitFront?"mission.split_front.defense.countdown":steelPush?"mission.steel_push.defense.countdown":airCorridor?"mission.air_corridor.defense.countdown":"mission.m03.defense.countdown", "Hold · arrival in {0}", $"{seconds / 60:00}:{seconds % 60:00}");
                    else
                    {
                        int alive = 0;
                        for (int i = 0; i < members.Length; i++)
                            if (members[i].ElementIndex == next && members[i].Defeated == 0) alive++;
                        text = phase + "\n" + GameText.Format(splitFront?"mission.split_front.defense.fighting":steelPush?"mission.steel_push.defense.fighting":airCorridor?"mission.air_corridor.defense.fighting":"mission.m03.defense.fighting", "Defend · {0} targets left", alive);
                    }
                }
            }
            // Keep the defense status present even between warning records. A resolved
            // scout alert is not the end of the mission or a reason to hide its timer.
            if (_defenseVisible && _defenseText == text && _defenseSession.Equals(runtime.SessionToken) &&
                _defenseAttempt == runtime.AttemptOrdinal) return true;
            if (!matchUi.TryShowMatchHudThreatWarning(text, float.PositiveInfinity)) return true;
            _defenseText = text;
            _defenseVisible = true;
            _defenseSession = runtime.SessionToken;
            _defenseAttempt = runtime.AttemptOrdinal;
            ledger.AcknowledgedPresentationVersion = ledger.PresentationVersion;
            em.SetComponentData(root, ledger);
            return true;
        }
    }
}
