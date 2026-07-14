#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal static class AudioPlaybackPresentationDiagnostics
    {
        private const int MaxLogs = 64;
        private static int s_LogCount;

        public static void Log(
            EntityManager em,
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry,
            AudioPlaybackPresentationResult result,
            float now)
        {
            if (!Application.isPlaying || s_LogCount >= MaxLogs)
                return;

            string requestBus = request.BusId.ToString();
            string catalogBus = entry?.BusId;
            string bus = string.IsNullOrWhiteSpace(catalogBus) ? requestBus : catalogBus;
            if (!IsDiagnosticBus(bus))
                return;

            s_LogCount++;
            Debug.Log(
                $"[AudioDiag] Playback event={request.EventId} bus={bus} requestId={request.RequestId} " +
                $"played={(result.Played ? 1 : 0)} status={result.Status} reason={result.Reason} at={now:F2} " +
                $"source={DescribeSource(em, request.SourceEntity)}");
        }

        private static bool IsDiagnosticBus(string bus)
        {
            return string.Equals(bus, "Alerts", StringComparison.Ordinal) ||
                   string.Equals(bus, "Voice", StringComparison.Ordinal);
        }

        private static string DescribeSource(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return "null";

            string displayName = em.HasComponent<UnitDisplayInfo>(entity)
                ? em.GetComponentData<UnitDisplayInfo>(entity).Name.ToString()
                : string.Empty;
            string sourceKey = em.HasComponent<UnitSourcePrefabKey>(entity)
                ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
                : string.Empty;
            string faction = em.HasComponent<Faction>(entity)
                ? em.GetComponentData<Faction>(entity).Id.ToString()
                : "?";
            string cell = em.HasComponent<UnitGrid>(entity)
                ? FormatCell(em.GetComponentData<UnitGrid>(entity).Cell)
                : "(?,?)";
            string health = em.HasComponent<UnitHealth>(entity)
                ? FormatHealth(em.GetComponentData<UnitHealth>(entity))
                : "?";

            return $"entity={entity.Index}:{entity.Version} name='{displayName}' source='{sourceKey}' faction={faction} cell={cell} hp={health}";
        }

        private static string FormatCell(Unity.Mathematics.int2 cell)
        {
            return $"({cell.x},{cell.y})";
        }

        private static string FormatHealth(UnitHealth health)
        {
            return $"{health.Current}/{health.Max}";
        }
    }
}
#endif
