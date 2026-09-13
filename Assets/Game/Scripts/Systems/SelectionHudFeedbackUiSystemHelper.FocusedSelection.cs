using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed partial class SelectionHudFeedbackUiSystemHelper
    {
        private MatchHudSelectionPanelModel BuildFocusedUnitPanelModel(
            Context context,
            EntityManager em,
            Entity entity,
            bool hasAttackModeOrderSnapshot,
            string attackModeOrderText,
            bool boardAvailable)
        {
            Sprite portraitSprite = context.ResolveSelectionPortraitSprite?.Invoke(em, entity);
            portraitSprite ??= _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.GenericSquad);
            bool owned = context.SelectionUiReadModelLookup.IsOwnedByPlayer(em, entity);
            bool movable = em.HasComponent<UnitMove>(entity);
            bool vehicle = context.SelectionUiReadModelLookup.IsVehicleForVisibleSelection(em, entity);
            TryGetHealthModel(context, em, entity, out string healthLabel, out float health01);
            string orderText = ResolveFocusedUnitOrderText(em, entity, context.SelectionUiReadModelLookup);
            string focusedName = context.SelectionUiReadModelLookup.ResolveFocusedUnitName(em, entity);
            string focusedDescription = context.SelectionUiReadModelLookup.ResolveFocusedUnitDescription(em, entity);
            if (hasAttackModeOrderSnapshot)
            {
                orderText = attackModeOrderText;
            }

            return new MatchHudSelectionPanelModel(
                true,
                focusedName,
                focusedDescription,
                orderText,
                healthLabel,
                health01,
                portraitSprite,
                !vehicle,
                null,
                owned && movable && !em.HasComponent<UnitTransportPassenger>(entity),
                owned && !em.HasComponent<CampaignMissionUnitRoleComponent>(entity),
                boardAvailable);
        }
        private bool CanDestroySelectedUnits(EntityManager em)
        {
            using var chunks = GetSelectedTagQuery(em).ToArchetypeChunkArray(Unity.Collections.Allocator.Temp);
            var missionRole = em.GetComponentTypeHandle<CampaignMissionUnitRoleComponent>(true);
            foreach (var chunk in chunks)
                if (chunk.Has(ref missionRole)) return false;
            return chunks.Length > 0;
        }
    }
}
