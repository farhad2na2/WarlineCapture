using Game.UI.Contracts;
using Game.Tactical.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private SkirmishMatchView watchSkirmish;
        private MatchHudSquadTrayView watchSquads;
        private BuildDrawerView watchBuild;
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
            { UiShellRuntimeGateway.PublishAriaSkirmishObservation(default); return; }
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
            if (UiShellRuntimeGateway.TryReadMatchHudSelection(out var selected)) view.SelectionVisible = selected.Visible;
            if (UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) view.AttackMode = command.ActiveCommandMode == TacticalCommandMode.Attack;
            view.Infantry = model.InfantryCount;
            view.PlayerHealth = model.PlayerHealth; view.EnemyHealth = model.EnemyHealth;
            view.Squad0 = ObserveWatchButton(watchSquads?.VisibleCardButton(0));
            view.Squad1 = ObserveWatchButton(watchSquads?.VisibleCardButton(1));
            view.Squad2 = ObserveWatchButton(watchSquads?.VisibleCardButton(2));
            view.Squad3 = ObserveWatchButton(watchSquads?.VisibleCardButton(3));
            view.Squad4 = ObserveWatchButton(watchSquads?.VisibleCardButton(4));
            view.FocusEnemy = ObserveWatchButton(watchSkirmish?.EnemyFocusButton);
            view.Attack = ObserveWatchButton(_commandControlsView?.AttackButton);
            view.Recruit = ObserveWatchButton(_highlightPresentationSystem.ResolveProductionTutorialControl(out _));
            view.CloseDrawer = ObserveWatchButton(watchBuild?.CloseButton);
            if (watchSkirmish != null && watchSkirmish.EnemyBaseMarkerVisible)
            {
                var point = watchSkirmish.VisibleEnemyBasePoint;
                view.EnemyBase = new AriaTouchTarget { Id = -20001, Position = point, Available = WatchTargetIsReachable(point, -20001, true) };
            }
            ObserveMapThreat(ref view);
            UiShellRuntimeGateway.PublishAriaSkirmishObservation(view);
        }
        private void ObserveMapThreat(ref AriaSkirmishObservation view)
        {
            if (watchFullMap == null) watchFullMap = Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            view.MapOpen = watchFullMap != null && watchFullMap.gameObject.activeInHierarchy;
            if (view.MapOpen) { watchMap = watchFullMap.Minimap; view.CloseMap = ObserveWatchButton(watchFullMap.CloseAction); }
            else if (watchMap == null || !watchMap.isActiveAndEnabled) watchMap = Object.FindAnyObjectByType<MatchHudMinimapView>();
            if (watchMap == null || !watchMap.isActiveAndEnabled || Camera.main == null) return;
            float best = float.MaxValue;
            MatchHudMinimapView.PresentedMapContact chosen = default;
            foreach (var enemy in watchMap.PresentedContacts)
            {
                if (enemy.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Enemy || !enemy.Marker.gameObject.activeInHierarchy) continue;
                foreach (var friendly in watchMap.PresentedContacts)
                {
                    if (friendly.Model.Allegiance != MatchHudMinimapMarkerAllegiance.Player) continue;
                    float distance = (enemy.Model.Position - friendly.Model.Position).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance; chosen = enemy;
                }
            }
            if (chosen.Marker == null || watchRaycast == null || UnityEngine.EventSystems.EventSystem.current == null) return;
            var mapPoint = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(watchMap.MapImage), chosen.Marker.position);
            watchRaycast.position = mapPoint;
            watchHits.Clear(); UnityEngine.EventSystems.EventSystem.current.RaycastAll(watchRaycast, watchHits);
            bool mapReachable = watchHits.Count > 0 && watchHits[0].gameObject.GetComponentInParent<MatchHudMinimapView>() == watchMap;
            view.MapContactInView = view.MapOpen && watchMap.ViewportRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(watchMap.ViewportRect, mapPoint, ResolveEventCamera(watchMap.MapImage));
            view.FocusThreat = new AriaTouchTarget { Id = view.MapOpen ? -20004 : -20003, Position = mapPoint, Available = mapReachable };
            var point = Camera.main.WorldToScreenPoint(chosen.Model.Position + Vector3.up);
            bool onScreen = point.z > 0 && point.x > 0 && point.y > 0 && point.x < Screen.width && point.y < Screen.height;
            view.Threat = new AriaTouchTarget { Id = -20002, Position = point, Available = onScreen && WatchTargetIsReachable(point, -20002, true) };
        }
    }
}
