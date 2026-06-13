using System.Collections.Generic;
using UnityEngine;

public interface ICatalogPrefabSource
{
    IReadOnlyList<GameObject> UnitSpawnPrefabs { get; }
    IReadOnlyList<GameObject> BuildingSpawnPrefabs { get; }
}
