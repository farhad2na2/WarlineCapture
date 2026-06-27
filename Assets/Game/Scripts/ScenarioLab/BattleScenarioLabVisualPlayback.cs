using System.Collections;
using UnityEngine;

public sealed class BattleScenarioLabVisualPlayback : MonoBehaviour
{
    [SerializeField] private Camera scenarioCamera;
    [SerializeField] private Transform groundLauncherVisual;
    [SerializeField] private Transform airLauncherVisual;
    [SerializeField] private Transform radarVisual;
    [SerializeField] private Transform defendedTargetVisual;
    [SerializeField] private Transform incomingMissileVisual;
    [SerializeField] private Transform interceptorVisual;
    [SerializeField] private LineRenderer incomingTrail;
    [SerializeField] private LineRenderer interceptorTrail;
    [SerializeField] private ParticleSystem groundLaunchFlash;
    [SerializeField] private ParticleSystem airLaunchFlash;
    [SerializeField] private ParticleSystem interceptExplosion;
    [SerializeField] private float playbackTimeScale = 0.65f;

    private static readonly Vector3 AirLauncherPosition = new(0f, 1.2f, 0f);
    private static readonly Vector3 DefendedTargetPosition = new(-40f, 1.2f, 0f);

    private Coroutine playbackRoutine;

    public void Play(BattleScenarioVariant variant, BattleScenarioMetrics metrics)
    {
        if (!isActiveAndEnabled)
            return;

        if (playbackRoutine != null)
            StopCoroutine(playbackRoutine);

        playbackRoutine = StartCoroutine(PlayRoutine(variant, metrics));
    }

    private IEnumerator PlayRoutine(BattleScenarioVariant variant, BattleScenarioMetrics metrics)
    {
        float interceptTime = metrics != null && metrics.Intercepted ? metrics.InterceptTimeSeconds : 3f;
        float launchTime = metrics != null && metrics.InterceptorLaunched ? metrics.LaunchTimeSeconds : 1f;
        float incomingDuration = Mathf.Max(interceptTime + 1.5f, 8f / Mathf.Max(0.1f, variant.IncomingThreatSpeedMultiplier));
        Vector3 incomingStart = GetIncomingStart(variant);
        Vector3 interceptPoint = IncomingPositionAt(variant, Mathf.Max(0.05f, interceptTime), incomingDuration);

        ResetVisuals(incomingStart);
        PositionSupportVisuals(variant);
        SetCamera(new Vector3(92f, 42f, -92f), new Vector3(54f, 7f, 0f));
        yield return WaitScaled(0.55f);

        groundLaunchFlash?.Play();
        Show(incomingMissileVisual, true);
        yield return MoveCamera(new Vector3(120f, 20f, -36f), incomingStart + new Vector3(-18f, 4f, 0f), 0.45f);

        float elapsed = 0f;
        while (elapsed < launchTime)
        {
            elapsed += Time.deltaTime / Mathf.Max(0.05f, playbackTimeScale);
            Vector3 incomingPosition = IncomingPositionAt(variant, elapsed, incomingDuration);
            SetMissilePose(incomingMissileVisual, incomingPosition, IncomingPositionAt(variant, elapsed + 0.05f, incomingDuration));
            SetTrail(incomingTrail, incomingStart, incomingPosition);
            SetCamera(incomingPosition + new Vector3(22f, 9f, -28f), incomingPosition + new Vector3(-12f, 1f, 0f));
            yield return null;
        }

        airLaunchFlash?.Play();
        Show(interceptorVisual, true);
        yield return MoveCamera(new Vector3(18f, 15f, -30f), AirLauncherPosition + new Vector3(30f, 10f, 0f), 0.28f);

        float interceptorElapsed = 0f;
        float interceptorDuration = Mathf.Max(0.35f, interceptTime - launchTime);
        Vector3 interceptorStart = AirLauncherPosition + new Vector3(0f, 2.5f, 0f);
        while (interceptorElapsed < interceptorDuration)
        {
            elapsed += Time.deltaTime / Mathf.Max(0.05f, playbackTimeScale);
            interceptorElapsed += Time.deltaTime / Mathf.Max(0.05f, playbackTimeScale);
            float normalized = Mathf.Clamp01(interceptorElapsed / interceptorDuration);
            Vector3 incomingPosition = IncomingPositionAt(variant, elapsed, incomingDuration);
            Vector3 interceptorPosition = Vector3.Lerp(interceptorStart, interceptPoint, EaseInOut(normalized));
            interceptorPosition.y += Mathf.Sin(normalized * Mathf.PI) * 9f;

            SetMissilePose(incomingMissileVisual, incomingPosition, IncomingPositionAt(variant, elapsed + 0.05f, incomingDuration));
            SetMissilePose(interceptorVisual, interceptorPosition, interceptPoint);
            SetTrail(incomingTrail, incomingStart, incomingPosition);
            SetTrail(interceptorTrail, interceptorStart, interceptorPosition);
            SetCamera(interceptorPosition + new Vector3(18f, 8f, -24f), Vector3.Lerp(interceptorPosition, incomingPosition, 0.6f));
            yield return null;
        }

        SetMissilePose(incomingMissileVisual, interceptPoint, interceptPoint + Vector3.left);
        SetMissilePose(interceptorVisual, interceptPoint, interceptPoint + Vector3.left);
        SetTrail(incomingTrail, incomingStart, interceptPoint);
        SetTrail(interceptorTrail, interceptorStart, interceptPoint);
        if (interceptExplosion != null)
        {
            interceptExplosion.transform.position = interceptPoint;
            interceptExplosion.Play();
        }

        Show(incomingMissileVisual, false);
        Show(interceptorVisual, false);
        yield return MoveCamera(interceptPoint + new Vector3(22f, 13f, -34f), interceptPoint, 0.45f);
        yield return WaitScaled(1.1f);
        yield return MoveCamera(new Vector3(92f, 42f, -92f), new Vector3(54f, 7f, 0f), 0.75f);
    }

    private void ResetVisuals(Vector3 incomingStart)
    {
        Show(incomingMissileVisual, false);
        Show(interceptorVisual, false);
        EnableLine(incomingTrail, false);
        EnableLine(interceptorTrail, false);
        if (incomingMissileVisual != null)
            incomingMissileVisual.position = incomingStart;
        if (interceptorVisual != null)
            interceptorVisual.position = AirLauncherPosition;
        groundLaunchFlash?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        airLaunchFlash?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        interceptExplosion?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void PositionSupportVisuals(BattleScenarioVariant variant)
    {
        if (groundLauncherVisual != null)
            groundLauncherVisual.position = GetIncomingStart(variant) + new Vector3(0f, -variant.IncomingThreatAltitude + 1.2f, 0f);
        if (airLauncherVisual != null)
            airLauncherVisual.position = AirLauncherPosition;
        if (defendedTargetVisual != null)
            defendedTargetVisual.position = DefendedTargetPosition;
        if (radarVisual != null)
        {
            radarVisual.gameObject.SetActive(variant.SupportMode == BattleScenarioSupportMode.RadarNear);
            radarVisual.position = new Vector3(variant.RadarDistanceFromLauncher, 1.1f, -12f);
        }
    }

    private static Vector3 GetIncomingStart(BattleScenarioVariant variant)
    {
        float distance = Mathf.Max(40f, variant.IncomingThreatStartDistance);
        float altitude = Mathf.Max(3f, variant.IncomingThreatAltitude);
        return new Vector3(distance, altitude, 0f);
    }

    private static Vector3 IncomingPositionAt(BattleScenarioVariant variant, float timeSeconds, float durationSeconds)
    {
        Vector3 start = GetIncomingStart(variant);
        float t = Mathf.Clamp01(timeSeconds / Mathf.Max(0.1f, durationSeconds));
        Vector3 position = Vector3.Lerp(start, DefendedTargetPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * Mathf.Max(4f, variant.IncomingThreatAltitude * 0.35f);
        return position;
    }

    private void SetCamera(Vector3 position, Vector3 lookAt)
    {
        if (scenarioCamera == null)
            return;

        scenarioCamera.transform.position = position;
        Vector3 direction = lookAt - position;
        if (direction.sqrMagnitude > 0.001f)
            scenarioCamera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private IEnumerator MoveCamera(Vector3 targetPosition, Vector3 targetLookAt, float duration)
    {
        if (scenarioCamera == null)
            yield break;

        Vector3 startPosition = scenarioCamera.transform.position;
        Vector3 startForward = scenarioCamera.transform.forward;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime / Mathf.Max(0.05f, playbackTimeScale);
            float t = EaseInOut(Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration)));
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);
            Vector3 forward = Vector3.Slerp(startForward, (targetLookAt - targetPosition).normalized, t);
            scenarioCamera.transform.position = position;
            scenarioCamera.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            yield return null;
        }

        SetCamera(targetPosition, targetLookAt);
    }

    private IEnumerator WaitScaled(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime / Mathf.Max(0.05f, playbackTimeScale);
            yield return null;
        }
    }

    private static void SetMissilePose(Transform missile, Vector3 position, Vector3 lookAt)
    {
        if (missile == null)
            return;

        missile.position = position;
        Vector3 direction = lookAt - position;
        if (direction.sqrMagnitude > 0.001f)
            missile.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static void SetTrail(LineRenderer line, Vector3 start, Vector3 end)
    {
        if (line == null)
            return;

        line.enabled = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private static void EnableLine(LineRenderer line, bool enabled)
    {
        if (line != null)
            line.enabled = enabled;
    }

    private static void Show(Transform target, bool visible)
    {
        if (target != null)
            target.gameObject.SetActive(visible);
    }

    private static float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
