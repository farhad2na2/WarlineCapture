using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        // Read-only per-mission guidance. The rendezvous position comes from an anchor that
        // already exists in the mission's own map layout; nothing is spawned or ordered.
        private static void ReadMissionGuidance(EntityManager em, Entity session, in SkirmishMatchState match, ref UiSkirmishModel model)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session)) return;
            string catalog = em.GetComponentData<SkirmishExpandedSessionComponent>(session).CatalogId.ToString();
            if (!SkirmishMissionHudGuidanceCatalog.TryGet(catalog, out var guidance)) return;
            if (SkirmishMissionHudGuidanceCatalog.ShowFirstOrder(guidance, match.ElapsedSeconds))
                model.FirstOrderText = GameText.Get(guidance.FirstOrderKey, guidance.FirstOrderEnglish);
            if (!guidance.HasRendezvous || !em.HasComponent<SkirmishLaunchDefinitionRecord>(session)) return;
            var layout = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session)?.Definition?.MapLayout;
            if (layout == null || !layout.TryGetAnchorById(guidance.RendezvousAnchorId, out var anchor)) return;
            float x = anchor.WorldX, z = anchor.WorldZ;
            if (layout.HasWorldBinding && !layout.TryProjectLocalToMap(x, z, out x, out z)) return;
            if (!float.IsFinite(x) || !float.IsFinite(z)) return;
            // Ground height from the map surface, as the public objective and service markers do
            // (UiShellEcsGateway.PublicObjective.cs, ConvoyService.cs). No sample, no chip.
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if (surfaces.CalculateEntityCount() != 1) return;
            var surface = surfaces.GetSingleton<MapSurfaceComponent>();
            if (surface.CellSize <= 0 || surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated) return;
            int2 cell = (int2)math.floor((new float2(x, z) - surface.GridOrigin.xz) / surface.CellSize);
            if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, cell, out var sample)) return;
            model.RendezvousKnown = true;
            model.RendezvousWorld = new Vector3(x, sample.Height, z);
            model.RendezvousLabel = GameText.Get(guidance.RendezvousLabelKey, guidance.RendezvousLabelEnglish);
        }
    }
}
