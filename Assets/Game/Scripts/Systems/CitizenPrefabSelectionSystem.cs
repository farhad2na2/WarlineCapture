using System.Collections.Generic;
using UnityEngine;

internal sealed class CitizenPrefabSelectionSystem
{
    private GameObject[] _maleCitizenPrefabs;
    private GameObject[] _femaleCitizenPrefabs;

    public void Init(CitizenPrefabSystem citizenPrefabSystem, CitizenPrefabSystem.Context citizenPrefabContext)
    {
        _maleCitizenPrefabs = LoadCitizenPrefabs(CitizenGender.Male, citizenPrefabSystem, citizenPrefabContext);
        _femaleCitizenPrefabs = LoadCitizenPrefabs(CitizenGender.Female, citizenPrefabSystem, citizenPrefabContext);
    }

    public void Reset()
    {
        _maleCitizenPrefabs = null;
        _femaleCitizenPrefabs = null;
    }

    public GameObject GetCitizenPrefab(CitizenRecordComponent citizen)
    {
        GameObject[] prefabs = citizen.Gender == CitizenGender.Male ? _maleCitizenPrefabs : _femaleCitizenPrefabs;
        if (prefabs == null || prefabs.Length == 0)
            return null;

        int index = Mathf.Abs(citizen.CitizenId) % prefabs.Length;
        return prefabs[index];
    }

    private static GameObject[] LoadCitizenPrefabs(
        CitizenGender gender,
        CitizenPrefabSystem citizenPrefabSystem,
        CitizenPrefabSystem.Context citizenPrefabContext)
    {
        string[] unitNames = gender == CitizenGender.Male
            ? new[]
            {
                "Unit_Chr_Civilian_Male_01",
                "Unit_Chr_Civilian_Male_02"
            }
            : new[]
            {
                "Unit_Chr_Civilian_Female_01",
                "Unit_Chr_Civilian_Female_02"
            };

        List<GameObject> prefabs = new();
        if (citizenPrefabSystem == null)
            return prefabs.ToArray();

        citizenPrefabSystem.LoadConfiguredUnitSpawnPrefabs(citizenPrefabContext, unitNames, prefabs);
        return prefabs.ToArray();
    }
}
