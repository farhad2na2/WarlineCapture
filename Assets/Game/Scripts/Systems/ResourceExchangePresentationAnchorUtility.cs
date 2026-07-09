using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class ResourceExchangePresentationAnchorUtility
    {
        private const float MinimumRadius = 0.01f;

        public static bool TryResolveAnchor(
            DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors,
            byte factionId,
            ResourceExchangePresentationAnchorKind preferredKind,
            out ResourceExchangePresentationAnchorComponent anchor,
            out ResourceExchangePresentationAnchorKind resolvedKind,
            out byte usedFallback)
        {
            anchor = default;
            resolvedKind = ResourceExchangePresentationAnchorKind.None;
            usedFallback = 0;

            if (preferredKind != ResourceExchangePresentationAnchorKind.None &&
                TryFindValidAnchor(anchors, factionId, preferredKind, out anchor))
            {
                resolvedKind = preferredKind;
                return true;
            }

            if (TryFindValidAnchor(anchors, factionId, ResourceExchangePresentationAnchorKind.FallbackSafe, out anchor))
            {
                resolvedKind = ResourceExchangePresentationAnchorKind.FallbackSafe;
                usedFallback = 1;
                return true;
            }

            for (int i = 0; i < FallbackOrder.Length; i++)
            {
                ResourceExchangePresentationAnchorKind fallbackKind = FallbackOrder[i];
                if (fallbackKind == preferredKind ||
                    fallbackKind == ResourceExchangePresentationAnchorKind.FallbackSafe)
                    continue;

                if (!TryFindValidAnchor(anchors, factionId, fallbackKind, out anchor))
                    continue;

                resolvedKind = fallbackKind;
                usedFallback = 1;
                return true;
            }

            return false;
        }

        public static bool IsValidAnchor(in ResourceExchangePresentationAnchorComponent anchor)
        {
            if (anchor.IsValid == 0 ||
                anchor.AnchorKind == ResourceExchangePresentationAnchorKind.None ||
                anchor.Radius < MinimumRadius)
                return false;

            return math.all(math.isfinite(anchor.Position)) &&
                   math.all(math.isfinite(anchor.Rotation.value));
        }

        private static bool TryFindValidAnchor(
            DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors,
            byte factionId,
            ResourceExchangePresentationAnchorKind kind,
            out ResourceExchangePresentationAnchorComponent anchor)
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                ResourceExchangePresentationAnchorComponent candidate = anchors[i];
                if (candidate.FactionId != factionId ||
                    candidate.AnchorKind != kind ||
                    !IsValidAnchor(candidate))
                    continue;

                anchor = candidate;
                return true;
            }

            anchor = default;
            return false;
        }

        private static readonly ResourceExchangePresentationAnchorKind[] FallbackOrder =
        {
            ResourceExchangePresentationAnchorKind.FallbackSafe,
            ResourceExchangePresentationAnchorKind.BaseDepot,
            ResourceExchangePresentationAnchorKind.Storage,
            ResourceExchangePresentationAnchorKind.RunwayLandingZone
        };
    }
}
