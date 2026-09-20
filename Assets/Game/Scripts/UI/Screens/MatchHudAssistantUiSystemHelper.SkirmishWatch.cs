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
        private bool watchThreatTracked;
        private MatchHudSquadTrayView watchSquads;
        private BuildDrawerView watchBuild;
        private BuildPlacementConfirmationBarView watchPlacement;
        private MatchHudMinimapView watchMap;
        private MatchHudFullMapPopupView watchFullMap;
        private AriaTouchTarget ObserveWatchButton(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return default;
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(button), rect.TransformPoint(rect.rect.center));
            int id = button.GetEntityId().GetHashCode();
            return new AriaTouchTarget { Id = id, Position = point, Available = WatchTargetIsReachable(point, id, false) };
        }
        private void ObserveSkirmishWatch(UiSkirmishModel model)
        {
            if (!UiShellRuntimeGateway.ReadAriaPlay().Active)
            { watchThreatTracked = false; UiShellRuntimeGateway.PublishAriaSkirmishObservation(default); return; }
            if (watchSkirmish == null) watchSkirmish = Object.FindAnyObjectByType<SkirmishMatchView>();
            if (watchSquads == null) watchSquads = Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            if (watchBuild == null) watchBuild = Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            var view = new AriaSkirmishObservation { Active = true, Finished = model.Finished || model.StartupFailed,
                Frame = Time.frameCount, Time = Time.unscaledTime, SelectedSlot = -1, DrawerOpen = watchBuild != null && watchBuild.IsOpen };
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
                view.SelectedCount = ReadDisplayedCount(selected.Title);
                if (view.SelectedCount == 0) view.SelectedCount = ReadDisplayedCount(selected.Subtitle);
            }
            if (UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) { view.AttackMode = command.ActiveCommandMode == TacticalCommandMode.Attack; view.SelectionMode = command.ActiveCommandMode == TacticalCommandMode.Select; }
            view.Infantry = model.InfantryCount;
            view.PlayerHealth = model.PlayerHealth; view.EnemyHealth = model.EnemyHealth;
            view.Squad0 = ObserveWatchButton(watchSquads?.VisibleCardButton(0));
            view.Squad1 = ObserveWatchButton(watchSquads?.VisibleCardButton(1));
            view.Squad2 = ObserveWatchButton(watchSquads?.VisibleCardButton(2));
            view.Squad3 = ObserveWatchButton(watchSquads?.VisibleCardButton(3));
            view.Squad4 = ObserveWatchButton(watchSquads?.VisibleCardButton(4));
            view.FocusPlayer = ObserveWatchButton(watchSkirmish?.PlayerFocusButton);
            view.Select = ObserveWatchButton(_commandControlsView?.SelectButton);
            view.FocusEnemy = ObserveWatchButton(watchSkirmish?.EnemyFocusButton);
            view.Attack = ObserveWatchButton(_commandControlsView?.AttackButton);
            view.Hold = ObserveWatchButton(_commandControlsView?.HoldButton);
            view.Recruit = ObserveWatchButton(_highlightPresentationSystem.ResolveProductionTutorialControl(out _));
            view.CloseDrawer = ObserveWatchButton(watchBuild?.CloseButton);
            view.DefenseBuild = ObserveWatchButton(_highlightPresentationSystem.ResolveBuildTutorialControl(true, true, out _));
            if (watchPlacement == null) watchPlacement = Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
            view.PlacementOpen = watchPlacement != null && watchPlacement.HasPendingPlacement;
            if (view.PlacementOpen)
            {
                view.PlacementConfirm = ObserveWatchButton(watchPlacement.ConfirmButton);
                view.PlacementCancel = ObserveWatchButton(watchPlacement.CancelButton);
                view.Site0 = ObserveDefenseSite(0); view.Site1 = ObserveDefenseSite(1);
                view.Site2 = ObserveDefenseSite(2); view.Site3 = ObserveDefenseSite(3);
                view.Site4 = ObserveDefenseSite(4); view.Site5 = ObserveDefenseSite(5);
            }
            if (watchSkirmish != null && watchSkirmish.EnemyBaseMarkerVisible)
            {
                var point = watchSkirmish.VisibleEnemyBasePoint;
                view.EnemyBase = new AriaTouchTarget { Id = -20001, Position = point, Available = WatchTargetIsReachable(point, -20001, true) };
            }
            ObserveMapThreat(ref view);
            ObserveGroupMap(ref view);
            ObserveAdvanceGround(ref view);
            ObserveGroupRectangle(ref view);
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
        private AriaTouchTarget ObserveDefenseSite(int index)
        {
            if (watchSkirmish == null || Camera.main == null) return default;
            var forward = watchSkirmish.EnemyBaseObjectivePosition - watchSkirmish.PlayerBaseObjectivePosition;
            forward.y = 0; forward.Normalize(); var side = new Vector3(-forward.z, 0, forward.x);
            var world = watchSkirmish.PlayerBaseObjectivePosition + forward * (25 + index / 2 * 10) + side * (index % 2 == 0 ? 18 : -18);
            var point = Camera.main.WorldToScreenPoint(world);
            return new AriaTouchTarget { Id = -20100 - index, Position = point,
                Available = point.z > 0 && Screen.safeArea.Contains(point) && WatchTargetIsReachable(point, -20100 - index, true) };
        }
        private void ObserveAdvanceGround(ref AriaSkirmishObservation view)
        {
            if (view.MapOpen || view.DrawerOpen || watchMap == null || watchSkirmish == null ||
                !watchSkirmish.EnemyBaseObjectiveAlive || Camera.main == null) return;
            // Use the base objective and contacts already presented on the player's map.
            // A point on its approach lets ordinary Attack Move fight the route's defenders.
            var objective = watchSkirmish.EnemyBaseObjectivePosition;
            Vector3 nearest = objective; float distance = float.MaxValue; int close = 0, assault = 0;
            foreach (var ally in watchMap.PresentedContacts)
            {
                if (ally.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Player) continue;
                float d = (ally.Model.Position - objective).sqrMagnitude;
                if (d < 45 * 45) close++;
                if (d < 120 * 120) assault++;
                if (d < distance) { distance = d; nearest = ally.Model.Position; }
            }
            int required = view.EnemyHealth > 0 && view.EnemyHealth <= 400
                ? 1 : Mathf.Max(4, Mathf.Min(8, view.SelectedCount));
            view.AssaultAtBase = assault >= required;
            view.AdvancePreferred = close < 4;
            if (!view.AdvancePreferred) return;
            // The two labelled base objectives remain public when the camera leaves
            // the army. Keep a usable approach instead of repeatedly focusing the base.
            if (distance == float.MaxValue) nearest = watchSkirmish.PlayerBaseObjectivePosition;
            var direction = nearest - objective; direction.y = 0;
            var ground = objective + direction.normalized * 25;
            var point = Camera.main.WorldToScreenPoint(ground);
            view.AdvanceGround = new AriaTouchTarget { Id = -20006, Position = point,
                Available = point.z > 0 && Screen.safeArea.Contains(point) && WatchTargetIsReachable(point, -20006, true) };
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
            view.MapOpen = watchFullMap != null && watchFullMap.gameObject.activeInHierarchy;
            if (view.MapOpen) { watchMap = watchFullMap.Minimap; view.CloseMap = ObserveWatchButton(watchFullMap.CloseAction); }
            else if (watchMap == null || !watchMap.isActiveAndEnabled) watchMap = Object.FindAnyObjectByType<MatchHudMinimapView>();
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
            MatchHudMinimapView.PresentedMapContact chosen = default, visible = default, tracked = default;
            float trackedDistance = 12 * 12;
            foreach (var enemy in watchMap.PresentedContacts)
            {
                if (enemy.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Enemy || !enemy.Marker.gameObject.activeInHierarchy) continue;
                // The labelled base is an objective, not an intervening defender.
                // Fight the contacts in front of the army before ordering it past them.
                var groundPoint = Camera.main.WorldToScreenPoint(enemy.Model.Position);
                if (watchSkirmish != null && watchSkirmish.EnemyBaseObjectiveAlive &&
                    ((Vector2)groundPoint - watchSkirmish.VisibleEnemyBasePoint).sqrMagnitude < 1) continue;
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
            watchThreatTracked = chosen.Marker != null;
            if (watchThreatTracked) watchThreatPosition = chosen.Model.Position;
            if (chosen.Marker == null || watchRaycast == null || UnityEngine.EventSystems.EventSystem.current == null) return;
            var mapPoint = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage), chosen.Marker.position);
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
            view.Threat = new AriaTouchTarget { Id = -20002, Position = point, Available = onScreen && WatchTargetIsReachable(point, -20002, true) };
        }
    }
}
