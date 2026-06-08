using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SoldierBadgeConfigEntry
{
    [SerializeField] private GameObject badgePrefab;
    [SerializeField] private string displayName;
    [SerializeField] private int rank;
    [SerializeField] private int tier;

    public GameObject BadgePrefab => badgePrefab;
    public string DisplayName => displayName;
    public int Rank => rank;
    public int Tier => tier;
}

[CreateAssetMenu(menuName = "Game/Config/Soldier Badge Catalog")]
public sealed class SoldierBadgeCatalogConfig : ScriptableObject
{
    [SerializeField] private List<SoldierBadgeConfigEntry> badges = new();

    public IReadOnlyList<SoldierBadgeConfigEntry> Badges => badges;
}
