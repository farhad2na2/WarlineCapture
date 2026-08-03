using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingSelectionRuntimeCompositionSystemHelper
    {
        public delegate bool TryGetGridDelegate(out GridConfig grid);
        public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
        public delegate bool BuildingIdAction(int buildingId);
        public delegate bool BuildingMoveOrderAction(Vector2Int minCell, Vector2Int sizeCells);
        public delegate bool ScreenPositionPredicate(Vector2 screenPosition);
        public delegate bool BuildingDefinitionPredicate(BuildingDefinition definition);
        public delegate void RuntimeAction();
        public delegate void BuildingHudSelectionAction(RuntimeBuildingEntity building);
        public delegate void CameraFocusAction(Vector3 worldPosition);

        public readonly struct Source
        {
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Camera WorldCamera;
            public readonly TryGetGridDelegate TryGetGrid;
            public readonly GetFootprintCenterDelegate GetFootprintCenter;
            public readonly RuntimeAction SuppressNextWorldClick;
            public readonly RuntimeAction RefreshMarkers;
            public readonly RuntimeAction ClearFocusedUnit;
            public readonly BuildingHudSelectionAction ShowHudSelection;
            public readonly CameraFocusAction SmoothMoveCameraGroundCenterTo;
            public readonly ScreenPositionPredicate IsBoardablePlayerTransportClick;
            public readonly BuildingIdAction TryAssignSelectedHaulerOrders;
            public readonly BuildingMoveOrderAction TryRequestMoveOrderToBuilding;
            public readonly BuildingDefinitionPredicate ShouldUseExpandedSelectionArea;

            public Source(
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                Camera worldCamera,
                TryGetGridDelegate tryGetGrid,
                GetFootprintCenterDelegate getFootprintCenter,
                RuntimeAction suppressNextWorldClick,
                RuntimeAction refreshMarkers,
                RuntimeAction clearFocusedUnit,
                BuildingHudSelectionAction showHudSelection,
                CameraFocusAction smoothMoveCameraGroundCenterTo,
                ScreenPositionPredicate isBoardablePlayerTransportClick,
                BuildingIdAction tryAssignSelectedHaulerOrders,
                BuildingMoveOrderAction tryIssueMoveOrderToBuilding,
                BuildingDefinitionPredicate shouldUseExpandedSelectionArea)
            {
                RuntimeBuildingSystem = runtimeBuildingSystem;
                RuntimeBuildings = runtimeBuildings;
                WorldCamera = worldCamera;
                TryGetGrid = tryGetGrid;
                GetFootprintCenter = getFootprintCenter;
                SuppressNextWorldClick = suppressNextWorldClick;
                RefreshMarkers = refreshMarkers;
                ClearFocusedUnit = clearFocusedUnit;
                ShowHudSelection = showHudSelection;
                SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
                IsBoardablePlayerTransportClick = isBoardablePlayerTransportClick;
                TryAssignSelectedHaulerOrders = tryAssignSelectedHaulerOrders;
                TryRequestMoveOrderToBuilding = tryIssueMoveOrderToBuilding;
                ShouldUseExpandedSelectionArea = shouldUseExpandedSelectionArea;
            }
        }

        public readonly struct Context
        {
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Camera WorldCamera;
            public readonly TryGetGridDelegate TryGetGrid;
            public readonly GetFootprintCenterDelegate GetFootprintCenter;
            public readonly RuntimeAction SuppressNextWorldClick;
            public readonly RuntimeAction RefreshMarkers;
            public readonly RuntimeAction ClearFocusedUnit;
            public readonly BuildingHudSelectionAction ShowHudSelection;
            public readonly CameraFocusAction SmoothMoveCameraGroundCenterTo;
            public readonly ScreenPositionPredicate IsBoardablePlayerTransportClick;
            public readonly BuildingIdAction TryAssignSelectedHaulerOrders;
            public readonly BuildingMoveOrderAction TryRequestMoveOrderToBuilding;
            public readonly BuildingDefinitionPredicate ShouldUseExpandedSelectionArea;

            public Context(
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                Camera worldCamera,
                TryGetGridDelegate tryGetGrid,
                GetFootprintCenterDelegate getFootprintCenter,
                RuntimeAction suppressNextWorldClick,
                RuntimeAction refreshMarkers,
                RuntimeAction clearFocusedUnit,
                BuildingHudSelectionAction showHudSelection,
                CameraFocusAction smoothMoveCameraGroundCenterTo,
                ScreenPositionPredicate isBoardablePlayerTransportClick,
                BuildingIdAction tryAssignSelectedHaulerOrders,
                BuildingMoveOrderAction tryIssueMoveOrderToBuilding,
                BuildingDefinitionPredicate shouldUseExpandedSelectionArea)
            {
                RuntimeBuildingSystem = runtimeBuildingSystem;
                RuntimeBuildings = runtimeBuildings;
                WorldCamera = worldCamera;
                TryGetGrid = tryGetGrid;
                GetFootprintCenter = getFootprintCenter;
                SuppressNextWorldClick = suppressNextWorldClick;
                RefreshMarkers = refreshMarkers;
                ClearFocusedUnit = clearFocusedUnit;
                ShowHudSelection = showHudSelection;
                SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
                IsBoardablePlayerTransportClick = isBoardablePlayerTransportClick;
                TryAssignSelectedHaulerOrders = tryAssignSelectedHaulerOrders;
                TryRequestMoveOrderToBuilding = tryIssueMoveOrderToBuilding;
                ShouldUseExpandedSelectionArea = shouldUseExpandedSelectionArea;
            }
        }

        public void ClearSelectedBuilding(Context context)
        {
            context.RuntimeBuildingSystem?.ClearSelection();
            context.RefreshMarkers?.Invoke();
        }

        public void DeleteSelectedBuilding(Context context, BuildingIdAction deleteBuildingById)
        {
            int? buildingId = context.RuntimeBuildingSystem?.CurrentActiveBuildingId;
            if (!buildingId.HasValue)
                return;

            deleteBuildingById?.Invoke(buildingId.Value);
        }

        public int EnqueueDeleteSelectedBuilding(EntityManager em)
        {
            return EnqueueUiSelectionCommand(
                em,
                BuildingUiSelectionCommandRequestElement.KindDeleteSelectedBuilding);
        }

        public int EnqueueClearSelectedBuilding(EntityManager em)
        {
            return EnqueueUiSelectionCommand(
                em,
                BuildingUiSelectionCommandRequestElement.KindClearSelectedBuilding);
        }

        public bool EnqueueAndProcessDeleteSelectedBuilding(
            EntityManager em,
            Context context,
            BuildingIdAction deleteBuildingById)
        {
            int requestId = EnqueueDeleteSelectedBuilding(em);
            ProcessPendingUiSelectionCommands(em, context, deleteBuildingById);
            return TryGetUiSelectionCommandResult(em, requestId, out BuildingUiSelectionCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public bool EnqueueAndProcessClearSelectedBuilding(EntityManager em, Context context)
        {
            int requestId = EnqueueClearSelectedBuilding(em);
            ProcessPendingUiSelectionCommands(em, context, null);
            return TryGetUiSelectionCommandResult(em, requestId, out BuildingUiSelectionCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public bool TryGetUiSelectionCommandResult(
            EntityManager em,
            int requestId,
            out BuildingUiSelectionCommandResultElement result)
        {
            result = default;
            Entity queueEntity = EnsureUiSelectionCommandEntity(em);
            DynamicBuffer<BuildingUiSelectionCommandResultElement> results =
                em.GetBuffer<BuildingUiSelectionCommandResultElement>(queueEntity);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].RequestId == requestId)
                {
                    result = results[i];
                    return true;
                }
            }

            return false;
        }

        public void ProcessPendingUiSelectionCommands(
            EntityManager em,
            Context context,
            BuildingIdAction deleteBuildingById)
        {
            Entity queueEntity = EnsureUiSelectionCommandEntity(em);
            DynamicBuffer<BuildingUiSelectionCommandRequestElement> requests =
                em.GetBuffer<BuildingUiSelectionCommandRequestElement>(queueEntity);
            if (requests.Length == 0)
                return;

            using NativeList<BuildingUiSelectionCommandRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
                pendingRequests.Add(requests[i]);
            requests.Clear();

            DynamicBuffer<BuildingUiSelectionCommandResultElement> results =
                em.GetBuffer<BuildingUiSelectionCommandResultElement>(queueEntity);
            results.Clear();

            NativeArray<BuildingUiSelectionCommandRequestElement> pendingArray = pendingRequests.AsArray();
            for (int i = 0; i < pendingArray.Length; i++)
            {
                BuildingUiSelectionCommandRequestElement request = pendingArray[i];
                bool accepted = ProcessUiSelectionCommand(
                    context,
                    request,
                    deleteBuildingById,
                    out int buildingId,
                    out byte resultCode);
                results = em.GetBuffer<BuildingUiSelectionCommandResultElement>(queueEntity);
                results.Add(new BuildingUiSelectionCommandResultElement
                {
                    RequestId = request.RequestId,
                    BuildingId = buildingId,
                    RequestKind = request.RequestKind,
                    Accepted = accepted ? (byte)1 : (byte)0,
                    ResultCode = resultCode
                });
            }
        }

        public Context CreateContext(Source source)
        {
            return new Context(
                source.RuntimeBuildingSystem,
                source.RuntimeBuildings,
                source.WorldCamera,
                source.TryGetGrid,
                source.GetFootprintCenter,
                source.SuppressNextWorldClick,
                source.RefreshMarkers,
                source.ClearFocusedUnit,
                source.ShowHudSelection,
                source.SmoothMoveCameraGroundCenterTo,
                source.IsBoardablePlayerTransportClick,
                source.TryAssignSelectedHaulerOrders,
                source.TryRequestMoveOrderToBuilding,
                source.ShouldUseExpandedSelectionArea);
        }

        private static bool ProcessUiSelectionCommand(
            Context context,
            BuildingUiSelectionCommandRequestElement request,
            BuildingIdAction deleteBuildingById,
            out int buildingId,
            out byte resultCode)
        {
            buildingId = 0;
            if (context.RuntimeBuildingSystem == null)
            {
                resultCode = BuildingUiSelectionCommandResultElement.MissingRuntimeSystem;
                return false;
            }

            switch (request.RequestKind)
            {
                case BuildingUiSelectionCommandRequestElement.KindDeleteSelectedBuilding:
                    int? selectedBuildingId = context.RuntimeBuildingSystem.CurrentActiveBuildingId;
                    if (!selectedBuildingId.HasValue)
                    {
                        resultCode = BuildingUiSelectionCommandResultElement.MissingSelection;
                        return false;
                    }

                    buildingId = selectedBuildingId.Value;
                    if (deleteBuildingById == null || !deleteBuildingById(buildingId))
                    {
                        resultCode = BuildingUiSelectionCommandResultElement.DeleteRejected;
                        return false;
                    }

                    resultCode = BuildingUiSelectionCommandResultElement.Completed;
                    return true;

                case BuildingUiSelectionCommandRequestElement.KindClearSelectedBuilding:
                    context.RuntimeBuildingSystem.ClearSelection();
                    context.RefreshMarkers?.Invoke();
                    resultCode = BuildingUiSelectionCommandResultElement.Completed;
                    return true;

                default:
                    resultCode = BuildingUiSelectionCommandResultElement.DeleteRejected;
                    return false;
            }
        }

        private static int EnqueueUiSelectionCommand(EntityManager em, byte requestKind)
        {
            Entity queueEntity = EnsureUiSelectionCommandEntity(em);
            BuildingUiSelectionCommandQueueComponent queue =
                em.GetComponentData<BuildingUiSelectionCommandQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<BuildingUiSelectionCommandRequestElement>(queueEntity).Add(new BuildingUiSelectionCommandRequestElement
            {
                RequestId = queue.LastRequestId,
                RequestKind = requestKind
            });
            return queue.LastRequestId;
        }

        private static Entity EnsureUiSelectionCommandEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingUiSelectionCommandQueueComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                Entity existing = query.GetSingletonEntity();
                EnsureUiSelectionCommandBuffers(em, existing);
                return existing;
            }

            Entity entity = em.CreateEntity(typeof(BuildingUiSelectionCommandQueueComponent));
            em.SetName(entity, "BuildingUiSelectionCommands");
            EnsureUiSelectionCommandBuffers(em, entity);
            return entity;
        }

        private static void EnsureUiSelectionCommandBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<BuildingUiSelectionCommandRequestElement>(entity))
                em.AddBuffer<BuildingUiSelectionCommandRequestElement>(entity);
            if (!em.HasBuffer<BuildingUiSelectionCommandResultElement>(entity))
                em.AddBuffer<BuildingUiSelectionCommandResultElement>(entity);
        }

        public Context CreateContext(
            RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Camera worldCamera,
            TryGetGridDelegate tryGetGrid,
            GetFootprintCenterDelegate getFootprintCenter,
            RuntimeAction suppressNextWorldClick,
            RuntimeAction refreshMarkers,
            RuntimeAction clearFocusedUnit,
            BuildingHudSelectionAction showHudSelection,
            CameraFocusAction smoothMoveCameraGroundCenterTo,
            ScreenPositionPredicate isBoardablePlayerTransportClick,
            BuildingIdAction tryAssignSelectedHaulerOrders,
            BuildingMoveOrderAction tryIssueMoveOrderToBuilding,
            BuildingDefinitionPredicate shouldUseExpandedSelectionArea)
        {
            return CreateContext(new Source(
                runtimeBuildingSystem,
                runtimeBuildings,
                worldCamera,
                tryGetGrid,
                getFootprintCenter,
                suppressNextWorldClick,
                refreshMarkers,
                clearFocusedUnit,
                showHudSelection,
                smoothMoveCameraGroundCenterTo,
                isBoardablePlayerTransportClick,
                tryAssignSelectedHaulerOrders,
                tryIssueMoveOrderToBuilding,
                shouldUseExpandedSelectionArea));
        }

        public void SelectAndFocusBuilding(Context context, RuntimeBuildingEntity building)
        {
            if (!IsSelectablePlayerBuilding(building))
                return;

            context.RuntimeBuildingSystem?.SelectBuilding(building.Id);
            context.SuppressNextWorldClick?.Invoke();
            context.RefreshMarkers?.Invoke();
            context.ClearFocusedUnit?.Invoke();
            context.ShowHudSelection?.Invoke(building);

            Vector3 focusWorldPosition = ResolveBuildingFocusWorldPosition(context, building);
            context.SmoothMoveCameraGroundCenterTo?.Invoke(focusWorldPosition);
        }

        public Vector3 ResolveBuildingFocusWorldPosition(Context context, RuntimeBuildingEntity building)
        {
            if (building?.Instance == null)
                return Vector3.zero;

            if (building.Definition != null &&
                context.TryGetGrid != null &&
                context.GetFootprintCenter != null &&
                context.TryGetGrid(out GridConfig grid))
            {
                return context.GetFootprintCenter(building.OriginCell, building.Definition.FootprintCells, grid);
            }

            Vector3 position = building.Instance.transform.position;
            position.y = 0f;
            return position;
        }

        public bool TryResolveBuildingFocusWorldPosition(Context context, RuntimeBuildingEntity building, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (building == null)
                return false;

            worldPosition = ResolveBuildingFocusWorldPosition(context, building);
            return true;
        }

        public bool HasVisibleSelectableBuilding(Context context, Camera camera, int screenWidth, int screenHeight)
        {
            if (camera == null || context.RuntimeBuildings == null)
                return false;

            Rect screenRect = new(0f, 0f, screenWidth, screenHeight);
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = pair.Value;
                if (building == null || building.IsDestroyed || building.Instance == null || !building.Instance.activeInHierarchy)
                    continue;
                if (!IsSelectablePlayerBuilding(building))
                    continue;

                Vector3 screen = camera.WorldToScreenPoint(ResolveBuildingFocusWorldPosition(context, building));
                if (screen.z > 0f && screenRect.Contains(new Vector2(screen.x, screen.y)))
                    return true;
            }

            return false;
        }

        public bool SelectFirstBuildingInScreenRect(Context context, Rect screenRect)
        {
            if (context.WorldCamera == null || context.RuntimeBuildings == null)
                return false;

            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = entry.Value;
                if (building?.Definition == null || building.IsDestroyed || building.Instance == null || !building.Instance.activeInHierarchy)
                    continue;
                if (!IsSelectablePlayerBuilding(building))
                    continue;

                if (!TryGetBuildingScreenRect(context, building, out Rect buildingRect, out _))
                    continue;
                if (!screenRect.Overlaps(buildingRect))
                    continue;

                Vector2Int min = building.OriginCell;
                Vector2Int size = building.Definition.FootprintCells;
                return SelectBuildingCandidate(context, entry.Key, min, size);
            }

            return false;
        }

        public bool HandleBuildingSelectionClick(Context context, Vector2 screenPosition, Vector2Int cell)
        {
            if (context.RuntimeBuildings == null)
                return false;

            if (context.IsBoardablePlayerTransportClick != null &&
                context.IsBoardablePlayerTransportClick(screenPosition))
            {
                return true;
            }

            // A visible building owns a direct tap on its presentation even when the
            // ground projection lands inside another building's (possibly expanded)
            // footprint. This is especially important for large authored compounds
            // with broad gameplay footprints surrounding smaller nearby buildings.
            if (TrySelectVisualBuildingAtScreenPosition(context, screenPosition))
                return true;

            int bestBuildingId = 0;
            RuntimeBuildingEntity bestBuilding = null;
            Vector2Int bestMin = default;
            Vector2Int bestSize = default;
            bool bestContainsCanonicalCell = false;
            int bestCanonicalArea = int.MaxValue;

            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = entry.Value;
                if (building?.Definition == null)
                    continue;
                if (!IsSelectablePlayerBuilding(building))
                    continue;

                Vector2Int canonicalMin = building.OriginCell;
                Vector2Int canonicalSize = building.Definition.FootprintCells;
                bool containsCanonicalCell =
                    cell.x >= canonicalMin.x &&
                    cell.y >= canonicalMin.y &&
                    cell.x < canonicalMin.x + canonicalSize.x &&
                    cell.y < canonicalMin.y + canonicalSize.y;
                Vector2Int min = canonicalMin;
                Vector2Int size = canonicalSize;
                if (context.ShouldUseExpandedSelectionArea != null &&
                    context.ShouldUseExpandedSelectionArea(building.Definition))
                {
                    min -= Vector2Int.one;
                    size += new Vector2Int(2, 2);
                }

                if (cell.x < min.x || cell.y < min.y || cell.x >= min.x + size.x || cell.y >= min.y + size.y)
                    continue;

                int canonicalArea = Mathf.Max(1, canonicalSize.x) * Mathf.Max(1, canonicalSize.y);
                if (bestBuilding != null &&
                    (bestContainsCanonicalCell && !containsCanonicalCell ||
                     bestContainsCanonicalCell == containsCanonicalCell && canonicalArea > bestCanonicalArea ||
                     bestContainsCanonicalCell == containsCanonicalCell && canonicalArea == bestCanonicalArea && entry.Key >= bestBuildingId))
                {
                    continue;
                }

                bestBuildingId = entry.Key;
                bestBuilding = building;
                bestMin = min;
                bestSize = size;
                bestContainsCanonicalCell = containsCanonicalCell;
                bestCanonicalArea = canonicalArea;
            }

            return bestBuilding != null && SelectBuildingCandidate(context, bestBuildingId, bestMin, bestSize);
        }

        private static bool SelectBuildingCandidate(
            Context context,
            int buildingId,
            Vector2Int min,
            Vector2Int size)
        {
            RuntimeBuildingEntity selectedBuilding = context.RuntimeBuildings != null &&
                                                   context.RuntimeBuildings.TryGetValue(buildingId, out RuntimeBuildingEntity building)
                ? building
                : null;
            if (!IsSelectablePlayerBuilding(selectedBuilding))
                return false;

            if (context.TryAssignSelectedHaulerOrders != null &&
                context.TryAssignSelectedHaulerOrders(buildingId))
            {
                context.SuppressNextWorldClick?.Invoke();
                context.ClearFocusedUnit?.Invoke();
                return true;
            }

            context.RuntimeBuildingSystem?.SelectBuilding(buildingId);
            context.SuppressNextWorldClick?.Invoke();
            context.RefreshMarkers?.Invoke();
            context.ClearFocusedUnit?.Invoke();
            context.ShowHudSelection?.Invoke(selectedBuilding);
            return true;
        }

        private bool TrySelectVisualBuildingAtScreenPosition(Context context, Vector2 screenPosition)
        {
            if (context.WorldCamera == null || context.RuntimeBuildings == null)
                return false;

            int bestBuildingId = 0;
            RuntimeBuildingEntity bestBuilding = null;
            float bestCenterDistanceSq = float.MaxValue;
            float bestDepth = float.MaxValue;
            float bestArea = float.MaxValue;

            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = entry.Value;
                if (building == null || building.IsDestroyed || building.Instance == null || !building.Instance.activeInHierarchy)
                    continue;
                if (!IsSelectablePlayerBuilding(building))
                    continue;

                if (!TryGetBuildingScreenRect(context, building, out Rect rect, out float depth))
                    continue;
                if (!rect.Contains(screenPosition))
                    continue;

                Vector3 hitCenterWorld;
                if (building.Instance.TryGetComponent(out MapAuthoredBuildingVisualComponent authoredVisual) &&
                    authoredVisual.HasPresentationWorldCenter)
                {
                    hitCenterWorld = authoredVisual.PresentationWorldCenter;
                }
                else
                {
                    hitCenterWorld = ResolveBuildingFocusWorldPosition(context, building);
                }

                Vector3 hitCenterScreen = context.WorldCamera.WorldToScreenPoint(hitCenterWorld);
                if (hitCenterScreen.z <= 0f)
                    continue;

                float centerDistanceSq =
                    (screenPosition - new Vector2(hitCenterScreen.x, hitCenterScreen.y)).sqrMagnitude;
                float area = rect.width * rect.height;
                if (centerDistanceSq > bestCenterDistanceSq + 0.01f)
                    continue;
                if (Mathf.Abs(centerDistanceSq - bestCenterDistanceSq) <= 0.01f &&
                    (area > bestArea + 0.01f ||
                     Mathf.Abs(area - bestArea) <= 0.01f &&
                     (depth > bestDepth + 0.001f ||
                      Mathf.Abs(depth - bestDepth) <= 0.001f && entry.Key >= bestBuildingId)))
                {
                    continue;
                }

                bestBuildingId = entry.Key;
                bestBuilding = building;
                bestCenterDistanceSq = centerDistanceSq;
                bestDepth = depth;
                bestArea = area;
            }

            if (bestBuilding == null || bestBuilding.Definition == null)
                return false;

            Vector2Int min = bestBuilding.OriginCell;
            Vector2Int size = bestBuilding.Definition.FootprintCells;
            if (context.ShouldUseExpandedSelectionArea != null &&
                context.ShouldUseExpandedSelectionArea(bestBuilding.Definition))
            {
                min -= Vector2Int.one;
                size += new Vector2Int(2, 2);
            }

            return SelectBuildingCandidate(context, bestBuildingId, min, size);
        }

        private static bool IsSelectablePlayerBuilding(RuntimeBuildingEntity building)
        {
            return building != null &&
                   !building.IsDestroyed &&
                   building.HasOwnerFaction &&
                   FactionIdentity.IsPlayerControlled(building.OwnerFactionId);
        }

        private static bool TryGetBuildingScreenRect(Context context, RuntimeBuildingEntity building, out Rect rect, out float depth)
        {
            rect = default;
            depth = float.MaxValue;
            Camera camera = context.WorldCamera;
            if (camera == null)
                return false;

            MapAuthoredBuildingVisualComponent authoredVisual = null;
            bool hasAuthoredPresentationCenter =
                building.Instance.TryGetComponent(out authoredVisual) &&
                authoredVisual.HasPresentationWorldCenter;

            // Static-reuse map owners can reference a packed renderer shared by many
            // buildings. That renderer is valid for faction tinting, but it cannot own
            // a direct click for any one building. Authored owners therefore always use
            // their baked center and canonical footprint for hit ownership.
            Renderer[] renderers = null;
            if (!hasAuthoredPresentationCenter)
            {
                renderers = building.FactionVisualRenderers;
                if (renderers == null || renderers.Length == 0)
                    renderers = building.Instance.GetComponentsInChildren<Renderer>(true);
            }

            Bounds plausibleOwnedRendererBounds = default;
            bool hasPlausibleOwnedRendererBounds = hasAuthoredPresentationCenter &&
                context.TryGetGrid != null &&
                context.TryGetGrid(out GridConfig ownedBoundsGrid) &&
                MapAuthoredBuildingSelectionGeometryUtility.TryResolvePlausibleOwnedRendererBounds(
                    building.Instance,
                    authoredVisual,
                    building.Definition.FootprintCells,
                    ownedBoundsGrid,
                    out plausibleOwnedRendererBounds);

            bool hasPoint = false;
            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            float minDepth = float.MaxValue;

            for (int i = 0; renderers != null && i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                Bounds bounds = renderer.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 world = new(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                    Vector3 screen = camera.WorldToScreenPoint(world);
                    if (screen.z <= 0f)
                        continue;

                    hasPoint = true;
                    min.x = Mathf.Min(min.x, screen.x);
                    min.y = Mathf.Min(min.y, screen.y);
                    max.x = Mathf.Max(max.x, screen.x);
                    max.y = Mathf.Max(max.y, screen.y);
                    minDepth = Mathf.Min(minDepth, screen.z);
                }
            }

            if (!hasPoint && hasPlausibleOwnedRendererBounds)
            {
                Bounds bounds = plausibleOwnedRendererBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 world = new(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                    Vector3 screen = camera.WorldToScreenPoint(world);
                    if (screen.z <= 0f)
                        continue;

                    hasPoint = true;
                    min.x = Mathf.Min(min.x, screen.x);
                    min.y = Mathf.Min(min.y, screen.y);
                    max.x = Mathf.Max(max.x, screen.x);
                    max.y = Mathf.Max(max.y, screen.y);
                    minDepth = Mathf.Min(minDepth, screen.z);
                }
            }

            // Candidate Android presentation can leave the managed selection owner
            // without child renderers. Prefer exact placement-prefab geometry; gameplay
            // footprint remains only the fallback when that presentation data is absent.
            if (!hasPoint &&
                hasAuthoredPresentationCenter &&
                context.TryGetGrid != null &&
                context.TryGetGrid(out GridConfig grid))
            {
                Vector2Int footprint = building.Definition.FootprintCells;
                Vector3 center = authoredVisual.PresentationWorldCenter;
                Vector3 size = authoredVisual.HasPresentationGeometry
                    ? authoredVisual.PresentationWorldSize
                    : new Vector3(
                        Mathf.Max(grid.CellSize, footprint.x * grid.CellSize),
                        building.Definition.HasLocalBounds
                            ? Mathf.Max(grid.CellSize, Mathf.Abs(building.Definition.LocalBounds.size.y))
                            : grid.CellSize,
                        Mathf.Max(grid.CellSize, footprint.y * grid.CellSize));
                Quaternion rotation = authoredVisual.HasPresentationGeometry
                    ? Quaternion.Euler(0f, authoredVisual.PresentationYawDegrees, 0f)
                    : Quaternion.identity;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 offset = new(
                        ((corner & 1) == 0 ? -0.5f : 0.5f) * size.x,
                        ((corner & 2) == 0 ? -0.5f : 0.5f) * size.y,
                        ((corner & 4) == 0 ? -0.5f : 0.5f) * size.z);
                    Vector3 world = center + rotation * offset;
                    Vector3 screen = camera.WorldToScreenPoint(world);
                    if (screen.z <= 0f)
                        continue;

                    hasPoint = true;
                    min.x = Mathf.Min(min.x, screen.x);
                    min.y = Mathf.Min(min.y, screen.y);
                    max.x = Mathf.Max(max.x, screen.x);
                    max.y = Mathf.Max(max.y, screen.y);
                    minDepth = Mathf.Min(minDepth, screen.z);
                }
            }

            if (!hasPoint)
                return false;

            const float PaddingPixels = 8f;
            rect = Rect.MinMaxRect(
                min.x - PaddingPixels,
                min.y - PaddingPixels,
                max.x + PaddingPixels,
                max.y + PaddingPixels);
            depth = minDepth;
            return rect.width > 0f && rect.height > 0f;
        }
    }
}
