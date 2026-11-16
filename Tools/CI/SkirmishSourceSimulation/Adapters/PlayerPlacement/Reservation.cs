using Game.Components;using UnityEngine;using Unity.Entities;namespace Game.Runtime {internal sealed class SourcePlayerConstructionReservation {
 internal bool IsActiveFor(Unity.Entities.EntityManager manager,Unity.Entities.Entity bank,int id)=>id>0&&_activeTransactionId==id&&_factionResources!=null&&_factionResources.IsBoundTo(manager,bank);
internal void ObservePlacement(SourceHostPlacementLifecycle.PlacementState placement,int id,
            SourceHostPlacementCommit.CommitOutcome? outcome=null)
        {
            if(SkirmishConstructionLedger.Observer==null)return;
            try
            {
                var before=_factionResources.CaptureConstructionLedger(this);
                Entity actor=outcome.HasValue&&outcome.Value.AutoSelectBuilding!=null?outcome.Value.AutoSelectBuilding.CombatEntity:Entity.Null;
                SkirmishConstructionLedger.End(before,outcome.HasValue?"PlayerPlacementOutcome":"PlayerPlacementRequest",id,
                    _activeCreditsCost,_activeMaterialsCost,placement.Definition?.Prefab!=null?placement.Definition.Prefab.name:null,
                    new Unity.Mathematics.int2(placement.OriginCell.x,placement.OriginCell.y),placement.AutoRotateVertical,
                    outcome.HasValue?outcome.Value.CommittedInstanceCount:0,outcome.HasValue?outcome.Value.ExpectedInstanceCount:0,actor);
            }
            catch(System.Exception error)when(!(error is System.OutOfMemoryException)){SkirmishConstructionLedger.UnsupportedPlacement();}
        }
private readonly SourceCampResourceReads _factionResources;private int _activeTransactionId;private int _activeCreditsCost;private int _activeMaterialsCost;private int _highestSettledTransactionId;internal SourcePlayerConstructionReservation(SourceCampResourceReads factionResources){_factionResources=factionResources;}
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

            var ledgerBefore=_factionResources.CaptureConstructionLedger(this);
            FactionConstructionResourceMutationResult result =
                _factionResources.TrySpendConstructionResources(creditsCost, materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _activeTransactionId = transactionId;
            _activeCreditsCost = Mathf.Max(0, creditsCost);
            _activeMaterialsCost = Mathf.Max(0, materialsCost);
            SkirmishConstructionLedger.End(ledgerBefore,"PlayerReserve",transactionId,creditsCost,materialsCost);
            return result;
        }
        public FactionConstructionResourceMutationResult TryFinalize(int transactionId)
        {
            if (transactionId <= 0 || transactionId != _activeTransactionId)
                return FactionConstructionResourceMutationResult.InvalidState;

            var ledgerBefore=_factionResources.CaptureConstructionLedger(this);
            int credits=_activeCreditsCost,materials=_activeMaterialsCost;
            Settle(transactionId);
            SkirmishConstructionLedger.End(ledgerBefore,"PlayerFinalize",transactionId,credits,materials);
            return FactionConstructionResourceMutationResult.Applied;
        }
        public FactionConstructionResourceMutationResult TryRollback(int transactionId)
        {
            if (_factionResources == null ||
                transactionId <= 0 ||
                transactionId != _activeTransactionId)
                return FactionConstructionResourceMutationResult.InvalidState;

            var ledgerBefore=_factionResources.CaptureConstructionLedger(this);
            int credits=_activeCreditsCost,materials=_activeMaterialsCost;
            FactionConstructionResourceMutationResult result =
                _factionResources.TryRestoreConstructionResources(
                    _activeCreditsCost,
                    _activeMaterialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
            {
                SkirmishConstructionLedger.End(ledgerBefore,"PlayerRollbackDeferred",transactionId,credits,materials);
                return result;
            }

            Settle(transactionId);
            SkirmishConstructionLedger.End(ledgerBefore,"PlayerRollback",transactionId,credits,materials);
            return result;
        }
        private void Settle(int transactionId)
        {
            _highestSettledTransactionId = transactionId;
            _activeTransactionId = 0;
            _activeCreditsCost = 0;
            _activeMaterialsCost = 0;
        }
}}
