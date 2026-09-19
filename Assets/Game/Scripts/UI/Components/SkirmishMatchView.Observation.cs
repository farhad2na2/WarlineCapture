using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed partial class SkirmishMatchView
    {
        public Button EnemyFocusButton => enemy != null ? enemy.GetComponentInParent<Button>() : null;
        // Supplied by the shared objective presentation, not by the ARIA planner.
        public Vector2 VisibleEnemyBasePoint { get; private set; }
        public bool EnemyBaseMarkerVisible { get; private set; }
        private RectTransform baseMarker;
        private string baseMarkerLocale;
        public void PresentEnemyBaseMarker(Vector3 screen, bool alive)
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
