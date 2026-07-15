using Game.Components;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class BuildingConstructionResourceTransactionSystemHelper
    {
        private readonly RuntimeFactionResourceSystemHelper _factionResources;
        private int _activeTransactionId;
        private int _activeCreditsCost;
        private int _activeMaterialsCost;
        private int _highestSettledTransactionId;

        public BuildingConstructionResourceTransactionSystemHelper(
            RuntimeFactionResourceSystemHelper factionResources)
        {
            _factionResources = factionResources;
        }

        public void Reset()
        {
            _activeTransactionId = 0;
            _activeCreditsCost = 0;
            _activeMaterialsCost = 0;
            _highestSettledTransactionId = 0;
        }

        public FactionConstructionResourceMutationResult TryReserve(
            int transactionId,
            int creditsCost,
            int materialsCost)
        {
            if (_factionResources == null || transactionId <= 0)
                return FactionConstructionResourceMutationResult.InvalidState;
            if (transactionId <= _highestSettledTransactionId ||
                transactionId == _activeTransactionId)
                return FactionConstructionResourceMutationResult.DuplicateTransaction;
            if (_activeTransactionId != 0)
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                _factionResources.TrySpendConstructionResources(creditsCost, materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _activeTransactionId = transactionId;
            _activeCreditsCost = Mathf.Max(0, creditsCost);
            _activeMaterialsCost = Mathf.Max(0, materialsCost);
            return result;
        }

        public FactionConstructionResourceMutationResult TryFinalize(int transactionId)
        {
            if (transactionId <= 0 || transactionId != _activeTransactionId)
                return FactionConstructionResourceMutationResult.InvalidState;

            Settle(transactionId);
            return FactionConstructionResourceMutationResult.Applied;
        }

        public FactionConstructionResourceMutationResult TryRollback(int transactionId)
        {
            if (_factionResources == null ||
                transactionId <= 0 ||
                transactionId != _activeTransactionId)
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                _factionResources.TryRestoreConstructionResources(
                    _activeCreditsCost,
                    _activeMaterialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            Settle(transactionId);
            return result;
        }

        private void Settle(int transactionId)
        {
            _highestSettledTransactionId = transactionId;
            _activeTransactionId = 0;
            _activeCreditsCost = 0;
            _activeMaterialsCost = 0;
        }
    }
}
