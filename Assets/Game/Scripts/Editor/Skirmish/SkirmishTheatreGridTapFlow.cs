#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.UI.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// v48 (P1, Game PM): proof for the tappable Theatre right-panel tiles. On the theatre, taps one real (unlocked)
    /// tile, one locked tile and the "+N" tile with real pointer events + the Button's onClick, re-opening the theatre
    /// before each tap, and asserts the [TheatreGridTap] log line and the resulting library state:
    /// real -> Battlefield, Map layout, that district, that battle selected; locked -> CardLocked heard, state unchanged;
    /// +N -> Battlefield, Grid layout, that district. Any miss throws, which fails the flow with the reason.
    /// Hosted by one hook call in SkirmishBattleLibraryFlowCapture.RunLocale.
    /// v49: before the taps, measures the objective block on the live panel (icon rect bottom -> caption glyph ink top
    /// for each objective) and fails fa if any gap differs from en's (same run, en first) by more than 2 px.
    /// </summary>
    public static class SkirmishTheatreGridTapFlow
    {
        private const string PanelName = "TheatreDetailPanel";
        private const string BattlesName = "TheatreBattles";
        private const string TilePrefix = "TheatreBattle_";
        private const string MoreName = "MoreBattles";
        private static readonly string[] PreferredDistricts = { "IB", "DB" };
        private static readonly string[] ObjectiveIds = SkirmishBattleLibraryArt.ObjectiveIds;
        private const float CaptionGapTolerance = 2f;
        private const float ObjectiveIconSize = 28f;
        private static readonly Dictionary<string, float> EnCaptionGaps = new Dictionary<string, float>();

        public static void Run(GameObject instance, QuickCustomScreenView view, string locale)
        {
            if (instance == null || view == null)
                throw new InvalidOperationException("[TheatreGridTapFlow] no screen instance locale=" + locale);
            var logs = new List<string>();
            var sounds = new List<UIAudioEventKind>();
            Application.LogCallback onLog = (message, stack, type) =>
            {
                if (message != null && message.StartsWith("[TheatreGridTap]", StringComparison.Ordinal))
                    logs.Add(message);
            };
            Action<UIAudioEventRequest> onSound = request => sounds.Add(request.Kind);
            Application.logMessageReceived += onLog;
            UIAudioEventGateway.AudioEventRequested += onSound;
            try
            {
                string district = PickDistrict(instance, view, out string realId, out string lockedId);

                // 0. Objective captions: icon -> caption ink gap matches en.
                Reopen(view, district);
                CheckCaptionGaps(instance, district, locale);

                // 1. Real tile.
                Reopen(view, district);
                Button real = Tile(instance, TilePrefix + realId, district, locale);
                logs.Clear();
                Tap(real);
                Expect(logs, "target=battle:" + realId + " district=" + district, locale);
                Check(view.Library.Level == SkirmishLibraryLevel.Battlefield, "real tile did not open the battlefield", locale);
                Check(view.Library.Battlefield.Layout == SkirmishBattlefieldLayout.Map, "real tile did not open the Map layout (layout=" + view.Library.Battlefield.Layout + ")", locale);
                Check(Same(view.Library.Battlefield.MapId, district), "real tile opened district " + view.Library.Battlefield.MapId + ", expected " + district, locale);
                Check(Same(view.Library.Briefing.ScenarioId, realId), "real tile selected " + view.Library.Briefing.ScenarioId + ", expected " + realId, locale);
                Debug.Log("[TheatreGridTapFlow] tap=real id=" + realId + " district=" + district + " layout=" + view.Library.Battlefield.Layout +
                    " selected=" + view.Library.Briefing.ScenarioId + " ok=1 locale=" + locale);

                // 2. Locked tile.
                Reopen(view, district);
                Button locked = Tile(instance, TilePrefix + lockedId, district, locale);
                SkirmishLibraryLevel level = view.Library.Level;
                SkirmishBattlefieldLayout layout = view.Library.Battlefield.Layout;
                string map = view.Library.Theater.SelectedMapId;
                string selected = view.Library.Briefing.ScenarioId;
                logs.Clear();
                sounds.Clear();
                Tap(locked);
                Expect(logs, "target=locked:" + lockedId + " district=" + district, locale);
                Check(sounds.Contains(UIAudioEventKind.CardLocked), "locked tile did not play CardLocked (heard " + string.Join(",", sounds) + ")", locale);
                Check(view.Library.Level == level, "locked tile changed the level to " + view.Library.Level, locale);
                Check(view.Library.Battlefield.Layout == layout, "locked tile changed the layout to " + view.Library.Battlefield.Layout, locale);
                Check(Same(view.Library.Theater.SelectedMapId, map), "locked tile changed the district to " + view.Library.Theater.SelectedMapId, locale);
                Check(Same(view.Library.Briefing.ScenarioId, selected), "locked tile changed the selection to " + view.Library.Briefing.ScenarioId, locale);
                Debug.Log("[TheatreGridTapFlow] tap=locked id=" + lockedId + " district=" + district + " sound=CardLocked unchanged=1 ok=1 locale=" + locale);

                // 3. "+N" tile.
                Reopen(view, district);
                Button more = Tile(instance, MoreName, district, locale);
                logs.Clear();
                Tap(more);
                Expect(logs, "target=grid:" + district + " district=" + district, locale);
                Check(view.Library.Level == SkirmishLibraryLevel.Battlefield, "+N did not open the battlefield", locale);
                Check(view.Library.Battlefield.Layout == SkirmishBattlefieldLayout.Grid, "+N did not open the Grid layout (layout=" + view.Library.Battlefield.Layout + ")", locale);
                Check(Same(view.Library.Battlefield.MapId, district), "+N opened district " + view.Library.Battlefield.MapId + ", expected " + district, locale);
                Debug.Log("[TheatreGridTapFlow] tap=more district=" + district + " layout=" + view.Library.Battlefield.Layout + " ok=1 locale=" + locale);
                Debug.Log("[TheatreGridTapFlow] result=Passed district=" + district + " real=" + realId + " locked=" + lockedId + " locale=" + locale);
            }
            finally
            {
                Application.logMessageReceived -= onLog;
                UIAudioEventGateway.AudioEventRequested -= onSound;
            }
        }

        /// <summary>The first district (IB, then DB, then the rest) whose panel shows a real tile, a locked tile and +N.</summary>
        private static string PickDistrict(GameObject instance, QuickCustomScreenView view, out string realId, out string lockedId)
        {
            var order = new List<string>(PreferredDistricts);
            SkirmishTheaterPinState[] pins = view.Library.Theater.Pins;
            for (int i = 0; pins != null && i < pins.Length; i++)
            {
                if (!string.IsNullOrEmpty(pins[i].MapId) && !order.Contains(pins[i].MapId))
                    order.Add(pins[i].MapId);
            }

            var seen = new List<string>();
            foreach (string district in order)
            {
                Reopen(view, district);
                Transform battles = Battles(instance);
                realId = null;
                lockedId = null;
                bool more = false;
                for (int i = 0; battles != null && i < battles.childCount; i++)
                {
                    Transform child = battles.GetChild(i);
                    if (child.name == MoreName)
                        more = child.GetComponent<Button>() != null;
                    else if (child.name.StartsWith(TilePrefix, StringComparison.Ordinal) && child.GetComponent<Button>() != null)
                    {
                        string id = child.name.Substring(TilePrefix.Length);
                        if (child.Find("Lock") != null)
                            lockedId ??= id;
                        else
                            realId ??= id;
                    }
                }

                seen.Add(district + "(real=" + (realId ?? "-") + " locked=" + (lockedId ?? "-") + " more=" + (more ? 1 : 0) + ")");
                if (realId != null && lockedId != null && more)
                    return district;
            }

            throw new InvalidOperationException("[TheatreGridTapFlow] result=Failed no district shows a real tile, a locked tile and +N: " + string.Join(" ", seen));
        }

        /// <summary>
        /// v49: on the live theatre panel, gap = caption glyph ink top - ObjectiveType_<id> rect bottom (panel space), for
        /// BA/FC/BT/CE (captions are the Subtitle labels after ObjectiveHeading, in that order). en records its gaps;
        /// fa fails if en was not measured, an icon is not 28 px, or |gap - enGap| > 2 px.
        /// </summary>
        private static void CheckCaptionGaps(GameObject instance, string district, string locale)
        {
            RectTransform panel = null;
            foreach (Transform t in instance.GetComponentsInChildren<Transform>(false))
            {
                if (t.name == PanelName)
                {
                    panel = t as RectTransform;
                    break;
                }
            }

            Check(panel != null, "no " + PanelName + " for the caption gap check", locale);
            Transform heading = panel.Find("ObjectiveHeading");
            Check(heading != null, "no ObjectiveHeading for the caption gap check", locale);
            var captions = new List<TMP_Text>();
            for (int i = heading.GetSiblingIndex() + 1; i < panel.childCount && captions.Count < ObjectiveIds.Length; i++)
            {
                Transform child = panel.GetChild(i);
                if (child.name == "Subtitle")
                    captions.Add(child.GetComponent<TMP_Text>());
            }

            Check(captions.Count == ObjectiveIds.Length, "found " + captions.Count + " objective captions, expected " + ObjectiveIds.Length, locale);
            bool en = locale != "fa-IR";
            if (en)
                EnCaptionGaps.Clear();
            var bad = new List<string>();
            for (int i = 0; i < ObjectiveIds.Length; i++)
            {
                string id = ObjectiveIds[i];
                var icon = panel.Find("ObjectiveType_" + id) as RectTransform;
                Check(icon != null, "no ObjectiveType_" + id, locale);
                Check(Mathf.Abs(icon.rect.height - ObjectiveIconSize) <= 0.5f, "ObjectiveType_" + id + " is " + icon.rect.height.ToString("0.0") + " px, expected 28", locale);
                Check(InkTop(panel, captions[i], out float inkTop), "caption under ObjectiveType_" + id + " has no visible ink", locale);
                float gap = inkTop - RectBottom(panel, icon);
                float enGap = gap;
                if (en)
                    EnCaptionGaps[id] = gap;
                else
                    Check(EnCaptionGaps.TryGetValue(id, out enGap), "no en caption gap recorded for " + id + " (en must run first)", locale);
                float delta = gap - enGap;
                bool ok = Mathf.Abs(delta) <= CaptionGapTolerance;
                Debug.Log("[TheatreGridTapFlow] captionGap obj=" + id + " gap=" + gap.ToString("0.0") + " enGap=" + enGap.ToString("0.0") +
                    " delta=" + delta.ToString("0.0") + " font=" + captions[i].fontSize.ToString("0.0") + " district=" + district + " ok=" + (ok ? 1 : 0) + " locale=" + locale);
                if (!ok)
                    bad.Add(id + " gap=" + gap.ToString("0.0") + " enGap=" + enGap.ToString("0.0"));
            }

            Check(bad.Count == 0, "caption gap differs from en by more than " + CaptionGapTolerance.ToString("0") + " px: " + string.Join(", ", bad), locale);
        }

        /// <summary>Distance from the panel's top edge down to the rect's bottom edge.</summary>
        private static float RectBottom(RectTransform panel, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float lowest = float.MaxValue;
            for (int i = 0; i < 4; i++)
                lowest = Mathf.Min(lowest, panel.InverseTransformPoint(corners[i]).y);
            return panel.rect.yMax - lowest;
        }

        /// <summary>Distance from the panel's top edge down to the highest visible glyph quad of the label.</summary>
        private static bool InkTop(RectTransform panel, TMP_Text text, out float top)
        {
            top = 0f;
            if (text == null || string.IsNullOrWhiteSpace(text.text) || !text.gameObject.activeInHierarchy)
                return false;
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            int count = info != null && info.characterInfo != null ? Mathf.Min(info.characterCount, info.characterInfo.Length) : 0;
            float highest = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible)
                    continue;
                highest = Mathf.Max(highest, panel.InverseTransformPoint(text.rectTransform.TransformPoint(c.topRight)).y);
            }

            if (highest == float.MinValue)
                return false;
            top = panel.rect.yMax - highest;
            return true;
        }

        private static void Reopen(QuickCustomScreenView view, string district)
        {
            view.FocusTheaterOnMap(district);
            view.PresentLibraryCapture("theater");
        }

        private static Transform Battles(GameObject instance)
        {
            foreach (Transform t in instance.GetComponentsInChildren<Transform>(false))
            {
                if (t.name == PanelName)
                    return t.Find(BattlesName);
            }

            return null;
        }

        private static Button Tile(GameObject instance, string name, string district, string locale)
        {
            Transform battles = Battles(instance);
            Transform tile = battles != null ? battles.Find(name) : null;
            Button button = tile != null ? tile.GetComponent<Button>() : null;
            if (button == null || !button.isActiveAndEnabled)
                throw new InvalidOperationException("[TheatreGridTapFlow] result=Failed missing tappable " + name + " district=" + district + " locale=" + locale);
            Graphic target = button.targetGraphic;
            if (target == null || !target.raycastTarget)
                throw new InvalidOperationException("[TheatreGridTapFlow] result=Failed " + name + " has no raycast target district=" + district + " locale=" + locale);
            return button;
        }

        private static void Tap(Button button)
        {
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            button.onClick.Invoke();
        }

        private static void Expect(List<string> logs, string fragment, string locale)
        {
            foreach (string line in logs)
            {
                if (line.Contains(fragment) && line.Contains(" ratio="))
                    return;
            }

            throw new InvalidOperationException("[TheatreGridTapFlow] result=Failed no [TheatreGridTap] line with '" + fragment + "' locale=" + locale +
                " got=" + (logs.Count > 0 ? string.Join(" | ", logs) : "<none>"));
        }

        private static void Check(bool condition, string reason, string locale)
        {
            if (!condition)
                throw new InvalidOperationException("[TheatreGridTapFlow] result=Failed " + reason + " locale=" + locale);
        }

        private static bool Same(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
#endif
