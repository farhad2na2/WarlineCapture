using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Collections;

namespace Game.Runtime
{
    internal sealed partial class ThreatWarningPresentationState
    {
        private WorldScopedComponentQueryCache<CampaignMissionRootComponent> _defenseQuery = new(readOnly: true);
        private bool _defenseVisible;
        private string _defenseText, _defenseLocale;
        private ThreatWarningRecord _defenseDisplayRecord;
        private FixedString64Bytes _defenseSession;
        private int _defenseAttempt;
        private uint _defenseSource;

        private bool TryPresentDefense(EntityManager em, IMatchRuntimeUi matchUi, float now)
        {
            EntityQuery query = _defenseQuery.Get(em);
            if (query.CalculateEntityCount() != 1 || matchUi == null) return false;
            Entity root = query.GetSingletonEntity();
            if (!em.HasComponent<ThreatWarningLedgerState>(root)) return false;
            var ledger = em.GetComponentData<ThreatWarningLedgerState>(root);
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (!ThreatWarningResolveSystem.Matches(in ledger, in runtime)) return false;
            if (ledger.Active == 0)
            {
                if (_defenseVisible) matchUi.TryShowMatchHudThreatWarning(_defenseText, now);
                _defenseVisible = false;
                return true;
            }
            var records = em.GetBuffer<ThreatWarningRecord>(root, true);
            for (int i = 0; i < records.Length; i++)
            {
                ThreatWarningRecord record = records[i];
                if (record.ElementIndex != ledger.FocusElementIndex || record.Resolved != 0) continue;
                string locale = GameLocalization.CurrentLocaleCode;
                if (_defenseVisible && _defenseSession.Equals(runtime.SessionToken) && _defenseAttempt==runtime.AttemptOrdinal &&
                    _defenseSource==runtime.SourceVersion && SameDisplay(record,_defenseDisplayRecord) && locale == _defenseLocale) return true;
                _defenseText = ThreatWarningDisplayText.Build(in record);
                if (!matchUi.TryShowMatchHudThreatWarning(_defenseText, float.PositiveInfinity)) return true;
                _defenseDisplayRecord=record; _defenseSession=runtime.SessionToken; _defenseAttempt=runtime.AttemptOrdinal;
                _defenseSource=runtime.SourceVersion; _defenseLocale = locale; _defenseVisible = true;
                ledger.AcknowledgedPresentationVersion = ledger.PresentationVersion;
                em.SetComponentData(root, ledger);
                return true;
            }
            return true;
        }
        private static bool SameDisplay(in ThreatWarningRecord a,in ThreatWarningRecord b)=>a.EtaSeconds==b.EtaSeconds &&
            a.ElementIndex==b.ElementIndex && a.KnownVehicleCount==b.KnownVehicleCount && a.Source==b.Source && a.Stale==b.Stale &&
            a.ContactWindowOpen==b.ContactWindowOpen;
    }
}
