using System;
using System.IO;
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
        private static bool nativeProductionRecovery;
        private static string NativeProductionMarker => "[Skirmish" + catalogId + "ProductionProbe]";
        private const string S005PaidSavePath = "Design/AgentReports/SkirmishExpansion/S005/native-paid-production-checkpoint-wire.json";

        public static void RunS005ProductionCheckpointProbeAndExit()
        {
            nativeProductionRecovery = false;
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC", null);
            Environment.SetEnvironmentVariable("WARLINE_S003_PRODUCTION_PROBE", "1");
            nativeProductionStage = 0;
            RunS005StartupEn();
        }

        public static void RunS005PaidProductionRecoveryProbeAndExit()
        {
            string path = Path.GetFullPath(S005PaidSavePath);
            if (!File.Exists(path)) throw new InvalidOperationException("Paid production diagnostic save missing.");
            nativeProductionRecovery = true;
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC", path);
            Environment.SetEnvironmentVariable("WARLINE_S003_PRODUCTION_PROBE", "1");
            nativeProductionStage = 0;
            RunS005StartupEn();
        }

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
            nativeProductionRecovery = false;
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
                            if (catalogId == "S005")
                            {
                                if (nativeProductionRecovery)
                                {
                                    var recovery = em.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(session);
                                    if (!recovery.Complete || !string.IsNullOrEmpty(recovery.Failure))
                                    { EndProductionProbe(false, "freshRecoveryIncomplete"); return; }
                                }
                                else
                                {
                                    var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                                    if (!SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, out var snapshot, out var failure))
                                    { EndProductionProbe(false, "paidCaptureRejected:" + failure); return; }
                                    bool paidQueue = Array.Exists(snapshot.Actors, actor => actor.OldRuntimeBuildingId == receipt.ProducerRuntimeId && actor.Production != null &&
                                        Array.Exists(actor.Production.Queue, item => item.ReceiptId == receipt.ReservationId &&
                                            item.ReceiptOwner == "$session" && item.ReceiptAttempt == snapshot.SessionId &&
                                            item.RemainingQuantity == 1 && item.PaidQuantity == 0 && item.RefundableMaterials == 0));
                                    File.WriteAllText(Path.GetFullPath(S005PaidSavePath + ".staging.json"), JsonUtility.ToJson(snapshot, true));
                                    if (!paidQueue || !SkirmishNativeCheckpointCodec.TryWriteAtomic(Path.GetFullPath(S005PaidSavePath), snapshot, setup) ||
                                        !SkirmishNativeCheckpointCodec.TryRead(Path.GetFullPath(S005PaidSavePath), setup, out _))
                                    { EndProductionProbe(false, "paidQueuePersistenceFailed"); return; }
                                    Debug.Log(NativeProductionMarker + " phase=PaidCheckpoint persisted=1 paidQueue=1 actors=" + snapshot.Actors.Length +
                                        " sources=" + snapshot.MapReferences.Length + " path=" + S005PaidSavePath);
                                }
                            }
                            nativeProductionStage = 2;
                            ScreenCapture.CaptureScreenshot("/private/tmp/" + catalogId.ToLowerInvariant() + "-production-paid.png");
                            Debug.Log(NativeProductionMarker + " phase=Paid materials=230 cost=220 receipt=" + receipt.ReservationId);
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
                Debug.Log(NativeProductionMarker + " phase=Touch target=" + button.name + " point=" + point);
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
            ScreenCapture.CaptureScreenshot("/private/tmp/" + catalogId.ToLowerInvariant() + "-production-delivered.png");
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
            var wedge = button.targetGraphic as V3RadialWedgeGraphic;
            var visiblePoint = wedge != null ? wedge.GuidanceTouchPoint : rect.TransformPoint(rect.rect.center);
            // A coast's bounding-box centre can contain its selected zoom button.
            // Select at the visible marker, while still requiring the native raycast
            // below to hit this exact region button rather than a different action.
            const string regionPrefix = "TheaterPin_";
            if (button.name.StartsWith(regionPrefix, StringComparison.Ordinal))
            {
                string map = button.name.Substring(regionPrefix.Length);
                var plate = button.transform.parent != null ? button.transform.parent.parent : null;
                if (plate != null && plate.Find("ZonePin_" + map) is RectTransform marker)
                    visiblePoint = marker.TransformPoint(marker.rect.center);
            }
            point = RectTransformUtility.WorldToScreenPoint(camera, visiblePoint);
            if (!Screen.safeArea.Contains(point)) return false;
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            return hits.Count > 0 && (hits[0].gameObject == button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform));
        }

        private static void EndProductionProbe(bool passed, string reason)
        {
            Debug.Log(NativeProductionMarker + " result=" + (passed ? "Passed" : "Failed") +
                " reason=" + reason + " input=Touch samples=" + (nativeProductionTouch?.AcceptedSamples ?? 0) +
                " acceptance=RecruitmentOnly freshRecovery=" + (nativeProductionRecovery ? 1 : 0) + " normalInputRecovery=0 playerReady=0");
            Environment.SetEnvironmentVariable("WARLINE_S003_PRODUCTION_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_NATIVE_RECOVERY_DIAGNOSTIC", null);
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
