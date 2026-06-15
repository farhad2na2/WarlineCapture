using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed partial class RuntimeCityDecorationPrefabGroupSystem : SystemBase
{
    public readonly struct Groups
    {
        public readonly List<GameObject> ClothCoverPrefabs;
        public readonly List<GameObject> ArchwayPrefabs;
        public readonly List<GameObject> FreeScatterPrefabs;

        public Groups(List<GameObject> clothCoverPrefabs, List<GameObject> archwayPrefabs, List<GameObject> freeScatterPrefabs)
        {
            ClothCoverPrefabs = clothCoverPrefabs;
            ArchwayPrefabs = archwayPrefabs;
            FreeScatterPrefabs = freeScatterPrefabs;
        }
    }

    private readonly RuntimeCityDecorationPrefabGroupState _state = new();

    public RuntimeCityDecorationPrefabGroupState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public Groups CreateGroups(List<GameObject> prefabs)
    {
        return _state.CreateGroups(prefabs);
    }
}

internal sealed class RuntimeCityDecorationPrefabGroupState
{
    public RuntimeCityDecorationPrefabGroupSystem.Groups CreateGroups(List<GameObject> prefabs)
    {
        int capacity = prefabs?.Count ?? 0;
        var clothCoverPrefabs = new List<GameObject>(capacity);
        var archwayPrefabs = new List<GameObject>(capacity);
        var freeScatterPrefabs = new List<GameObject>(capacity);

        if (prefabs == null)
            return new RuntimeCityDecorationPrefabGroupSystem.Groups(clothCoverPrefabs, archwayPrefabs, freeScatterPrefabs);

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;

            if (prefab.name.Contains("ClothCover", StringComparison.OrdinalIgnoreCase))
                clothCoverPrefabs.Add(prefab);
            else if (prefab.name.Contains("Archway", StringComparison.OrdinalIgnoreCase))
                archwayPrefabs.Add(prefab);
            else
                freeScatterPrefabs.Add(prefab);
        }

        return new RuntimeCityDecorationPrefabGroupSystem.Groups(clothCoverPrefabs, archwayPrefabs, freeScatterPrefabs);
    }
}
