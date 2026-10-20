using Game.Skirmish.Contracts;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// v49d (P1, Game PM): approved-2 briefing stat row, three tiles in the mockup's order: TIME, UNITS, MAP SIZE, with
    /// the "COMBINED ARMS · FIELD BASE" subtitle above. Only real data is shown; a slot without it keeps the earlier tile:
    /// - Time: SkirmishBriefingState.DeadlineSeconds (ShowTime), from SkirmishLibraryComposition.ReadBriefingSource
    ///   (definition.ObjectiveConfig.DeadlineSeconds(size)). Without it the slot shows the Objective tile.
    /// - Units: no unit counts exist in the briefing state or the scenario definition (SkirmishStartPackageConfig only
    ///   has StandardStructureCount), so this slot always shows the Army (force) tile.
    /// - Map size: SkirmishBriefingState.SizeLocKey (ShowSize), from definition.RecommendedSize. Without it the slot
    ///   shows the Start (base) tile.
    /// - Subtitle: SkirmishBriefingState.ArmyLocKey / StartLocKey from the catalog strings ("Combined arms",
    ///   "Field base" / "نیروی ترکیبی", "پایگاه میدانی"); the short profile labels only when a key has no text.
    /// </summary>
    public sealed partial class QuickCustomScreenView
    {
        private static readonly string[] StatTileNames = { "Time", "Size", "Objective", "Army", "Start" };

        private void PresentApproved2StatTiles(SkirmishBriefingState briefing, string objectiveId, string armyLabel, string startLabel, bool fa)
        {
            if (_statRow == null)
                return;
            bool time = briefing.ShowTime && IsTileShown("Time");
            bool size = briefing.ShowSize && IsTileShown("Size");
            string[] slots = { time ? "Time" : "Objective", "Army", size ? "Size" : "Start" };
            for (int i = 0; i < StatTileNames.Length; i++)
            {
                string name = StatTileNames[i];
                if (System.Array.IndexOf(slots, name) < 0)
                    SetActive(_statRow.Find(name), false);
            }

            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (IsTileShown(slots[i]))
                    count++;
            }

            string shown = string.Empty;
            if (count > 0)
            {
                float width = _statRow.rect.width > 200f ? _statRow.rect.width : _statRow.sizeDelta.x;
                if (width < 200f)
                    width = 518f;
                const float gap = 8f;
                float tileWidth = (width - gap * (count - 1)) / count;
                float x = 0f;
                for (int i = 0; i < slots.Length; i++)
                {
                    Transform tile = _statRow.Find(slots[i]);
                    if (tile == null || !tile.gameObject.activeSelf)
                        continue;
                    PlaceTruthfulTile(tile, x, tileWidth);
                    x += tileWidth + gap;
                    shown += (shown.Length > 0 ? "," : string.Empty) + slots[i];
                }
            }

            // approved-2: icon tiles at mockup weight (icon over value, no caption).
            DecorateIconTile(_statRow.Find("Time"), FooterGlyph.Clock, null, fa);
            DecorateIconTile(_statRow.Find("Size"), FooterGlyph.Squares, null, fa);
            DecorateIconTile(_statRow.Find("Objective"), FooterGlyph.None, string.IsNullOrEmpty(objectiveId) ? null : SkirmishBattleLibraryArt.ObjectiveIcon(objectiveId), fa);
            DecorateIconTile(_statRow.Find("Army"), FooterGlyph.Shield, null, fa);
            DecorateIconTile(_statRow.Find("Start"), FooterGlyph.Pin, null, fa);

            string army = string.IsNullOrEmpty(briefing.ArmyLocKey) ? string.Empty : LibraryText(briefing.ArmyLocKey, fa);
            string start = string.IsNullOrEmpty(briefing.StartLocKey) ? string.Empty : LibraryText(briefing.StartLocKey, fa);
            PresentBriefingSubtitle(string.IsNullOrWhiteSpace(army) ? armyLabel : army, string.IsNullOrWhiteSpace(start) ? startLabel : start, fa);
            Debug.Log("[BriefingTiles] scenario=" + briefing.ScenarioId + " tiles=" + shown +
                " time=" + (time ? "deadline:" + briefing.DeadlineSeconds : "fallback:Objective") +
                " units=fallback:Army size=" + (size ? "size:" + briefing.SizeLocKey : "fallback:Start") +
                " subtitle=" + (string.IsNullOrWhiteSpace(army) ? "profile" : "catalog") + " locale=" + (fa ? "fa-IR" : "en"));
        }

        private bool IsTileShown(string name)
        {
            Transform tile = _statRow != null ? _statRow.Find(name) : null;
            return tile != null && tile.gameObject.activeSelf;
        }
    }
}
