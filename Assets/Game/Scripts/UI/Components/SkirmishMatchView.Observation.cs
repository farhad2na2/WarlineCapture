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
        private string baseMarkerLocale;
        public void PresentEnemyBaseMarker(Vector3 screen, bool alive, Vector3 worldPosition, Vector3 playerPosition, bool targetVisible = true)
        {
            if (baseMarker == null)
            {
                baseMarker = Panel("EnemyBaseObjectiveMarker", transform, false);
                var label = Label(baseMarker, "", 22);
                label.color = new Color(1, .65f, .1f);
                Stretch(label.rectTransform);
                baseMarker.sizeDelta = new Vector2(190, 36);
            }
            bool shown = alive && hud != null && hud.activeInHierarchy && screen.z > 0 &&
                screen.x > 0 && screen.y > 0 && screen.x < Screen.width && screen.y < Screen.height;
            baseMarker.gameObject.SetActive(shown);
            EnemyBaseMarkerVisible = shown;
            EnemyBaseTargetVisible = shown && targetVisible;
            EnemyBaseObjectiveAlive = alive;
            EnemyBaseObjectivePosition = worldPosition;
            PlayerBaseObjectivePosition = playerPosition;
            VisibleEnemyBasePoint = screen;
            if (!shown) return;
            baseMarker.position = screen;
            string locale = UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            if (baseMarkerLocale != locale)
            {
                baseMarkerLocale = locale;
                UiLocalizedText.Set(baseMarker.GetComponentInChildren<TMPro.TMP_Text>(), UiShellRuntimeGateway.Localization.IsRightToLeft ? "پایگاه دشمن" : "ENEMY BASE");
            }
        }
    }
}
