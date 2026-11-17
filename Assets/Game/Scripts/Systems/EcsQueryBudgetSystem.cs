using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>
    /// Widens the query block allocator when the game World is created (see <see cref="EcsQueryBudget"/>) and logs
    /// <c>[EcsQueryBudget]</c> once a minute, plus a toggle-dimension census when 50/75/90 % of the budget is crossed.
    /// Diagnostics only: no gameplay state is read or written.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class EcsQueryBudgetSystem : SystemBase
    {
        private const double LogIntervalSeconds = 60d;
        private const int CensusTop = 24;
        private double _nextLog;
        private double _created;
        private int _reportedThreshold;

        protected override void OnCreate()
        {
            _created = UnityEngine.Time.realtimeSinceStartupAsDouble;
            _nextLog = _created + LogIntervalSeconds;
            bool widened = EcsQueryBudget.TryEnsureHeadroom(World, EcsQueryBudget.HeadroomBudgetBytes, out string detail);
            Debug.Log("[EcsQueryBudget] world=" + World.Name + " headroom=" + (widened ? 1 : 0) + " detail=" + detail);
        }

        protected override void OnUpdate()
        {
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            if (now < _nextLog) return;
            _nextLog = now + LogIntervalSeconds;
            LogNow(now - _created);
        }

        /// <summary>Logs the current snapshot; also used by validation harnesses at match end.</summary>
        public static EcsQueryBudget.Snapshot Log(World world, double elapsed, bool census)
        {
            if (!EcsQueryBudget.TryRead(world, out var snapshot)) return snapshot;
            Debug.Log(EcsQueryBudgetMath.FormatSnapshot(world.Name, snapshot.Archetypes, snapshot.QueryShapes,
                snapshot.UsedBlocks, snapshot.BudgetBlocks, snapshot.HeadroomApplied, elapsed));
            if (census) Debug.Log("[EcsQueryBudget] toggles=" + EcsQueryBudget.ToggleReport(world, CensusTop));
            return snapshot;
        }

        private void LogNow(double elapsed)
        {
            if (!EcsQueryBudget.TryRead(World, out var snapshot)) return;
            int threshold = snapshot.Valid ? EcsQueryBudgetMath.Threshold(snapshot.UsedBlocks, snapshot.BudgetBlocks) : 0;
            bool census = threshold > _reportedThreshold;
            if (census) _reportedThreshold = threshold;
            string line = EcsQueryBudgetMath.FormatSnapshot(World.Name, snapshot.Archetypes, snapshot.QueryShapes,
                snapshot.UsedBlocks, snapshot.BudgetBlocks, snapshot.HeadroomApplied, elapsed);
            if (threshold >= 75) Debug.LogWarning(line); else Debug.Log(line);
            if (census) Debug.Log("[EcsQueryBudget] toggles=" + EcsQueryBudget.ToggleReport(World, CensusTop));
        }
    }
}
