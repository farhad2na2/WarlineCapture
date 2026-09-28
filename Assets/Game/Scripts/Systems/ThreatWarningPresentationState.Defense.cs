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
            bool steelPush=runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.SteelPush));
            bool airCorridor=runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.AirCorridor));
            string text;
            if (preparing)
                text = steelPush?GameText.Get("mission.steel_push.warning.prepare"):airCorridor?GameText.Get("mission.air_corridor.warning.prepare"):GameText.Get("mission.m03.prepare.short") + "\n" + GameText.Get("mission.m03.prepare.progress");
            else
            {
                int next = 0;
                while (next < elements.Length && elements[next].Resolved != 0) next++;
                if (next >= elements.Length)
                    text = GameText.Get(steelPush?"mission.steel_push.defense.complete":airCorridor?"mission.air_corridor.defense.complete":"mission.m03.defense.complete");
                else
                {
                    int seconds = math.max(0, (definition.Defense.Elements[next].ContactAtMilliseconds - facts.ElapsedMilliseconds + 999) / 1000);
                    string phase = GameText.Format(steelPush?"mission.steel_push.defense.wave":airCorridor?"mission.air_corridor.defense.wave":"mission.m03.defense.wave", "Wave {0}/{1}", next + 1, elements.Length);
                    if (seconds > 0)
                        text = phase + "\n" + GameText.Format(steelPush?"mission.steel_push.defense.countdown":airCorridor?"mission.air_corridor.defense.countdown":"mission.m03.defense.countdown", "Hold · arrival in {0}", $"{seconds / 60:00}:{seconds % 60:00}");
                    else
                    {
                        int alive = 0;
                        for (int i = 0; i < members.Length; i++)
                            if (members[i].ElementIndex == next && members[i].Defeated == 0) alive++;
                        text = phase + "\n" + GameText.Format(steelPush?"mission.steel_push.defense.fighting":airCorridor?"mission.air_corridor.defense.fighting":"mission.m03.defense.fighting", "Defend · {0} targets left", alive);
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
