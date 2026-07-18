using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct UiMatchHudResourceReadModelSystem : ISystem
    {
        private EntityQuery _boundaryQuery;
        private Entity _lastBoundary;
        private int _lastMaterials;
        private int _lastMaterialsCapacity;
        private uint _lastMaterialsVersion;
        private byte _hasProjected;

        public void OnCreate(ref SystemState state)
        {
            _boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiMatchHudHeaderComponent>());
            state.RequireForUpdate(_boundaryQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int materials = 0;
            int materialsCapacity = 0;
            uint materialsVersion = 0u;
            bool foundPlayer = false;

            foreach (RefRO<FactionTacticalMaterialsComponent> tacticalMaterials
                     in SystemAPI.Query<RefRO<FactionTacticalMaterialsComponent>>())
            {
                if (!FactionIdentity.IsPlayerControlled(tacticalMaterials.ValueRO.FactionId))
                    continue;

                materials = math.max(0, tacticalMaterials.ValueRO.Current);
                materialsCapacity = math.max(0, tacticalMaterials.ValueRO.Capacity);
                materialsVersion = tacticalMaterials.ValueRO.Version;
                foundPlayer = true;
                break;
            }

            if (!foundPlayer)
                return;

            Entity boundary = _boundaryQuery.GetSingletonEntity();
            if (_hasProjected != 0 &&
                _lastBoundary == boundary &&
                _lastMaterials == materials &&
                _lastMaterialsCapacity == materialsCapacity &&
                _lastMaterialsVersion == materialsVersion)
            {
                return;
            }

            UiMatchHudHeaderComponent header =
                state.EntityManager.GetComponentData<UiMatchHudHeaderComponent>(boundary);
            header.ResourceVersion = NextVersion(header.ResourceVersion);
            header.MaterialsText = FormatMaterials(materials, materialsCapacity);
            state.EntityManager.SetComponentData(boundary, header);

            _lastBoundary = boundary;
            _lastMaterials = materials;
            _lastMaterialsCapacity = materialsCapacity;
            _lastMaterialsVersion = materialsVersion;
            _hasProjected = 1;
        }

        private static uint NextVersion(uint version)
        {
            return version == uint.MaxValue ? 1u : version + 1u;
        }

        private static FixedString32Bytes FormatMaterials(int current, int capacity)
        {
            FixedString32Bytes text = default;
            AppendGroupedAmount(ref text, current);
            text.Append('/');
            AppendGroupedAmount(ref text, capacity);
            return text;
        }

        private static void AppendGroupedAmount(ref FixedString32Bytes text, int value)
        {
            value = math.max(0, value);
            int divisor = 1;
            while (divisor <= 1000000 && value / divisor >= 1000)
                divisor *= 1000;

            text.Append(value / divisor);
            while (divisor > 1)
            {
                divisor /= 1000;
                int group = value / divisor % 1000;
                text.Append(',');
                text.Append((char)('0' + group / 100));
                text.Append((char)('0' + group / 10 % 10));
                text.Append((char)('0' + group % 10));
            }
        }
    }
}
