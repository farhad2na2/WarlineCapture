using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static readonly HashSet<string> DamagePairs = new();
        private static int lastDamageEvent;
        private static void ResetCombatDiagnostics() { DamagePairs.Clear(); lastDamageEvent = 0; }

        private static void LogCombatDamage(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
            if (query.CalculateEntityCount() != 1) return;
            Entity queue = query.GetSingletonEntity();
            if (!em.HasBuffer<CombatDamageObservationElement>(queue)) return;
            var observations = em.GetBuffer<CombatDamageObservationElement>(queue, true);
            for (int i = 0; i < observations.Length; i++)
            {
                var item = observations[i];
                if (item.EventId <= lastDamageEvent) continue;
                lastDamageEvent = item.EventId;
                if (!em.HasComponent<CampaignMissionUnitRoleComponent>(item.TargetEntity)) continue;
                string pair = item.SourceEntity + ":" + item.TargetEntity;
                if (!DamagePairs.Add(pair)) continue;
                string source = em.Exists(item.SourceEntity) && em.HasComponent<UnitSourcePrefabKey>(item.SourceEntity)
                    ? em.GetComponentData<UnitSourcePrefabKey>(item.SourceEntity).Value.ToString() : item.SourceEntity.ToString();
                Debug.Log($"[M03DamageProbe] source={source} sourcePos={item.SourceWorldPosition} target={item.TargetEntity} targetPos={item.TargetWorldPosition} kind={item.SourceKind} damage={item.DamageApplied} healthAfter={item.TargetHealthAfter}");
            }
        }

        private static void LogMovement(EntityManager em, Entity entity)
        {
            string details = "entity=" + entity.Index;
            if (em.HasComponent<EngageTarget>(entity)) details += " engage=" + JsonUtility.ToJson(em.GetComponentData<EngageTarget>(entity));
            if (em.HasComponent<UnitFuelConsumption>(entity)) details += " fuel=" + JsonUtility.ToJson(em.GetComponentData<UnitFuelConsumption>(entity));
            if (em.HasComponent<UnitTarget>(entity)) details += " target=" + JsonUtility.ToJson(em.GetComponentData<UnitTarget>(entity));
            if (em.HasComponent<UnitPathRequest>(entity)) details += " pathRequest=" + JsonUtility.ToJson(em.GetComponentData<UnitPathRequest>(entity));
            if (em.HasComponent<UnitPathFollow>(entity)) details += " follow=" + JsonUtility.ToJson(em.GetComponentData<UnitPathFollow>(entity));
            if (em.HasComponent<UnitPathRange>(entity)) details += " range=" + JsonUtility.ToJson(em.GetComponentData<UnitPathRange>(entity));
            if (em.HasComponent<UnitMove>(entity)) details += " move=" + JsonUtility.ToJson(em.GetComponentData<UnitMove>(entity));
            Debug.Log("[M03MovementProbe] " + details);
        }
    }
}
