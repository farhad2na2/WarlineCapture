using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static partial class SkirmishNativeCheckpointParts
    {
        // Pure staging: rebase before any destination entity is mutated. Native
        // unit systems and the managed production/haul owner use different clocks.
        internal static Part[] RebaseClocks(Part[] parts, float worldOffset, float unityOffset, int frameOffset)
        {
            if (!math.isfinite(worldOffset) || !math.isfinite(unityOffset) || parts == null)
                throw new ArgumentException("Invalid checkpoint clock offsets.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in parts)
            {
                if (part?.References == null) throw new ArgumentException("Invalid checkpoint clock record.");
                foreach (var reference in part.References)
                    if (reference?.ObjectId != null && reference.ObjectId.Length != 0) ids.Add(reference.ObjectId);
            }
            if (!Validate(parts, ids)) throw new ArgumentException("Invalid checkpoint components.");
            var result = new Part[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var original = parts[i];
                var references = new Reference[original.References.Length];
                for (int r = 0; r < references.Length; r++) references[r] = new Reference
                { Offset = original.References[r].Offset, ObjectId = original.References[r].ObjectId };
                var part = new Part
                {
                    Type = original.Type, Layout = original.Layout, Data = original.Data,
                    Count = original.Count, Enabled = original.Enabled, References = references
                };
                result[i] = part;
                byte[] bytes = Convert.FromBase64String(part.Data);
                if (part.Type == typeof(FactionEconomy).FullName)
                {
                    var value = Unpack<FactionEconomy>(bytes);
                    value.LastSellTime = Shift(value.LastSellTime, worldOffset);
                    value.LastLogTime = Shift(value.LastLogTime, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportRopeDisembarkRequest).FullName)
                {
                    var value = Unpack<UnitTransportRopeDisembarkRequest>(bytes);
                    value.NextDropAt = Shift(value.NextDropAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportAirdropRequest).FullName)
                {
                    var value = Unpack<UnitTransportAirdropRequest>(bytes);
                    value.NextDropAt = Shift(value.NextDropAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportRopeDropComponent).FullName)
                {
                    var value = Unpack<UnitTransportRopeDropComponent>(bytes);
                    value.StartedAt = Shift(value.StartedAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportParachuteDropComponent).FullName)
                {
                    var value = Unpack<UnitTransportParachuteDropComponent>(bytes);
                    value.StartedAt = Shift(value.StartedAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportCargoDropComponent).FullName)
                {
                    var value = Unpack<UnitTransportCargoDropComponent>(bytes);
                    value.StartedAt = Shift(value.StartedAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportAirdropSettleComponent).FullName)
                {
                    var value = Unpack<UnitTransportAirdropSettleComponent>(bytes);
                    value.StartedAt = Shift(value.StartedAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportRopeDisperseComponent).FullName)
                {
                    var value = Unpack<UnitTransportRopeDisperseComponent>(bytes);
                    value.StartedAt = Shift(value.StartedAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitTransportAirdropVisualCleanup).FullName)
                {
                    var value = Unpack<UnitTransportAirdropVisualCleanup>(bytes);
                    if (value.DestroyAt != 0) value.DestroyAt = Shift(value.DestroyAt, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitResourceHaulOrder).FullName)
                {
                    var value = Unpack<UnitResourceHaulOrder>(bytes);
                    if (value.ActionEndsAt != 0) value.ActionEndsAt = Shift(value.ActionEndsAt, unityOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(UnitScanOrder).FullName)
                {
                    var value = Unpack<UnitScanOrder>(bytes);
                    if (value.HasStarted != 0)
                    {
                        value.StartedTimeSeconds = Shift(value.StartedTimeSeconds, worldOffset);
                        value.StartedFrame = unchecked(value.StartedFrame + frameOffset);
                    }
                    value.NextRevealTimeSeconds = Shift(value.NextRevealTimeSeconds, worldOffset);
                    value.NextPatrolMoveTimeSeconds = Shift(value.NextPatrolMoveTimeSeconds, worldOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
                else if (part.Type == typeof(ScanIntelLastSeen).FullName)
                {
                    var value = Unpack<ScanIntelLastSeen>(bytes);
                    value.LastScanFrame = unchecked(value.LastScanFrame + frameOffset);
                    part.Data = Convert.ToBase64String(Pack(value));
                }
            }
            return result;
        }

        private static float Shift(float value, float offset)
        {
            float result = value + offset;
            if (!math.isfinite(value) || !math.isfinite(result))
                throw new ArgumentException("Checkpoint timer is not finite.");
            return result;
        }
    }
}
