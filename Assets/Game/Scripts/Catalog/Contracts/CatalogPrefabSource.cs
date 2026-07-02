using System.Collections.Generic;
using UnityEngine;

namespace Game.Catalog.Contracts
{
    public interface ICatalogPrefabSource
    {
        IReadOnlyList<GameObject> UnitSpawnPrefabs { get; }
        IReadOnlyList<GameObject> BuildingSpawnPrefabs { get; }
    }
}
