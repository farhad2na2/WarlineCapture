using Unity.Collections;
using Unity.Entities;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UiShellStateSystem : ISystem
    {
        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            boundaryQuery = state.GetEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
            // RequireForUpdate intentionally omitted: this startup boundary creates the singleton it would require.
            EnsureBoundary(ref state);
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        private void EnsureBoundary(ref SystemState state)
        {
            int boundaryCount = boundaryQuery.CalculateEntityCount();
            if (boundaryCount > 0)
            {
                Entity existingBoundary = ResolveBoundaryEntity(ref state, boundaryCount);
                EnsureShellStateComponent(ref state, existingBoundary);
                EnsureLoadingProgressComponent(ref state, existingBoundary);
                EnsureMatchIntroComponent(ref state, existingBoundary);
                EnsureDiagnosticsOverlayComponent(ref state, existingBoundary);
                EnsureCommanderProfileComponent(ref state, existingBoundary);
                EnsureMainMenuResourcesComponent(ref state, existingBoundary);
                EnsureActivePopupComponent(ref state, existingBoundary);
                EnsureMatchHudPassengerDrawerStateComponent(ref state, existingBoundary);
                EnsureMatchHudSquadTrayStateComponent(ref state, existingBoundary);
                EnsureMatchHudHeaderComponent(ref state, existingBoundary);
                EnsureMatchHudStatusSurfacesComponent(ref state, existingBoundary);
                EnsureMatchHudMinimapComponent(ref state, existingBoundary);
                EnsureBuildDrawerStateComponent(ref state, existingBoundary);
                EnsureBuildDrawerDetailComponent(ref state, existingBoundary);
                EnsureBuildDrawerActiveProductionComponent(ref state, existingBoundary);
                EnsureUiBuildDrawerCatalogBuffer(ref state, existingBoundary);
                EnsureUiBuildDrawerQueueBuffer(ref state, existingBoundary);
                EnsureResourceExchangeStateComponent(ref state, existingBoundary);
                EnsureResourceExchangeDetailComponent(ref state, existingBoundary);
                EnsureUiResourceExchangeRecipeCardBuffer(ref state, existingBoundary);
                EnsureUiResourceExchangeQueueRowBuffer(ref state, existingBoundary);
                EnsureBuildPlacementConfirmationBarComponent(ref state, existingBoundary);
                EnsureUiActionRequestBuffer(ref state, existingBoundary);
                EnsureUiBuildCatalogRequestBuffer(ref state, existingBoundary);
                EnsureUiBuildProductionRequestBuffer(ref state, existingBoundary);
                EnsureUiBuildPrimaryRequestBuffer(ref state, existingBoundary);
                EnsureUiShellArmoryCategoryRequestBuffer(ref state, existingBoundary);
                EnsureUiShellRouteRequestBuffer(ref state, existingBoundary);
                EnsureUiShellRouteHistoryBuffer(ref state, existingBoundary);
                EnsureUiShellPopupRequestBuffer(ref state, existingBoundary);
                EnsureUiShellPresentationCommandBuffer(ref state, existingBoundary);
                EnsureUiShellTransitionCompleteBuffer(ref state, existingBoundary);
                EnsureUiShellLoadingProgressRequestBuffer(ref state, existingBoundary);
                return;
            }

            Entity boundary = state.EntityManager.CreateEntity(typeof(UiShellRootComponent));
            state.EntityManager.SetName(boundary, "UiShellState");
            state.EntityManager.AddComponentData(boundary, new UiShellStateComponent
            {
                CurrentMode = UiShellMode.None,
                ActiveRoute = UIRoute.Splash,
                Phase = UiShellTransitionPhase.Idle,
                TransitionSequenceId = 0,
                IsTransitionRunning = 0
            });
            state.EntityManager.AddComponentData(boundary, new UiShellLoadingProgressComponent
            {
                Progress01 = 0f,
                Status = new FixedString64Bytes("Starting"),
                IsComplete = 0
            });
            state.EntityManager.AddComponentData(boundary, DefaultDiagnosticsOverlay());
            state.EntityManager.AddComponentData(boundary, new MatchIntroTransitionComponent
            {
                State = MatchIntroTransitionStateKind.Inactive,
                Progress01 = 0f,
                InputLocked = 0,
                SequenceId = 0,
                Status = new FixedString64Bytes("Inactive")
            });
            state.EntityManager.AddComponentData(boundary, new UiShellArmoryCategoryComponent
            {
                Category = ArmoryCatalogCategory.Characters
            });
            state.EntityManager.AddComponentData(boundary, DefaultCommanderProfile());
            state.EntityManager.AddComponentData(boundary, DefaultMainMenuResources());
            state.EntityManager.AddComponentData(boundary, new UiShellActivePopupComponent
            {
                PopupKind = UiShellPopupKind.ThreatAlert,
                Visible = 0
            });
            state.EntityManager.AddComponentData(boundary, new UiMatchHudPassengerDrawerStateComponent
            {
                Visible = 0
            });
            state.EntityManager.AddComponentData(boundary, new UiMatchHudSquadTrayStateComponent
            {
                SelectedSlot = MatchHudSquadTraySlot.None
            });
            state.EntityManager.AddComponentData(boundary, DefaultMatchHudHeader());
            state.EntityManager.AddComponentData(boundary, DefaultMatchHudStatusSurfaces());
            state.EntityManager.AddComponentData(boundary, DefaultMatchHudMinimap());
            state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerState());
            state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerDetail());
            state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerActiveProduction());
            state.EntityManager.AddComponentData(boundary, DefaultResourceExchangeState());
            state.EntityManager.AddComponentData(boundary, DefaultResourceExchangeDetail());
            state.EntityManager.AddComponentData(boundary, DefaultBuildPlacementConfirmationBar());
            DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
                state.EntityManager.AddBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
            SeedBuildDrawerCatalog(catalog);
            DynamicBuffer<UiBuildDrawerQueueRowComponent> queue =
                state.EntityManager.AddBuffer<UiBuildDrawerQueueRowComponent>(boundary);
            SeedBuildDrawerQueue(queue);
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> exchangeCards =
                state.EntityManager.AddBuffer<UiResourceExchangeRecipeCardComponent>(boundary);
            SeedResourceExchangeRecipeCards(exchangeCards);
            DynamicBuffer<UiResourceExchangeQueueRowComponent> exchangeQueue =
                state.EntityManager.AddBuffer<UiResourceExchangeQueueRowComponent>(boundary);
            SeedResourceExchangeQueue(exchangeQueue);
            state.EntityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
            state.EntityManager.AddBuffer<UiActionRequestComponent>(boundary);
            state.EntityManager.AddBuffer<UiBuildCatalogRequestComponent>(boundary);
            state.EntityManager.AddBuffer<UiBuildProductionRequestComponent>(boundary);
            state.EntityManager.AddBuffer<UiBuildPrimaryRequestComponent>(boundary);
            state.EntityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
            state.EntityManager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
            state.EntityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
            state.EntityManager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
            state.EntityManager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);
            state.EntityManager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        }

        private Entity ResolveBoundaryEntity(ref SystemState state, int boundaryCount)
        {
            if (boundaryCount == 1)
                return boundaryQuery.GetSingletonEntity();

            using NativeArray<Entity> boundaries = boundaryQuery.ToEntityArray(Allocator.Temp);
            Entity primary = boundaries[0];
            for (int i = 1; i < boundaries.Length; i++)
                state.EntityManager.DestroyEntity(boundaries[i]);

            return primary;
        }

        private static void EnsureShellStateComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiShellStateComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, new UiShellStateComponent
            {
                CurrentMode = UiShellMode.None,
                ActiveRoute = UIRoute.Splash,
                Phase = UiShellTransitionPhase.Idle,
                TransitionSequenceId = 0,
                IsTransitionRunning = 0
            });
        }

        private static void EnsureLoadingProgressComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiShellLoadingProgressComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, new UiShellLoadingProgressComponent
            {
                Progress01 = 0f,
                Status = new FixedString64Bytes("Starting"),
                IsComplete = 0
            });
        }

        private static void EnsureMatchIntroComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<MatchIntroTransitionComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, new MatchIntroTransitionComponent
            {
                State = MatchIntroTransitionStateKind.Inactive,
                Progress01 = 0f,
                InputLocked = 0,
                SequenceId = 0,
                Status = new FixedString64Bytes("Inactive")
            });
        }

        private static void EnsureDiagnosticsOverlayComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiDiagnosticsOverlayComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultDiagnosticsOverlay());
        }

        private static void EnsureCommanderProfileComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiShellCommanderProfileComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultCommanderProfile());
        }

        private static void EnsureMainMenuResourcesComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiShellMainMenuResourcesComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultMainMenuResources());
        }

        private static void EnsureActivePopupComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiShellActivePopupComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, new UiShellActivePopupComponent
            {
                PopupKind = UiShellPopupKind.ThreatAlert,
                Visible = 0
            });
        }

        private static void EnsureMatchHudPassengerDrawerStateComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiMatchHudPassengerDrawerStateComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, new UiMatchHudPassengerDrawerStateComponent
            {
                Visible = 0
            });
        }

        private static void EnsureMatchHudSquadTrayStateComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiMatchHudSquadTrayStateComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, new UiMatchHudSquadTrayStateComponent
            {
                SelectedSlot = MatchHudSquadTraySlot.None
            });
        }

        private static void EnsureMatchHudHeaderComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiMatchHudHeaderComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultMatchHudHeader());
        }

        private static void EnsureMatchHudStatusSurfacesComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiMatchHudStatusSurfacesComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultMatchHudStatusSurfaces());
        }

        private static void EnsureMatchHudMinimapComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiMatchHudMinimapComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultMatchHudMinimap());
        }

        private static void EnsureBuildDrawerDetailComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiBuildDrawerDetailComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerDetail());
        }

        private static void EnsureBuildDrawerStateComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiBuildDrawerStateComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerState());
        }

        private static void EnsureBuildDrawerActiveProductionComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiBuildDrawerActiveProductionComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerActiveProduction());
        }

        private static void EnsureUiBuildDrawerCatalogBuffer(ref SystemState state, Entity boundary)
        {
            DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog;
            if (state.EntityManager.HasBuffer<UiBuildDrawerCatalogItemComponent>(boundary))
            {
                catalog = state.EntityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
            }
            else
            {
                catalog = state.EntityManager.AddBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
            }

            if (catalog.Length == 0)
                SeedBuildDrawerCatalog(catalog);
        }

        private static void EnsureUiBuildDrawerQueueBuffer(ref SystemState state, Entity boundary)
        {
            DynamicBuffer<UiBuildDrawerQueueRowComponent> queue;
            if (state.EntityManager.HasBuffer<UiBuildDrawerQueueRowComponent>(boundary))
            {
                queue = state.EntityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary);
            }
            else
            {
                queue = state.EntityManager.AddBuffer<UiBuildDrawerQueueRowComponent>(boundary);
            }

            if (queue.Length == 0)
                SeedBuildDrawerQueue(queue);
        }

        private static void EnsureResourceExchangeStateComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiResourceExchangeStateComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultResourceExchangeState());
        }

        private static void EnsureResourceExchangeDetailComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiResourceExchangeDetailComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultResourceExchangeDetail());
        }

        private static void EnsureUiResourceExchangeRecipeCardBuffer(ref SystemState state, Entity boundary)
        {
            DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards;
            if (state.EntityManager.HasBuffer<UiResourceExchangeRecipeCardComponent>(boundary))
            {
                cards = state.EntityManager.GetBuffer<UiResourceExchangeRecipeCardComponent>(boundary);
            }
            else
            {
                cards = state.EntityManager.AddBuffer<UiResourceExchangeRecipeCardComponent>(boundary);
            }

            if (cards.Length == 0)
                SeedResourceExchangeRecipeCards(cards);
        }

        private static void EnsureUiResourceExchangeQueueRowBuffer(ref SystemState state, Entity boundary)
        {
            DynamicBuffer<UiResourceExchangeQueueRowComponent> queue;
            if (state.EntityManager.HasBuffer<UiResourceExchangeQueueRowComponent>(boundary))
            {
                queue = state.EntityManager.GetBuffer<UiResourceExchangeQueueRowComponent>(boundary);
            }
            else
            {
                queue = state.EntityManager.AddBuffer<UiResourceExchangeQueueRowComponent>(boundary);
            }

            if (queue.Length == 0)
                SeedResourceExchangeQueue(queue);
        }

        private static void EnsureBuildPlacementConfirmationBarComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiBuildPlacementConfirmationBarComponent>(boundary))
                return;

            state.EntityManager.AddComponentData(boundary, DefaultBuildPlacementConfirmationBar());
        }

        private static void EnsureUiActionRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiActionRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiActionRequestComponent>(boundary);
        }

        private static void EnsureUiBuildCatalogRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiBuildCatalogRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiBuildCatalogRequestComponent>(boundary);
        }

        private static void EnsureUiBuildProductionRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiBuildProductionRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiBuildProductionRequestComponent>(boundary);
        }

        private static void EnsureUiBuildPrimaryRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiBuildPrimaryRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiBuildPrimaryRequestComponent>(boundary);
        }

        private static void EnsureUiShellArmoryCategoryRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiShellArmoryCategoryRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        }

        private static void EnsureUiShellRouteRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiShellRouteRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        }

        private static void EnsureUiShellRouteHistoryBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiShellRouteHistoryComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
        }

        private static void EnsureUiShellPopupRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiShellPopupRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        }

        private static void EnsureUiShellPresentationCommandBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiShellPresentationCommandComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
        }

        private static void EnsureUiShellTransitionCompleteBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiShellTransitionCompleteComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);
        }

        private static void EnsureUiShellLoadingProgressRequestBuffer(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasBuffer<UiShellLoadingProgressRequestComponent>(boundary))
                return;

            state.EntityManager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        }

        private static UiShellCommanderProfileComponent DefaultCommanderProfile()
        {
            return new UiShellCommanderProfileComponent
            {
                Name = new FixedString64Bytes("COL. ALEX MORGAN"),
                Subtitle = new FixedString64Bytes("VICTORY IS PLANNED"),
                PortraitClass = new FixedString64Bytes("commander-portrait-default")
            };
        }

        private static UiShellMainMenuResourcesComponent DefaultMainMenuResources()
        {
            return new UiShellMainMenuResourcesComponent
            {
                CreditsText = new FixedString32Bytes("12,450"),
                SuppliesText = new FixedString32Bytes("1,280"),
                CommandText = new FixedString32Bytes("78/100")
            };
        }

        private static UiBuildDrawerDetailComponent DefaultBuildDrawerDetail()
        {
            return new UiBuildDrawerDetailComponent
            {
                Name = new FixedString64Bytes("GUARD TOWER"),
                Role = new FixedString32Bytes("DEFENSE"),
                Description = new FixedString128Bytes("Provides overwatch and expands line of sight."),
                FootprintText = new FixedString32Bytes("3 x 3"),
                RequirementsText = new FixedString64Bytes("HQ LEVEL 1"),
                PlacementText = new FixedString64Bytes("VALID GROUND"),
                ProductionTimeText = new FixedString32Bytes("00:18"),
                CreditsCostText = new FixedString32Bytes("420"),
                SuppliesCostText = new FixedString32Bytes("80"),
                InstructionText = new FixedString128Bytes("Tap a valid footprint to place the structure."),
                ProductionTitle = new FixedString32Bytes("QUEUE"),
                ProductionCountText = new FixedString32Bytes("2/3"),
                BuildEnabled = 1,
                RushEnabled = 1,
                ClearEnabled = 1,
                NoProductionVisible = 0
            };
        }

        private static UiBuildDrawerStateComponent DefaultBuildDrawerState()
        {
            return new UiBuildDrawerStateComponent
            {
                ActiveCategory = BuildDrawerCategory.Buildings,
                SelectedCatalogSlot = 0,
                BuildingsCount = 2,
                VehiclesCount = 0,
                AircraftsCount = 0,
                SoldiersCount = 0
            };
        }

        private static UiBuildDrawerActiveProductionComponent DefaultBuildDrawerActiveProduction()
        {
            return new UiBuildDrawerActiveProductionComponent
            {
                Visible = 1,
                CancelEnabled = 1,
                Name = new FixedString64Bytes("BARRACKS"),
                PercentText = new FixedString32Bytes("65%"),
                Progress01 = 0.65f
            };
        }

        private static UiBuildPlacementConfirmationBarComponent DefaultBuildPlacementConfirmationBar()
        {
            return new UiBuildPlacementConfirmationBarComponent
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
            };
        }

        private static UiResourceExchangeStateComponent DefaultResourceExchangeState()
        {
            return new UiResourceExchangeStateComponent
            {
                ActiveTab = UiResourceExchangeTab.Export,
                SelectedRecipeSlot = 0,
                QueueCapacityText = new FixedString32Bytes("0/0"),
                CreditsText = new FixedString32Bytes("0"),
                MaterialsText = new FixedString32Bytes("0"),
                OilText = new FixedString32Bytes("0"),
                FuelText = new FixedString32Bytes("0"),
                RushTicketsText = new FixedString32Bytes("0"),
                ExchangeEnabled = 0,
                RushAllEnabled = 0,
                ClearCompletedEnabled = 0
            };
        }

        private static UiResourceExchangeDetailComponent DefaultResourceExchangeDetail()
        {
            return new UiResourceExchangeDetailComponent
            {
                Name = new FixedString64Bytes("RESOURCE EXCHANGE"),
                RouteText = new FixedString32Bytes("EXPORT"),
                RateText = new FixedString64Bytes("No route selected."),
                AmountText = new FixedString32Bytes("0"),
                InputCostText = new FixedString32Bytes("0"),
                OutputPreviewText = new FixedString32Bytes("0"),
                DurationText = new FixedString32Bytes("00:00"),
                RequirementsText = new FixedString64Bytes("Exchange unavailable."),
                InstructionText = new FixedString128Bytes("Select an exchange route."),
                ConfirmEnabled = 0,
                WarningVisible = 0
            };
        }

        private static void SeedBuildDrawerCatalog(DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog)
        {
            catalog.Add(new UiBuildDrawerCatalogItemComponent
            {
                Visible = 1,
                Enabled = 1,
                Selected = 1,
                Category = BuildDrawerCategory.Buildings,
                Title = new FixedString64Bytes("GUARD TOWER"),
                Role = new FixedString32Bytes("DEFENSE"),
                CreditsText = new FixedString32Bytes("420"),
                SuppliesText = new FixedString32Bytes("80"),
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
                CreditsText = new FixedString32Bytes("900"),
                SuppliesText = new FixedString32Bytes("120"),
                TimeText = new FixedString32Bytes("00:30")
            });
        }

        private static void SeedBuildDrawerQueue(DynamicBuffer<UiBuildDrawerQueueRowComponent> queue)
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

        private static void SeedResourceExchangeRecipeCards(DynamicBuffer<UiResourceExchangeRecipeCardComponent> cards)
        {
            cards.Add(new UiResourceExchangeRecipeCardComponent
            {
                Visible = 1,
                Enabled = 0,
                Selected = 1,
                Locked = 1,
                Tab = UiResourceExchangeTab.Export,
                Title = new FixedString64Bytes("EXPORT ROUTE"),
                InputText = new FixedString32Bytes("0"),
                OutputText = new FixedString32Bytes("0"),
                DurationText = new FixedString32Bytes("00:00"),
                ReasonText = new FixedString64Bytes("Exchange unavailable")
            });
        }

        private static void SeedResourceExchangeQueue(DynamicBuffer<UiResourceExchangeQueueRowComponent> queue)
        {
            queue.Add(new UiResourceExchangeQueueRowComponent
            {
                Visible = 0,
                RushEnabled = 0,
                CancelEnabled = 0,
                CompletedVisible = 0,
                NumberText = new FixedString32Bytes("1"),
                Name = new FixedString64Bytes("NO ACTIVE EXCHANGE"),
                InputText = new FixedString32Bytes("0"),
                OutputText = new FixedString32Bytes("0"),
                TimeText = new FixedString32Bytes("00:00"),
                PercentText = new FixedString32Bytes("0%"),
                StateText = new FixedString64Bytes("IDLE")
            });
        }

        private static UiMatchHudHeaderComponent DefaultMatchHudHeader()
        {
            return new UiMatchHudHeaderComponent
            {
                OrderText = new FixedString32Bytes("MOVE ORDER"),
                SquadText = new FixedString32Bytes("RIFLE SQUAD"),
                CreditsText = new FixedString32Bytes("187,540"),
                FuelText = new FixedString32Bytes("2,860"),
                SupplyText = new FixedString32Bytes("92/120"),
                CivilianRiskText = new FixedString32Bytes("MED")
            };
        }

        private static UiMatchHudStatusSurfacesComponent DefaultMatchHudStatusSurfaces()
        {
            return new UiMatchHudStatusSurfacesComponent
            {
                ObjectivesTitle = new FixedString32Bytes("OBJECTIVES"),
                Objective0Text = new FixedString64Bytes("Neutralize hostile patrol"),
                Objective1Text = new FixedString64Bytes("Protect civilians"),
                Objective2Text = new FixedString64Bytes("Keep losses low"),
                Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked,
                Objective1IconKind = UiMatchHudObjectiveIconKind.Checked,
                Objective2IconKind = UiMatchHudObjectiveIconKind.Star,
                ElapsedText = new FixedString32Bytes("ELAPSED: 07:42"),
                ThreatVisible = 1,
                ThreatTitle = new FixedString64Bytes("HOSTILE CELL SPOTTED"),
                ThreatSubtitle = new FixedString64Bytes("Market quarter, 140m"),
                JumpEnabled = 1,
                FeedbackVisible = 1,
                FeedbackText = new FixedString64Bytes(GameText.Get("match.feedback.blocked_civilian_zone", "Blocked: civilian zone")),
                BoardAllVisible = 1,
                BoardAllEnabled = 1,
                CancelVisible = 1,
                CancelEnabled = 1
            };
        }

        private static UiMatchHudMinimapComponent DefaultMatchHudMinimap()
        {
            return new UiMatchHudMinimapComponent
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
            };
        }

        private static UiDiagnosticsOverlayComponent DefaultDiagnosticsOverlay()
        {
            return new UiDiagnosticsOverlayComponent
            {
                Fps = 0,
                LogVisible = 0,
                LogText = new FixedString4096Bytes("Runtime log ready.")
            };
        }
    }
}
