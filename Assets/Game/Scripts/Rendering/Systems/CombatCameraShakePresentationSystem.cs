using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public sealed partial class CombatCameraShakePresentationSystem : SystemBase
    {
        private const string ReducedMotionPreferenceKey = "Game.Settings.Accessibility.ReducedMotion";
        private const float ReducedMotionRefreshSeconds = 1f;

        private EntityQuery _audioRequestQuery;
        private EntityQuery _finalKillQuery;
        private int _lastSeenRequestId = -1;
        private bool _finalKillKicked;
        private float _trauma;
        private float _time;
        private float _reducedMotionRefreshTimer;
        private bool _reducedMotion;
        private Camera _worldCamera;
        private Camera _offsetCamera;
        private Vector3 _appliedOffset;

        protected override void OnCreate()
        {
            _audioRequestQuery = GetEntityQuery(
                ComponentType.ReadOnly<AudioPlaybackRequestQueueComponent>(),
                ComponentType.ReadOnly<AudioPlaybackRequestElement>());
            _finalKillQuery = GetEntityQuery(ComponentType.ReadOnly<CampaignMissionFinalKillCinematicComponent>());
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RemoveOffset();
            _worldCamera = null;
        }

        protected override void OnUpdate()
        {
            float deltaTime = UnityEngine.Time.unscaledDeltaTime;
            RefreshReducedMotion(deltaTime);
            if (!RuntimeCameraReferenceSystem.TryGetWorldCamera(World, out Camera camera))
            {
                _worldCamera = null;
                _trauma = 0f;
                return;
            }

            _worldCamera = camera;
            Vector3 cameraPosition = camera.transform.position;
            ConsumeExplosionRequests(new float3(cameraPosition.x, cameraPosition.y, cameraPosition.z), cameraPosition.y);
            ConsumeFinalKill();
            _trauma = _reducedMotion ? 0f : CombatCameraShakeHelper.Decay(_trauma, deltaTime);
            _time += deltaTime;
        }

        private void ConsumeExplosionRequests(float3 cameraPosition, float cameraHeight)
        {
            if (_audioRequestQuery.CalculateEntityCount() != 1)
                return;

            Entity audioEntity = _audioRequestQuery.GetSingletonEntity();
            int lastRequestId = EntityManager.GetComponentData<AudioPlaybackRequestQueueComponent>(audioEntity).LastRequestId;
            if (_lastSeenRequestId < 0 || lastRequestId < _lastSeenRequestId)
            {
                _lastSeenRequestId = lastRequestId;
                return;
            }

            if (lastRequestId == _lastSeenRequestId)
                return;

            DynamicBuffer<AudioPlaybackRequestElement> requests =
                EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity, true);
            for (int i = requests.Length - 1; i >= 0; i--)
            {
                AudioPlaybackRequestElement request = requests[i];
                if (request.RequestId <= _lastSeenRequestId)
                    break;
                if (request.HasWorldPosition == 0)
                    continue;

                float trauma = CombatCameraShakeHelper.ResolveEventTrauma(request.EventHash);
                if (trauma <= 0f)
                    continue;

                float distance = math.distance(cameraPosition, request.WorldPosition);
                _trauma = math.min(1f, _trauma + CombatCameraShakeHelper.Attenuate(trauma, distance, cameraHeight));
            }

            _lastSeenRequestId = lastRequestId;
        }

        private void ConsumeFinalKill()
        {
            if (_finalKillQuery.CalculateEntityCount() != 1 ||
                _finalKillQuery.GetSingleton<CampaignMissionFinalKillCinematicComponent>().Active == 0)
            {
                _finalKillKicked = false;
                return;
            }

            if (_finalKillKicked)
                return;
            _finalKillKicked = true;
            _trauma = math.max(_trauma, CombatCameraShakeHelper.FinalKillTrauma);
        }

        private void RefreshReducedMotion(float deltaTime)
        {
            _reducedMotionRefreshTimer -= deltaTime;
            if (_reducedMotionRefreshTimer > 0f)
                return;
            _reducedMotionRefreshTimer = ReducedMotionRefreshSeconds;
            _reducedMotion = PlayerPrefs.GetInt(ReducedMotionPreferenceKey, 0) != 0;
        }

        // The offset exists only while the world camera renders, so input, picking and
        // camera controllers never observe a shaken transform.
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null || camera != _worldCamera || _offsetCamera != null || _trauma <= 0.001f)
                return;

            Transform cameraTransform = camera.transform;
            float2 offset = CombatCameraShakeHelper.EvaluateOffset(_trauma, _time, cameraTransform.position.y);
            _appliedOffset = cameraTransform.right * offset.x + cameraTransform.up * offset.y;
            cameraTransform.position += _appliedOffset;
            _offsetCamera = camera;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _offsetCamera)
                RemoveOffset();
        }

        private void RemoveOffset()
        {
            if (_offsetCamera != null)
                _offsetCamera.transform.position -= _appliedOffset;
            _offsetCamera = null;
            _appliedOffset = Vector3.zero;
        }
    }
}
