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
        private static void EnsureDiagnosticsOverlayState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiDiagnosticsOverlayComponent>(boundary))
                return;

            entityManager.AddComponentData(boundary, new UiDiagnosticsOverlayComponent
            {
                Fps = 0,
                LogVisible = 0,
                LogText = new FixedString4096Bytes("Runtime log ready.")
            });
        }

        private static string GetDiagnosticsLogText(FixedString4096Bytes logText)
        {
            if (hasCachedDiagnosticsLogText && cachedDiagnosticsLogFixedText.Equals(logText))
                return cachedDiagnosticsLogText;

            cachedDiagnosticsLogFixedText = logText;
            cachedDiagnosticsLogText = logText.ToString();
            hasCachedDiagnosticsLogText = true;
            return cachedDiagnosticsLogText;
        }

        private static void EnsureBuildDrawerState(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiBuildDrawerStateComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiBuildDrawerStateComponent
                {
                    ActiveCategory = BuildDrawerCategory.Buildings,
                    SelectedCatalogSlot = 0,
                    BuildingsCount = 2
                });
            }

            if (!entityManager.HasComponent<UiBuildDrawerDetailComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiBuildDrawerDetailComponent
                {
                    Name = new FixedString64Bytes("GUARD TOWER"),
                    Role = new FixedString32Bytes("DEFENSE"),
                    Description = new FixedString128Bytes("Provides overwatch and expands line of sight."),
                    FootprintText = new FixedString32Bytes("3 x 3"),
                    RequirementsText = new FixedString64Bytes("HQ LEVEL 1"),
                    PlacementText = new FixedString64Bytes("VALID GROUND"),
                    ProductionTimeText = new FixedString32Bytes("00:18"),
                    MaterialsCostText = new FixedString32Bytes("80"),
                    FuelCostText = default,
                    InstructionText = new FixedString128Bytes("Tap a valid footprint to place the structure."),
                    ProductionTitle = new FixedString32Bytes("QUEUE"),
                    ProductionCountText = new FixedString32Bytes("2/3"),
                    BuildEnabled = 1,
                    RushEnabled = 1,
                    ClearEnabled = 1,
                    NoProductionVisible = 0
                });
            }

            if (!entityManager.HasComponent<UiBuildDrawerActiveProductionComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiBuildDrawerActiveProductionComponent
                {
                    Visible = 1,
                    CancelEnabled = 1,
                    Name = new FixedString64Bytes("BARRACKS"),
                    PercentText = new FixedString32Bytes("65%"),
                    Progress01 = 0.65f
                });
            }

            DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog;
            if (entityManager.HasBuffer<UiBuildDrawerCatalogItemComponent>(boundary))
            {
                catalog = entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
            }
            else
            {
                catalog = entityManager.AddBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
            }

            if (catalog.Length == 0)
            {
                catalog.Add(new UiBuildDrawerCatalogItemComponent
                {
                    Visible = 1,
                    Enabled = 1,
                    Selected = 1,
                    Category = BuildDrawerCategory.Buildings,
                    Title = new FixedString64Bytes("GUARD TOWER"),
                    Role = new FixedString32Bytes("DEFENSE"),
                    MaterialsText = new FixedString32Bytes("80"),
                    FuelText = default,
                    TimeText = new FixedString32Bytes("00:18")
                });
                catalog.Add(new UiBuildDrawerCatalogItemComponent
                {
                    Visible = 1,
                    Enabled = 0,
                    Selected = 0,
                    Category = BuildDrawerCategory.Buildings,
                    Title = new FixedString64Bytes("BARRACKS"),
                    Role = new FixedString32Bytes("INFANTRY"),
                    MaterialsText = new FixedString32Bytes("120"),
                    FuelText = default,
                    TimeText = new FixedString32Bytes("00:30")
                });
            }

            DynamicBuffer<UiBuildDrawerQueueRowComponent> queue;
            if (entityManager.HasBuffer<UiBuildDrawerQueueRowComponent>(boundary))
            {
                queue = entityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary);
            }
            else
            {
                queue = entityManager.AddBuffer<UiBuildDrawerQueueRowComponent>(boundary);
            }

            if (queue.Length == 0)
            {
                queue.Add(new UiBuildDrawerQueueRowComponent
                {
                    Visible = 1,
                    ActionEnabled = 1,
                    NumberText = new FixedString32Bytes("1"),
                    Name = new FixedString64Bytes("BARRACKS"),
                    TimeText = new FixedString32Bytes("00:14")
                });
            }
        }

        private static void EnsureResourceExchangeUiState(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiResourceExchangeStateComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiResourceExchangeStateComponent
                {
                    ActiveTab = UiResourceExchangeTab.Export,
                    SelectedRecipeSlot = 0,
                    QueueCapacityText = new FixedString32Bytes("0/0"),
                    MaterialsText = new FixedString32Bytes("0"),
                    OilText = new FixedString32Bytes("0"),
                    FuelText = new FixedString32Bytes("0"),
                    RushTicketsText = new FixedString32Bytes("0")
                });
            }

            if (!entityManager.HasComponent<UiResourceExchangeDetailComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiResourceExchangeDetailComponent
                {
                    Name = new FixedString64Bytes("RESOURCE EXCHANGE"),
                    RouteText = new FixedString32Bytes("EXPORT"),
                    RequirementsText = new FixedString64Bytes("Exchange unavailable."),
                    InstructionText = new FixedString128Bytes("Resource Exchange is not enabled for this scenario.")
                });
            }

            if (!entityManager.HasBuffer<UiResourceExchangeRecipeCardComponent>(boundary))
                entityManager.AddBuffer<UiResourceExchangeRecipeCardComponent>(boundary);

            if (!entityManager.HasBuffer<UiResourceExchangeQueueRowComponent>(boundary))
                entityManager.AddBuffer<UiResourceExchangeQueueRowComponent>(boundary);
        }

        private static void EnsureBuildPlacementConfirmationBarState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiBuildPlacementConfirmationBarComponent>(boundary))
                return;

            entityManager.AddComponentData(boundary, new UiBuildPlacementConfirmationBarComponent
            {
                Visible = 0,
                CanConfirm = 0,
                CanCancel = 0,
                CanRotate = 0,
                Title = new FixedString64Bytes(GameText.Get("build.placement.title.default", "PLACE BUILDING")),
                Status = new FixedString64Bytes(GameText.Get("build.placement.status.valid_ground", "VALID GROUND")),
                CostText = new FixedString32Bytes("2,000"),
                DurationText = new FixedString32Bytes("00:30"),
                InstructionText = new FixedString128Bytes(GameText.Get("build.placement.instruction.confirm", "DRAG TO POSITION, CONFIRM TO BUILD"))
            });
        }

        private static void EnsureCommanderProfileState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiShellCommanderProfileComponent>(boundary))
                return;

            entityManager.AddComponentData(boundary, new UiShellCommanderProfileComponent
            {
                Name = new FixedString64Bytes("COL. ALEX MORGAN"),
                Subtitle = new FixedString64Bytes("VICTORY IS PLANNED"),
                PortraitClass = new FixedString64Bytes("commander-portrait-default")
            });
        }

        private static void EnsureMainMenuResourcesState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiShellMainMenuResourcesComponent>(boundary))
                return;

            entityManager.AddComponentData(boundary, new UiShellMainMenuResourcesComponent
            {
                CreditsText = new FixedString32Bytes("12,450"),
                CommandText = new FixedString32Bytes("78/100")
            });
        }

        private static void EnsureMatchHudHeaderState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiMatchHudHeaderComponent>(boundary))
                return;

            entityManager.AddComponentData(boundary, new UiMatchHudHeaderComponent
            {
                OrderText = new FixedString32Bytes("MOVE ORDER"),
                SquadText = new FixedString32Bytes("RIFLE SQUAD"),
                FuelText = new FixedString32Bytes("2,860"),
                MaterialsText = new FixedString32Bytes("0/0"),
                CivilianRiskText = new FixedString32Bytes("MED")
            });
        }

        private static void EnsureMatchHudStatusSurfacesState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiMatchHudStatusSurfacesComponent>(boundary))
                return;

            entityManager.AddComponentData(boundary, new UiMatchHudStatusSurfacesComponent
            {
                ObjectivesTitle = new FixedString32Bytes("OBJECTIVES"),
                Objective0Text = default,
                Objective1Text = default,
                Objective2Text = default,
                Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked,
                Objective1IconKind = UiMatchHudObjectiveIconKind.Unchecked,
                Objective2IconKind = UiMatchHudObjectiveIconKind.Unchecked,
                ElapsedText = default,
                ThreatVisible = 0,
                ThreatTitle = default,
                ThreatSubtitle = default,
                ThreatAudioEventId = default,
                JumpEnabled = 0,
                FeedbackVisible = 0,
                FeedbackText = default,
                FeedbackAudioEventId = default,
                BoardAllVisible = 1,
                BoardAllEnabled = 1,
                CancelVisible = 1,
                CancelEnabled = 1
            });
        }

        private static void EnsureMatchHudMinimapState(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiMatchHudMinimapComponent>(boundary))
                return;

            entityManager.AddComponentData(boundary, new UiMatchHudMinimapComponent
            {
                ViewportLeftPercent = 26f,
                ViewportTopPercent = 34f,
                ViewportWidthPercent = 40f,
                ViewportHeightPercent = 34f,
                ZoomInEnabled = 1,
                ZoomOutEnabled = 1,
                FocusEnabled = 1,
                FriendlyAVisible = 1,
                FriendlyALeftPercent = 47f,
                FriendlyATopPercent = 57f,
                FriendlyBVisible = 1,
                FriendlyBLeftPercent = 58f,
                FriendlyBTopPercent = 63f,
                HostileAVisible = 1,
                HostileALeftPercent = 55f,
                HostileATopPercent = 37f,
                CivilianVisible = 1,
                CivilianLeftPercent = 75f,
                CivilianTopPercent = 52f
            });
        }

        }
    }
}
