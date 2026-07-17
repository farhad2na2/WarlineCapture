using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.UI.Contracts;

namespace Game.Runtime
{
    internal sealed class ThreatWarningPresentationState
    {
        private const float VisibleSeconds = 5f;
        private WorldScopedComponentQueryCache<ThreatWarningRuntimeStateComponent> _queryCache = new(readOnly: false);

        public void Present(World world, IMatchRuntimeUi matchUi, float now, bool simulationActive)
        {
            if (!simulationActive || world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = _queryCache.Get(entityManager);
            if (matchUi == null ||
                !ThreatWarningRuntimeState.TryRead(
                    entityManager,
                    query,
                    out ThreatWarningRuntimeStateComponent warningState) ||
                warningState.HasPendingWarning == 0)
            {
                return;
            }

            string title = BuildTitle(
                warningState.PendingType,
                warningState.PendingEtaSeconds,
                warningState.PendingThreatCount);
            if (matchUi.TryShowMatchHudThreatWarning(title, now + VisibleSeconds))
                ThreatWarningRuntimeState.ClearPendingWarning(entityManager, query);
        }

        public void Dispose()
        {
            _queryCache.Dispose();
            _queryCache = new WorldScopedComponentQueryCache<ThreatWarningRuntimeStateComponent>(readOnly: false);
        }

        private static string BuildTitle(ThreatWarningType type, float etaSeconds, int threatCount)
        {
            string key = type == ThreatWarningType.Air ? "warning_air_attack_type" : "warning_ground_attack_type";
            string fallback = type == ThreatWarningType.Air ? "Air attack detected" : "Ground vehicle attack detected";
            string title = GameStrings.Get(key);
            if (string.IsNullOrWhiteSpace(title) || title == key)
                title = fallback;

            int eta = Mathf.CeilToInt(Mathf.Max(0f, etaSeconds));
            if (eta > 0)
                title = GameStrings.Format("warning_attack_eta_suffix", "{0} - ETA {1}s", title, eta);
            if (threatCount > 1)
                title = GameStrings.Format("warning_attack_count_suffix", "{0} x{1}", title, threatCount);
            return title;
        }
    }
}
