using Game.Components;namespace Game.Runtime {internal sealed partial class SourceCampResourceReads {
 internal bool IsBoundTo(Unity.Entities.EntityManager manager,Unity.Entities.Entity bank)=>ReferenceEquals(_entityManager,manager)&&_controlledFactionResourceEntity==bank&&manager.Exists(bank);
        internal SkirmishConstructionLedger.Point CaptureConstructionLedger(object owner)
        {
            var point=SkirmishConstructionLedger.Begin(_entityManager,_controlledFactionResourceEntity);
            if(point!=null)point.TransactionOwner=owner;
            return point;
        }
        public FactionConstructionResourceMutationResult TrySpendConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            if (!TryGetControlledFactionResources(
                    out FactionEconomy economy,
                    out FactionTacticalMaterialsComponent materials))
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                FactionConstructionResourceUtilitySystemHelper.TrySpend(
                    ref economy,
                    ref materials,
                    creditsCost,
                    materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
            if (materialsCost > 0)
                _entityManager.SetComponentData(_controlledFactionResourceEntity, materials);
            return result;
        }
        public FactionConstructionResourceMutationResult TryRestoreConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            if (!TryGetControlledFactionResources(
                    out FactionEconomy economy,
                    out FactionTacticalMaterialsComponent materials))
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                FactionConstructionResourceUtilitySystemHelper.TryRollback(
                    ref economy,
                    ref materials,
                    creditsCost,
                    materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
            if (materialsCost > 0)
                _entityManager.SetComponentData(_controlledFactionResourceEntity, materials);
            return result;
        }
}}
