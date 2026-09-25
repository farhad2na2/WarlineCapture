using System.Collections.Generic;
using Game.Configs;
using Unity.Collections;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingDefinitionPrefabSystemHelper
    {
        private Dictionary<GameObject, BuildingProductionRecipe> unitRecipes = new();
        private Dictionary<GameObject, List<BuildingDefinition.ProductionSlotDefinition>> producerRecipes = new();

        public void ConfigureProductionRecipes(IReadOnlyList<BuildingProductionRecipe> recipes)
        {
            var units = new Dictionary<GameObject, BuildingProductionRecipe>();
            var producers = new Dictionary<GameObject, List<BuildingDefinition.ProductionSlotDefinition>>();
            if (recipes != null)
                foreach (var recipe in recipes)
                {
                    if (recipe == null || recipe.ProducerPrefab == null || recipe.UnitPrefab == null ||
                        recipe.Quantity < 1 || recipe.MaterialsCost < 0 || recipe.CreditsCost < 0)
                        throw new System.ArgumentException("Invalid production recipe.");
                    if (units.TryGetValue(recipe.UnitPrefab, out var prior) &&
                        (prior.MaterialsCost != recipe.MaterialsCost || prior.CreditsCost != recipe.CreditsCost || prior.Quantity != recipe.Quantity))
                        throw new System.ArgumentException("Conflicting production recipe for " + recipe.UnitPrefab.name);
                    units[recipe.UnitPrefab] = new BuildingProductionRecipe {
                        ProducerPrefab = recipe.ProducerPrefab, UnitPrefab = recipe.UnitPrefab,
                        Quantity = recipe.Quantity, MaterialsCost = recipe.MaterialsCost, CreditsCost = recipe.CreditsCost
                    };
                    if (!producers.TryGetValue(recipe.ProducerPrefab, out var slots))
                        producers.Add(recipe.ProducerPrefab, slots = new List<BuildingDefinition.ProductionSlotDefinition>());
                    if (slots.Exists(slot => slot.SpawnUnitPrefab == recipe.UnitPrefab))
                        throw new System.ArgumentException("Duplicate production recipe for " + recipe.UnitPrefab.name);
                    slots.Add(new BuildingDefinition.ProductionSlotDefinition {
                        SpawnUnitPrefab = recipe.UnitPrefab,
                        SpawnUnitSourceKey = new FixedString64Bytes(recipe.UnitPrefab.name),
                        Quantity = recipe.Quantity
                    });
                }
            // Invalid input never replaces a working catalog with a partially applied one.
            unitRecipes = units;
            producerRecipes = producers;
        }

        private List<BuildingDefinition.ProductionSlotDefinition> ResolveProductionRecipes(GameObject producer,
            List<BuildingDefinition.ProductionSlotDefinition> authored) =>
            producer != null && producerRecipes.TryGetValue(producer, out var recipes)
                ? new List<BuildingDefinition.ProductionSlotDefinition>(recipes) : authored;
    }
}
