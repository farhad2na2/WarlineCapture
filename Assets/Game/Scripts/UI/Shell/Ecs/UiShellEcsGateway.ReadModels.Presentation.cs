using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static partial class UiShellReadModelAdapter
        {
        public static bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)
        {
            placementBar = UiBuildPlacementConfirmationBarModel.Hidden;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureBuildPlacementConfirmationBarState(entityManager, boundary);
            UiBuildPlacementConfirmationBarComponent component =
                entityManager.GetComponentData<UiBuildPlacementConfirmationBarComponent>(boundary);
            placementBar = new UiBuildPlacementConfirmationBarModel(
                component.Visible != 0,
                component.CanConfirm != 0,
                component.CanCancel != 0,
                component.CanRotate != 0,
                component.Title.ToString(),
                component.Status.ToString(),
                component.CostText.ToString(),
                component.DurationText.ToString(),
                component.InstructionText.ToString());
            return true;
        }

        private static string ToSelectionOrderText(int status)
        {
            return status switch
            {
                1 => "MOVING",
                2 => "ENGAGING TARGET",
                3 => "RETURNING TO BASE",
                4 => "MISSILE LAUNCHED",
                5 => "AIRSPACE CLEAR",
                6 => "TRACKING AIR TARGET",
                7 => "INTERCEPTING MISSILE",
                8 => "RELOADING",
                _ => "IDLE"
            };
        }

        private static UiMatchHudPassengerRowModel ToPassengerRow(FocusedUnitPassengerUiReadModelElement passenger)
        {
            string name = passenger.DisplayName.ToString();
            if (string.IsNullOrWhiteSpace(name))
                name = "PASSENGER";

            int healthMax = Mathf.Max(0, passenger.HealthMax);
            int healthCurrent = Mathf.Clamp(passenger.HealthCurrent, 0, healthMax);
            string healthText = healthMax > 0 ? $"{healthCurrent} / {healthMax}" : "HEALTH -";
            float health01 = healthMax > 0 ? Mathf.Clamp01((float)healthCurrent / healthMax) : 0f;
            return new UiMatchHudPassengerRowModel(name, "ONBOARD", healthText, health01);
        }

        private static UiBuildDrawerCatalogItemModel ToBuildDrawerCatalogItem(
            UiBuildDrawerCatalogItemComponent item)
        {
            return new UiBuildDrawerCatalogItemModel(
                item.Visible != 0,
                item.Enabled != 0,
                item.Selected != 0,
                ResolveBuildDrawerSprite(item.ThumbnailSpriteKey),
                item.Title.ToString(),
                item.Role.ToString(),
                item.CreditsText.ToString(),
                item.SuppliesText.ToString(),
                item.TimeText.ToString());
        }

        private static UiBuildDrawerQueueRowModel ToBuildDrawerQueueRow(UiBuildDrawerQueueRowComponent row)
        {
            return new UiBuildDrawerQueueRowModel(
                row.Visible != 0,
                row.ActionEnabled != 0,
                ResolveBuildDrawerSprite(row.ThumbnailSpriteKey),
                row.NumberText.ToString(),
                row.Name.ToString(),
                row.TimeText.ToString());
        }

        private static UiResourceExchangeRecipeCardModel ToResourceExchangeRecipeCard(
            UiResourceExchangeRecipeCardComponent card,
            int slotIndex)
        {
            return new UiResourceExchangeRecipeCardModel(
                card.Visible != 0,
                card.Enabled != 0,
                card.Selected != 0,
                card.Locked != 0,
                card.WarningVisible != 0,
                slotIndex,
                card.RecipeId.ToString(),
                card.Title.ToString(),
                card.InputText.ToString(),
                card.OutputText.ToString(),
                card.DurationText.ToString(),
                card.ReasonText.ToString());
        }

        private static UiResourceExchangeQueueRowModel ToResourceExchangeQueueRow(
            UiResourceExchangeQueueRowComponent row,
            int slotIndex)
        {
            return new UiResourceExchangeQueueRowModel(
                row.Visible != 0,
                row.RushEnabled != 0,
                row.CancelEnabled != 0,
                row.CompletedVisible != 0,
                row.State == UiResourceExchangeQueueState.Blocked,
                row.QueueItemId,
                slotIndex,
                ToResourceExchangeQueueStateKind(row.State),
                row.NumberText.ToString(),
                row.Name.ToString(),
                row.InputText.ToString(),
                row.OutputText.ToString(),
                row.TimeText.ToString(),
                row.PercentText.ToString(),
                row.StateText.ToString(),
                row.Progress01);
        }

        private static UiResourceExchangeQueueStateKind ToResourceExchangeQueueStateKind(
            UiResourceExchangeQueueState state)
        {
            switch (state)
            {
                case UiResourceExchangeQueueState.Pending:
                    return UiResourceExchangeQueueStateKind.Pending;
                case UiResourceExchangeQueueState.InProgress:
                    return UiResourceExchangeQueueStateKind.InProgress;
                case UiResourceExchangeQueueState.Completed:
                    return UiResourceExchangeQueueStateKind.Completed;
                case UiResourceExchangeQueueState.Cancelled:
                    return UiResourceExchangeQueueStateKind.Cancelled;
                case UiResourceExchangeQueueState.Blocked:
                    return UiResourceExchangeQueueStateKind.Blocked;
                default:
                    return UiResourceExchangeQueueStateKind.None;
            }
        }

        private static Sprite ResolveBuildDrawerSprite(FixedString64Bytes spriteKey)
        {
            return UiBuildDrawerReadModelSource.ResolveSprite(spriteKey.ToString());
        }


        }
    }
}
