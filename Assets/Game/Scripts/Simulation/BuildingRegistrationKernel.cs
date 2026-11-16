using System;

namespace Game.Simulation
{
    // Shared registration decisions. Hosts supply source data/effects, never another registry.
    public static class BuildingRegistrationKernel
    {
        public sealed class Context
        {
            public Func<bool> DeferSideEffects;
            public Func<int> AllocateId;
            public Action<int> PrepareSourceData;
            public Func<bool> ShouldBlockPathing;
            public Action RemoveOverlappingBlockers;
            public Action<int, bool> CreateActorsAndFoundation;
            public Action AddDeferredRedirectFootprint;
            public Action RedirectUnits;
            public Action<int, bool> CommitData;
        }

        public readonly struct Decision
        {
            public readonly int BuildingId;
            public readonly bool DeferSideEffects;
            public readonly bool PathBlocking;
            public Decision(int buildingId, bool deferSideEffects, bool pathBlocking)
            { BuildingId = buildingId; DeferSideEffects = deferSideEffects; PathBlocking = pathBlocking; }
        }

        public static Decision Register(Context context, bool removeOverlappingBlockers)
        {
            if (context == null || context.AllocateId == null || context.PrepareSourceData == null ||
                context.CreateActorsAndFoundation == null || context.CommitData == null)
                throw new ArgumentException("Complete building registration owner required", nameof(context));

            bool defer = context.DeferSideEffects?.Invoke() == true;
            int buildingId = context.AllocateId();
            context.PrepareSourceData(buildingId);
            bool pathBlocking = context.ShouldBlockPathing == null || context.ShouldBlockPathing();
            if (removeOverlappingBlockers && pathBlocking)
                context.RemoveOverlappingBlockers?.Invoke();
            context.CreateActorsAndFoundation(buildingId, pathBlocking);
            if (defer)
            {
                if (pathBlocking)
                    context.AddDeferredRedirectFootprint?.Invoke();
            }
            else if (pathBlocking)
            {
                context.RedirectUnits?.Invoke();
            }
            context.CommitData(buildingId, defer);
            return new Decision(buildingId, defer, pathBlocking);
        }
    }
}
