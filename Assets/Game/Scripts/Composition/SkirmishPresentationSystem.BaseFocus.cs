using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>
    /// Base focus framing (S002 designer retest 2026-11-18: the alert tap framed bare sand with
    /// the attacker off the right edge). A player base tap (alert row, "YOUR BASE", full map)
    /// frames the home base together with the attacker the HUD shows as in sight, or, when the
    /// attacker is hidden, with the struck side of the base, inside the open play area the HUD
    /// leaves (live layout, else the measured default). An attacker too far away to pair with the
    /// base stays in the play area with the way back toward the base (see
    /// SkirmishCameraFraming.TryFrameHomeThreat). Writes only the existing one-shot camera focus
    /// request: no selection, no unit orders, no order changes.
    /// </summary>
    public partial class SkirmishPresentationSystem
    {
        // Same window as the HUD's "recently hit" facts.
        private const float BaseFocusRecentDamageSeconds = 8f;
        private const float FallbackFocusAspect = 2400f / 1080f;

        private bool TryRequestBaseThreatFocus(Entity home, Vector3 homeWorld, in SkirmishGroundFraming framing, Entity requestEntity)
        {
            var camera = Camera.main;
            float aspect = camera != null && camera.aspect > 0f && float.IsFinite(camera.aspect) ? camera.aspect : FallbackFocusAspect;
            Rect play = SkirmishCameraFraming.DefaultHudPlayViewport;
            bool live = false;
            if (view != null && camera != null && view.TryReadSafePlayArea(out var pixels) &&
                SkirmishCameraFraming.TryPixelsToViewport(pixels, camera.pixelRect, out var measured))
            { play = measured; live = true; }

            // Only what the player is shown: the attacker while the HUD reports it in sight (the
            // HUD admits faction-1 fog-visible hostiles only). A hidden attacker is never framed;
            // the struck side of the base (a fixed short distance, direction only) is used instead.
            bool hasThreat = false;
            Vector3 threat = default;
            string source = "None";
            if (view != null && view.BaseAlertAttackerShown)
            { hasThreat = true; threat = view.BaseAlertAttackerWorld; source = "AttackerInSight"; }
            else if (TryReadDamagedSide(home, homeWorld, out threat))
            { hasThreat = true; source = "DamagedSide"; }

            float maximum = SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(framing.WideHeight);
            if (!SkirmishCameraFraming.TryFrameHomeThreat(framing, homeWorld, hasThreat, threat, aspect, play, maximum,
                    out var eye, out var kind, out float corridor)) return false;
            var ground = SkirmishCameraFraming.ZeroPlaneGroundCenter(eye, framing.Pitch, framing.Yaw);
            EntityManager.SetComponentData(requestEntity, new RuntimeCameraFocusRequestComponent
            {
                Requested = 1, UseExplicitPerspective = 1,
                Perspective = new float4(eye.y, framing.Pitch, framing.Yaw, framing.FieldOfView),
                World = ground
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[SkirmishBaseFocus] kind=" + kind + " threat=" + source + " corridor=" + corridor.ToString("F2") +
                " home=" + homeWorld + (source == "AttackerInSight" ? " attacker=" + threat : source == "DamagedSide" ? " side=" + threat : "") +
                " height=" + eye.y.ToString("F2") + " pitch=" + framing.Pitch + " yaw=" + framing.Yaw.ToString("F1") +
                " aspect=" + aspect.ToString("F3") + " playArea=" + (live ? "Live" : "Default") +
                "(" + play.xMin.ToString("F2") + "," + play.yMin.ToString("F2") + "," + play.xMax.ToString("F2") + "," + play.yMax.ToString("F2") + ")" +
                " groundRequest=" + ground + " orders=0 selection=0");
#endif
            return true;
        }

        // Latest recent hit on the home base: a point DamagedSideMetres out on the struck side.
        private bool TryReadDamagedSide(Entity home, Vector3 homeWorld, out Vector3 side)
        {
            side = default;
            using var queues = EntityManager.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
            if (queues.CalculateEntityCount() != 1) return false;
            var queue = queues.GetSingletonEntity();
            if (!EntityManager.HasBuffer<CombatDamageObservationElement>(queue)) return false;
            var observations = EntityManager.GetBuffer<CombatDamageObservationElement>(queue, true);
            float now = (float)World.Time.ElapsedTime;
            int latest = -1;
            for (int i = 0; i < observations.Length; i++)
            {
                var observation = observations[i];
                if (observation.TargetEntity != home || observation.DamageApplied <= 0 || now - observation.ObservedAt > BaseFocusRecentDamageSeconds) continue;
                if (math.all(observation.SourceWorldPosition == float3.zero) || !math.all(math.isfinite(observation.SourceWorldPosition))) continue;
                if (latest < 0 || observation.EventId > observations[latest].EventId) latest = i;
            }
            return latest >= 0 && SkirmishCameraFraming.TryDamagedSide(homeWorld, observations[latest].SourceWorldPosition,
                SkirmishCameraFraming.DamagedSideMetres, out side);
        }
    }
}
