using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct UiShellBoundarySystem : ISystem
{
    private EntityQuery boundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        UiShellEcsGateway.RegisterAsRuntimeGateway();
        boundaryQuery = state.GetEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!boundaryQuery.IsEmptyIgnoreFilter)
        {
            Entity existingBoundary = boundaryQuery.GetSingletonEntity();
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
            EnsureBuildDrawerDetailComponent(ref state, existingBoundary);
            EnsureBuildDrawerActiveProductionComponent(ref state, existingBoundary);
            EnsureUiBuildDrawerCatalogBuffer(ref state, existingBoundary);
            EnsureUiBuildDrawerQueueBuffer(ref state, existingBoundary);
            EnsureBuildPlacementConfirmationBarComponent(ref state, existingBoundary);
            EnsureUiActionRequestBuffer(ref state, existingBoundary);
            EnsureUiBuildCatalogRequestBuffer(ref state, existingBoundary);
            EnsureUiBuildProductionRequestBuffer(ref state, existingBoundary);
            EnsureUiBuildPrimaryRequestBuffer(ref state, existingBoundary);
            return;
        }

        Entity boundary = state.EntityManager.CreateEntity(typeof(UiShellBoundaryComponent));
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
        state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerDetail());
        state.EntityManager.AddComponentData(boundary, DefaultBuildDrawerActiveProduction());
        state.EntityManager.AddComponentData(boundary, DefaultBuildPlacementConfirmationBar());
        DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
            state.EntityManager.AddBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
        SeedBuildDrawerCatalog(catalog);
        DynamicBuffer<UiBuildDrawerQueueRowComponent> queue =
            state.EntityManager.AddBuffer<UiBuildDrawerQueueRowComponent>(boundary);
        SeedBuildDrawerQueue(queue);
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
            Title = new FixedString64Bytes("PLACE BUILDING"),
            Status = new FixedString64Bytes("VALID GROUND"),
            CostText = new FixedString32Bytes("2,000"),
            DurationText = new FixedString32Bytes("00:30"),
            InstructionText = new FixedString128Bytes("DRAG TO POSITION, CONFIRM TO BUILD")
        };
    }

    private static void SeedBuildDrawerCatalog(DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog)
    {
        catalog.Add(new UiBuildDrawerCatalogItemComponent
        {
            Visible = 1,
            Enabled = 1,
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
            FeedbackText = new FixedString64Bytes("Blocked: civilian zone"),
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
