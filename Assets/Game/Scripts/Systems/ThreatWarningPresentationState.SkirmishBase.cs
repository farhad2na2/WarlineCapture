using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed partial class ThreatWarningPresentationState
    {
        private const float SkirmishBaseThreatVisibleSeconds = 6f;
        private FixedString64Bytes _skirmishBaseSession;
        private int _skirmishBaseRevision = -1;

        // Skirmish Base Assault: the existing HUD threat strip names a base threat once per
        // SkirmishBaseThreatAlertState revision (hostile inside the perimeter, first hit).
        private void PresentSkirmishBaseThreat(EntityManager em, IMatchRuntimeUi matchUi, float now)
        {
            if (matchUi == null) return;
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishBaseThreatAlertState));
            if (query.CalculateEntityCount() != 1) return;
            Entity session = query.GetSingletonEntity();
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            var alert = em.GetComponentData<SkirmishBaseThreatAlertState>(session);
            if (!id.Equals(_skirmishBaseSession)) { _skirmishBaseSession = id; _skirmishBaseRevision = alert.Revision; return; }
            if (alert.Revision == _skirmishBaseRevision) return;
            _skirmishBaseRevision = alert.Revision;
            string title = alert.Reason == SkirmishBaseThreatReason.Perimeter
                ? GameText.Get("ui.skirmish.base_threat.perimeter", "ENEMY AT YOUR BASE")
                : GameText.Get("ui.skirmish.base_threat.damage", "YOUR BASE IS UNDER FIRE");
            matchUi.TryShowMatchHudThreatWarning(title, now + SkirmishBaseThreatVisibleSeconds);
        }
    }
}
