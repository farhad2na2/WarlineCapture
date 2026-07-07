using System;
using System.IO;
using Game.Components;
using Game.Runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class TacticalFollowAttackCinematicVisualValidation
    {
        private const string DefaultOutputDirectory = "/private/tmp/warline-attack-cinematic-visual";
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const float MinimumLuma = 0.035f;
        private const float MinimumColorVariance = 0.00025f;

        private static readonly string[] PhaseNames =
        {
            "launch",
            "missile-path",
            "impact",
            "flyover"
        };

        public static void RunShotSequenceCapture()
        {
            string outputDirectory = Environment.GetEnvironmentVariable("WARLINE_ATTACK_CINEMATIC_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = DefaultOutputDirectory;

            GameObject root = null;
            Camera camera = null;
            RenderTexture renderTexture = null;
            Texture2D readback = null;
            try
            {
                Directory.CreateDirectory(outputDirectory);
                root = new GameObject("AttackCinematicVisualValidationRoot");
                SceneView.lastActiveSceneView?.Repaint();

                BuildValidationScene(root.transform);
                camera = CreateCamera(root.transform);
                renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
                {
                    name = "AttackCinematicVisualValidationRT",
                    antiAliasing = 4
                };
                readback = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false, false)
                {
                    name = "AttackCinematicVisualValidationReadback"
                };

                var context = new TacticalFollowAttackCinematicHelper.ShotContext(
                    new float3(0f, 16f, -34f),
                    new float3(0f, 1f, 54f),
                    new float3(0f, 0f, 1f),
                    new float3(0f, 16f, -34f),
                    true);
                TacticalFollowAttackCinematicPhase[] phases =
                {
                    TacticalFollowAttackCinematicPhase.Launch,
                    TacticalFollowAttackCinematicPhase.MissilePath,
                    TacticalFollowAttackCinematicPhase.Impact,
                    TacticalFollowAttackCinematicPhase.Flyover
                };
                float[] phaseTimes =
                {
                    0.35f,
                    0.55f,
                    0.35f,
                    0.95f
                };

                float totalLuma = 0f;
                float totalVariance = 0f;
                for (int i = 0; i < phases.Length; i++)
                {
                    TacticalFollowAttackCinematicHelper.Shot shot =
                        TacticalFollowAttackCinematicHelper.EvaluateShot(phases[i], phaseTimes[i], context);
                    ApplyShot(camera, shot, renderTexture);
                    DrawCinematicMarkers(context, phases[i], phaseTimes[i]);
                    string path = Path.Combine(outputDirectory, $"{i + 1:00}-{PhaseNames[i]}.png");
                    Capture(camera, renderTexture, readback, path, out float luma, out float variance);
                    totalLuma += luma;
                    totalVariance += variance;
                    Debug.Log(
                        $"[AttackCinematicVisualValidation] phase={phases[i]} path={path} luma={luma:0.0000} variance={variance:0.000000} camera={shot.CameraPosition} lookAt={shot.LookAt} fov={shot.FieldOfView:0.0}");
                }

                float averageLuma = totalLuma / phases.Length;
                float averageVariance = totalVariance / phases.Length;
                if (averageLuma < MinimumLuma || averageVariance < MinimumColorVariance)
                {
                    Fail(
                        $"Captured attack cinematic shots are too dark/flat. avgLuma={averageLuma:0.0000} avgVariance={averageVariance:0.000000} dir={outputDirectory}");
                    return;
                }

                Debug.Log(
                    $"[AttackCinematicVisualValidation] result=Passed captures={phases.Length} avgLuma={averageLuma:0.0000} avgVariance={averageVariance:0.000000} dir={outputDirectory}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
            finally
            {
                if (renderTexture != null)
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildValidationScene(Transform root)
        {
            RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.58f);
            RenderSettings.skybox = null;

            GameObject lightObject = new GameObject("ValidationSun");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;

            CreatePrimitive(root, PrimitiveType.Plane, "DesertGround", new Vector3(0f, 0f, 18f), new Vector3(14f, 1f, 12f), new Color(0.76f, 0.62f, 0.34f));
            CreateValidationJet(root, new Vector3(0f, 16f, -34f));
            CreatePrimitive(root, PrimitiveType.Cube, "ValidationMissileTrail", new Vector3(0f, 10f, 10f), new Vector3(0.32f, 0.32f, 2f), new Color(0.95f, 0.9f, 0.35f));
            CreatePrimitive(root, PrimitiveType.Cylinder, "ValidationTarget", new Vector3(0f, 1.2f, 54f), new Vector3(4.8f, 2.4f, 4.8f), new Color(0.38f, 0.33f, 0.25f));
            CreatePrimitive(root, PrimitiveType.Sphere, "ValidationImpact", new Vector3(0f, 4.2f, 54f), new Vector3(5.8f, 5.8f, 5.8f), new Color(1f, 0.48f, 0.12f));
            CreatePrimitive(root, PrimitiveType.Cube, "ValidationHangar", new Vector3(-16f, 4f, 44f), new Vector3(10f, 8f, 14f), new Color(0.32f, 0.35f, 0.32f));
            CreatePrimitive(root, PrimitiveType.Cube, "ValidationWall", new Vector3(11f, 3f, 38f), new Vector3(2f, 6f, 28f), new Color(0.45f, 0.42f, 0.35f));
        }

        private static void CreateValidationJet(Transform root, Vector3 position)
        {
            GameObject jetRoot = new GameObject("ValidationJetRoot");
            jetRoot.transform.SetParent(root, false);
            jetRoot.transform.SetPositionAndRotation(position, Quaternion.identity);

            CreatePrimitive(jetRoot.transform, PrimitiveType.Cube, "ValidationJetBody", Vector3.zero, new Vector3(1.15f, 0.55f, 7.2f), new Color(0.12f, 0.17f, 0.22f));
            CreatePrimitive(jetRoot.transform, PrimitiveType.Cube, "ValidationJetWing", new Vector3(0f, -0.02f, -0.35f), new Vector3(8.2f, 0.12f, 1.25f), new Color(0.09f, 0.13f, 0.18f));
            CreatePrimitive(jetRoot.transform, PrimitiveType.Cube, "ValidationJetTail", new Vector3(0f, 0.38f, -3.25f), new Vector3(3.2f, 0.12f, 0.85f), new Color(0.09f, 0.13f, 0.18f));
            CreatePrimitive(jetRoot.transform, PrimitiveType.Cube, "ValidationJetNose", new Vector3(0f, 0f, 3.85f), new Vector3(0.75f, 0.45f, 0.9f), new Color(0.18f, 0.22f, 0.26f));
        }

        private static GameObject CreatePrimitive(
            Transform root,
            PrimitiveType type,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            Quaternion rotation = default)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(root, false);
            gameObject.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            gameObject.transform.localScale = scale;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateMaterial(color);
            return gameObject;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var material = new Material(shader)
            {
                color = color
            };
            return material;
        }

        private static Camera CreateCamera(Transform root)
        {
            GameObject cameraObject = new GameObject("AttackCinematicVisualValidationCamera");
            cameraObject.transform.SetParent(root, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.43f, 0.68f, 0.88f);
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 500f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            return camera;
        }

        private static void ApplyShot(
            Camera camera,
            TacticalFollowAttackCinematicHelper.Shot shot,
            RenderTexture renderTexture)
        {
            camera.transform.position = (Vector3)shot.CameraPosition;
            Vector3 direction = (Vector3)(shot.LookAt - shot.CameraPosition);
            camera.transform.rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
            camera.fieldOfView = shot.FieldOfView;
            camera.targetTexture = renderTexture;
        }

        private static void DrawCinematicMarkers(
            in TacticalFollowAttackCinematicHelper.ShotContext context,
            TacticalFollowAttackCinematicPhase phase,
            float phaseElapsedSeconds)
        {
            float elapsed = phase switch
            {
                TacticalFollowAttackCinematicPhase.Launch => phaseElapsedSeconds,
                TacticalFollowAttackCinematicPhase.MissilePath => TacticalFollowAttackCinematicHelper.LaunchDurationSeconds + phaseElapsedSeconds,
                TacticalFollowAttackCinematicPhase.Impact => TacticalFollowAttackCinematicHelper.LaunchDurationSeconds + TacticalFollowAttackCinematicHelper.MissilePathDurationSeconds + phaseElapsedSeconds,
                TacticalFollowAttackCinematicPhase.Flyover => TacticalFollowAttackCinematicHelper.LaunchDurationSeconds + TacticalFollowAttackCinematicHelper.MissilePathDurationSeconds + TacticalFollowAttackCinematicHelper.ImpactDurationSeconds + phaseElapsedSeconds,
                _ => 0f
            };
            float projectileProgress = TacticalFollowAttackCinematicHelper.EvaluateProjectileProgress(elapsed);
            Vector3 missilePosition = (Vector3)math.lerp(context.LaunchPosition, context.ImpactPosition, projectileProgress);
            GameObject marker = GameObject.Find("ValidationMissileTrail");
            if (marker != null)
            {
                Vector3 launchPosition = (Vector3)context.LaunchPosition;
                float trailLength = Mathf.Max(2f, Vector3.Distance(launchPosition, missilePosition));
                marker.transform.SetPositionAndRotation(
                    Vector3.Lerp(launchPosition, missilePosition, 0.5f),
                    Quaternion.LookRotation((Vector3)context.AttackDirection, Vector3.up));
                marker.transform.localScale = new Vector3(0.32f, 0.32f, trailLength);
            }

            GameObject jet = GameObject.Find("ValidationJetRoot");
            if (jet != null)
            {
                float flyoverNormalized = phase == TacticalFollowAttackCinematicPhase.Flyover
                    ? math.saturate(phaseElapsedSeconds / TacticalFollowAttackCinematicHelper.FlyoverDurationSeconds)
                    : 0f;
                float3 jetPosition = phase switch
                {
                    TacticalFollowAttackCinematicPhase.Launch => context.LaunchPosition,
                    TacticalFollowAttackCinematicPhase.MissilePath => context.LaunchPosition + context.AttackDirection * 30f + new float3(0f, 0.5f, 0f),
                    TacticalFollowAttackCinematicPhase.Impact => context.ImpactPosition - context.AttackDirection * 10f + new float3(0f, 11f, 0f),
                    TacticalFollowAttackCinematicPhase.Flyover => context.ImpactPosition + context.AttackDirection * math.lerp(8f, 46f, flyoverNormalized) + new float3(0f, 13f, 0f),
                    _ => context.LaunchPosition
                };
                jet.transform.SetPositionAndRotation(
                    (Vector3)jetPosition,
                    Quaternion.LookRotation((Vector3)context.AttackDirection, Vector3.up));
            }
        }

        private static void Capture(
            Camera camera,
            RenderTexture renderTexture,
            Texture2D readback,
            string path,
            out float luma,
            out float variance)
        {
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.Render();
                RenderTexture.active = renderTexture;
                readback.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0, false);
                readback.Apply(false, false);
                File.WriteAllBytes(path, readback.EncodeToPNG());
                CalculateImageStats(readback.GetPixels32(), out luma, out variance);
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private static void CalculateImageStats(Color32[] pixels, out float luma, out float variance)
        {
            double sum = 0d;
            double sumSq = 0d;
            for (int i = 0; i < pixels.Length; i++)
            {
                float value = (pixels[i].r * 0.2126f + pixels[i].g * 0.7152f + pixels[i].b * 0.0722f) / 255f;
                sum += value;
                sumSq += value * value;
            }

            double average = sum / pixels.Length;
            luma = (float)average;
            variance = (float)math.max(0d, sumSq / pixels.Length - average * average);
        }

        private static void Fail(string message)
        {
            Debug.LogError($"[AttackCinematicVisualValidation] result=Failed {message}");
            EditorApplication.Exit(1);
        }
    }
}
