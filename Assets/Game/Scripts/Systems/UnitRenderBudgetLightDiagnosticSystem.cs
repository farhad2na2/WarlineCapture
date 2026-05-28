using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnitDistance = UnitRenderBudgetDistanceSystem.UnitDistance;

public readonly struct UnitRenderBudgetLightDiagnosticSystem
{
    public void LogRenderBudgetStateLight(
        EntityManager em,
        NativeList<UnitDistance> distances,
        int detailedCount,
        bool cameraMotionActive,
        float alwaysDetailedDistanceSq,
        UnitRenderBudgetClassificationSystem classificationSystem,
        ref UnitRenderBudgetDiagnosticLogSystem diagnosticLogSystem)
    {
        int targetDetail = 0;
        int targetMid = 0;
        int targetLow = 0;
        int targetFar = 0;
        int visibleCharacters = 0;
        int visibleCharacterNotDetail = 0;
        int visibleCharacterActiveMid = 0;
        int visibleCharacterActiveLow = 0;
        int visibleCharacterActiveFar = 0;
        int visibleCharacterScreenEdge = 0;
        int visibleCharacterScreenEdgeDetail = 0;
        int visibleNearDetail = 0;
        int visibleNearMid = 0;

        for (int i = 0; i < distances.Length; i++)
        {
            Entity unit = distances[i].Unit;
            if (!em.Exists(unit))
                continue;

            UnitRenderVisualKind activeVisual = em.HasComponent<UnitRenderVisualState>(unit)
                ? (UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualState>(unit).Current
                : UnitRenderVisualKind.Detail;
            if (activeVisual == UnitRenderVisualKind.Detail)
                targetDetail++;
            else if (activeVisual == UnitRenderVisualKind.Mid)
                targetMid++;
            else if (activeVisual == UnitRenderVisualKind.Low)
                targetLow++;
            else
                targetFar++;

            bool isCharacter = classificationSystem.IsCharacterUnit(em, unit);
            if (!isCharacter || distances[i].Visible == 0)
                continue;

            visibleCharacters++;
            if (distances[i].ScreenEdge != 0)
                visibleCharacterScreenEdge++;
            if (activeVisual != UnitRenderVisualKind.Detail)
                visibleCharacterNotDetail++;
            if (activeVisual == UnitRenderVisualKind.Mid)
                visibleCharacterActiveMid++;
            else if (activeVisual == UnitRenderVisualKind.Low)
                visibleCharacterActiveLow++;
            else if (activeVisual == UnitRenderVisualKind.Far)
                visibleCharacterActiveFar++;
            if (distances[i].ScreenEdge != 0 && activeVisual == UnitRenderVisualKind.Detail)
                visibleCharacterScreenEdgeDetail++;
            if (distances[i].DistanceSq <= alwaysDetailedDistanceSq)
            {
                if (activeVisual == UnitRenderVisualKind.Detail)
                    visibleNearDetail++;
                else if (activeVisual == UnitRenderVisualKind.Mid)
                    visibleNearMid++;
            }
        }

        diagnosticLogSystem.EnqueueLog(
            em,
            $"[UnitRenderBudgetState] frame={Time.frameCount} units={distances.Length} targetDetail={targetDetail} targetMid={targetMid} targetLow={targetLow} targetFar={targetFar} " +
            $"cameraMotion={(cameraMotionActive ? 1 : 0)} visibleCharacters={visibleCharacters} visibleCharacterNotDetail={visibleCharacterNotDetail} visibleCharacterActiveMid={visibleCharacterActiveMid} visibleCharacterActiveLow={visibleCharacterActiveLow} visibleCharacterActiveFar={visibleCharacterActiveFar} visibleCharacterScreenEdge={visibleCharacterScreenEdge} visibleCharacterScreenEdgeDetail={visibleCharacterScreenEdgeDetail} visibleNearDetail={visibleNearDetail} visibleNearMid={visibleNearMid} detailedCap={detailedCount} light=1");
    }
}
