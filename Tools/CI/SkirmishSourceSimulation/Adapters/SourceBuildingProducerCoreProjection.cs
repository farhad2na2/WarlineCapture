extern alias ActualCore;
using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>
    /// Binds the narrow static Barrack core from the exact source definition row.
    /// Mission production-slot overlays stay in RuntimeDefinition.ProductionSlots.
    /// </summary>
    internal static class SourceBuildingProducerCoreProjection
    {
        private const string BarrackKey = "Building_Barrack";

        internal static void Apply(RuntimeDefinition definition, IReadOnlyDictionary<string, JObject> assetIdentities)
        {
            if (definition == null || definition.Prefab == null ||
                !string.Equals(definition.Prefab.name, BarrackKey, StringComparison.Ordinal))
                return;

            if (assetIdentities == null || !assetIdentities.TryGetValue(BarrackKey, out JObject identity) ||
                !string.Equals((string)identity["key"], BarrackKey, StringComparison.Ordinal) ||
                !string.Equals((string)identity["path"], "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab", StringComparison.Ordinal) ||
                !string.Equals((string)identity["guid"], "683195b1e21c94df7add42ab26cd2a33", StringComparison.Ordinal) ||
                (long?)identity["local_id"] != 6603657636351658259L)
                throw new InvalidOperationException("Barrack producer core requires its exact package prefab GUID/local-ID provenance");

            // Package admission is strict here, separately from the shared
            // projection, which must preserve every value the native factory
            // historically accepts.
            if (definition.FootprintCells.x < 1 || definition.FootprintCells.y < 1 ||
                definition.CreditsCost < 0 || definition.MaterialsCost < 0 || definition.MaxHealth < 1 ||
                !(definition.ProductionDurationSeconds > 0f) ||
                float.IsNaN(definition.ProductionDurationSeconds) || float.IsInfinity(definition.ProductionDurationSeconds))
                throw new InvalidDataException("Qualified Barrack core fields are outside the supported source package domain");

            BuildingProducerCoreProjection core = BuildingProducerCoreProjection.FromFactoryValues(
                definition.Prefab.name,
                new int2(definition.FootprintCells.x, definition.FootprintCells.y),
                definition.CreditsCost,
                definition.MaterialsCost,
                definition.MaxHealth,
                definition.ProductionDurationSeconds);

            definition.FootprintCells = new ActualCore::UnityEngine.Vector2Int(core.FootprintCells.x, core.FootprintCells.y);
            definition.CreditsCost = core.CreditsCost;
            definition.MaterialsCost = core.MaterialsCost;
            definition.MaxHealth = core.MaxHealth;
            definition.ProductionDurationSeconds = core.ConstructionDurationSeconds;
        }
    }
}
