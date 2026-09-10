using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using System.Reflection;
using Game.Missions.Contracts;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Opt-in, read-only observation of the real mission camera and GPU animation playback.</summary>
    public static class MissionMotionEditorAudit
    {
        private const string ActiveKey = "Warline.Completion.MotionAudit";
        private static double nextSample, nextCapture;
        private static int captureCount;
        public static bool Active => SessionState.GetBool(ActiveKey, false);
        public static void Begin()
        {
            SessionState.SetBool(ActiveKey, true);
            string output = "/private/tmp/warline-completion-motion/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            SessionState.SetString(ActiveKey + ".Output", output);
            Debug.Log("[MissionMotionAudit] output=" + output);
            nextSample = nextCapture = 0;
            captureCount = 0;
        }

        public static void RunM3()
        {
            Begin();
            M03RadarWarningEditorLaunchProbe.RunCommittedResultValidation();
        }

        public static void Sample(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if (!Active || Camera.main == null || EditorApplication.timeSinceStartup < nextSample ||
                runtime.Phase < MissionPhaseKind.FindSquad || runtime.Phase > MissionPhaseKind.SecureCorridor) return;
            nextSample = EditorApplication.timeSinceStartup + .2;
            Camera camera = Camera.main;
            string folder = SessionState.GetString(ActiveKey + ".Output", "/private/tmp/warline-completion-motion") + "/" + runtime.MissionId;
            Directory.CreateDirectory(folder);
            var sample = new SampleRecord
            {
                frame = Time.frameCount, clock = facts.ElapsedMilliseconds, phase = runtime.Phase.ToString(),
                camera = camera.transform.position, cameraEuler = camera.transform.eulerAngles,
                openingStage = em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)
                    ? em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage : -1
            };
            if(UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel))
            {
                var view = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>(FindObjectsInactive.Include);
                var shell = UnityEngine.Object.FindAnyObjectByType<UIShellContentView>();
                object owner = shell != null ? typeof(UIShellContentView).GetField("_mainMenuPlayUi", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(shell) : null;
                object helper = owner?.GetType().GetField("_matchHudAssistantUiSystem", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner);
                string state = "";
                if(helper != null) foreach(string field in new[] { "_pendingTutorialStep", "_completedTutorialStep", "_displayedTutorialStep", "_tutorialShowAtUnscaledTime", "_finalTutorialSuppressed" })
                    state += field + "=" + helper.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(helper) + ";";
                sample.aria = new AriaRecord { step = panel.TutorialStep, hasRecommendation = panel.HasRecommendation,
                    title = panel.RecommendationTitle, body = panel.RecommendationBody, visible = view != null && view.IsPresentationVisible,
                    actualBody = view != null ? view.BodyText.text : "", state = state };
            }
            using var query = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(LocalTransform), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(actor);
                if (!role.SessionToken.Equals(runtime.SessionToken)) continue;
                var position = em.GetComponentData<LocalTransform>(actor).Position;
                var record = new ActorRecord
                {
                    source = em.HasComponent<UnitSourcePrefabKey>(actor) ? em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString() : "",
                    vehicleMotion = em.HasComponent<UnitMovementBehavior>(actor) && em.GetComponentData<UnitMovementBehavior>(actor).UsesVehicleMotion != 0,
                    entity = actor.Index, name = role.MissionRoleId.ToString(), position = position,
                    viewport = camera.WorldToViewportPoint(position), health = em.GetComponentData<UnitHealth>(actor).Current,
                    moving = em.HasComponent<UnitMoveVisualComponent>(actor) && em.GetComponentData<UnitMoveVisualComponent>(actor).IsMoving != 0,
                    engaged = em.HasComponent<EngageTarget>(actor), aboard = em.HasComponent<UnitTransportPassenger>(actor),
                    visual = em.HasComponent<UnitRenderVisualComponent>(actor) ? em.GetComponentData<UnitRenderVisualComponent>(actor).Current : -1
                };
                if (em.HasComponent<UnitResolvedAnimationIndex>(actor))
                {
                    record.index = em.GetComponentData<UnitResolvedAnimationIndex>(actor).Value;
                    if (em.HasBuffer<UnitAnimationOrderEntry>(actor))
                    {
                        var order = em.GetBuffer<UnitAnimationOrderEntry>(actor);
                        if (record.index > 0 && record.index <= order.Length)
                            record.clip = ((UnitAnimationKind)order[record.index - 1].Kind).ToString();
                    }
                }
                var seen = new HashSet<Entity>();
                Inspect(em, actor, false, seen, record.renderers);
                if (em.HasComponent<UnitDetailedVisualReference>(actor)) Inspect(em, em.GetComponentData<UnitDetailedVisualReference>(actor).Root, false, seen, record.renderers);
                if (em.HasComponent<UnitModelInstanceReference>(actor)) Inspect(em, em.GetComponentData<UnitModelInstanceReference>(actor).Instance, false, seen, record.renderers);
                if (em.HasComponent<UnitMidLodInstanceReference>(actor)) Inspect(em, em.GetComponentData<UnitMidLodInstanceReference>(actor).Instance, false, seen, record.renderers);
                if (em.HasComponent<UnitLowLodInstanceReference>(actor)) Inspect(em, em.GetComponentData<UnitLowLodInstanceReference>(actor).Instance, false, seen, record.renderers);
                sample.actors.Add(record);
            }
            File.AppendAllText(folder + "/motion.jsonl", JsonUtility.ToJson(sample) + "\n");
            if (captureCount < 40 && EditorApplication.timeSinceStartup >= nextCapture)
            {
                nextCapture = EditorApplication.timeSinceStartup + 5;
                ScreenCapture.CaptureScreenshot(folder + "/camera-" + (++captureCount).ToString("D2") + "-" + facts.ElapsedMilliseconds + ".png");
            }
        }

        private static void Inspect(EntityManager em, Entity entity, bool parentHidden, HashSet<Entity> seen, List<RendererRecord> rows)
        {
            if (!em.Exists(entity) || !seen.Add(entity)) return;
            bool hidden = parentHidden || em.HasComponent<Disabled>(entity) || em.HasComponent<DisableRendering>(entity);
            if (em.HasComponent<MaterialPropertyShowModel>(entity)) hidden |= em.GetComponentData<MaterialPropertyShowModel>(entity).Value < .5f;
            Entity animator = em.HasComponent<MeshLODComponent>(entity) ? em.GetComponentData<MeshLODComponent>(entity).Group : entity;
            if (em.HasComponent<MaterialAnimationData>(animator) &&
                (entity == animator || em.HasComponent<MaterialPropertyRenderPixel>(entity)))
            {
                var data = em.GetComponentData<MaterialAnimationData>(animator);
                rows.Add(new RendererRecord
                {
                    entity = entity.Index, hidden = hidden, mesh = em.HasComponent<MaterialMeshInfo>(entity),
                    target = em.HasComponent<MaterialAnimationIndex>(animator) ? em.GetComponentData<MaterialAnimationIndex>(animator).Value : -1,
                    playing = data.AnimationIndex, time = data.Time, pixels = data.RenderConfig,
                    materialPixels = em.HasComponent<MaterialPropertyRenderPixel>(entity)
                        ? em.GetComponentData<MaterialPropertyRenderPixel>(entity).Value : default
                });
            }
            if (!em.HasBuffer<Child>(entity)) return;
            var children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++) Inspect(em, children[i].Value, hidden, seen, rows);
        }

        [Serializable] private sealed class SampleRecord
        {
            public int frame, clock, openingStage;
            public string phase;
            public AriaRecord aria;
            public Vector3 camera, cameraEuler;
            public List<ActorRecord> actors = new();
        }
        [Serializable] private sealed class AriaRecord
        {
            public int step;
            public bool hasRecommendation, visible;
            public string title, body, actualBody, state;
        }
        [Serializable] private sealed class ActorRecord
        {
            public int entity, health, index, visual;
            public string name, clip, source;
            public float3 position;
            public Vector3 viewport;
            public bool moving, engaged, aboard, vehicleMotion;
            public List<RendererRecord> renderers = new();
        }
        [Serializable] private sealed class RendererRecord
        {
            public int entity, target, playing;
            public bool hidden, mesh;
            public float time;
            public float3 pixels, materialPixels;
        }
    }
}
