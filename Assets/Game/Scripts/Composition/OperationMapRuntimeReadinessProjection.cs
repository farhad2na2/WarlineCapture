using Game.Components;
using Unity.Entities;

namespace Game.Composition
{
    internal static class OperationMapRuntimeReadinessProjection
    {
        public static bool TryApply(
            EntityManager entityManager,
            Entity rootEntity,
            int generation,
            OperationMapReadinessFlags readyFlags,
            OperationMapReadinessFlags failedFlags,
            out string error)
        {
            ActiveOperationMapComponent active =
                entityManager.GetComponentData<ActiveOperationMapComponent>(rootEntity);
            OperationMapReadinessComponent readiness =
                entityManager.GetComponentData<OperationMapReadinessComponent>(rootEntity);
            if (active.Generation != generation || readiness.Generation != generation)
            {
                error = "Operation-map readiness generation does not match the active map.";
                return false;
            }

            OperationMapReadinessFlags allowedFlags = readiness.RequiredFlags;
            if ((readyFlags & ~allowedFlags) != 0 || (failedFlags & ~allowedFlags) != 0)
            {
                error = "Operation-map readiness contains flags outside the active requirement set.";
                return false;
            }

            if (readiness.ReadyFlags == readyFlags && readiness.FailedFlags == failedFlags)
            {
                error = null;
                return true;
            }

            readiness.ReadyFlags = readyFlags;
            readiness.FailedFlags = failedFlags;
            entityManager.SetComponentData(rootEntity, readiness);

            bool complete = HasRequired(readyFlags, readiness.RequiredFlags);
            OperationMapLoadStateComponent state =
                entityManager.GetComponentData<OperationMapLoadStateComponent>(rootEntity);
            state.Generation = generation;
            state.Progress01 = CalculateProgress(readyFlags, readiness.RequiredFlags);
            state.Status = failedFlags != OperationMapReadinessFlags.None
                ? OperationMapLoadStatusKind.Failed
                : complete
                    ? OperationMapLoadStatusKind.Ready
                    : (readyFlags & OperationMapReadinessFlags.SubScene) == 0
                        ? OperationMapLoadStatusKind.LoadingSubScene
                        : OperationMapLoadStatusKind.PreloadingPresentation;
            state.Readiness = readyFlags;
            state.IsBusy = complete || failedFlags != OperationMapReadinessFlags.None
                ? (byte)0
                : (byte)1;
            entityManager.SetComponentData(rootEntity, state);
            error = null;
            return true;
        }

        public static bool HasRequired(
            OperationMapReadinessFlags readyFlags,
            OperationMapReadinessFlags requiredFlags) =>
            (readyFlags & requiredFlags) == requiredFlags;

        private static float CalculateProgress(
            OperationMapReadinessFlags readyFlags,
            OperationMapReadinessFlags requiredFlags)
        {
            int readyCount = 0;
            int requiredCount = 0;
            ushort ready = (ushort)(readyFlags & requiredFlags);
            ushort required = (ushort)requiredFlags;
            while (required != 0)
            {
                if ((required & 1) != 0)
                {
                    requiredCount++;
                    if ((ready & 1) != 0)
                        readyCount++;
                }
                required >>= 1;
                ready >>= 1;
            }

            return requiredCount == 0 ? 1f : (float)readyCount / requiredCount;
        }
    }
}
