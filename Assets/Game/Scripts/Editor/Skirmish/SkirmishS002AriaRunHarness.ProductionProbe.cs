using System;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static int nativeProductionStage;
        private static uint nativeProductionReceipt;
        private static double nativeProductionDeadline, nextProductionTap;
        private static AriaTouchInputUiSystemHelper nativeProductionTouch;

        private static void StartNativeProductionProbe(Entity session)
        {
            // Queue input in a player frame. EditorApplication.update can consume
            // touch state in an Editor input update before the game's UI sees it.
            var runner = new GameObject("NativeProductionInputProbe").AddComponent<SkirmishProductionInputProbeFrame>();
            runner.Session = session;
            deadline = UnityEditor.EditorApplication.timeSinceStartup + 180d;
        }

        internal static void TickProductionPlayerFrame(Entity session)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (nativeProductionStage == 0 || world == null || !world.IsCreated || !world.EntityManager.Exists(session)) return;
            TickNativeProductionProbe(world.EntityManager, session, UnityEditor.EditorApplication.timeSinceStartup);
        }

        public static void RunS003ProductionProbeAndExit()
        {
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG", "S003");
            Environment.SetEnvironmentVariable("WARLINE_S003_PRODUCTION_PROBE", "1");
            nativeProductionStage = 0;
            RunS003StartupAndExit();
        }

        private static void TickNativeProductionProbe(EntityManager em, Entity session, double now)
        {
            next = now + .1d;
            if (nativeProductionTouch == null)
            {
                nativeProductionTouch = new AriaTouchInputUiSystemHelper();
                nativeProductionDeadline = now + 150d;
                deadline = now + 180d;
                if (!nativeProductionTouch.Start()) { EndProductionProbe(false, "touchStartRejected"); return; }
            }
            nativeProductionTouch.Tick(Time.unscaledTime);
            if (nativeProductionTouch.PlayerInterrupted || !nativeProductionTouch.IsRunning)
            { EndProductionProbe(false, "inputInterrupted"); return; }
            if (now > nativeProductionDeadline) { EndProductionProbe(false, "deliveryTimeout"); return; }
            if (!Mathf.Approximately(Time.timeScale, 1f)) { EndProductionProbe(false, "notNormalSpeed"); return; }
            if (nativeProductionStage == 3)
            {
                if (now >= nextProductionTap) EndProductionProbe(true, "nativeAaDelivered");
                return;
            }
            if (nativeProductionStage == 1)
            {
                if (em.HasBuffer<SkirmishProductionReservation>(session))
                    foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                        if (receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.AntiAir)
                        {
                            if (receipt.MaterialsPaid != 220 || receipt.MemberCount != 1 || receipt.ProducerRuntimeId <= 0 ||
                                receipt.Phase != SkirmishReservationPhase.Reserved && receipt.Phase != SkirmishReservationPhase.Producing ||
                                SkirmishMaterialsService.Read(em, session, 1) != 230)
                            { EndProductionProbe(false, "paymentOrReceiptMismatch"); return; }
                            nativeProductionReceipt = receipt.ReservationId;
                            nativeProductionStage = 2;
                            ScreenCapture.CaptureScreenshot("/private/tmp/s003-production-paid.png");
                            Debug.Log("[SkirmishS003ProductionProbe] phase=Paid materials=230 cost=220 receipt=" + receipt.ReservationId);
                            return;
                        }
                if (nativeProductionTouch.IsBusy || now < nextProductionTap) return;
                var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                Button button = drawer != null && drawer.IsOpen
                    ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Vehicles, "Unit_Veh_Missle_Launcher_Air")
                    : UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>()?.BuildButton;
                if (!TryPresentedButtonPoint(button, out var point)) return;
                if (!nativeProductionTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                { EndProductionProbe(false, "touchRejected"); return; }
                nextProductionTap = now + 1d;
                Debug.Log("[SkirmishS003ProductionProbe] phase=Touch target=" + button.name + " point=" + point);
                return;
            }
            var state = default(SkirmishProductionReservation);
            foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                if (receipt.ReservationId == nativeProductionReceipt) state = receipt;
            if (state.Phase == SkirmishReservationPhase.Lost || state.Phase == SkirmishReservationPhase.Cancelled)
            { EndProductionProbe(false, "producerOrQueueLost"); return; }
            if (state.Phase != SkirmishReservationPhase.Live) return;
            using var units = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using var entities = units.ToEntityArray(Allocator.Temp);
            int delivered = 0;
            var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            foreach (var unit in entities)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!owner.SessionId.Equals(sessionId) || owner.ReservationId != nativeProductionReceipt || owner.FactionId != 1) continue;
                if (!em.HasComponent<SkirmishSharedActorTag>(unit) || !em.HasComponent<UnitGrid>(unit) ||
                    !em.HasComponent<CombatTargetPolicy>(unit) || em.GetComponentData<UnitHealth>(unit).Current <= 0)
                { EndProductionProbe(false, "deliveredActorIncomplete"); return; }
                delivered++;
            }
            if (delivered != 1 || state.DeliveredMembers != 1 || state.ProductionGroupId == 0)
            { EndProductionProbe(false, "deliveryCountMismatch"); return; }
            ScreenCapture.CaptureScreenshot("/private/tmp/s003-production-delivered.png");
            nativeProductionStage = 3;
            nextProductionTap = now + 1d;
        }

        private static bool TryPresentedButtonPoint(Button button, out Vector2 point)
        {
            point = default;
            if (button == null || !button.IsActive() || !button.IsInteractable() || EventSystem.current == null) return false;
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            if (!Screen.safeArea.Contains(point)) return false;
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            return hits.Count > 0 && (hits[0].gameObject == button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform));
        }

        private static void EndProductionProbe(bool passed, string reason)
        {
            Debug.Log("[SkirmishS003ProductionProbe] result=" + (passed ? "Passed" : "Failed") +
                " reason=" + reason + " input=Touch samples=" + (nativeProductionTouch?.AcceptedSamples ?? 0) +
                " acceptance=RecruitmentOnly");
            nativeProductionStage = 0;
            Finish(!passed, reason);
        }
    }

    internal sealed class SkirmishProductionInputProbeFrame : MonoBehaviour
    {
        internal Entity Session;
        private void Update() => SkirmishS002AriaRunHarness.TickProductionPlayerFrame(Session);
    }
}
