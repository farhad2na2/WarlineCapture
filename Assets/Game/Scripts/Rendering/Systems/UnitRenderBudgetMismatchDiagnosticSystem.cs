using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnitDistance = UnitRenderBudgetDistance.UnitDistance;

public readonly struct UnitRenderBudgetMismatchDiagnostic
{
    public void LogMidLodDiagnostics(
        EntityManager em,
        NativeList<UnitDistance> distances,
        NativeHashSet<Entity> detailedUnits,
        NativeHashSet<Entity> midLodUnits,
        NativeHashSet<Entity> lowLodUnits,
        BufferLookup<Child> childLookup,
        int detailedCount,
        bool cameraMotionActive,
        UnitRenderBudgetClassification classificationSystem,
        UnitRenderBudgetLodReferences lodReferenceSystem,
        UnitRenderBudgetAnimationReadiness animationReadinessSystem,
        UnitRenderBudgetRenderableState renderableQuerySystem,
        UnitRenderBudgetReadiness readinessSystem,
        UnitRenderBudgetVisualPlan visualPlanSystem,
        UnitRenderBudgetDiagnosticState diagnosticStateSystem,
        float visibleCharacterLowDistanceSq,
        float visibleCharacterImpostorNearDistance,
        float visibleCharacterImpostorFarDistance,
        ref UnitRenderBudgetDiagnosticLog diagnosticLogSystem)
    {
        int targetDetail = 0;
        int targetMid = 0;
        int targetLow = 0;
        int targetFar = 0;
        int missingMidInstance = 0;
        int missingLowInstance = 0;
        int invisible = 0;
        int wrongDetail = 0;
        int wrongMid = 0;
        int wrongLow = 0;
        int wrongFar = 0;
        int doubleVisible = 0;
        int detailVisibleCount = 0;
        int midVisibleCount = 0;
        int lowVisibleCount = 0;
        int farVisibleCount = 0;
        int detailAndMidVisible = 0;
        int detailAndFarVisible = 0;
        int midAndFarVisible = 0;
        int lowOverlapVisible = 0;
        int visibleCharacters = 0;
        int visibleCharacterNotDetail = 0;
        int visibleCharacterSafeUnits = 0;
        int visibleCharacterMidInstances = 0;
        int visibleCharacterSafeMidInstances = 0;
        int visibleCharacterLowInstances = 0;
        int visibleCharacterSafeLowInstances = 0;
        int visibleCharacterActiveMid = 0;
        int visibleCharacterActiveLow = 0;
        int visibleCharacterActiveFar = 0;
        int visibleCharacterScreenEdge = 0;
        int visibleCharacterScreenEdgeDetail = 0;
        string sample = string.Empty;

        for (int i = 0; i < distances.Length; i++)
        {
            Entity unit = distances[i].Unit;
            if (!em.Exists(unit))
                continue;

            UnitRenderBudgetLodReferences.UnitReferences lodReferences = lodReferenceSystem.ResolveUnitReferences(em, unit);
            bool hasMidPrefab = lodReferences.HasMidLodPrefab;
            bool hasMidInstance = lodReferences.HasMidLodInstance;
            if (hasMidPrefab && !hasMidInstance)
                missingMidInstance++;
            bool hasLowPrefab = lodReferences.HasLowLodPrefab;
            bool hasLowInstance = lodReferences.HasLowLodInstance;
            if (hasLowPrefab && !hasLowInstance)
                missingLowInstance++;
            bool isCharacter = classificationSystem.IsCharacterUnit(em, unit);
            if (isCharacter && distances[i].Visible != 0)
            {
                visibleCharacters++;
                if (distances[i].ScreenEdge != 0)
                    visibleCharacterScreenEdge++;
                if (em.HasComponent<UnitUsesSafeVisibleCharacterLodTag>(unit))
                    visibleCharacterSafeUnits++;
                if (hasMidInstance)
                {
                    visibleCharacterMidInstances++;
                    if (renderableQuerySystem.IsSafeVisibleCharacterLod(em, lodReferences.MidRoot))
                        visibleCharacterSafeMidInstances++;
                }
                if (hasLowInstance)
                {
                    visibleCharacterLowInstances++;
                    if (renderableQuerySystem.IsSafeVisibleCharacterLod(em, lodReferences.LowRoot))
                        visibleCharacterSafeLowInstances++;
                }
            }

            UnitRenderVisualKind activeVisual = em.HasComponent<UnitRenderVisualComponent>(unit)
                ? (UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualComponent>(unit).Current
                : visualPlanSystem.ResolveDesiredVisualForDiagnostics(
                    isCharacter,
                    detailedUnits.Contains(unit),
                    hasMidInstance && midLodUnits.Contains(unit),
                    hasLowInstance && lowLodUnits.Contains(unit));
            bool shouldDetail = activeVisual == UnitRenderVisualKind.Detail;
            bool shouldMid = activeVisual == UnitRenderVisualKind.Mid;
            bool shouldLow = activeVisual == UnitRenderVisualKind.Low;
            bool shouldFar = activeVisual == UnitRenderVisualKind.Far;
            if (isCharacter && distances[i].Visible != 0 && !shouldDetail)
                visibleCharacterNotDetail++;
            if (isCharacter && distances[i].Visible != 0 && shouldMid)
                visibleCharacterActiveMid++;
            if (isCharacter && distances[i].Visible != 0 && shouldLow)
                visibleCharacterActiveLow++;
            if (isCharacter && distances[i].Visible != 0 && shouldFar)
                visibleCharacterActiveFar++;
            if (isCharacter && distances[i].Visible != 0 && distances[i].ScreenEdge != 0 && shouldDetail)
                visibleCharacterScreenEdgeDetail++;

            if (shouldDetail)
                targetDetail++;
            else if (shouldMid)
                targetMid++;
            else if (shouldLow)
                targetLow++;
            else
                targetFar++;

            bool detailVisible = false;
            Entity detailRoot = lodReferences.DetailRoot;
            if (lodReferences.HasDetailRoot)
            {
                detailVisible = renderableQuerySystem.IsRenderableVisibleRecursive(em, detailRoot, childLookup);
            }

            bool midVisible = false;
            Entity midRoot = lodReferences.MidRoot;
            if (hasMidInstance)
            {
                midVisible = renderableQuerySystem.IsRenderableVisibleRecursive(em, midRoot, childLookup);
            }

            bool lowVisible = false;
            Entity lowRoot = lodReferences.LowRoot;
            if (hasLowInstance)
            {
                lowVisible = renderableQuerySystem.IsRenderableVisibleRecursive(em, lowRoot, childLookup);
            }

            bool farVisible = em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit);
            if (detailVisible)
                detailVisibleCount++;
            if (midVisible)
                midVisibleCount++;
            if (lowVisible)
                lowVisibleCount++;
            if (farVisible)
                farVisibleCount++;

            int visibleCount = (detailVisible ? 1 : 0) + (midVisible ? 1 : 0) + (lowVisible ? 1 : 0) + (farVisible ? 1 : 0);
            if (visibleCount == 0)
            {
                invisible++;
                AppendDiagnosticSample(ref sample, em, childLookup, unit, "none", detailRoot, midRoot, lowRoot, farVisible, animationReadinessSystem, renderableQuerySystem, diagnosticStateSystem);
            }
            bool expectedHandoff =
                detailVisible &&
                ((shouldMid && midVisible && !readinessSystem.IsVisualReadyForExclusiveDisplay(em, midRoot, childLookup, animationReadinessSystem, renderableQuerySystem)) ||
                 (shouldLow && lowVisible && !readinessSystem.IsVisualReadyForExclusiveDisplay(em, lowRoot, childLookup, animationReadinessSystem, renderableQuerySystem)));
            bool expectedVisibleCharacterSafeHandoff =
                isCharacter &&
                distances[i].Visible != 0 &&
                detailVisible &&
                shouldLow &&
                lowVisible &&
                hasLowInstance &&
                renderableQuerySystem.IsSafeVisibleCharacterLod(em, lowRoot);
            if (visibleCount > 1 && !expectedHandoff && !expectedVisibleCharacterSafeHandoff)
            {
                doubleVisible++;
                if (detailVisible && midVisible)
                    detailAndMidVisible++;
                if (detailVisible && farVisible)
                    detailAndFarVisible++;
                if (midVisible && farVisible)
                    midAndFarVisible++;
                if (lowVisible)
                    lowOverlapVisible++;
            }
            bool wrongDetailState = shouldDetail && !detailVisible;
            bool wrongMidState = shouldMid && !midVisible;
            bool wrongLowState = shouldLow && !lowVisible;
            bool wrongFarState = shouldFar && !farVisible;
            if (wrongDetailState)
                wrongDetail++;
            if (wrongMidState)
                wrongMid++;
            if (wrongLowState)
                wrongLow++;
            if (wrongFarState)
                wrongFar++;
            if (wrongDetailState || wrongMidState || wrongLowState || wrongFarState)
                AppendDiagnosticSample(ref sample, em, childLookup, unit, "wrong", detailRoot, midRoot, lowRoot, farVisible, animationReadinessSystem, renderableQuerySystem, diagnosticStateSystem);
        }

        int mismatches = invisible + wrongDetail + wrongMid + wrongLow + wrongFar;
        bool hasProblems = mismatches != 0 ||
                           doubleVisible != 0 ||
                           missingMidInstance != 0 ||
                           missingLowInstance != 0 ||
                           visibleCharacterNotDetail > visibleCharacterSafeMidInstances + visibleCharacterSafeLowInstances + visibleCharacterActiveFar;
        if (!hasProblems)
        {
            diagnosticLogSystem.EnqueueLog(
                em,
                $"[UnitRenderBudgetState] frame={Time.frameCount} units={distances.Length} targetDetail={targetDetail} targetMid={targetMid} targetLow={targetLow} targetFar={targetFar} " +
                $"visibleDetail={detailVisibleCount} visibleMid={midVisibleCount} visibleLow={lowVisibleCount} visibleFar={farVisibleCount} cameraMotion={(cameraMotionActive ? 1 : 0)} visibleCharacters={visibleCharacters} visibleCharacterNotDetail={visibleCharacterNotDetail} visibleCharacterActiveMid={visibleCharacterActiveMid} visibleCharacterActiveLow={visibleCharacterActiveLow} visibleCharacterActiveFar={visibleCharacterActiveFar} visibleCharacterScreenEdge={visibleCharacterScreenEdge} visibleCharacterScreenEdgeDetail={visibleCharacterScreenEdgeDetail} visibleCharacterSafeUnits={visibleCharacterSafeUnits} visibleCharacterMidInstances={visibleCharacterMidInstances} visibleCharacterSafeMidInstances={visibleCharacterSafeMidInstances} visibleCharacterLowInstances={visibleCharacterLowInstances} visibleCharacterSafeLowInstances={visibleCharacterSafeLowInstances} visibleCharacterLowDistance={math.sqrt(visibleCharacterLowDistanceSq):F0} visibleCharacterImpostorBand={visibleCharacterImpostorNearDistance:F0}-{visibleCharacterImpostorFarDistance:F0} detailedCap={detailedCount}");
            return;
        }

        diagnosticLogSystem.EnqueueWarning(
            em,
            $"[UnitRenderVisibilityDiag] frame={Time.frameCount} units={distances.Length} targetDetail={targetDetail} targetMid={targetMid} targetLow={targetLow} targetFar={targetFar} " +
            $"visibleDetail={detailVisibleCount} visibleMid={midVisibleCount} visibleLow={lowVisibleCount} visibleFar={farVisibleCount} cameraMotion={(cameraMotionActive ? 1 : 0)} invisible={invisible} doubleVisible={doubleVisible} " +
            $"detailMid={detailAndMidVisible} detailFar={detailAndFarVisible} midFar={midAndFarVisible} lowOverlap={lowOverlapVisible} " +
            $"wrongDetail={wrongDetail} wrongMid={wrongMid} wrongLow={wrongLow} wrongFar={wrongFar} missingMid={missingMidInstance} missingLow={missingLowInstance} visibleCharacters={visibleCharacters} visibleCharacterNotDetail={visibleCharacterNotDetail} visibleCharacterActiveMid={visibleCharacterActiveMid} visibleCharacterActiveLow={visibleCharacterActiveLow} visibleCharacterActiveFar={visibleCharacterActiveFar} visibleCharacterScreenEdge={visibleCharacterScreenEdge} visibleCharacterScreenEdgeDetail={visibleCharacterScreenEdgeDetail} visibleCharacterSafeUnits={visibleCharacterSafeUnits} visibleCharacterMidInstances={visibleCharacterMidInstances} visibleCharacterSafeMidInstances={visibleCharacterSafeMidInstances} visibleCharacterLowInstances={visibleCharacterLowInstances} visibleCharacterSafeLowInstances={visibleCharacterSafeLowInstances} visibleCharacterLowDistance={math.sqrt(visibleCharacterLowDistanceSq):F0} visibleCharacterImpostorBand={visibleCharacterImpostorNearDistance:F0}-{visibleCharacterImpostorFarDistance:F0} detailedCap={detailedCount} samples={sample}");
    }

    private static void AppendDiagnosticSample(
        ref string sample,
        EntityManager em,
        BufferLookup<Child> childLookup,
        Entity unit,
        string state,
        Entity detailRoot,
        Entity midRoot,
        Entity lowRoot,
        bool farVisible,
        UnitRenderBudgetAnimationReadiness animationReadinessSystem,
        UnitRenderBudgetRenderableState renderableQuerySystem,
        UnitRenderBudgetDiagnosticState diagnosticStateSystem)
    {
        if (!diagnosticStateSystem.ShouldAppendDiagnosticSample(sample))
            return;

        if (sample.Length > 0)
            sample += " | ";

        string key = em.HasComponent<UnitSourcePrefabKey>(unit)
            ? em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString()
            : "unknown";
        sample += $"{unit}:{state}:{key} " +
                  $"detail={DescribeVisualRoot(em, childLookup, detailRoot, animationReadinessSystem, renderableQuerySystem)} " +
                  $"mid={DescribeVisualRoot(em, childLookup, midRoot, animationReadinessSystem, renderableQuerySystem)} " +
                  $"low={DescribeVisualRoot(em, childLookup, lowRoot, animationReadinessSystem, renderableQuerySystem)} " +
                  $"far={(farVisible ? 1 : 0)}";
    }

    private static string DescribeVisualRoot(
        EntityManager em,
        BufferLookup<Child> childLookup,
        Entity root,
        UnitRenderBudgetAnimationReadiness animationReadinessSystem,
        UnitRenderBudgetRenderableState renderableQuerySystem)
    {
        if (root == Entity.Null)
            return "null";
        if (!em.Exists(root))
            return $"{root}:missing";

        int disabled = em.HasComponent<Disabled>(root) || em.HasComponent<DisableRendering>(root) ? 1 : 0;
        int culled = em.HasComponent<UnitRenderBudgetCulledTag>(root) ? 1 : 0;
        int alpha = animationReadinessSystem.HasMaterialAlphaCompleteRecursive(em, root, childLookup) ? 1 : 0;
        int renderable = renderableQuerySystem.HasRenderableRecursive(em, root, childLookup) ? 1 : 0;
        int visible = renderableQuerySystem.IsRenderableVisibleRecursive(em, root, childLookup) ? 1 : 0;
        return $"{root}:d{disabled}:c{culled}:a{alpha}:r{renderable}:v{visible}";
    }
}
