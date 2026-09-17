using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Configs;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class M05EnemyCompoundTests
    {
        [Test] public void GateIsTheOnlyEntranceAndObjectivesAreInside()
        {
            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(M05BreachAssaultConfigBuilder.MapPath);
            var layout = map.AdditionalBuildingPlacements;
            var gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(M05EnemyCompoundBuilder.Root + "/" + M05EnemyCompoundBuilder.GateId + ".prefab");
            Assert.That(gatePrefab.GetComponent<BuildingDefinitionAuthoring>().ConfiguredCanRequest, Is.False, "Enemy gate leaked into player build catalogue");
            Assert.That(layout, Is.Not.Null);
            var walls = new List<RectInt>();
            foreach (var entry in layout.Placements)
            {
                var authoring = entry.BuildingPrefab.GetComponent<BuildingDefinitionAuthoring>();
                Assert.That(authoring, Is.Not.Null);
                Assert.That(entry.InstantiateVisualWhenSourceMissing, Is.True);
                Vector2Int size = authoring.ConfiguredFootprintCells;
                if (entry.RotateVertical) size = new Vector2Int(size.y, size.x);
                walls.Add(new RectInt(Mathf.RoundToInt(entry.WorldCenter.x) - size.x / 2,
                    Mathf.RoundToInt(entry.WorldCenter.z) - size.y / 2, size.x, size.y));
            }
            var gate = new RectInt(1020, 444, 8, 2);
            foreach (var wall in walls) Assert.That(wall.Overlaps(gate), Is.False, "Fortification overlaps breach opening");
            walls.Add(gate);
            Assert.That(Reachable(walls, new Vector2Int(1024, 438), new Vector2Int(1024, 454), 0), Is.False,
                "Infantry can bypass the closed gate");
            walls.RemoveAt(walls.Count - 1);
            Assert.That(Reachable(walls, new Vector2Int(1024, 438), new Vector2Int(1024, 454), 2), Is.True,
                "APC has no clearance through the destroyed gate");
            foreach (var anchor in map.Anchors)
                if (anchor.AnchorId == M05BreachAssaultConfigBuilder.Prefix + "approach" || anchor.AnchorId == M05BreachAssaultConfigBuilder.Prefix + "core" || anchor.AnchorId == M05BreachAssaultConfigBuilder.Prefix + "archive")
                    Assert.That(new Rect(996, 446, 56, 28).Contains(new Vector2(anchor.Position.x, anchor.Position.z)), Is.True, "Objective outside compound");
        }

        private static bool Reachable(List<RectInt> blockers, Vector2Int start, Vector2Int end, int radius)
        {
            var visited = new HashSet<Vector2Int> { start }; var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); if (p == end) return true;
                foreach (var direction in directions)
                {
                    var n = p + direction;
                    if (n.x < 986 || n.x > 1062 || n.y < 436 || n.y > 484 || visited.Contains(n)) continue;
                    bool blocked = false;
                    foreach (var rect in blockers)
                        if (new RectInt(rect.xMin - radius, rect.yMin - radius, rect.width + radius * 2, rect.height + radius * 2).Contains(n)) { blocked = true; break; }
                    if (!blocked) { visited.Add(n); queue.Enqueue(n); }
                }
            }
            return false;
        }
        public static void ValidateLiveClosedGate()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            using var stateQuery = em.CreateEntityQuery(typeof(CampaignMissionBreachState));
            var state = stateQuery.GetSingleton<CampaignMissionBreachState>();
            Assert.That(state.Ready, Is.EqualTo(1));
            Assert.That(state.GateDestroyed, Is.Zero);
            var gate = em.GetComponentData<RuntimeBuildingCombatInfo>(state.Gate);
            Assert.That(gate.OriginCell, Is.EqualTo(new Unity.Mathematics.int2(1020, 444)), "Gate relocated away from its authored wall opening");
            Assert.That(gate.FootprintCells, Is.EqualTo(new Unity.Mathematics.int2(8, 2)), "Gate footprint must fit the opening");
            using var query = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo));
            using var buildings = query.ToComponentDataArray<RuntimeBuildingCombatInfo>(Allocator.Temp);
            var blockers = new List<RectInt>();
            foreach (var b in buildings)
                blockers.Add(new RectInt(b.OriginCell.x, b.OriginCell.y, b.FootprintCells.x, b.FootprintCells.y));
            Assert.That(Reachable(blockers, new Vector2Int(1024, 438), new Vector2Int(1024, 453), 0), Is.False,
                "Live building footprints leave a route around the closed gate");
            using var gridQuery = em.CreateEntityQuery(typeof(GridConfig), typeof(DynamicBlockerComponent));
            var gridEntity = gridQuery.GetSingletonEntity();
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            var cells = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            Assert.That(cells.Counts[444 * grid.Width + 1024], Is.GreaterThan(0), "Gate does not block navigation");
            Debug.Log("[M05EnemyCompoundTests] live=Passed closed gate seals actual navigation footprints");
        }
        public static void RunFocusedValidation()
        {
            new M05EnemyCompoundTests().GateIsTheOnlyEntranceAndObjectivesAreInside();
            Debug.Log("[M05EnemyCompoundTests] result=Passed sealed-perimeter, APC-clearance, interior-objectives");
        }
    }
}
