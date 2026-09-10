using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct ResourceExchangeRequestValidationSystem
    {
        private static void PublishRequestFeedback(EntityManager em, Entity entity,
            DynamicBuffer<ResourceExchangeResultComponent> results,
            DynamicBuffer<ResourceExchangeEconomyEventComponent> events, int eventStart)
        {
            bool hasToasts = em.HasBuffer<ResourceExchangeToastComponent>(entity);
            bool hasAnnouncements = em.HasBuffer<ResourceExchangeAriaAnnouncementComponent>(entity);
            var toasts = hasToasts ? em.GetBuffer<ResourceExchangeToastComponent>(entity) : default;
            var announcements = hasAnnouncements ? em.GetBuffer<ResourceExchangeAriaAnnouncementComponent>(entity) : default;
            foreach (var result in results)
            {
                ResourceExchangeToastTextUtility.TryAppendToast(toasts, hasToasts, result);
                ResourceExchangeAriaTextUtility.TryAppendAnnouncement(announcements, hasAnnouncements, result);
            }
            if (!em.HasBuffer<ResourceExchangeDeltaFlyoutComponent>(entity)) return;
            var flyouts = em.GetBuffer<ResourceExchangeDeltaFlyoutComponent>(entity);
            for (int i = eventStart; i < events.Length; i++)
            {
                var entry = events[i];
                if (entry.Amount == 0) continue;
                var kind = entry.ResourceKind == ResourceExchangeResourceKind.RushTickets && entry.Amount < 0
                    ? ResourceExchangeDeltaFlyoutKind.RushTicketsSpent
                    : entry.ResultKind == ResourceExchangeResultKind.QueueCancelled
                        ? ResourceExchangeDeltaFlyoutKind.InputRefunded
                        : entry.Amount > 0 ? ResourceExchangeDeltaFlyoutKind.OutputGranted
                        : ResourceExchangeDeltaFlyoutKind.InputReserved;
                flyouts.Add(new ResourceExchangeDeltaFlyoutComponent
                {
                    SequenceId = flyouts.Length + 1, QueueItemId = entry.QueueItemId,
                    FactionId = entry.FactionId, FlyoutKind = kind, ResultKind = entry.ResultKind,
                    ResourceKind = entry.ResourceKind, Amount = entry.Amount, RecipeId = entry.RecipeId
                });
            }
        }
    }
}
