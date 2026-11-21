using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Per-mission guidance from SkirmishMissionHudGuidanceCatalog (owner designer playtest
    /// 2026-11-17, S026): a first-order prompt during the opening seconds and a rendezvous chip on an
    /// existing layout anchor. Since W40 (Board 2) the first order is the objective card's next-action
    /// line instead of a second stacked centre bar. Presentation only; no orders.
    /// </summary>
    public sealed partial class SkirmishMatchView
    {
        private RectTransform rendezvousMarker;
        private GameObject rendezvousStem;
        private HudDirectionArrowGraphic rendezvousCue;
        private TMPro.TMP_Text rendezvousTitle;
        private string rendezvousShown;
        private static readonly Color RendezvousCyan = new(0f, .73f, .85f, 1f);

        public bool FirstOrderPresented => ObjectiveCardShowsFirstOrder && hud != null && hud.activeSelf;
        public bool RendezvousMarkerPresented => rendezvousMarker != null && rendezvousMarker.gameObject.activeSelf;

        private void RefreshMissionGuidance(in UiSkirmishModel model)
        {
            bool live = hud != null && hud.activeSelf && !model.Finished && !model.StartupFailed;
            PresentRendezvous(model, live);
        }

        /// <summary>Extra downward offset for rows stacked below the card (none since W40).</summary>
        private float GuidanceStackOffset => 0f;

        private void PresentRendezvous(in UiSkirmishModel model, bool live)
        {
            var camera = Camera.main;
            Vector3 screen = live && model.RendezvousKnown && camera != null ? camera.WorldToScreenPoint(model.RendezvousWorld) : Vector3.back;
            // In front of the camera is enough: an anchor off screen or under the HUD is clamped into
            // the play area with an arrow toward it. Behind the camera there is no direction to show.
            bool shown = screen.z > 0 && float.IsFinite(screen.x) && float.IsFinite(screen.y);
            if (!shown) { if (rendezvousMarker != null) rendezvousMarker.gameObject.SetActive(false); HideCue(rendezvousCue); return; }
            if (rendezvousMarker == null)
            {
                rendezvousMarker = Panel("RendezvousMarker", transform, false);
                rendezvousMarker.sizeDelta = new Vector2(250, 76);
                rendezvousMarker.pivot = new Vector2(.5f, 0);
                var stem = Panel("Stem", rendezvousMarker, false);
                stem.anchorMin = stem.anchorMax = new Vector2(.5f, 0); stem.pivot = new Vector2(.5f, 0);
                stem.sizeDelta = new Vector2(4, 26);
                rendezvousStem = stem.gameObject;
                var stemImage = stem.gameObject.AddComponent<Image>(); stemImage.color = RendezvousCyan; stemImage.raycastTarget = false;
                var chip = Panel("Chip", rendezvousMarker, false);
                chip.anchorMin = Vector2.zero; chip.anchorMax = Vector2.one;
                chip.offsetMin = new Vector2(0, 26); chip.offsetMax = Vector2.zero;
                var graphic = chip.gameObject.AddComponent<V3GradientGraphic>();
                graphic.Configure(new Color(.10f, .14f, .15f, .92f), new Color(.025f, .07f, .08f, .92f), RendezvousCyan, 2);
                graphic.raycastTarget = false;
                rendezvousTitle = Label(chip, "", 20);
                Stretch(rendezvousTitle.rectTransform);
                rendezvousTitle.color = RendezvousCyan;
            }
            // Never over the HUD: an anchor under the top bar or a side panel is clamped into
            // the open play area with an arrow toward it (SkirmishHudChipPlacement).
            if (!PlaceWorldChip(rendezvousMarker, rendezvousStem, ref rendezvousCue, RendezvousCyan, screen)) return;
            string signature = model.RendezvousLabel + UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            if (rendezvousShown != signature) { rendezvousShown = signature; UiLocalizedText.Set(rendezvousTitle, model.RendezvousLabel); }
        }
    }
}
