using System;
using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;

namespace Game.Runtime
{
    internal static partial class SkirmishNativeCheckpointParts
    {
        // Runtime producers and streamed map placements use separate ID spaces.
        // Map placement IDs must never enter this runtime-owner lookup.
        internal static Part[] RemapBuildingIds(Part[] parts, IReadOnlyDictionary<int, int> runtimeIds,
            int? destinationBuildingId = null)
        {
            if (runtimeIds == null) throw new ArgumentNullException(nameof(runtimeIds));
            var result = RebaseClocks(parts, 0, 0, 0);
            int Resolve(int oldId)
            {
                if (oldId == 0) return 0;
                if (oldId < 0 || !runtimeIds.TryGetValue(oldId, out int newId) || newId <= 0)
                    throw new ArgumentException("Native runtime building was not reconstructed: " + oldId);
                return newId;
            }
            foreach (var part in result)
            {
                byte[] bytes = Convert.FromBase64String(part.Data);
                if (part.Type == typeof(RuntimeBuildingCombatInfo).FullName)
                {
                    if (!destinationBuildingId.HasValue || destinationBuildingId.Value <= 0)
                        throw new ArgumentException("Native destination building ID is missing.");
                    var value = Unpack<RuntimeBuildingCombatInfo>(bytes);
                    value.RuntimeBuildingId = destinationBuildingId.Value;
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(BuildingResourceStorageComponent).FullName)
                {
                    if (!destinationBuildingId.HasValue || destinationBuildingId.Value <= 0)
                        throw new ArgumentException("Native destination storage ID is missing.");
                    var value = Unpack<BuildingResourceStorageComponent>(bytes);
                    value.RuntimeBuildingId = destinationBuildingId.Value;
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(MaterialFabricationComponent).FullName)
                {
                    if (!destinationBuildingId.HasValue || destinationBuildingId.Value <= 0)
                        throw new ArgumentException("Native destination fabrication ID is missing.");
                    var value = Unpack<MaterialFabricationComponent>(bytes);
                    value.RuntimeBuildingId = destinationBuildingId.Value;
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitResourceHaulOrder).FullName)
                {
                    var value = Unpack<UnitResourceHaulOrder>(bytes);
                    value.SourceBuildingId = Resolve(value.SourceBuildingId);
                    value.DestinationBuildingId = Resolve(value.DestinationBuildingId);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitResourceHaulReservation).FullName)
                {
                    var value = Unpack<UnitResourceHaulReservation>(bytes);
                    value.SourceBuildingId = Resolve(value.SourceBuildingId);
                    value.DestinationBuildingId = Resolve(value.DestinationBuildingId);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(SkirmishProductionReservation).FullName)
                {
                    using var raw = new NativeArray<byte>(bytes, Allocator.Temp);
                    var values = raw.Reinterpret<SkirmishProductionReservation>(1);
                    for (int i = 0; i < values.Length; i++)
                    {
                        var value = values[i];
                        // A completed/lost receipt still owns delivered-member capacity,
                        // but its former producer may legitimately have been destroyed.
                        // Active queues and every hauling reference remain mandatory.
                        bool producerIsHistorical = value.Phase == SkirmishReservationPhase.Live ||
                            value.Phase == SkirmishReservationPhase.Released ||
                            value.Phase == SkirmishReservationPhase.Cancelled ||
                            value.Phase == SkirmishReservationPhase.Lost;
                        value.ProducerRuntimeId = producerIsHistorical && value.ProducerRuntimeId > 0 &&
                            !runtimeIds.ContainsKey(value.ProducerRuntimeId)
                            ? 0 : Resolve(value.ProducerRuntimeId);
                        values[i] = value;
                    }
                    part.Data = Convert.ToBase64String(raw.ToArray());
                }
            }
            return result;
        }
    }
}
