namespace Game.Runtime.Spawning
{
    // Numeric fields follow the persisted MapSurface type/flag/movement contract.
    // Native tests pin that boundary; this file has no Unity dependency.
    public struct SpawnGroundingFacts
    {
        public float Height;
        public int LayerId;
        public byte SurfaceType;
        public ushort Flags, MovementMask;
    }
    public interface ISpawnGroundingSamples<TSample>
    {
        bool TryGetSample(int x, int z, out TSample sample);
        SpawnGroundingFacts Facts(TSample sample);
    }
    public static class SpawnGroundingKernel
    {
        public static bool IsRoadLikeSurface(byte surfaceType, ushort flags)
        {
            return surfaceType == 1 || surfaceType == 2 || surfaceType == 4 ||
                   surfaceType == 3 || surfaceType == 5 || (flags & 1) != 0;
        }
        public static bool CanUseInfantrySupportSample(in SpawnGroundingFacts anchor, in SpawnGroundingFacts candidate)
        {
            if ((candidate.MovementMask & 1) == 0) return false;
            if (candidate.LayerId != anchor.LayerId) return false;
            return IsRoadLikeSurface(anchor.SurfaceType, anchor.Flags) ==
                   IsRoadLikeSurface(candidate.SurfaceType, candidate.Flags);
        }
        public static bool TryGetHighestInfantrySupport<TContext, TSample>(ref TContext context,
            int centerX, int centerZ, TSample anchor, out TSample supportSample)
            where TContext : struct, ISpawnGroundingSamples<TSample>
        {
            supportSample = anchor;
            bool found = false;
            var anchorFacts = context.Facts(anchor);
            float bestHeight = anchorFacts.Height;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (!context.TryGetSample(centerX + x, centerZ + y, out TSample candidate)) continue;
                    var candidateFacts = context.Facts(candidate);
                    if (!CanUseInfantrySupportSample(anchorFacts, candidateFacts)) continue;
                    if (found && candidateFacts.Height <= bestHeight) continue;
                    supportSample = candidate;
                    bestHeight = candidateFacts.Height;
                    found = true;
                }
            }
            return found;
        }
        public static bool ShouldLiftToSupport(float anchorHeight, float supportHeight)
            => supportHeight > anchorHeight && supportHeight - anchorHeight <= 1.5f;
        public static bool CanSampleOverlay(ushort movementMask) => (movementMask & 7) != 0;
        public static bool AcceptOverlayHeight(float height, ref float bestHeight)
        {
            // Do not replace this with >: NaN intentionally follows the original <= skip.
            if (height <= bestHeight) return false;
            bestHeight = height;
            return true;
        }
        public static float FinalHeight(float height, float groundOffset) => height + groundOffset;
    }
}
