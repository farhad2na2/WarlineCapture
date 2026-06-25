using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using ConnectorMarkerData = RoadVisualVariantSystem.ConnectorMarkerData;
using MarkerLayoutData = RoadVisualVariantSystem.MarkerLayoutData;
using RoadTileData = RoadNetworkCompositionSystemHelper.RoadTileData;
using RoadVisualType = RoadNetworkCompositionSystemHelper.RoadVisualType;
using StrokeData = RoadNetworkCompositionSystemHelper.StrokeData;
using TileConnectionMask = RoadNetworkCompositionSystemHelper.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

public sealed partial class RoadSpecialVisualSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnDestroy()
    {
        DisposeVisuals();
    }

    protected override void OnUpdate()
    {
    }

    public delegate GameObject GetPrefabAction(RoadVisualType type);
    public delegate bool TryGetVariantAction(RoadVisualType type, TileConnectionMask mask, out VariantData variant);

    public readonly struct Context
    {
        public readonly Dictionary<Vector2Int, RoadTileData> RoadTiles;
        public readonly Dictionary<int, StrokeData> Strokes;
        public readonly Dictionary<RoadVisualType, MarkerLayoutData> MarkerLayouts;
        public readonly ConnectorMarkerData? AutobahnConnectorMarkerData;
        public readonly Transform RoadRoot;
        public readonly Transform SpecialRoadRoot;
        public readonly Transform SpecialRoadConnectorRoot;
        public readonly Transform DebugStraightRoadRoot;
        public readonly Vector3 GridOrigin;
        public readonly float BuildPlaneY;
        public readonly float RoadGridSize;
        public readonly int ChunkSizeInCells;
        public readonly GetPrefabAction GetPrefab;
        public readonly TryGetVariantAction TryGetVariant;

        public Context(
            Dictionary<Vector2Int, RoadTileData> roadTiles,
            Dictionary<int, StrokeData> strokes,
            Dictionary<RoadVisualType, MarkerLayoutData> markerLayouts,
            ConnectorMarkerData? autobahnConnectorMarkerData,
            Transform roadRoot,
            Transform specialRoadRoot,
            Transform specialRoadConnectorRoot,
            Transform debugStraightRoadRoot,
            Vector3 gridOrigin,
            float buildPlaneY,
            float roadGridSize,
            int chunkSizeInCells,
            GetPrefabAction getPrefab,
            TryGetVariantAction tryGetVariant)
        {
            RoadTiles = roadTiles;
            Strokes = strokes;
            MarkerLayouts = markerLayouts;
            AutobahnConnectorMarkerData = autobahnConnectorMarkerData;
            RoadRoot = roadRoot;
            SpecialRoadRoot = specialRoadRoot;
            SpecialRoadConnectorRoot = specialRoadConnectorRoot;
            DebugStraightRoadRoot = debugStraightRoadRoot;
            GridOrigin = gridOrigin;
            BuildPlaneY = buildPlaneY;
            RoadGridSize = roadGridSize;
            ChunkSizeInCells = chunkSizeInCells;
            GetPrefab = getPrefab;
            TryGetVariant = tryGetVariant;
        }
    }

    public Dictionary<Vector2Int, GameObject> SpecialRoadObjects { get; } = new();

    private readonly List<GameObject> _debugStraightRoadObjects = new();

    public void ClearSpecialRoadObjects()
    {
        foreach (var roadObject in SpecialRoadObjects.Values)
        {
            if (roadObject != null)
                UnityEngine.Object.Destroy(roadObject);
        }

        SpecialRoadObjects.Clear();
    }

    public void ClearDebugStraightRoadObjects()
    {
        foreach (var roadObject in _debugStraightRoadObjects)
        {
            if (roadObject != null)
                UnityEngine.Object.Destroy(roadObject);
        }

        _debugStraightRoadObjects.Clear();
    }

    public void DisposeVisuals()
    {
        ClearSpecialRoadObjects();
        ClearDebugStraightRoadObjects();
    }

    public void RebuildSpecialRoadObjects(Context context)
    {
        RebuildAllSpecialRoadObjects(context);
    }

    public bool TryGetAutobahnConnectorRoadCell(Context context, Vector2Int connectorCell, out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;

        if (!SpecialRoadObjects.TryGetValue(connectorCell, out var connectorObject) || connectorObject == null)
            return false;
        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) ||
            !connectorLayout.RoadConnectLocalPosition.HasValue)
            return false;
        if (context.RoadGridSize <= 0f)
            return false;

        Vector3 worldPosition = GetObjectMarkerWorldPosition(
            connectorObject.transform,
            connectorLayout.RoadConnectLocalPosition.Value);
        Vector3 localPoint = worldPosition - context.GridOrigin;
        Vector2 rawRoadCell = new(
            localPoint.x / context.RoadGridSize,
            localPoint.z / context.RoadGridSize);
        roadConnectionCell = new Vector2Int(
            Mathf.RoundToInt(rawRoadCell.x),
            Mathf.RoundToInt(rawRoadCell.y));
        return true;
    }

    public bool TryLogRoadConnectMarkers(Context context, Vector2Int roadCell)
    {
        if (!context.RoadTiles.TryGetValue(roadCell, out var tile))
            return false;
        if (!context.MarkerLayouts.TryGetValue(tile.Type, out var layout) || layout.ConnectLocalPositions.Count == 0)
            return false;

        VariantData variant = new(tile.Rotation, tile.Scale);
        Vector3 placedRoadPosition = GetPlacementPosition(context, roadCell, variant);
        for (int i = 0; i < layout.ConnectLocalPositions.Count; i++)
        {
            Vector3 worldPosition = placedRoadPosition + variant.Rotation * Vector3.Scale(layout.ConnectLocalPositions[i], variant.Scale);
        }

        return true;
    }

    public bool CreateStandaloneStraightRoadChainFromConnector(Context context, Vector2Int connectorCell, Vector2Int direction, int length)
    {
        ClearDebugStraightRoadObjects();

        if (length <= 0)
            return false;
        if (!SpecialRoadObjects.TryGetValue(connectorCell, out var connectorObject) || connectorObject == null)
            return false;
        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) ||
            !connectorLayout.RoadConnectLocalPosition.HasValue)
            return false;
        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return false;
        if (!context.TryGetVariant(RoadVisualType.Straight, BuildAxisMask(direction), out var straightVariant))
            return false;

        Vector3 previousConnectWorldPosition = GetObjectMarkerWorldPosition(
            connectorObject.transform,
            connectorLayout.RoadConnectLocalPosition.Value);

        for (int i = 0; i < length; i++)
        {
            GameObject prefab = context.GetPrefab(RoadVisualType.Straight);
            if (prefab == null)
                return false;

            GameObject roadObject = UnityEngine.Object.Instantiate(
                prefab,
                context.DebugStraightRoadRoot != null ? context.DebugStraightRoadRoot : context.RoadRoot);
            roadObject.name = $"{prefab.name}_Debug_{connectorCell.x}_{connectorCell.y}_{i}";

            Vector3 incomingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                -direction);
            Vector3 outgoingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                direction);

            PlaceObjectByMarker(
                roadObject.transform,
                straightVariant,
                incomingMarkerLocalPosition,
                previousConnectWorldPosition);
            previousConnectWorldPosition = GetObjectMarkerWorldPosition(
                roadObject.transform,
                outgoingMarkerLocalPosition);
            _debugStraightRoadObjects.Add(roadObject);
        }

        return true;
    }

    public bool TryGetStandaloneStraightChainEndRoadCell(Context context, Vector2Int direction, out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;

        if (_debugStraightRoadObjects.Count == 0)
            return false;
        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return false;
        if (context.RoadGridSize <= 0f)
            return false;

        GameObject lastStraight = _debugStraightRoadObjects[_debugStraightRoadObjects.Count - 1];
        if (lastStraight == null)
            return false;

        VariantData variant = new(lastStraight.transform.rotation, lastStraight.transform.localScale);
        Vector3 endMarkerWorldPosition = GetObjectMarkerWorldPosition(
            lastStraight.transform,
            GetMarkerLocalPositionForDirection(straightLayout, variant, direction));

        Vector3 localPoint = endMarkerWorldPosition - context.GridOrigin;
        roadConnectionCell = new Vector2Int(
            Mathf.RoundToInt(localPoint.x / context.RoadGridSize),
            Mathf.RoundToInt(localPoint.z / context.RoadGridSize));
        return true;
    }

    public bool CreateStandaloneDebugCityRoadNetworkFromStraightChain(Context context, Vector2Int direction, int branchLength)
    {
        if (_debugStraightRoadObjects.Count == 0 || branchLength <= 0)
            return false;
        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return false;
        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.Intersection, out var intersectionLayout) ||
            intersectionLayout.ConnectLocalPositions.Count < 4)
            return false;

        GameObject lastStraight = _debugStraightRoadObjects[_debugStraightRoadObjects.Count - 1];
        VariantData lastStraightVariant = new(lastStraight.transform.rotation, lastStraight.transform.localScale);
        Vector3 chainEndWorldPosition = GetObjectMarkerWorldPosition(
            lastStraight.transform,
            GetMarkerLocalPositionForDirection(straightLayout, lastStraightVariant, direction));

        Vector2Int leftDirection = new(-direction.y, direction.x);
        Vector2Int rightDirection = new(direction.y, -direction.x);
        TileConnectionMask intersectionMask = BuildMaskFromDirections(direction, -direction, leftDirection, rightDirection);
        if (!context.TryGetVariant(RoadVisualType.Intersection, intersectionMask, out var intersectionVariant))
            return false;
        GameObject intersectionPrefab = context.GetPrefab(RoadVisualType.Intersection);
        if (intersectionPrefab == null)
            return false;

        GameObject intersectionObject = UnityEngine.Object.Instantiate(
            intersectionPrefab,
            context.DebugStraightRoadRoot != null ? context.DebugStraightRoadRoot : context.RoadRoot);
        intersectionObject.name = $"{intersectionPrefab.name}_DebugCityHub";
        PlaceObjectByMarker(
            intersectionObject.transform,
            intersectionVariant,
            GetMarkerLocalPositionForDirection(intersectionLayout, intersectionVariant, -direction),
            chainEndWorldPosition);
        _debugStraightRoadObjects.Add(intersectionObject);

        CreateStandaloneStraightBranch(context, intersectionObject.transform, intersectionLayout, intersectionVariant, direction, branchLength + 3);
        CreateStandaloneStraightBranch(context, intersectionObject.transform, intersectionLayout, intersectionVariant, leftDirection, branchLength + 2);
        CreateStandaloneStraightBranch(context, intersectionObject.transform, intersectionLayout, intersectionVariant, rightDirection, branchLength + 2);

        return true;
    }

    private void RebuildAllSpecialRoadObjects(Context context)
    {
        var expectedCells = new HashSet<Vector2Int>();
        foreach (var stroke in context.Strokes.Values)
        {
            if (!stroke.IsAutobahn || stroke.Cells.Count < 2)
                continue;

            RebuildSpecialRoadStrokeObjects(context, stroke, expectedCells);
        }

        var cellsToRemove = new List<Vector2Int>();
        foreach (var cell in SpecialRoadObjects.Keys)
        {
            if (!expectedCells.Contains(cell))
                cellsToRemove.Add(cell);
        }

        for (int i = 0; i < cellsToRemove.Count; i++)
            DestroySpecialRoadObject(cellsToRemove[i]);
    }

    private void RebuildSpecialRoadStrokeObjects(Context context, StrokeData stroke, HashSet<Vector2Int> expectedCells)
    {
        if (stroke.Cells.Count < 2)
            return;

        int firstAutobahnCellIndex = stroke.UseAutobahnConnectorAtStart ? 1 : 0;
        int lastAutobahnCellIndex = stroke.Cells.Count - (stroke.UseAutobahnConnectorAtEnd ? 2 : 1);
        if (firstAutobahnCellIndex > lastAutobahnCellIndex)
            return;

        Vector2Int autobahnDirection = stroke.Cells[Mathf.Min(firstAutobahnCellIndex, stroke.Cells.Count - 1)] - stroke.Cells[Mathf.Max(firstAutobahnCellIndex - 1, 0)];
        if (autobahnDirection == Vector2Int.zero)
            autobahnDirection = stroke.Cells[Mathf.Min(firstAutobahnCellIndex + 1, stroke.Cells.Count - 1)] - stroke.Cells[firstAutobahnCellIndex];

        Vector2Int cityDirection = -autobahnDirection;
        Vector3 previousConnectWorldPosition = default;
        bool hasPreviousConnectWorldPosition = false;

        if (stroke.UseAutobahnConnectorAtStart)
        {
            Vector2Int connectorCell = stroke.Cells[0];
            if (TryGetAutobahnConnectorVariant(context, autobahnDirection, out var connectorVariant) &&
                context.MarkerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) &&
                connectorLayout.RoadConnectLocalPosition.HasValue &&
                connectorLayout.AutobahnConnectLocalPosition.HasValue &&
                TryGetNeighborRoadConnectWorldPosition(context, connectorCell, cityDirection, out Vector3 cityConnectWorldPosition))
            {
                if (stroke.Cells.Count > firstAutobahnCellIndex &&
                    context.MarkerLayouts.TryGetValue(RoadVisualType.Autobahn, out var startAutobahnLayout) &&
                    startAutobahnLayout.ConnectLocalPositions.Count >= 2 &&
                    context.TryGetVariant(RoadVisualType.Autobahn, BuildAxisMask(autobahnDirection), out var startAutobahnVariant))
                {
                    Vector3 autobahnTargetWorldPosition = cityConnectWorldPosition +
                        new Vector3(autobahnDirection.x, 0f, autobahnDirection.y) * context.RoadGridSize;

                    if (TryGetAutobahnConnectorVariantForTargets(
                        context,
                        cityConnectWorldPosition,
                        autobahnTargetWorldPosition,
                        autobahnDirection,
                        out var bestConnectorVariant))
                    {
                        connectorVariant = bestConnectorVariant;
                    }
                }

                GameObject connectorObject = GetOrCreateSpecialRoadObject(context, connectorCell, RoadVisualType.AutobahnConnect);
                PlaceObjectByMarker(
                    connectorObject.transform,
                    connectorVariant,
                    connectorLayout.RoadConnectLocalPosition.Value,
                    cityConnectWorldPosition);
                previousConnectWorldPosition = GetObjectMarkerWorldPosition(
                    connectorObject.transform,
                    connectorLayout.AutobahnConnectLocalPosition.Value);
                hasPreviousConnectWorldPosition = true;
                expectedCells.Add(connectorCell);
            }
            else
            {
                DestroySpecialRoadObject(connectorCell);
            }
        }

        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.Autobahn, out var autobahnLayout) || autobahnLayout.ConnectLocalPositions.Count < 2)
            return;

        int availableAutobahnCellCount = lastAutobahnCellIndex - firstAutobahnCellIndex + 1;
        int autobahnSpanInCells = GetAutobahnSpanInCells(context, autobahnLayout);
        int autobahnObjectCount = Mathf.Max(1, Mathf.FloorToInt(availableAutobahnCellCount / (float)autobahnSpanInCells));

        for (int pieceIndex = 0; pieceIndex < autobahnObjectCount; pieceIndex++)
        {
            int sampleOffset = Mathf.FloorToInt((pieceIndex * availableAutobahnCellCount) / (float)autobahnObjectCount);
            int cellIndex = Mathf.Clamp(firstAutobahnCellIndex + sampleOffset, firstAutobahnCellIndex, lastAutobahnCellIndex);
            Vector2Int cell = stroke.Cells[cellIndex];
            Vector2Int forwardDirection = autobahnDirection;

            if (!context.TryGetVariant(RoadVisualType.Autobahn, BuildAxisMask(forwardDirection), out var autobahnVariant))
                continue;

            Vector3 incomingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                autobahnLayout,
                autobahnVariant,
                -forwardDirection);
            Vector3 outgoingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                autobahnLayout,
                autobahnVariant,
                forwardDirection);

            if (!hasPreviousConnectWorldPosition)
            {
                if (!TryGetNeighborRoadConnectWorldPosition(context, cell, -forwardDirection, out previousConnectWorldPosition))
                    continue;
                hasPreviousConnectWorldPosition = true;
            }

            GameObject autobahnObject = GetOrCreateSpecialRoadObject(context, cell, RoadVisualType.Autobahn);
            PlaceObjectByMarker(
                autobahnObject.transform,
                autobahnVariant,
                incomingMarkerLocalPosition,
                previousConnectWorldPosition);
            Vector3 autobahnOutgoingWorldPosition = GetObjectMarkerWorldPosition(
                autobahnObject.transform,
                outgoingMarkerLocalPosition);
            previousConnectWorldPosition = autobahnOutgoingWorldPosition;
            expectedCells.Add(cell);
        }

        if (stroke.UseAutobahnConnectorAtEnd)
        {
            Vector2Int connectorCell = stroke.Cells[stroke.Cells.Count - 1];
            Vector2Int connectorAutobahnDirection = stroke.Cells[stroke.Cells.Count - 2] - connectorCell;
            Vector2Int connectorRoadDirection = -connectorAutobahnDirection;

            if (context.MarkerLayouts.TryGetValue(RoadVisualType.AutobahnConnect, out var connectorLayout) &&
                connectorLayout.RoadConnectLocalPosition.HasValue &&
                connectorLayout.AutobahnConnectLocalPosition.HasValue)
            {
                bool hasCityTarget = TryGetNeighborRoadConnectWorldPosition(
                    context,
                    connectorCell,
                    connectorRoadDirection,
                    out Vector3 cityConnectWorldPosition);

                VariantData connectorVariant;
                if (hasCityTarget &&
                    hasPreviousConnectWorldPosition &&
                    TryGetAutobahnConnectorVariantForTargets(
                        context,
                        cityConnectWorldPosition,
                        previousConnectWorldPosition,
                        connectorAutobahnDirection,
                        out var bestConnectorVariant))
                {
                    connectorVariant = bestConnectorVariant;
                }
                else if (!TryGetAutobahnConnectorVariant(context, connectorAutobahnDirection, out connectorVariant))
                {
                    DestroySpecialRoadObject(connectorCell);
                    return;
                }

                GameObject connectorObject = GetOrCreateSpecialRoadObject(context, connectorCell, RoadVisualType.AutobahnConnect);
                if (hasCityTarget)
                {
                    PlaceObjectByMarker(
                        connectorObject.transform,
                        connectorVariant,
                        connectorLayout.RoadConnectLocalPosition.Value,
                        cityConnectWorldPosition);

                    if (hasPreviousConnectWorldPosition)
                    {
                        Vector3 currentAutobahnWorldPosition = GetObjectMarkerWorldPosition(
                            connectorObject.transform,
                            connectorLayout.AutobahnConnectLocalPosition.Value);
                        Vector3 desiredDelta = previousConnectWorldPosition - currentAutobahnWorldPosition;
                        Vector3 axis = new(connectorAutobahnDirection.x, 0f, connectorAutobahnDirection.y);
                        if (axis.sqrMagnitude > 0.0001f)
                        {
                            axis.Normalize();
                            float signedDistance = Vector3.Dot(desiredDelta, axis);
                            connectorObject.transform.position += axis * signedDistance;
                        }
                    }
                }
                else if (hasPreviousConnectWorldPosition)
                {
                    PlaceObjectByMarker(
                        connectorObject.transform,
                        connectorVariant,
                        connectorLayout.AutobahnConnectLocalPosition.Value,
                        previousConnectWorldPosition);
                }
                else
                {
                    DestroySpecialRoadObject(connectorCell);
                    return;
                }

                expectedCells.Add(connectorCell);
            }
            else
            {
                DestroySpecialRoadObject(connectorCell);
            }
        }
    }

    private int GetAutobahnSpanInCells(Context context, MarkerLayoutData layout)
    {
        if (layout == null || layout.ConnectLocalPositions.Count < 2 || context.RoadGridSize <= 0f)
            return 1;

        float span = 0f;
        for (int i = 0; i < layout.ConnectLocalPositions.Count; i++)
        {
            for (int j = i + 1; j < layout.ConnectLocalPositions.Count; j++)
            {
                Vector3 delta = layout.ConnectLocalPositions[j] - layout.ConnectLocalPositions[i];
                float axisSpan = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.z));
                if (axisSpan > span)
                    span = axisSpan;
            }
        }

        if (span <= 0f)
            return 1;

        return Mathf.Max(1, Mathf.RoundToInt(span / context.RoadGridSize));
    }

    private GameObject GetOrCreateSpecialRoadObject(Context context, Vector2Int cell, RoadVisualType type)
    {
        if (SpecialRoadObjects.TryGetValue(cell, out var roadObject) && roadObject != null)
            return roadObject;

        GameObject prefab = context.GetPrefab(type);
        Transform parent = type == RoadVisualType.AutobahnConnect
            ? (context.SpecialRoadConnectorRoot != null ? context.SpecialRoadConnectorRoot : context.RoadRoot)
            : (context.SpecialRoadRoot != null ? context.SpecialRoadRoot : context.RoadRoot);

        roadObject = UnityEngine.Object.Instantiate(prefab, parent);
        roadObject.name = $"{prefab.name}_{cell.x}_{cell.y}";
        SpecialRoadObjects[cell] = roadObject;
        return roadObject;
    }

    private bool TryGetNeighborRoadConnectWorldPosition(Context context, Vector2Int cell, Vector2Int direction, out Vector3 worldPosition)
    {
        Vector2Int neighborCell = cell + direction;
        if (!context.RoadTiles.TryGetValue(neighborCell, out var tile))
        {
            worldPosition = default;
            return false;
        }

        if (!context.MarkerLayouts.TryGetValue(tile.Type, out var layout) || layout.ConnectLocalPositions.Count == 0)
        {
            worldPosition = default;
            return false;
        }

        VariantData variant = new(tile.Rotation, tile.Scale);
        Vector3 localMarkerPosition = GetMarkerLocalPositionForDirection(layout, variant, -direction);
        Vector3 placedRoadPosition = GetPlacementPosition(context, neighborCell, variant);
        worldPosition = placedRoadPosition + variant.Rotation * Vector3.Scale(localMarkerPosition, variant.Scale);

        return true;
    }

    private static Vector3 GetMarkerLocalPositionForDirection(MarkerLayoutData layout, VariantData variant, Vector2Int direction)
    {
        Vector3 selectedLocalPosition = layout.ConnectLocalPositions[0];
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < layout.ConnectLocalPositions.Count; i++)
        {
            Vector3 offset = layout.ConnectLocalPositions[i] - layout.Center;
            Vector3 transformedOffset = variant.Rotation * Vector3.Scale(offset, variant.Scale);
            float score = direction.x * transformedOffset.x + direction.y * transformedOffset.z;
            if (score > bestScore)
            {
                bestScore = score;
                selectedLocalPosition = layout.ConnectLocalPositions[i];
            }
        }

        return selectedLocalPosition;
    }

    private static Vector3 GetObjectMarkerWorldPosition(Transform target, Vector3 localMarkerPosition)
    {
        return target.position + target.rotation * Vector3.Scale(localMarkerPosition, target.localScale);
    }

    private static void PlaceObjectByMarker(Transform target, VariantData variant, Vector3 localMarkerPosition, Vector3 targetWorldPosition)
    {
        Vector3 worldPosition = targetWorldPosition - variant.Rotation * Vector3.Scale(localMarkerPosition, variant.Scale);
        target.SetPositionAndRotation(worldPosition, variant.Rotation);
        target.localScale = variant.Scale;
    }

    private bool TryGetAutobahnConnectorVariantForTargets(
        Context context,
        Vector3 cityConnectWorldPosition,
        Vector3 autobahnTargetWorldPosition,
        Vector2Int autobahnDirection,
        out VariantData variant)
    {
        variant = default;
        if (!context.AutobahnConnectorMarkerData.HasValue)
            return false;

        ConnectorMarkerData markerData = context.AutobahnConnectorMarkerData.Value;
        int[] rotationAngles = { 0, 90, 180, 270 };
        int[] flipValues = { 1, -1 };
        float bestDistanceSq = float.PositiveInfinity;
        float bestDirectionScore = float.NegativeInfinity;
        bool found = false;
        Vector3 desiredDelta = autobahnTargetWorldPosition - cityConnectWorldPosition;
        Vector3 desiredPlanarDirection = new(desiredDelta.x, 0f, desiredDelta.z);
        bool hasDesiredDirection = desiredPlanarDirection.sqrMagnitude > 0.0001f;
        if (hasDesiredDirection)
            desiredPlanarDirection.Normalize();

        foreach (int angle in rotationAngles)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            foreach (int scaleX in flipValues)
            {
                foreach (int scaleZ in flipValues)
                {
                    Vector3 scale = new(scaleX, 1f, scaleZ);
                    VariantData candidate = new(rotation, scale);
                    Vector3 candidatePosition = cityConnectWorldPosition - rotation * Vector3.Scale(markerData.RoadConnectLocalPosition, scale);
                    Vector3 candidateAutobahnWorldPosition = candidatePosition + rotation * Vector3.Scale(markerData.AutobahnConnectLocalPosition, scale);
                    float distanceSq = (candidateAutobahnWorldPosition - autobahnTargetWorldPosition).sqrMagnitude;
                    Vector3 candidateDelta = candidateAutobahnWorldPosition - cityConnectWorldPosition;
                    bool matchesDirection =
                        autobahnDirection.x > 0 ? candidateDelta.x > 0f && Mathf.Abs(candidateDelta.x) >= Mathf.Abs(candidateDelta.z) :
                        autobahnDirection.x < 0 ? candidateDelta.x < 0f && Mathf.Abs(candidateDelta.x) >= Mathf.Abs(candidateDelta.z) :
                        autobahnDirection.y > 0 ? candidateDelta.z > 0f && Mathf.Abs(candidateDelta.z) >= Mathf.Abs(candidateDelta.x) :
                        candidateDelta.z < 0f && Mathf.Abs(candidateDelta.z) >= Mathf.Abs(candidateDelta.x);

                    if (!matchesDirection)
                        continue;

                    Vector3 candidatePlanarDirection = new(candidateDelta.x, 0f, candidateDelta.z);
                    float directionScore = 0f;
                    if (hasDesiredDirection && candidatePlanarDirection.sqrMagnitude > 0.0001f)
                    {
                        candidatePlanarDirection.Normalize();
                        directionScore = Vector3.Dot(candidatePlanarDirection, desiredPlanarDirection);
                    }

                    const float distanceEpsilon = 0.0001f;
                    bool isBetterDistance = distanceSq < bestDistanceSq - distanceEpsilon;
                    bool isTieButBetterDirection = Mathf.Abs(distanceSq - bestDistanceSq) <= distanceEpsilon &&
                                                  directionScore > bestDirectionScore + 0.0001f;

                    if (!isBetterDistance && !isTieButBetterDirection)
                        continue;

                    bestDistanceSq = distanceSq;
                    bestDirectionScore = directionScore;
                    variant = candidate;
                    found = true;
                }
            }
        }

        return found;
    }

    private bool TryGetAutobahnConnectorVariant(Context context, Vector2Int autobahnDirection, out VariantData variant)
    {
        variant = default;
        if (!context.AutobahnConnectorMarkerData.HasValue)
            return false;

        ConnectorMarkerData markerData = context.AutobahnConnectorMarkerData.Value;
        int[] rotationAngles = { 0, 90, 180, 270 };
        int[] flipValues = { 1, -1 };
        foreach (int angle in rotationAngles)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            foreach (int scaleX in flipValues)
            {
                foreach (int scaleZ in flipValues)
                {
                    Vector3 scale = new(scaleX, 1f, scaleZ);
                    Vector3 roadOffset = rotation * Vector3.Scale(markerData.RoadConnectLocalPosition - markerData.Center, scale);
                    Vector3 autobahnOffset = rotation * Vector3.Scale(markerData.AutobahnConnectLocalPosition - markerData.Center, scale);
                    Vector3 delta = autobahnOffset - roadOffset;

                    bool matchesDirection =
                        autobahnDirection.x > 0 ? delta.x > 0f && Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) :
                        autobahnDirection.x < 0 ? delta.x < 0f && Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) :
                        autobahnDirection.y > 0 ? delta.z > 0f && Mathf.Abs(delta.z) >= Mathf.Abs(delta.x) :
                        delta.z < 0f && Mathf.Abs(delta.z) >= Mathf.Abs(delta.x);

                    if (!matchesDirection)
                        continue;

                    variant = new VariantData(rotation, scale);
                    return true;
                }
            }
        }

        return false;
    }

    private void DestroySpecialRoadObject(Vector2Int cell)
    {
        if (!SpecialRoadObjects.TryGetValue(cell, out var roadObject))
            return;

        if (roadObject != null)
            UnityEngine.Object.Destroy(roadObject);

        SpecialRoadObjects.Remove(cell);
    }

    private GameObject CreateStandaloneStraightBranch(
        Context context,
        Transform sourceTransform,
        MarkerLayoutData sourceLayout,
        VariantData sourceVariant,
        Vector2Int branchDirection,
        int branchLength)
    {
        if (branchLength <= 0)
            return sourceTransform != null ? sourceTransform.gameObject : null;
        if (!context.MarkerLayouts.TryGetValue(RoadVisualType.Straight, out var straightLayout) ||
            straightLayout.ConnectLocalPositions.Count < 2)
            return sourceTransform != null ? sourceTransform.gameObject : null;
        if (!context.TryGetVariant(RoadVisualType.Straight, BuildAxisMask(branchDirection), out var straightVariant))
            return sourceTransform != null ? sourceTransform.gameObject : null;

        Vector3 previousConnectWorldPosition = GetObjectMarkerWorldPosition(
            sourceTransform,
            GetMarkerLocalPositionForDirection(sourceLayout, sourceVariant, branchDirection));
        GameObject lastRoadObject = sourceTransform.gameObject;

        for (int i = 0; i < branchLength; i++)
        {
            GameObject prefab = context.GetPrefab(RoadVisualType.Straight);
            if (prefab == null)
                return lastRoadObject;

            GameObject roadObject = UnityEngine.Object.Instantiate(
                prefab,
                context.DebugStraightRoadRoot != null ? context.DebugStraightRoadRoot : context.RoadRoot);
            roadObject.name = $"{prefab.name}_DebugCityBranch_{branchDirection.x}_{branchDirection.y}_{i}";

            Vector3 incomingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                -branchDirection);
            Vector3 outgoingMarkerLocalPosition = GetMarkerLocalPositionForDirection(
                straightLayout,
                straightVariant,
                branchDirection);

            PlaceObjectByMarker(
                roadObject.transform,
                straightVariant,
                incomingMarkerLocalPosition,
                previousConnectWorldPosition);
            previousConnectWorldPosition = GetObjectMarkerWorldPosition(
                roadObject.transform,
                outgoingMarkerLocalPosition);
            _debugStraightRoadObjects.Add(roadObject);
            lastRoadObject = roadObject;
        }

        return lastRoadObject;
    }

    private static TileConnectionMask BuildAxisMask(Vector2Int direction)
    {
        return RoadVisualVariantSystem.BuildAxisMask(direction);
    }

    private static TileConnectionMask BuildMaskFromDirections(params Vector2Int[] directions)
    {
        return RoadVisualVariantSystem.BuildMaskFromDirections(directions);
    }

    private static Vector3 GetPlacementPosition(Context context, Vector2Int cell, VariantData variant)
    {
        var chunkContext = new RoadChunkVisualSystem.Context(
            context.RoadTiles,
            null,
            null,
            null,
            context.RoadRoot,
            context.GridOrigin,
            context.BuildPlaneY,
            context.RoadGridSize,
            context.ChunkSizeInCells);
        return RoadChunkVisualSystem.GetPlacementPosition(chunkContext, cell, variant);
    }
}
