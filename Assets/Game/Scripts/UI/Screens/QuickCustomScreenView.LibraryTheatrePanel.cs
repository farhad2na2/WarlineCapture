using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// v40 (P1) approved-1 Theatre right panel: the selected district's battles as thumbnails with their real medal state,
    /// access (lock) and objective-type icon. Data comes only from the view model: <see cref="SkirmishLibraryViewModel.Battlefield"/>
    /// is built for the selected map (the same map as <see cref="SkirmishTheaterState.SelectedMapId"/>), and each
    /// <see cref="SkirmishBattleCardState"/> carries ScenarioId, ObjectiveId, PreviewSpriteKey, Medals (per-tier mask) and Access.
    /// Icons are the existing Resources/SkirmishBattleLibrary set (medal_*, icon_objective_*, icon_lock).
    /// Grid slots with no catalog entry (no ScenarioId) are not drawn: there is nothing real to show for them.
    /// Called once from PresentTheatreDetailPanel; it takes the space between the panel header and the medal row.
    /// </summary>
    public sealed partial class QuickCustomScreenView
    {
        private const string TheatrePanelBattlesName = "TheatreBattles";
        private const int TheatrePanelColumns = 6;
        private const float TheatrePanelMinThumb = 28f;
        private const float TheatrePanelGap = 4f;

        private void BuildTheatrePanel(SkirmishTheaterPinState selectedZone, RectTransform parent)
        {
            if (parent == null || _libraryViewModel == null)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            Transform old = parent.Find(TheatrePanelBattlesName);
            if (old != null)
                DestroyUi(old.gameObject);

            SkirmishBattlefieldState field = _libraryViewModel.Battlefield;
            if (!string.Equals(field.MapId, selectedZone.MapId, StringComparison.OrdinalIgnoreCase) || field.GridCards == null)
            {
                Debug.Log("[TheatrePanel] map=" + selectedZone.MapId + " skipped=map-mismatch field=" + field.MapId);
                return;
            }

            var battles = new List<SkirmishBattleCardState>();
            for (int i = 0; i < field.GridCards.Length; i++)
            {
                if (!IsEmptyCard(field.GridCards[i]))
                    battles.Add(field.GridCards[i]);
            }

            if (!TryTheatrePanelArea(parent, out Rect area, out Transform blurb))
            {
                Debug.Log("[TheatrePanel] map=" + selectedZone.MapId + " battles=" + battles.Count + " skipped=no-area");
                return;
            }

            int rowsNeeded = Mathf.Max(1, Mathf.CeilToInt(battles.Count / (float)TheatrePanelColumns));
            float thumbW = (area.width - TheatrePanelGap * (TheatrePanelColumns - 1)) / TheatrePanelColumns;
            float thumbH = Mathf.Min(thumbW * 0.75f, (area.height - TheatrePanelGap * (rowsNeeded - 1)) / rowsNeeded);
            if (battles.Count == 0 || thumbH < TheatrePanelMinThumb)
            {
                Debug.Log("[TheatrePanel] map=" + selectedZone.MapId + " battles=" + battles.Count +
                    " skipped=too-small area=" + area.width.ToString("0") + "x" + area.height.ToString("0"));
                return;
            }

            // The zone blurb sat in this space; the battle list replaces it (it is still the zone's own copy elsewhere).
            if (blurb != null)
                blurb.gameObject.SetActive(false);

            var holder = new GameObject(TheatrePanelBattlesName, typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            PlaceTopLeft(holder, area.x, area.y, area.width, area.height);

            int playable = 0;
            int won = 0;
            int medals = 0;
            for (int i = 0; i < battles.Count; i++)
            {
                SkirmishBattleCardState card = battles[i];
                int column = i % TheatrePanelColumns;
                int row = i / TheatrePanelColumns;
                int visual = fa ? TheatrePanelColumns - 1 - column : column;
                float x = visual * (thumbW + TheatrePanelGap);
                float y = row * (thumbH + TheatrePanelGap);
                bool open = card.Access == SkirmishCardAccess.Playable;
                if (open)
                    playable++;
                if (card.Medals != SkirmishMedalMask.None)
                    won++;
                medals += MedalCount(card.Medals);

                var thumb = new GameObject("TheatreBattle_" + card.ScenarioId, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                thumb.transform.SetParent(holder, false);
                thumb.raycastTarget = false;
                Texture2D preview = ResolveGridPreview(card.PreviewSpriteKey, card.ScenarioId, card.MapId, out bool dimmed);
                PlaceCoverPreview(thumb, thumbW, thumbH, preview, dimmed || !open);
                PlaceTopLeft(thumb.rectTransform, x, y, thumbW, thumbH);
                if (!open)
                    thumb.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                Outline edge = thumb.gameObject.AddComponent<Outline>();
                edge.effectColor = card.IsSelected ? new Color(0.95f, 0.75f, 0.3f, 1f) : new Color(0.35f, 0.3f, 0.2f, 0.9f);
                edge.effectDistance = new Vector2(1f, -1f);

                float badge = Mathf.Clamp(thumbH * 0.38f, 10f, 18f);
                var objective = new GameObject("Objective", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                objective.transform.SetParent(thumb.transform, false);
                objective.raycastTarget = false;
                objective.preserveAspect = true;
                PlaceTopLeft(objective.rectTransform, fa ? thumbW - badge - 1f : 1f, 1f, badge, badge);
                ShowIcon(objective, SkirmishBattleLibraryArt.ObjectiveIcon(card.ObjectiveId), Color.white);

                var medal = new GameObject("Medal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                medal.transform.SetParent(thumb.transform, false);
                medal.raycastTarget = false;
                medal.preserveAspect = true;
                PlaceTopLeft(medal.rectTransform, fa ? 1f : thumbW - badge - 1f, thumbH - badge - 1f, badge, badge);
                ShowIcon(medal, HighestMedalIcon(card.Medals), Color.white);

                if (!open)
                {
                    var lockIcon = new GameObject("Lock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    lockIcon.transform.SetParent(thumb.transform, false);
                    lockIcon.raycastTarget = false;
                    lockIcon.preserveAspect = true;
                    float lockSize = Mathf.Clamp(thumbH * 0.45f, 10f, 22f);
                    PlaceTopLeft(lockIcon.rectTransform, (thumbW - lockSize) * 0.5f, (thumbH - lockSize) * 0.5f, lockSize, lockSize);
                    ShowIcon(lockIcon, SkirmishBattleLibraryArt.ResourceRoot + "icon_lock", Color.white);
                }
            }

            Debug.Log("[TheatrePanel] map=" + selectedZone.MapId +
                " battles=" + battles.Count +
                " playable=" + playable +
                " won=" + won +
                " medals=" + medals +
                " pinWon=" + selectedZone.WonCount + "/" + selectedZone.PublishedCount +
                " thumb=" + thumbW.ToString("0.0") + "x" + thumbH.ToString("0.0") +
                " locale=" + (fa ? "fa-IR" : "en"));
        }

        /// <summary>Space between the header block (Title/Stats/Rule/Preview) and the medal row or objective heading.</summary>
        private static bool TryTheatrePanelArea(RectTransform parent, out Rect area, out Transform blurb)
        {
            area = Rect.zero;
            blurb = null;
            float top = 0f;
            float bottom = float.MaxValue;
            float left = float.MaxValue;
            float right = 0f;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is not RectTransform child || !child.gameObject.activeSelf)
                    continue;
                float y = -child.anchoredPosition.y;
                float h = child.sizeDelta.y;
                string n = child.name;
                if (n == "ZoneBlurb")
                {
                    blurb = child;
                    continue;
                }

                if (n == "Title" || n == "Stats" || n == "Rule" || n == "Preview")
                {
                    top = Mathf.Max(top, y + h);
                    left = Mathf.Min(left, child.anchoredPosition.x);
                    right = Mathf.Max(right, child.anchoredPosition.x + child.sizeDelta.x);
                }
                else if (n.StartsWith("ZoneMedal_", StringComparison.Ordinal) || n == "ObjectiveHeading")
                    bottom = Mathf.Min(bottom, y);
            }

            if (bottom == float.MaxValue || right <= left)
                return false;
            float start = top + 8f;
            float end = bottom - 18f; // medal icons are placed a little under their row top
            if (end - start < TheatrePanelMinThumb)
                return false;
            area = new Rect(left, start, right - left, end - start);
            return true;
        }

        private static string HighestMedalIcon(SkirmishMedalMask mask)
        {
            if ((mask & SkirmishMedalMask.Commander) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Commander);
            if ((mask & SkirmishMedalMask.Veteran) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Veteran);
            if ((mask & SkirmishMedalMask.Regular) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Regular);
            if ((mask & SkirmishMedalMask.Recruit) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Recruit);
            return SkirmishBattleLibraryArt.Medal(false, SkirmishDifficultyId.Recruit);
        }

        private static int MedalCount(SkirmishMedalMask mask)
        {
            int count = 0;
            for (int bits = (int)mask; bits != 0; bits &= bits - 1)
                count++;
            return count;
        }
    }
}
