using Game.Configs;
using Game.UI.Contracts;
using Game.Tactical.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private readonly Vector3[] watchFront = new Vector3[4];
        private readonly float[] watchFrontDistances = new float[4];
        private SkirmishMatchView watchSkirmish;
        private Vector3 watchThreatPosition;
        private bool watchThreatTracked, watchFlankReached;
        private int watchScenarioIndex;
        private MatchHudSquadTrayView watchSquads;
        private BuildDrawerView watchBuild;
        private BuildDrawerCatalogRuntimeView watchCatalog;
        private BuildPlacementConfirmationBarView watchPlacement;
        private MatchHudMinimapView watchMap;
        private MatchHudFullMapPopupView watchFullMap;
        private MatchHudSelectionPanelView watchSelection;
        private AriaTouchTarget ObserveWatchButton(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return default;
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(button), rect.TransformPoint(rect.rect.center));
            int id = button.GetEntityId().GetHashCode();
            return new AriaTouchTarget { Id = id, Position = point, Available = WatchTargetIsReachable(point, id, false) };
        }
        private AriaTouchTarget ObserveCatalogTarget(BuildDrawerCategory category, string prefabKey)
        {
            return ObserveScrollableButton(watchCatalog?.ResolveCatalogTarget(category, prefabKey),
                CatalogItemId(category, prefabKey));
        }
        private static int CatalogItemId(BuildDrawerCategory category, string prefabKey)
        {
            // Stable across pooled/recreated catalog buttons in this match.
            // FNV-1a over the typed category and UTF-16 key bytes; never return zero.
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (byte)category) * 16777619u;
                if (prefabKey != null)
                    foreach (char c in prefabKey)
                    {
                        hash = (hash ^ (byte)c) * 16777619u;
                        hash = (hash ^ (byte)(c >> 8)) * 16777619u;
                    }
                int id = (int)(hash & 0x7fffffffu);
                return id == 0 ? 1 : id;
            }
        }
        private AriaTouchTarget ObserveScrollableButton(Button button, int logicalItemId)
        {
            var target = ObserveWatchButton(button);
            if (target.Available || button == null || !button.IsActive() || !button.IsInteractable()) return target;
            var scroll = button.GetComponentInParent<ScrollRect>();
            if (scroll == null || !scroll.vertical || scroll.viewport == null) return default;
            var viewport = scroll.viewport;
            var rect = (RectTransform)button.transform;
            var local = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            if (viewport.rect.Contains(local)) return default; // Occluded, not off-screen.
            bool up = local.y < viewport.rect.yMin;
            var bounds = viewport.rect;
            var camera = ResolveEventCamera(button);
            var from = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(
                new Vector3(bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .25f : .75f), 0)));
            var to = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(
                new Vector3(bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .75f : .25f), 0)));
            if (!Screen.safeArea.Contains(from) || !Screen.safeArea.Contains(to) || watchRaycast == null) return default;
            watchRaycast.position = from; watchHits.Clear();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(watchRaycast, watchHits);
            if (watchHits.Count == 0 || watchHits[0].gameObject.GetComponentInParent<ScrollRect>() != scroll) return default;
            // Measure the actual rendered catalog movement toward this button. A long
            // catalog can need several successful drags while the scroll actuator ID
            // remains unchanged; the logical catalog key distinguishes those tasks.
            float remaining = up ? bounds.yMin - local.y : local.y - bounds.yMax;
            float distanceViewports = Mathf.Max(0f, remaining) / Mathf.Max(1f, bounds.height);
            return new AriaTouchTarget { Id = scroll.GetEntityId().GetHashCode() ^ (up ? 1 : 2),
                Position = from, DragEnd = to, Drag = true, Available = true,
                ScrollTargetId = logicalItemId,
                ScrollDistanceViewports = distanceViewports };
        }
#if UNITY_EDITOR
        private static void RecordEnemyBaseDiagnostic(in AriaSkirmishObservation view, SkirmishMatchView watchSkirmish)
        {
                bool present = watchSkirmish != null;
                bool markerVisible = present && watchSkirmish.EnemyBaseMarkerVisible;
                bool targetVisible = present && watchSkirmish.EnemyBaseTargetVisible;
                SkirmishMatchView.RecordNativeEnemyBaseTouchEvidence(new SkirmishMatchView.NativeEnemyBaseTouchEvidence
                {
                    collected = true, frame = view.Frame, viewPresent = present,
                    viewIdentity = present ? watchSkirmish.GetEntityId().GetHashCode() : 0,
                    markerVisible = markerVisible, targetVisible = targetVisible,
                    reachabilityEvaluated = markerVisible && targetVisible,
                    reachable = view.EnemyBase.Available, target = view.EnemyBase
                });
        }
#endif

        private void ObserveSkirmishWatch(UiSkirmishModel model)
        {
            AriaPlayModel aria = UiShellRuntimeGateway.ReadAriaPlay();
            if (!aria.Active)
            {
                ResetConvoyServiceJourney();
                watchThreatTracked = false;
                watchFlankReached = false;
                UiShellRuntimeGateway.PublishAriaSkirmishObservation(default);
                return;
            }
            watchScenarioIndex = model.ScenarioIndex;
            if (watchSkirmish == null) watchSkirmish = Object.FindAnyObjectByType<SkirmishMatchView>();
            if (watchSquads == null) watchSquads = Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            if (watchBuild == null) watchBuild = Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            var view = new AriaSkirmishObservation { Active = true, Finished = model.Finished || model.StartupFailed,
                Frame = Time.frameCount, Time = Time.unscaledTime, SelectedSlot = -1, DrawerOpen = watchBuild != null && watchBuild.IsOpen,
                ExpandedSession = model.Expanded, PlayerDesignatedAlive = model.PlayerDesignatedAlive,
                EnemyDesignatedAlive = model.EnemyDesignatedAlive };
            if (model.Expanded && UiShellRuntimeGateway.TryReadExpandedSquadPage(out UiExpandedSquadPage page))
            {
                view.ExpandedAssaultMask = page.AssaultMask;
                view.ExpandedSelectedMask = page.SelectedMask;
                view.ExpandedStructureMask = page.StructureMask;
                view.ExpandedAttackOrderMask = page.AttackOrderMask;
                view.ExpandedBaseAssault=page.BaseAssault;
                view.ExpandedGroup0=page.Group0;view.ExpandedGroup1=page.Group1;view.ExpandedGroup2=page.Group2;
                view.ExpandedGroup3=page.Group3;view.ExpandedGroup4=page.Group4;view.PrecisionAttack=page.PrecisionAttack;
                view.ExpandedAirMask = page.AirMask;
                view.ExpandedCanPreviousPage = page.PreviousPage;
                view.ExpandedCanNextPage = page.NextPage;
                view.ExpandedPageIndex = page.PageIndex;
                view.ExpandedPageCount = page.PageCount;
                view.ExpandedTotalGroups = page.TotalGroups;
                view.ExpandedSelectedGroups = page.SelectedGroups;
                view.ExpandedSelectedOffPage = page.SelectedOffPage;
                view.ExpandedRosterRevision = page.RosterRevision;
                view.PreviousSquadPage = MatchHudSquadTouchNavigation.PageTarget(watchSquads, false);
                view.NextSquadPage = MatchHudSquadTouchNavigation.PageTarget(watchSquads, true);
                view.SelectedSquadToToggle = MatchHudSquadTouchNavigation.SelectedCardTarget(watchSquads);
            }
            if (UiShellRuntimeGateway.TryReadMatchHudSquadTray(out var squads))
            {
                view.SelectedSlot = watchSquads != null ? (int)watchSquads.VisibleSelectedSlot - 1 : -1;
                for (int i = 0; i < 5; i++)
                {
                    var card = squads.GetCard(i);
                    if (!card.Visible || card.Health01 <= 0) continue;
                    view.AvailableSquads |= 1 << i;
                    view.ForceHealth += card.Health01;
                }
            }
            if (UiShellRuntimeGateway.TryReadMatchHudSelection(out var selected))
            {
                view.SelectionVisible = selected.Visible;
                view.SelectedAircraft = selected.Visible && selected.IsAircraft;
                view.SelectedCount = ReadDisplayedCount(selected.Title);
                if (view.SelectedCount == 0) view.SelectedCount = ReadDisplayedCount(selected.Subtitle);
            }
            if (watchSelection == null) watchSelection = Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
            if (view.SelectedAircraft)
            {
                view.ReturnAircraft = ObserveWatchButton(watchSelection?.PresentedReturnButton);
                // The current HUD presents selection actions in the Commands
                // wheel. Opening it is a separate touch from choosing Return.
                var wheel = _commandControlsView?.CommandWheelPanel;
                if (!view.ReturnAircraft.Available && wheel != null)
                {
                    if (wheel.IsOpen) view.ReturnAircraft = ObserveWatchButton(wheel.NextReturnButton);
                    else view.OpenAircraftActions = ObserveWatchButton(wheel.OpenButton);
                }
            }
            if (UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) { view.AttackMode = command.ActiveCommandMode == TacticalCommandMode.Attack; view.SelectionMode = command.ActiveCommandMode == TacticalCommandMode.Select; }
            view.Infantry = model.InfantryCount;
            view.PlayerHealth = model.PlayerHealth; view.EnemyHealth = model.EnemyHealth;
            view.Squad0 = MatchHudSquadTouchNavigation.SlotTarget(watchSquads, 0);
            view.Squad1 = MatchHudSquadTouchNavigation.SlotTarget(watchSquads, 1);
            view.Squad2 = MatchHudSquadTouchNavigation.SlotTarget(watchSquads, 2);
            view.Squad3 = MatchHudSquadTouchNavigation.SlotTarget(watchSquads, 3);
            view.Squad4 = MatchHudSquadTouchNavigation.SlotTarget(watchSquads, 4);
            view.FocusPlayer = ObserveWatchButton(watchSkirmish?.PlayerFocusButton);
            view.Select = ObserveWatchButton(_commandControlsView?.SelectButton);
            view.FocusEnemy = ObserveWatchButton(watchSkirmish?.EnemyFocusButton);
            view.Attack = ObserveWatchButton(_commandControlsView?.AttackButton);
            view.Hold = ObserveWatchButton(_commandControlsView?.HoldButton);
            view.Recruit = ObserveWatchButton(_highlightPresentationSystem.ResolveProductionTutorialControl(out _));
            view.CloseDrawer = ObserveWatchButton(watchBuild?.CloseButton);
            if (model.Expanded)
            {
                view.OwnMaterials = model.OwnMaterials;
                view.CanAffordRifle = model.CanAffordRifle;
                view.CombinedArmsField = model.CombinedArmsField;
                view.CanAffordRocketeer = model.CanAffordRocketeer;
                view.RocketeerRecruitPending = model.RocketeerRecruitPending;
                view.OwnStructureAttackUnits = model.OwnStructureAttackUnits;
                view.CanAffordAntiAir = model.CanAffordAntiAir;
                view.CanAffordLogisticsTruck = model.CanAffordLogisticsTruck;
                view.LogisticsTruckCommitted = model.LogisticsTruckCommitted;
                view.CanAffordTanker = model.CanAffordTanker;
                view.TankersCommitted = model.TankersCommitted;
                view.RifleRecruitPending = model.RifleRecruitPending;
                view.AirProfile = model.AirProfile;
                view.OwnDefenseTowers = model.OwnDefenseTowers;
                view.AntiAirCommitted = model.AntiAirCommitted;
                view.PadPresent = model.PadPresent;
                view.ReadinessEligible = model.ReadinessEligible;
                view.PadReady = model.PadPresent && model.ReadinessEligible;
                view.AirQueueOffered = model.CanQueueAir;
                view.AirRecruitPending = model.AirRecruitPending;
                view.OwnAttackAirLive = model.OwnAttackAirLive;
                view.OwnAttackAirActive = model.OwnAttackAirActive;
                view.OwnAttackAirLanded = model.OwnAttackAirLanded;
                view.OwnAirFuel = model.OwnAirFuel;
                view.VisibleHostileCombat = model.VisibleHostileCombat;
                view.VisibleHostileAir = model.VisibleHostileAir;
                if (watchCatalog == null) watchCatalog = Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                view.CanBuildAirPad = model.CanBuildAirPad;
                var openBuild = ObserveWatchButton(_commandControlsView?.BuildButton);
                view.Recruit = view.DrawerOpen ? ObserveCatalogTarget(BuildDrawerCategory.Soldiers, "Unit_Chr_Soldier_Male_02_Alt_04") : openBuild;
                view.RecruitRocketeer = view.DrawerOpen ? ObserveCatalogTarget(BuildDrawerCategory.Soldiers, "Unit_Chr_Ghillie_Male_01") : openBuild;
                view.RecruitAntiAir = view.DrawerOpen ? ObserveCatalogTarget(BuildDrawerCategory.Vehicles, "Unit_Veh_Missle_Launcher_Air") : openBuild;
                view.AirPad = view.DrawerOpen ? ObserveCatalogTarget(BuildDrawerCategory.Buildings, "Building_Helipad") : openBuild;
                view.RecruitAir = view.DrawerOpen ? ObserveCatalogTarget(BuildDrawerCategory.Aircrafts, "Unit_Veh_Helicopter_Attack_Small") : openBuild;
                view.RecruitLogisticsTruck = view.DrawerOpen ? ObserveCatalogTarget(BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tray") : openBuild;
                view.RecruitTanker = view.DrawerOpen ? ObserveCatalogTarget(BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tanker") : openBuild;
                if (UiShellRuntimeGateway.TryReadSkirmishReadiness(out var readiness))
                {
                    view.CanUpgradeReadiness = readiness.CanUpgrade;
                    view.UpgradeReadiness = view.DrawerOpen ? ObserveWatchButton(watchBuild?.ReadinessButton) : openBuild;
                }
            }
            // Skirmish defense plans place firing towers on ordinary ground.
            // The Campaign lesson prefers a road barrier, whose placement rules
            // do not match those sites. Resolve the tower's actual catalog controls,
            // including category changes and scrolling, without changing that lesson.
            if (watchCatalog == null) watchCatalog = Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
            view.DefenseBuild = !model.Expanded
                ? ObserveWatchButton(_highlightPresentationSystem.ResolveBuildTutorialControl(true, true, out _))
                : view.DrawerOpen
                    ? ObserveCatalogTarget(BuildDrawerCategory.Buildings, "Building_GuardTower")
                    : ObserveWatchButton(_commandControlsView?.BuildButton);
            if (watchPlacement == null) watchPlacement = Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
            view.PlacementOpen = watchPlacement != null && watchPlacement.HasPendingPlacement;
            if (view.PlacementOpen)
            {
                view.PlacementConfirm = ObserveWatchButton(watchPlacement.ConfirmButton);
                view.PlacementCancel = ObserveWatchButton(watchPlacement.CancelButton);
                view.Site0 = ObserveDefenseSite(0, view.ExpandedSession); view.Site1 = ObserveDefenseSite(1, view.ExpandedSession);
                view.Site2 = ObserveDefenseSite(2, view.ExpandedSession); view.Site3 = ObserveDefenseSite(3, view.ExpandedSession);
                view.Site4 = ObserveDefenseSite(4, view.ExpandedSession); view.Site5 = ObserveDefenseSite(5, view.ExpandedSession);
                view.PadSite0 = ObserveHomePadSite(0); view.PadSite1 = ObserveHomePadSite(1);
                view.PadSite2 = ObserveHomePadSite(2); view.PadSite3 = ObserveHomePadSite(3);
            }
            if (watchSkirmish != null && watchSkirmish.EnemyBaseMarkerVisible && watchSkirmish.EnemyBaseTargetVisible)
            {
                var point = watchSkirmish.VisibleEnemyBasePoint;
                view.EnemyBase = new AriaTouchTarget { Id = -20001, Position = point, Available = WatchTargetIsReachable(point, -20001, true) };
            }

#if UNITY_EDITOR
            if (SkirmishMatchView.NativeCombatEvidenceEnabled)
            {
                RecordEnemyBaseDiagnostic(in view, watchSkirmish);
            }
#endif

            ObserveMapThreat(ref view);
            ObserveGroupMap(ref view);
            ObserveAdvanceGround(ref view);
            ObserveGroupRectangle(ref view);
            ObservePublicObjective(ref view);
            ObservePublicScan(ref view);
            ObservePublicBoarding(ref view);
            ObserveOffensiveAir(ref view);
            var ordinaryObjective=view.PublicObjective;
            ObserveFieldRepair(ref view);
            ObserveConvoyService(ref view,ordinaryObjective);
            UiShellRuntimeGateway.PublishAriaSkirmishObservation(view);
        }
        private static int ReadDisplayedCount(string text)
        {
            // The localized squad heading already tells the player how many were selected.
            int value = 0; bool found = false;
            foreach (char c in text ?? string.Empty)
            {
                int digit = (int)char.GetNumericValue(c);
                if (char.IsDigit(c) && digit >= 0 && digit <= 9) { value = value * 10 + digit; found = true; }
                else if (found) break;
            }
            return value;
        }
        private AriaTouchTarget ObserveDefenseSite(int index, bool expanded)
        {
            if (watchSkirmish == null || Camera.main == null) return default;
            var forward = watchSkirmish.EnemyBaseObjectivePosition - watchSkirmish.PlayerBaseObjectivePosition;
            forward.y = 0; forward.Normalize(); var side = new Vector3(-forward.z, 0, forward.x);
            // Search beside and behind home as well as the front. Dense Campaign
            // streets can block every forward site. Native preview/confirm stays
            // authoritative; these points are only ordinary visible touch targets.
            Vector3 offset = index switch
            {
                0 => side * 24f,
                1 => -(forward + side) * 22.6274f,
                2 => -forward * 40f,
                3 => (-forward + side) * 28.2843f,
                4 => side * 40f,
                _ => forward * 40f
            };
            var world = watchSkirmish.PlayerBaseObjectivePosition + (expanded ? offset :
                forward * (25 + index / 2 * 10) + side * (index % 2 == 0 ? 18 : -18));
            var point = Camera.main.WorldToScreenPoint(world);
            return new AriaTouchTarget { Id = -20100 - index, Position = point,
                Available = point.z > 0 && Screen.safeArea.Contains(point) && WatchTargetIsReachable(point, -20100 - index, true) };
        }
#if UNITY_EDITOR
        private SkirmishMatchView.NativeHomePadSiteEvidence homePadSiteEvidence;
#endif
        private AriaTouchTarget HomePadSiteResult(string refusal, AriaTouchTarget target=default)
        {
#if UNITY_EDITOR
            if(SkirmishMatchView.NativeCombatEvidenceEnabled)
            {
                homePadSiteEvidence.refusal=refusal;homePadSiteEvidence.reachable=target.Available;
                SkirmishMatchView.RecordNativeHomePadSiteEvidence(homePadSiteEvidence);
            }
#endif
            return target;
        }
        private AriaTouchTarget ObserveHomePadSite(int index)
        {
#if UNITY_EDITOR
            if(SkirmishMatchView.NativeCombatEvidenceEnabled)
                homePadSiteEvidence=new SkirmishMatchView.NativeHomePadSiteEvidence {collected=true,
                    index=index,frame=Time.frameCount,uiHitCount=-1,
                    viewIdentity=watchSkirmish!=null?watchSkirmish.GetEntityId().GetHashCode():0};
#endif
            if (watchSkirmish == null || watchMap == null || Camera.main == null) return HomePadSiteResult("MissingViewMapCamera");
            Vector3 home = watchSkirmish.PlayerBaseObjectivePosition;
            Vector3 enemy = watchSkirmish.EnemyBaseObjectivePosition;
#if UNITY_EDITOR
            if(SkirmishMatchView.NativeCombatEvidenceEnabled)
            {homePadSiteEvidence.homeKnown=true;homePadSiteEvidence.home=home;homePadSiteEvidence.enemy=enemy;}
#endif
            Vector3 forward = enemy - home;
            forward.y = 0;
            if (forward.sqrMagnitude < 1) return HomePadSiteResult("DegenerateHomeEnemyDirection");
            forward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            // The pad footprint and landing approach need a broad clear area. These
            // The lateral home sites sit 36-46 world units from the Barracks,
            // outside the forward tower lane (18 wide), and at least 75 from
            // the hostile base. The first site was verified with normal touch
            // placement on the desert map.
            const float footprintAndAccessClearance = 16f;
            const float hostileContactClearance = 36f;
            const float enemyBaseClearance = 75f;
            Vector3 site = home + forward * (index / 2 == 0 ? 18f : 12f) -
                side * (index % 2 == 0 ? 34f : 42f);
#if UNITY_EDITOR
            if(SkirmishMatchView.NativeCombatEvidenceEnabled)
            {homePadSiteEvidence.siteKnown=true;homePadSiteEvidence.site=site;}
#endif
            float enemyDistanceSquared=(site-enemy).sqrMagnitude;
            if(enemyDistanceSquared<enemyBaseClearance*enemyBaseClearance)
            {
#if UNITY_EDITOR
                if(SkirmishMatchView.NativeCombatEvidenceEnabled)
                {homePadSiteEvidence.rejectedDistanceSquared=enemyDistanceSquared;homePadSiteEvidence.requiredClearance=enemyBaseClearance;}
#endif
                return HomePadSiteResult("EnemyBaseClearance");
            }
            foreach (var contact in watchMap.PresentedContacts)
            {
                float clearance = contact.Model.Allegiance == MatchHudMinimapMarkerAllegiance.Enemy
                    ? hostileContactClearance : footprintAndAccessClearance;
                float contactDistanceSquared=(site-contact.Model.Position).sqrMagnitude;
                if(contactDistanceSquared<clearance*clearance)
                {
#if UNITY_EDITOR
                    if(SkirmishMatchView.NativeCombatEvidenceEnabled)
                    {homePadSiteEvidence.rejectedDistanceSquared=contactDistanceSquared;homePadSiteEvidence.requiredClearance=clearance;}
#endif
                    return HomePadSiteResult(contact.Model.Allegiance==MatchHudMinimapMarkerAllegiance.Enemy?"PublicEnemyContactClearance":"PublicFriendlyContactClearance");
                }
            }
            var point = Camera.main.WorldToScreenPoint(site);
            int id = -20200 - index;
            bool inSafeArea=point.z>0 && Screen.safeArea.Contains(point);
            var target = new AriaTouchTarget { Id = id, Position = point,
                Available = point.z > 0 && inSafeArea && WatchTargetIsReachable(point, id, true) };
#if UNITY_EDITOR
            if(SkirmishMatchView.NativeCombatEvidenceEnabled)
            {homePadSiteEvidence.screenKnown=true;homePadSiteEvidence.screen=point;homePadSiteEvidence.screenValid=inSafeArea;
             homePadSiteEvidence.reachabilityEvaluated=point.z>0&&inSafeArea;
             homePadSiteEvidence.uiHitCount=homePadSiteEvidence.reachabilityEvaluated&&UnityEngine.EventSystems.EventSystem.current!=null?watchHits.Count:-1;}
#endif
            return HomePadSiteResult(target.Available?"None":point.z<=0?"BehindCamera":!inSafeArea?"OutsideSafeArea":"ReachabilityRejected",target);
        }
        private void ResolveAssaultObservationMap(bool mapOpen)
        {
            if (mapOpen)
            { watchMap = watchFullMap != null ? watchFullMap.Minimap : null; return; }
            if (watchMap != null && watchMap.isActiveAndEnabled &&
                !watchMap.UseFullMapProjection) return;
            watchMap = null;
            // Only reacquire on a changed/closed surface, never from hidden gameplay.
            foreach (var map in Object.FindObjectsByType<MatchHudMinimapView>(FindObjectsSortMode.None))
            {
                if (!map.isActiveAndEnabled || map.UseFullMapProjection) continue;
                if (watchMap != null) { watchMap = null; return; } // Ambiguous compact identity.
                watchMap = map;
            }
        }
        internal static bool ExpectedAssaultMapIdentity(MatchHudMinimapView map,
            MatchHudFullMapPopupView popup, bool open) => map != null && map.isActiveAndEnabled &&
            (open ? popup != null && popup.IsOpen && popup.Minimap == map && map.UseFullMapProjection
                  : !map.UseFullMapProjection);
        private void ObserveAssaultMap(ref AriaSkirmishObservation view)
        {
            if (watchMap == null || !watchMap.isActiveAndEnabled || watchMap.MapRect == null ||
                !watchMap.MapRect.gameObject.activeInHierarchy || watchSkirmish == null ||
                !watchSkirmish.EnemyBaseObjectiveAlive || Camera.main == null || view.DrawerOpen ||
                !watchMap.HasCurrentPresentedMapArea(ResolveEventCamera(watchMap.MapImage))) return;
            bool expected = ExpectedAssaultMapIdentity(watchMap, watchFullMap, view.MapOpen);
            var grid = watchMap.PresentedProjection;
            view.AssaultMap = new AriaAssaultMapObservation {
                Identity = watchMap.GetEntityId().GetHashCode(), ExpectedIdentity = expected, FullMap = view.MapOpen,
                MarkerFrame = watchMap.PresentedMarkerFrame, MarkerRevision = watchMap.PresentedMarkerRevision,
                MarkerTime = watchMap.PresentedMarkerTime, Complete = watchMap.PresentedMarkersComplete,
                ProjectionValid = float.IsFinite(grid.Width) && float.IsFinite(grid.Height) &&
                    grid.Width > 0 && grid.Height > 0,
                ObjectiveHudIdentity = watchSkirmish.GetEntityId().GetHashCode(),
                ObjectiveWorld = watchSkirmish.EnemyBaseObjectivePosition };
            if (!view.MapOpen && expected && watchMap.OpensFullMapOnClick &&
                UnityEngine.EventSystems.EventSystem.current != null)
            {
                var rect = watchMap.MapRect;
                var point = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage),
                    rect.TransformPoint(rect.rect.center));
                watchRaycast.position = point; watchHits.Clear();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(watchRaycast, watchHits);
                bool reachable = Screen.safeArea.Contains(point) && watchHits.Count > 0 &&
                    watchHits[0].gameObject.GetComponentInParent<MatchHudMinimapView>() == watchMap;
                view.AssaultMap.OpenMap = new AriaTouchTarget { Id = -20010, Position = point, Available = reachable };
            }
        }

        private void ObserveAdvanceGround(ref AriaSkirmishObservation view)
        {
            if (view.DrawerOpen || watchMap == null || watchSkirmish == null ||
                !watchSkirmish.EnemyBaseObjectiveAlive || Camera.main == null ||
                !view.AssaultMap.IsCurrent(view.Frame, view.Time)) return;
            // Use the base objective and contacts already presented on the player's map.
            // A point on its approach lets ordinary Attack Move fight the route's defenders.
            var objective = watchSkirmish.EnemyBaseObjectivePosition;
            Vector3 nearest = objective; float distance = float.MaxValue; int close = 0, assault = 0;
            foreach (var ally in watchMap.PresentedContacts)
            {
                if (ally.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Player ||
                    !watchMap.IsContactCurrentlyPresented(ally, ResolveEventCamera(watchMap.MapImage))) continue;
                float d = (ally.Model.Position - objective).sqrMagnitude;
                if (d < 45 * 45) close++;
                if (d < 50 * 50) assault++;
                if (d < distance) { distance = d; nearest = ally.Model.Position; }
            }
            view.AssaultDistanceVisible = distance < float.MaxValue;
            view.NearestFriendlyObjectiveDistance = view.AssaultDistanceVisible ? Mathf.Sqrt(distance) : 0f;
            int required = view.EnemyHealth > 0 && view.EnemyHealth <= 400
                ? 1 : Mathf.Max(4, Mathf.Min(8, view.SelectedCount));
            view.AssaultAtBase = assault >= required;
            view.AdvancePreferred = close < 4;
            if (watchScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex)
            {
                var forward = objective - watchSkirmish.PlayerBaseObjectivePosition; forward.y = 0; forward.Normalize();
                var side = new Vector3(-forward.z, 0, forward.x);
                var flank = objective - forward * 50 + side * 70;
                int arrived = 0;
                foreach (var ally in watchMap.PresentedContacts)
                    if (ally.Model.Allegiance == MatchHudMinimapMarkerAllegiance.Player &&
                        (ally.Model.Position - flank).sqrMagnitude < 35 * 35) arrived++;
                if (arrived >= 4) watchFlankReached = true;
                var goal = watchFlankReached ? objective + side * 20 : flank;
                view.AdvancePreferred = !view.AssaultAtBase;
                // The labelled base control provides an exact, visible camera action
                // for the final approach; reserve map navigation for the remote flank.
                if (watchFlankReached) view.FocusAdvance = view.FocusEnemy;
                else ObserveAdvanceMapFocus(goal, ref view);
                if (!view.MapOpen)
                {
                    view.AdvanceGround = ObserveClearAttackGround(goal,
                        watchSkirmish.PlayerBaseObjectivePosition - goal, -20006, 20);
                    if (!view.AdvanceGround.Available)
                    {
                        var point = Camera.main.WorldToScreenPoint(goal);
                        view.AdvanceGround = new AriaTouchTarget { Id = -20006, Position = point,
                            Available = point.z > 0 && Screen.safeArea.Contains(point) && WatchTargetIsReachable(point, -20006, true) };
                    }
                }
                return;
            }
            if (watchScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex)
            {
                // NW→SE basin uses the verified base-relative approach corridor below.
                // City Crossroads' wide eastern flank projected into blocked fabric here.
                view.AdvancePreferred = !view.AssaultAtBase && close < 8;
            }
            if (!view.AdvancePreferred) return;
            // The two labelled base objectives remain public when the camera leaves
            // the army. Keep a usable approach instead of repeatedly focusing the base.
            if (distance == float.MaxValue) nearest = watchSkirmish.PlayerBaseObjectivePosition;
            var direction = nearest - objective; direction.y = 0;
            // Preserve the first battlefield's verified clear base approach. Wider
            // offsets project behind its neutral ruins and become rejected attacks.
            float standoff = watchScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex ? 30f : 25f;
            var ground = objective + direction.normalized * standoff;
            if (view.MapOpen)
            {
                if (watchScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex)
                    ObserveAdvanceMapFocus(ground, ref view);
                return;
            }
            if (watchScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex)
            {
                view.AdvanceGround = ObserveClearAttackGround(ground,
                    watchSkirmish.PlayerBaseObjectivePosition - objective, -20006, 25);
                if (view.AdvanceGround.Available) return;
            }
            var screen = Camera.main.WorldToScreenPoint(ground);
            view.AdvanceGround = new AriaTouchTarget { Id = -20006, Position = screen,
                Available = screen.z > 0 && Screen.safeArea.Contains(screen) && WatchTargetIsReachable(screen, -20006, true) };
            if (!view.AdvanceGround.Available &&
                watchScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex &&
                view.FocusEnemy.Available)
                view.FocusAdvance = view.FocusEnemy;
            else if (!view.AdvanceGround.Available &&
                watchScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex)
                ObserveAdvanceMapFocus(ground, ref view);
        }
        private void ObserveAdvanceMapFocus(Vector3 goal, ref AriaSkirmishObservation view)
        {
            Vector2 point;
            var rect = watchMap.MapRect;
            var camera = ResolveEventCamera(watchMap.MapImage);
            if (view.MapOpen)
            {
                if (!watchMap.TryGetPresentedMapPoint(goal, camera, out point)) return;
            }
            else point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            watchRaycast.position = point; watchHits.Clear();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(watchRaycast, watchHits);
            bool reachable = watchHits.Count > 0 && watchHits[0].gameObject.GetComponentInParent<MatchHudMinimapView>() == watchMap;
            view.FocusAdvance = new AriaTouchTarget { Id = view.MapOpen ? -20011 : -20010, Position = point, Available = reachable };
            if (view.MapOpen && reachable && watchMap.ViewportRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(watchMap.ViewportRect, point, camera))
            {
                var viewport = watchMap.ViewportRect;
                var start = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(viewport.rect.center));
                view.FocusAdvance.Position = start; view.FocusAdvanceDrag = true;
                view.FocusAdvanceDragEnd = (start - point).sqrMagnitude > 25 ? point : point + Vector2.up * 12;
            }
        }
        private AriaTouchTarget ObserveClearAttackGround(Vector3 objective, Vector3 approach, int id, float distance = 55)
        {
            approach.y = 0;
            if (approach.sqrMagnitude < 1) return default;
            approach.Normalize();
            var side = Vector3.Cross(Vector3.up, approach);
            // Ground taps must stay away from the visible contacts. Otherwise the
            // normal hit test turns Attack Move into a direct building attack.
            for (int ring = 0; ring < 3; ring++)
            for (int lane = 0; lane < 3; lane++)
            {
                var ground = objective + approach * (distance + ring * 15) + side * (lane == 0 ? 0 : lane == 1 ? 35 : -35);
                bool clear = true;
                foreach (var contact in watchMap.PresentedContacts)
                {
                    float clearance = contact.Model.Allegiance == MatchHudMinimapMarkerAllegiance.Enemy ? 15 : 8;
                    if ((contact.Model.Position - ground).sqrMagnitude < clearance * clearance) { clear = false; break; }
                }
                if (!clear) continue;
                var point = Camera.main.WorldToScreenPoint(ground);
                if (point.z > 0 && Screen.safeArea.Contains(point) && WatchTargetIsReachable(point, id, true))
                    return new AriaTouchTarget { Id = id, Position = point, Available = true };
            }
            return default;
        }

        private void ObserveGroupRectangle(ref AriaSkirmishObservation view)
        {
            if (watchMap == null || view.MapOpen || view.DrawerOpen || Camera.main == null) return;
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            int count = 0;
            foreach (var ally in watchMap.PresentedContacts)
            {
                if (ally.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Player) continue;
                if (watchGroupValid && (ally.Model.Position - watchGroupCenter).sqrMagnitude > 45 * 45) continue;
                Vector3 point = Camera.main.WorldToScreenPoint(ally.Model.Position + Vector3.up);
                if (point.z <= 0 || !Screen.safeArea.Contains(point) || !WatchTargetIsReachable(point, -20005, true)) continue;
                min = Vector2.Min(min, point); max = Vector2.Max(max, point); count++;
            }
            if (count < 2) return;
            // Optional visual padding must not turn a selectable army into an
            // unavailable box when its outer soldiers are close to a HUD edge.
            for (int padding = 14; padding >= 0; padding -= 7)
            {
                var a = min - Vector2.one * padding;
                var b = max + Vector2.one * padding;
                if (TryGroupDrag(a, b, ref view) ||
                    TryGroupDrag(new Vector2(a.x, b.y), new Vector2(b.x, a.y), ref view)) return;
            }
        }
        private bool TryGroupDrag(Vector2 start, Vector2 end, ref AriaSkirmishObservation view)
        {
            for (int i = 0; i <= 20; i++)
                if (!WatchTargetIsReachable(Vector2.Lerp(start, end, i / 20f), -20005, true)) return false;
            view.GroupStart = new AriaTouchTarget { Id = -20005, Position = start, Available = true };
            view.GroupEnd = new AriaTouchTarget { Id = -20005, Position = end, Available = true };
            return true;
        }
        private void ObserveMapThreat(ref AriaSkirmishObservation view)
        {
            if (watchFullMap == null) watchFullMap = Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            view.MapOpen = watchFullMap != null && watchFullMap.IsOpen;
            ResolveAssaultObservationMap(view.MapOpen);
            if (view.MapOpen) view.CloseMap = ObserveWatchButton(watchFullMap.CloseAction);
            ObserveAssaultMap(ref view);
            if (watchMap == null || !watchMap.isActiveAndEnabled || Camera.main == null) return;
            float best = float.MaxValue, bestVisible = float.MaxValue;
            Vector3 armyCenter = Vector3.zero; int friendlies = 0;
            var objective = watchSkirmish != null ? watchSkirmish.EnemyBaseObjectivePosition : Vector3.zero;
            for (int i = 0; i < 4; i++) watchFrontDistances[i] = float.MaxValue;
            foreach (var friendly in watchMap.PresentedContacts)
            {
                if (friendly.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Player) continue;
                float distance = (friendly.Model.Position - objective).sqrMagnitude;
                for (int i = 0; i < 4; i++)
                {
                    if (distance >= watchFrontDistances[i]) continue;
                    for (int j = 3; j > i; j--) { watchFront[j] = watchFront[j - 1]; watchFrontDistances[j] = watchFrontDistances[j - 1]; }
                    watchFront[i] = friendly.Model.Position; watchFrontDistances[i] = distance; break;
                }
                friendlies++;
            }
            if (friendlies == 0) return;
            // Track a frontline group, not one isolated scout or immobile buildings at home.
            int frontCount = Mathf.Min(4, friendlies);
            for (int i = 0; i < frontCount; i++) armyCenter += watchFront[i];
            armyCenter /= frontCount;
            MatchHudMinimapView.PresentedMapContact chosen = default, visible = default, tracked = default, homeThreat = default;
            float homeDistance = 120f * 120f;
            float trackedDistance = 12 * 12;
            foreach (var enemy in watchMap.PresentedContacts)
            {
                if (enemy.Model.LastSeen || enemy.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Enemy || !enemy.Marker.gameObject.activeInHierarchy) continue;
                // The labelled base is an objective, not an intervening defender.
                // Fight the contacts in front of the army before ordering it past them.
                var groundPoint = Camera.main.WorldToScreenPoint(enemy.Model.Position);
                if (watchSkirmish != null && watchSkirmish.EnemyBaseObjectiveAlive &&
                    ((Vector2)groundPoint - watchSkirmish.VisibleEnemyBasePoint).sqrMagnitude < 1) continue;
                if (watchSkirmish != null)
                {
                    float distance = (enemy.Model.Position - watchSkirmish.PlayerBaseObjectivePosition).sqrMagnitude;
                    if (distance < homeDistance) { homeDistance = distance; homeThreat = enemy; }
                }
                float nearest = (enemy.Model.Position - armyCenter).sqrMagnitude;
                float continuity = (enemy.Model.Position - watchThreatPosition).sqrMagnitude;
                if (watchThreatTracked && nearest <= 90 * 90 && continuity < trackedDistance)
                { trackedDistance = continuity; tracked = enemy; }
                if (nearest < best) { best = nearest; chosen = enemy; }
                var screenPoint = Camera.main.WorldToScreenPoint(enemy.Model.Position + Vector3.up);
                if (!view.MapOpen && nearest < bestVisible && screenPoint.z > 0 &&
                    Screen.safeArea.Contains(screenPoint) && WatchTargetIsReachable(screenPoint, -20002, true))
                { bestVisible = nearest; visible = enemy; }
            }
            // Prefer a nearby visible threat, but do not chase a distant screen contact
            // while the army is fighting elsewhere. One isolated scout cannot redirect the army.
            if (visible.Marker != null && bestVisible <= best + 30 * 30) chosen = visible;
            // Keep following the same presented contact through a squad rotation.
            // Reacquire spatially when markers are pooled; no hidden entity identity
            // or world order crosses into the touch planner.
            if (tracked.Marker != null) chosen = tracked;
            // A displayed contact threatening our labelled home takes priority
            // over the outgoing column. Only presented minimap contacts are read.
            view.PresentedHomeThreat = homeThreat.Marker != null;
            if (view.PresentedHomeThreat) chosen = homeThreat;
            watchThreatTracked = chosen.Marker != null;
            if (watchThreatTracked) watchThreatPosition = chosen.Model.Position;
            if (chosen.Marker == null || watchRaycast == null || UnityEngine.EventSystems.EventSystem.current == null) return;
            var mapPoint = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage), chosen.Marker.position);
            // The small map opens from a fixed visible surface, not a contact that
            // can leave its clipped bounds during the hand's approach.
            if (!view.MapOpen)
            {
                var mapRect = watchMap.MapImage.rectTransform;
                mapPoint = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage), mapRect.TransformPoint(mapRect.rect.center));
            }
            watchRaycast.position = mapPoint;
            watchHits.Clear(); UnityEngine.EventSystems.EventSystem.current.RaycastAll(watchRaycast, watchHits);
            bool mapReachable = watchHits.Count > 0 && watchHits[0].gameObject.GetComponentInParent<MatchHudMinimapView>() == watchMap;
            view.MapContactInView = view.MapOpen && watchMap.ViewportRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(watchMap.ViewportRect, mapPoint, ResolveEventCamera(watchMap.MapImage));
            view.FocusThreat = new AriaTouchTarget { Id = view.MapOpen ? -20004 : -20003, Position = mapPoint, Available = mapReachable };
            if (view.MapContactInView && mapReachable)
            {
                // A tap inside the viewport intentionally does nothing. Drag the
                // viewport itself to center the contact, just as a player would.
                var viewport = watchMap.ViewportRect;
                var start = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage), viewport.TransformPoint(viewport.rect.center));
                view.FocusThreat.Position = start;
                view.FocusThreatDrag = true;
                view.FocusThreatDragEnd = (start - mapPoint).sqrMagnitude > 25 ? mapPoint : mapPoint + Vector2.up * 12;
            }
            view.ThreatNearForce = (chosen.Model.Position - armyCenter).sqrMagnitude <= 60 * 60;
            var point = Camera.main.WorldToScreenPoint(chosen.Model.Position + Vector3.up);
            bool onScreen = point.z > 0 && point.x > 0 && point.y > 0 && point.x < Screen.width && point.y < Screen.height;
            view.ThreatGround = ObserveClearAttackGround(chosen.Model.Position, armyCenter - chosen.Model.Position, -20006, view.PresentedHomeThreat ? 18f : 35f);
            view.Threat = new AriaTouchTarget { Id = -20002, Position = point, Available = onScreen && WatchTargetIsReachable(point, -20002, true) };
        }


    }
}
