using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed partial class SkirmishMatchView
    {

#if UNITY_EDITOR
        // Validation observer only. Never consumed by ARIA decisions.
        public static readonly bool NativeCombatEvidenceEnabled =
            System.Environment.GetEnvironmentVariable("WARLINE_S005_COMBAT_EVIDENCE") == "1";
        [System.Serializable]
        public struct NativeEnemyBaseTouchEvidence
        {
            public bool collected, viewPresent, markerVisible, targetVisible, reachabilityEvaluated, reachable;
            public int frame, viewIdentity;
            public Game.UI.Contracts.AriaTouchTarget target;
        }
        [System.Serializable]
        public struct NativeHomePadSiteEvidence
        {
            public bool collected, homeKnown, siteKnown, screenKnown, screenValid, reachabilityEvaluated, reachable;
            public int frame, index, viewIdentity, uiHitCount;
            public string refusal;
            public Vector3 home, enemy, site, screen;
            public float rejectedDistanceSquared, requiredClearance;
        }
        public static NativeHomePadSiteEvidence NativeHomePadSite0, NativeHomePadSite1,
            NativeHomePadSite2, NativeHomePadSite3;
        public static void RecordNativeHomePadSiteEvidence(NativeHomePadSiteEvidence value)
        {
            if (!NativeCombatEvidenceEnabled) return;
            switch(value.index)
            {
                case 0: NativeHomePadSite0=value; break; case 1: NativeHomePadSite1=value; break;
                case 2: NativeHomePadSite2=value; break; case 3: NativeHomePadSite3=value; break;
            }
        }
        public static NativeEnemyBaseTouchEvidence NativeEnemyBaseTouchSnapshot { get; private set; }
        public static void RecordNativeEnemyBaseTouchEvidence(NativeEnemyBaseTouchEvidence value)
        {
            if (NativeCombatEvidenceEnabled) NativeEnemyBaseTouchSnapshot = value;
        }
#endif

        public Button PlayerFocusButton => player != null ? player.GetComponentInParent<Button>() : null;
        public Button EnemyFocusButton => enemy != null ? enemy.GetComponentInParent<Button>() : null;
        // Supplied by the shared objective presentation, not by the ARIA planner.
        public Vector2 VisibleEnemyBasePoint { get; private set; }
        public Vector3 EnemyBaseObjectivePosition { get; private set; }
        public Vector3 PlayerBaseObjectivePosition { get; private set; }
        public bool EnemyBaseMarkerVisible { get; private set; }
        public bool EnemyBaseTargetVisible { get; private set; }
        public bool EnemyBaseObjectiveAlive { get; private set; }
        private RectTransform baseMarker;
        private TMPro.TMP_Text baseMarkerTitle, baseMarkerDetail;
        private V3GradientGraphic baseMarkerChip;
        private HudOwnershipGlyphGraphic baseMarkerGlyph;
        private Image baseMarkerStem;
        private HudDirectionArrowGraphic baseMarkerCue;
        private string baseMarkerShown;
        // Latest fog-respecting knowledge from the public Skirmish model (set in Update).
        private bool enemyBaseKnowledgeKnown;
        private byte enemyBaseKnowledge;
        private float enemyBaseLastSeenAge;
        private void ObserveEnemyBaseKnowledge(in UiSkirmishModel model)
        {
            enemyBaseKnowledgeKnown = model.EnemyBaseKnowledgeKnown;
            enemyBaseKnowledge = model.EnemyBaseKnowledge;
            enemyBaseLastSeenAge = model.EnemyBaseLastSeenAgeSeconds;
        }
        public void PresentEnemyBaseMarker(Vector3 screen, bool alive, Vector3 worldPosition, Vector3 playerPosition, bool targetVisible = true)
        {
            if (baseMarker == null) BuildEnemyBaseMarker();
            bool shown = alive && hud != null && hud.activeInHierarchy && screen.z > 0 &&
                screen.x > 0 && screen.y > 0 && screen.x < Screen.width && screen.y < Screen.height;
            baseMarker.gameObject.SetActive(shown);
            EnemyBaseMarkerVisible = shown;
            EnemyBaseTargetVisible = shown && targetVisible;
            EnemyBaseObjectiveAlive = alive;
            EnemyBaseObjectivePosition = worldPosition;
            PlayerBaseObjectivePosition = playerPosition;
            VisibleEnemyBasePoint = screen;
            if (!shown) { HideCue(baseMarkerCue); return; }
            // The stem's foot sits on the base point; the chip rides above it so the
            // ground under the point stays visible and tappable (nothing here raycasts).
            // The visibility facts above describe the projected base point; only the chip
            // moves: under the HUD it is clamped into the play area with an arrow.
            var knowledge = SkirmishEnemyBaseMarkerState.Resolve(enemyBaseKnowledgeKnown, enemyBaseKnowledge, targetVisible);
            var text = UiShellRuntimeGateway.Localization;
            int distance = SkirmishEnemyBaseMarkerState.RoundedGroundDistance(playerPosition, worldPosition);
            // The player base position is public; a zero vector means it was not supplied.
            string range = playerPosition == Vector3.zero ? string.Empty
                : text.Format("ui.skirmish.marker.distance_from_base", "{0} m from your base", distance);
            string state = knowledge == SkirmishEnemyBaseMarkerKnowledge.InSight
                ? text.Get("ui.skirmish.marker.in_sight", "IN SIGHT")
                : knowledge == SkirmishEnemyBaseMarkerKnowledge.LastSeen
                    ? text.Format("ui.skirmish.marker.last_seen_ago", "LAST SEEN {0} AGO", SkirmishEnemyBaseMarkerState.FormatAge(enemyBaseLastSeenAge))
                    : text.Get("ui.skirmish.marker.reported", "REPORTED POSITION");
            // W40 (Board 4): the enemy base is always enemy red-orange with a diamond; knowledge
            // (in sight, last seen, reported) reads from the status copy and the keyline strength.
            Color enemy = SkirmishObjectiveCardLayout.Enemy;
            float strength = knowledge == SkirmishEnemyBaseMarkerKnowledge.InSight ? 1f
                : knowledge == SkirmishEnemyBaseMarkerKnowledge.LastSeen ? .8f : .62f;
            Color tone = new(enemy.r, enemy.g, enemy.b, strength);
            if (!PlaceWorldChip(baseMarker, baseMarkerStem.gameObject, ref baseMarkerCue, tone, screen)) return;
            string detail = string.IsNullOrEmpty(range) ? state : state + "\n" + range;
            string signature = text.CurrentLocaleCode + "|" + (int)knowledge + "|" + detail;
            if (baseMarkerShown == signature) return;
            baseMarkerShown = signature;
            UiLocalizedText.Set(baseMarkerTitle, text.Get("ui.skirmish.marker.enemy_base", text.IsRightToLeft ? "پایگاه دشمن" : "ENEMY BASE"));
            UiLocalizedText.Set(baseMarkerDetail, detail);
            baseMarkerChip.Configure(new Color32(25, 34, 38, 247), new Color32(3, 8, 10, 252), tone, 3f);
            baseMarkerStem.color = new Color(tone.r, tone.g, tone.b, knowledge == SkirmishEnemyBaseMarkerKnowledge.InSight ? 1f : .6f);
            PlaceBaseMarkerGlyph(text.IsRightToLeft);
        }

        // The diamond sits before the title (after it in right-to-left), centred on the title line.
        private void PlaceBaseMarkerGlyph(bool rtl)
        {
            if (baseMarkerGlyph == null) return;
            baseMarkerTitle.ForceMeshUpdate();
            float title = baseMarkerTitle.GetPreferredValues(baseMarkerTitle.text, float.PositiveInfinity, float.PositiveInfinity).x;
            float limit = baseMarkerTitle.rectTransform.rect.width;
            if (limit > 0f) title = Mathf.Min(title, limit);
            const float size = 24f, gap = 10f;
            var rect = baseMarkerGlyph.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f); rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = Vector2.one * size;
            float offset = title * .5f + gap + size * .5f;
            rect.anchoredPosition = new Vector2(rtl ? offset : -offset, -34f);
            baseMarkerGlyph.Present(HudOwnershipGlyph.Diamond, SkirmishObjectiveCardLayout.Enemy);
        }
        private void BuildEnemyBaseMarker()
        {
            baseMarker = Panel("EnemyBaseObjectiveMarker", transform, false);
            const float stemHeight = 22f;
            baseMarker.sizeDelta = new Vector2(350f, 184f);
            baseMarker.pivot = new Vector2(.5f, 0);
            var stem = Panel("Stem", baseMarker, false);
            stem.anchorMin = stem.anchorMax = new Vector2(.5f, 0); stem.pivot = new Vector2(.5f, 0);
            stem.sizeDelta = new Vector2(4f, stemHeight); stem.anchoredPosition = Vector2.zero;
            baseMarkerStem = stem.gameObject.AddComponent<Image>(); baseMarkerStem.raycastTarget = false;
            var chip = Panel("Chip", baseMarker, false);
            chip.anchorMin = new Vector2(0, 0); chip.anchorMax = new Vector2(1, 1);
            chip.offsetMin = new Vector2(0f, stemHeight); chip.offsetMax = Vector2.zero;
            baseMarkerChip = chip.gameObject.AddComponent<V3GradientGraphic>(); baseMarkerChip.raycastTarget = false;
            baseMarkerTitle = Label(chip, "", 30f);
            baseMarkerTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            baseMarkerTitle.rectTransform.anchorMax = Vector2.one;
            // Side insets leave room for the enemy diamond beside the centred title.
            baseMarkerTitle.rectTransform.offsetMin = new Vector2(48f, -64f);
            baseMarkerTitle.rectTransform.offsetMax = new Vector2(-48f, -4f);
            var glyph = Panel("OwnershipGlyph", chip, false);
            baseMarkerGlyph = glyph.gameObject.AddComponent<HudOwnershipGlyphGraphic>();
            baseMarkerGlyph.Present(HudOwnershipGlyph.Diamond, SkirmishObjectiveCardLayout.Enemy);

            baseMarkerDetail = Label(chip, "", 24f);
            baseMarkerDetail.color = new Color32(222, 233, 237, 255);
            baseMarkerDetail.rectTransform.anchorMin = Vector2.zero;
            baseMarkerDetail.rectTransform.anchorMax = Vector2.one;
            baseMarkerDetail.rectTransform.offsetMin = new Vector2(14f, 5f);
            baseMarkerDetail.rectTransform.offsetMax = new Vector2(-14f, -64f);
        }
    }
}
