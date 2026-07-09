using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class ResourceExchangeAriaTextUtility
    {
        public static bool TryAppendAnnouncement(
            DynamicBuffer<ResourceExchangeAriaAnnouncementComponent> announcements,
            bool emitAnnouncements,
            in ResourceExchangeResultComponent result)
        {
            if (!emitAnnouncements)
                return false;

            if (!TryCreateAnnouncement(result, announcements.Length + 1, out ResourceExchangeAriaAnnouncementComponent announcement))
                return false;

            announcements.Add(announcement);
            return true;
        }

        public static bool TryCreateAnnouncement(
            in ResourceExchangeResultComponent result,
            int sequenceId,
            out ResourceExchangeAriaAnnouncementComponent announcement)
        {
            announcement = default;
            if (!TryResolveKind(result, out ResourceExchangeAriaAnnouncementKind kind))
                return false;

            announcement = new ResourceExchangeAriaAnnouncementComponent
            {
                SequenceId = sequenceId,
                RequestId = result.RequestId,
                QueueItemId = result.QueueItemId,
                FactionId = result.FactionId,
                AnnouncementKind = kind,
                Priority = ResolvePriority(kind),
                ResultKind = result.ResultKind,
                Reason = result.Reason,
                InputResource = result.InputResource,
                OutputResource = result.OutputResource,
                InputAmount = result.InputAmount,
                OutputAmount = result.OutputAmount,
                RecipeId = result.RecipeId,
                SuppressionKey = ResolveSuppressionKey(kind, result.Reason),
                Text = ResolveAnnouncementText(result, kind)
            };
            return true;
        }

        public static FixedString128Bytes ResolveAnnouncementText(
            in ResourceExchangeResultComponent result,
            ResourceExchangeAriaAnnouncementKind kind)
        {
            switch (kind)
            {
                case ResourceExchangeAriaAnnouncementKind.InsufficientResources:
                    return ResolveInsufficientResourceText(result.Reason);
                case ResourceExchangeAriaAnnouncementKind.ExchangeStarted:
                    return new FixedString128Bytes("Exchange queued. Logistics timer started.");
                case ResourceExchangeAriaAnnouncementKind.ExchangeComplete:
                    return new FixedString128Bytes("Exchange complete. Resources received.");
                case ResourceExchangeAriaAnnouncementKind.ExchangeBlocked:
                    return ResolveBlockedText(result.Reason);
                default:
                    return new FixedString128Bytes("Resource Exchange updated.");
            }
        }

        public static bool IsInsufficientResourceReason(ResourceExchangeReason reason)
        {
            return reason == ResourceExchangeReason.InsufficientCredits ||
                   reason == ResourceExchangeReason.InsufficientMaterials ||
                   reason == ResourceExchangeReason.InsufficientOil ||
                   reason == ResourceExchangeReason.InsufficientFuel ||
                   reason == ResourceExchangeReason.InsufficientRushTickets;
        }

        private static bool TryResolveKind(
            in ResourceExchangeResultComponent result,
            out ResourceExchangeAriaAnnouncementKind kind)
        {
            if (IsInsufficientResourceReason(result.Reason))
            {
                kind = ResourceExchangeAriaAnnouncementKind.InsufficientResources;
                return true;
            }

            if (result.Accepted == 0 ||
                result.ResultKind == ResourceExchangeResultKind.RequestRejected ||
                result.ResultKind == ResourceExchangeResultKind.RushRejected ||
                result.ResultKind == ResourceExchangeResultKind.QueueBlocked)
            {
                kind = ResourceExchangeAriaAnnouncementKind.ExchangeBlocked;
                return true;
            }

            if ((result.ResultKind == ResourceExchangeResultKind.RequestAccepted ||
                 result.ResultKind == ResourceExchangeResultKind.QueueStarted) &&
                result.QueueItemId > 0 &&
                !result.RecipeId.IsEmpty)
            {
                kind = ResourceExchangeAriaAnnouncementKind.ExchangeStarted;
                return true;
            }

            if (result.ResultKind == ResourceExchangeResultKind.QueueCompleted && result.Accepted != 0)
            {
                kind = ResourceExchangeAriaAnnouncementKind.ExchangeComplete;
                return true;
            }

            kind = ResourceExchangeAriaAnnouncementKind.None;
            return false;
        }

        private static AssistantMessagePriority ResolvePriority(ResourceExchangeAriaAnnouncementKind kind)
        {
            switch (kind)
            {
                case ResourceExchangeAriaAnnouncementKind.InsufficientResources:
                case ResourceExchangeAriaAnnouncementKind.ExchangeBlocked:
                    return AssistantMessagePriority.High;
                case ResourceExchangeAriaAnnouncementKind.ExchangeComplete:
                    return AssistantMessagePriority.Normal;
                default:
                    return AssistantMessagePriority.Low;
            }
        }

        private static FixedString64Bytes ResolveSuppressionKey(
            ResourceExchangeAriaAnnouncementKind kind,
            ResourceExchangeReason reason)
        {
            switch (kind)
            {
                case ResourceExchangeAriaAnnouncementKind.InsufficientResources:
                    return new FixedString64Bytes("resource_exchange.insufficient_resource");
                case ResourceExchangeAriaAnnouncementKind.ExchangeStarted:
                    return new FixedString64Bytes("resource_exchange.started");
                case ResourceExchangeAriaAnnouncementKind.ExchangeComplete:
                    return new FixedString64Bytes("resource_exchange.complete");
                case ResourceExchangeAriaAnnouncementKind.ExchangeBlocked:
                    return reason == ResourceExchangeReason.StorageFull
                        ? new FixedString64Bytes("resource_exchange.blocked.storage_full")
                        : new FixedString64Bytes("resource_exchange.blocked");
                default:
                    return new FixedString64Bytes("resource_exchange.update");
            }
        }

        private static FixedString128Bytes ResolveInsufficientResourceText(ResourceExchangeReason reason)
        {
            switch (reason)
            {
                case ResourceExchangeReason.InsufficientCredits:
                    return new FixedString128Bytes("Not enough Credits for this exchange.");
                case ResourceExchangeReason.InsufficientMaterials:
                    return new FixedString128Bytes("Not enough Materials for this exchange.");
                case ResourceExchangeReason.InsufficientOil:
                    return new FixedString128Bytes("Not enough Oil for this exchange.");
                case ResourceExchangeReason.InsufficientFuel:
                    return new FixedString128Bytes("Not enough Fuel for this exchange.");
                case ResourceExchangeReason.InsufficientRushTickets:
                    return new FixedString128Bytes("Not enough Rush Tickets for this exchange.");
                default:
                    return new FixedString128Bytes("Not enough resources for this exchange.");
            }
        }

        private static FixedString128Bytes ResolveBlockedText(ResourceExchangeReason reason)
        {
            switch (reason)
            {
                case ResourceExchangeReason.QueueFull:
                    return new FixedString128Bytes("Exchange blocked. The exchange queue is full.");
                case ResourceExchangeReason.StorageFull:
                    return new FixedString128Bytes("Exchange blocked. Output storage is full.");
                case ResourceExchangeReason.StorageMissing:
                    return new FixedString128Bytes("Exchange blocked. Required storage is missing.");
                case ResourceExchangeReason.RecipeLocked:
                    return new FixedString128Bytes("Exchange blocked. This route is locked.");
                case ResourceExchangeReason.ExchangeUnavailable:
                    return new FixedString128Bytes("Exchange blocked. Resource Exchange is unavailable.");
                case ResourceExchangeReason.RushUnavailable:
                    return new FixedString128Bytes("Exchange blocked. Rush is unavailable.");
                case ResourceExchangeReason.TransportUnavailable:
                    return new FixedString128Bytes("Exchange blocked. Transport is unavailable.");
                case ResourceExchangeReason.MissionEnding:
                    return new FixedString128Bytes("Exchange blocked. Mission is ending.");
                default:
                    return new FixedString128Bytes("Exchange blocked. Check route requirements.");
            }
        }
    }
}
