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
        private EntityQuery _damageQuery;
        private int _lastSeenDamageEventId = -1;
        private bool _firstContactSeen;
        private float _nextBaseHitTime;
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
            _damageQuery = GetEntityQuery(
                ComponentType.ReadOnly<CombatDamageObservationQueueComponent>(),
                ComponentType.ReadOnly<CombatDamageObservationElement>());
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
            ConsumeContactAndBaseDamage(new float3(cameraPosition.x, cameraPosition.y, cameraPosition.z), cameraPosition.y);
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

        // First contact and main-base damage reuse the existing render-only shake. The camera transform
        // is never changed outside the render callbacks, so framing and picking are unaffected.
        private void ConsumeContactAndBaseDamage(float3 cameraPosition, float cameraHeight)
        {
            if (_damageQuery.CalculateEntityCount() != 1)
                return;

            Entity queueEntity = _damageQuery.GetSingletonEntity();
            int lastEventId = EntityManager.GetComponentData<CombatDamageObservationQueueComponent>(queueEntity).LastEventId;
            if (_lastSeenDamageEventId < 0 || lastEventId < _lastSeenDamageEventId)
            {
                // New attempt (or first frame): do not replay old events, re-arm first contact.
                _lastSeenDamageEventId = lastEventId;
                _firstContactSeen = false;
                return;
            }

            if (lastEventId == _lastSeenDamageEventId)
                return;

            float now = UnityEngine.Time.unscaledTime;
            DynamicBuffer<CombatDamageObservationElement> events =
                EntityManager.GetBuffer<CombatDamageObservationElement>(queueEntity, true);
            for (int i = 0; i < events.Length; i++)
            {
                CombatDamageObservationElement hit = events[i];
                if (hit.EventId <= _lastSeenDamageEventId)
                    continue;

                byte sourceFaction = FactionOf(hit.SourceEntity);
                byte targetFaction = FactionOf(hit.TargetEntity);
                bool exchange = CombatCameraShakeHelper.IsPlayerEnemyExchange(sourceFaction, targetFaction);
                bool firstContact = exchange && !_firstContactSeen;
                if (exchange)
                    _firstContactSeen = true;

                bool mainBaseHit = exchange && now >= _nextBaseHitTime && IsMainBase(hit.TargetEntity);
                float trauma = CombatCameraShakeHelper.ResolveContactTrauma(
                    firstContact, mainBaseHit, hit.DamageApplied, hit.TargetMaxHealth);
                if (trauma <= 0f)
                    continue;

                if (mainBaseHit)
                    _nextBaseHitTime = now + CombatCameraShakeHelper.MainBaseHitCooldownSeconds;
                float distance = math.distance(cameraPosition, hit.TargetWorldPosition);
                _trauma = math.min(1f, _trauma + CombatCameraShakeHelper.Attenuate(trauma, distance, cameraHeight));
            }

            _lastSeenDamageEventId = lastEventId;
        }

        private byte FactionOf(Entity entity) =>
            entity != Entity.Null && EntityManager.Exists(entity) && EntityManager.HasComponent<Faction>(entity)
                ? EntityManager.GetComponentData<Faction>(entity).Id
                : (byte)0;

        private bool IsMainBase(Entity entity) =>
            entity != Entity.Null && EntityManager.Exists(entity) && EntityManager.HasComponent<SkirmishMainBase>(entity);

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
