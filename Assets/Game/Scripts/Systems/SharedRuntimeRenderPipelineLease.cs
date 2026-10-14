using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Runtime
{
    /// <summary>
    /// Owns runtime URP assets across scene changes. Match quality and Campaign
    /// presentation share one asset so Replay does not rebuild URP every match.
    /// </summary>
    public static class SharedRuntimeRenderPipelineLease
    {
        private sealed class CachedPipeline
        {
            public UniversalRenderPipelineAsset Source;
            public UniversalRenderPipelineAsset Runtime;
            public float ShadowDistance;
            public int ShadowResolution;
            public int ShadowCascades;
        }

        private static readonly Dictionary<UniversalRenderPipelineAsset, CachedPipeline> Cache = new();
        private static readonly List<UniversalRenderPipelineAsset> Retired = new();
        private static readonly List<object> CampaignOwners = new();
        private static RenderPipelineAsset originalQualityOverride;
        private static CachedPipeline selected;
        private static bool initialized;

#if UNITY_EDITOR
        private static bool editorRetirementQueued;
#endif

        public static int RuntimeAssetCount => Cache.Count;
        public static UniversalRenderPipelineAsset SelectedAsset => selected?.Runtime;
        public static bool HasCampaignShadowOverride => CampaignOwners.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBeforeSceneLoad()
        {
            // Also handles Editor play sessions with domain reload disabled.
            ShutdownSession();
        }

        public static UniversalRenderPipelineAsset SourceFor(RenderPipelineAsset candidate)
        {
            if (candidate == null)
                return null;
            foreach (CachedPipeline entry in Cache.Values)
                if (ReferenceEquals(entry.Runtime, candidate))
                    return entry.Source;
            return null;
        }

        public static UniversalRenderPipelineAsset SelectQuality(
            UniversalRenderPipelineAsset source, float renderScale)
        {
            if (source == null)
                return null;

            EnsureSession();
            if (!Cache.TryGetValue(source, out CachedPipeline next) || next.Runtime == null)
            {
                var runtime = UnityEngine.Object.Instantiate(source);
                runtime.name = "Runtime Quality: " + source.name;
                runtime.hideFlags = HideFlags.DontSave;
                next = new CachedPipeline
                {
                    Source = source,
                    Runtime = runtime,
                    ShadowDistance = source.shadowDistance,
                    ShadowResolution = source.mainLightShadowmapResolution,
                    ShadowCascades = source.shadowCascadeCount
                };
                Cache[source] = next;
            }

            if (!ReferenceEquals(selected, next) && HasCampaignShadowOverride && selected != null)
                RestoreShadows(selected);

            // URP's setter only writes the backing field, but avoid even that
            // write when a match reselects the same quality on Replay.
            if (!Mathf.Approximately(next.Runtime.renderScale, renderScale))
                next.Runtime.renderScale = renderScale;
            selected = next;
            if (HasCampaignShadowOverride)
                ApplyCampaignShadows(next);

            EnsureSelected();
            return next.Runtime;
        }

        public static bool AcquireCampaignShadows(object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (selected == null)
            {
                // Campaign scene binding may complete before the match quality
                // system initializes. Its eventual quality selection will reuse
                // this entry when it refers to the same source asset.
                if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset source)
                    return false;
                SelectQuality(source, source.renderScale);
            }

            if (!HasOwner(owner))
                CampaignOwners.Add(owner);
            ApplyCampaignShadows(selected);
            EnsureSelected();
            return true;
        }

        public static void ReleaseCampaignShadows(object owner)
        {
            int index = FindOwner(owner);
            if (index < 0)
                return;
            CampaignOwners.RemoveAt(index);
            if (!HasCampaignShadowOverride && selected != null)
                RestoreShadows(selected);
        }

        public static void EnsureSelected()
        {
            if (selected != null && selected.Runtime != null &&
                QualitySettings.renderPipeline != selected.Runtime)
                QualitySettings.renderPipeline = selected.Runtime;
        }

        public static void ShutdownSession()
        {
            if (!initialized)
            {
                RetireInactiveAssets();
                return;
            }

            if (HasCampaignShadowOverride && selected != null)
                RestoreShadows(selected);
            CampaignOwners.Clear();

            if (SourceFor(QualitySettings.renderPipeline) != null)
                QualitySettings.renderPipeline = originalQualityOverride;

            foreach (CachedPipeline entry in Cache.Values)
                if (entry.Runtime != null)
                    Retired.Add(entry.Runtime);
            Cache.Clear();
            selected = null;
            originalQualityOverride = null;
            initialized = false;
            Application.quitting -= ShutdownSession;
            RetireInactiveAssets();
#if UNITY_EDITOR
            // A pipeline can remain Unity's current pipeline until the next
            // Editor turn after restoring the quality override. Retry once;
            // never spin while GraphicsSettings still owns that asset.
            if (Retired.Count > 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
                QueueEditorRetirement();
#endif
        }

        private static void EnsureSession()
        {
            if (initialized)
                return;

            originalQualityOverride = QualitySettings.renderPipeline;
            initialized = true;
            Application.quitting -= ShutdownSession;
            Application.quitting += ShutdownSession;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnEditorPlayModeChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
#endif
        }

        private static void ApplyCampaignShadows(CachedPipeline entry)
        {
            if (entry?.Runtime == null)
                return;
            entry.Runtime.shadowDistance = Mathf.Max(entry.ShadowDistance,
                Application.isMobilePlatform ? 160f : 260f);
            if (!Application.isMobilePlatform)
            {
                entry.Runtime.mainLightShadowmapResolution = Mathf.Max(entry.ShadowResolution, 2048);
                entry.Runtime.shadowCascadeCount = Mathf.Max(entry.ShadowCascades, 2);
            }
        }

        private static bool HasOwner(object owner) => FindOwner(owner) >= 0;

        private static int FindOwner(object owner)
        {
            for (int index = 0; index < CampaignOwners.Count; index++)
                if (ReferenceEquals(CampaignOwners[index], owner))
                    return index;
            return -1;
        }

        private static void RestoreShadows(CachedPipeline entry)
        {
            if (entry?.Runtime == null)
                return;
            entry.Runtime.shadowDistance = entry.ShadowDistance;
            entry.Runtime.mainLightShadowmapResolution = entry.ShadowResolution;
            entry.Runtime.shadowCascadeCount = entry.ShadowCascades;
        }

        private static void RetireInactiveAssets()
        {
            for (int index = Retired.Count - 1; index >= 0; index--)
            {
                UniversalRenderPipelineAsset asset = Retired[index];
                if (asset == null)
                {
                    Retired.RemoveAt(index);
                    continue;
                }
                if (QualitySettings.renderPipeline == asset ||
                    GraphicsSettings.currentRenderPipeline == asset)
                    continue;

                Retired.RemoveAt(index);
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(asset);
                else
                    UnityEngine.Object.DestroyImmediate(asset);
            }
        }

#if UNITY_EDITOR
        private static void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ShutdownSession();
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RetireInactiveAssets();
                if (Retired.Count > 0)
                    QueueEditorRetirement();
            }
        }

        private static void QueueEditorRetirement()
        {
            if (editorRetirementQueued)
                return;
            editorRetirementQueued = true;
            EditorApplication.delayCall += OnEditorRetirementDelayCall;
        }

        private static void OnEditorRetirementDelayCall()
        {
            editorRetirementQueued = false;
            RetireInactiveAssets();
        }
#endif
    }
}
