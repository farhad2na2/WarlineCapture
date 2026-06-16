using Unity.Collections;
using Unity.Entities;

internal partial struct CitizenPrefabSelectionSystem : ISystem
{
    public struct State
    {
        public FixedString64Bytes MaleCitizenPrefab0;
        public FixedString64Bytes MaleCitizenPrefab1;
        public FixedString64Bytes FemaleCitizenPrefab0;
        public FixedString64Bytes FemaleCitizenPrefab1;
        public int MaleCitizenPrefabCount;
        public int FemaleCitizenPrefabCount;

        public void Reset()
        {
            MaleCitizenPrefab0 = default;
            MaleCitizenPrefab1 = default;
            FemaleCitizenPrefab0 = default;
            FemaleCitizenPrefab1 = default;
            MaleCitizenPrefabCount = 0;
            FemaleCitizenPrefabCount = 0;
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public void Init(ref State state, CitizenPrefabSystem citizenPrefabSystem, CitizenPrefabSystem.Context citizenPrefabContext)
    {
        state.Reset();
        TryAddCitizenPrefab(ref state, CitizenGender.Male, "Unit_Chr_Civilian_Male_01", citizenPrefabSystem, citizenPrefabContext);
        TryAddCitizenPrefab(ref state, CitizenGender.Male, "Unit_Chr_Civilian_Male_02", citizenPrefabSystem, citizenPrefabContext);
        TryAddCitizenPrefab(ref state, CitizenGender.Female, "Unit_Chr_Civilian_Female_01", citizenPrefabSystem, citizenPrefabContext);
        TryAddCitizenPrefab(ref state, CitizenGender.Female, "Unit_Chr_Civilian_Female_02", citizenPrefabSystem, citizenPrefabContext);
    }

    public void Reset(ref State state)
    {
        state.Reset();
    }

    public bool TryGetCitizenPrefabSourceKey(in State state, CitizenRecordComponent citizen, out FixedString64Bytes sourceKey)
    {
        sourceKey = default;
        int count = citizen.Gender == CitizenGender.Male
            ? state.MaleCitizenPrefabCount
            : state.FemaleCitizenPrefabCount;
        if (count <= 0)
            return false;

        int index = PositiveModulo(citizen.CitizenId, count);
        sourceKey = citizen.Gender == CitizenGender.Male
            ? GetMaleCitizenPrefab(state, index)
            : GetFemaleCitizenPrefab(state, index);
        return sourceKey.Length > 0;
    }

    private static void TryAddCitizenPrefab(
        ref State state,
        CitizenGender gender,
        string sourceName,
        CitizenPrefabSystem citizenPrefabSystem,
        CitizenPrefabSystem.Context citizenPrefabContext)
    {
        string sourceKey = BuildingDefinitionSystem.GetSpawnableLookupKey(sourceName);
        if (string.IsNullOrWhiteSpace(sourceKey))
            return;

        FixedString64Bytes fixedSourceKey = new FixedString64Bytes(sourceKey);
        if (!citizenPrefabSystem.TryResolveConfiguredUnitPrefabEntity(citizenPrefabContext, fixedSourceKey, out Entity prefabEntity) ||
            prefabEntity == Entity.Null)
        {
            return;
        }

        AddCitizenPrefab(ref state, gender, fixedSourceKey);
    }

    private static void AddCitizenPrefab(ref State state, CitizenGender gender, FixedString64Bytes sourceKey)
    {
        if (gender == CitizenGender.Male)
        {
            if (state.MaleCitizenPrefabCount == 0)
                state.MaleCitizenPrefab0 = sourceKey;
            else if (state.MaleCitizenPrefabCount == 1)
                state.MaleCitizenPrefab1 = sourceKey;
            else
                return;
            state.MaleCitizenPrefabCount++;
            return;
        }

        if (state.FemaleCitizenPrefabCount == 0)
            state.FemaleCitizenPrefab0 = sourceKey;
        else if (state.FemaleCitizenPrefabCount == 1)
            state.FemaleCitizenPrefab1 = sourceKey;
        else
            return;
        state.FemaleCitizenPrefabCount++;
    }

    private static FixedString64Bytes GetMaleCitizenPrefab(in State state, int index)
    {
        return index == 0 ? state.MaleCitizenPrefab0 : state.MaleCitizenPrefab1;
    }

    private static FixedString64Bytes GetFemaleCitizenPrefab(in State state, int index)
    {
        return index == 0 ? state.FemaleCitizenPrefab0 : state.FemaleCitizenPrefab1;
    }

    private static int PositiveModulo(int value, int count)
    {
        long magnitude = value < 0 ? -(long)value : value;
        return (int)(magnitude % count);
    }
}
