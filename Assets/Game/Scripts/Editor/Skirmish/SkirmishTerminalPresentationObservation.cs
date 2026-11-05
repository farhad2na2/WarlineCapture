#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    // QA observation only. This class never changes text, layout, routes or gameplay.
    public static class SkirmishTerminalPresentationObservation
    {
        public static int NativeSessionCount(EntityManager em)
        {
            using var query = em.CreateEntityQuery(new EntityQueryDesc {
                Any = new[] { ComponentType.ReadOnly<SkirmishMatchState>(), ComponentType.ReadOnly<SkirmishExpandedSessionComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities });
            return query.CalculateEntityCount();
        }

        [Serializable]
        public sealed class Proof
        {
            public string attempt, nativeOutcome, locale, ownerPath, controlPath, titlePath;
            public string expectedTitle, actualTitle, intent, route;
            public int frame;
            public float pointX, pointY;
            public bool rendered, presented, copyMatches, pushHistory;
        }

        public static string ExpectedResultTitle(SkirmishOutcome outcome)
        {
            if (outcome != SkirmishOutcome.Victory && outcome != SkirmishOutcome.Defeat && outcome != SkirmishOutcome.Draw)
                return string.Empty;
            return GameText.Get("ui.skirmish.result." + outcome.ToString().ToLowerInvariant(), outcome.ToString());
        }

        public static bool AcceptResult(Proof proof, string attempt, SkirmishOutcome outcome) =>
            proof != null && !string.IsNullOrEmpty(attempt) && proof.attempt == attempt &&
            proof.nativeOutcome == outcome.ToString() && proof.frame > 0 && proof.rendered && proof.copyMatches &&
            proof.locale == UiShellRuntimeGateway.Localization.CurrentLocaleCode && !string.IsNullOrEmpty(proof.locale) && !string.IsNullOrEmpty(ExpectedResultTitle(outcome)) &&
            proof.expectedTitle == UiLocalizedText.CatalogLabel(ExpectedResultTitle(outcome)) &&
            CopyMatches(proof.actualTitle, proof.expectedTitle) &&
            !string.IsNullOrEmpty(proof.ownerPath) && proof.titlePath == proof.ownerPath + "/Outcome/Title" &&
            proof.ownerPath.EndsWith("/SkirmishResult/CampaignStyleResult", StringComparison.Ordinal);

        public static bool AcceptMenu(Proof proof, string attempt) =>
            proof != null && !string.IsNullOrEmpty(attempt) && proof.attempt == attempt && proof.frame > 0 &&
            proof.rendered && proof.presented && proof.copyMatches && proof.pushHistory &&
            proof.locale == UiShellRuntimeGateway.Localization.CurrentLocaleCode && !string.IsNullOrEmpty(proof.locale) &&
            proof.expectedTitle == UiLocalizedText.CatalogLabel("SKIRMISH") && CopyMatches(proof.actualTitle, proof.expectedTitle) &&
            !string.IsNullOrEmpty(proof.ownerPath) && proof.ownerPath.EndsWith("/Card_Skirmish", StringComparison.Ordinal) &&
            proof.controlPath == proof.ownerPath + "/Hotspot" && proof.titlePath == proof.ownerPath + "/Title" &&
            proof.intent == UiShellRouteIntent.OpenMenuRoute.ToString() && proof.route == UIRoute.QuickCustomSetup.ToString();

        public static Proof ReadResult(SkirmishMatchView view, string attempt, SkirmishOutcome outcome)
        {
            var owner = view == null ? null : view.transform.Find("SkirmishResult/CampaignStyleResult");
            var title = owner == null ? null : owner.GetComponent<SharedMissionResultView>()?.Controls.Outcome;
            var proof = Read(title, owner, attempt, ExpectedResultTitle(outcome));
            proof.nativeOutcome = outcome.ToString();
            return proof;
        }

        // The caller supplies the existing normal presented-button predicate; no alternate raycast/input path.
        public static Proof ReadMenu(string attempt, Func<Button, Vector2?> presentedPoint)
        {
            var candidates = new List<Proof>();
            foreach (var binding in UnityEngine.Object.FindObjectsByType<UIShellRouteButtonView>(FindObjectsInactive.Exclude))
            {
                var button = binding.GetComponent<Button>();
                var owner = button == null ? null : button.transform.parent;
                if (owner == null || owner.name != "Card_Skirmish" || button.name != "Hotspot" ||
                    owner.Find("Hotspot") != button.transform || binding.Intent != UiShellRouteIntent.OpenMenuRoute ||
                    binding.Route != UIRoute.QuickCustomSetup || !binding.PushHistory) continue;
                var title = owner.Find("Title")?.GetComponent<TMP_Text>();
                var proof = Read(title, owner, attempt, "SKIRMISH");
                proof.controlPath = PathOf(button.transform);
                proof.intent = binding.Intent.ToString(); proof.route = binding.Route.ToString(); proof.pushHistory = binding.PushHistory;
                var point = presentedPoint == null ? null : presentedPoint(button);
                proof.presented = point.HasValue;
                if (point.HasValue) { proof.pointX = point.Value.x; proof.pointY = point.Value.y; }
                candidates.Add(proof);
            }
            return SelectUniqueMenu(candidates, attempt);
        }

        public static Proof SelectUniqueMenu(IEnumerable<Proof> candidates, string attempt)
        {
            Proof result = null;
            foreach (var proof in candidates)
            {
                if (!AcceptMenu(proof, attempt)) continue;
                if (result != null) return null; // Ambiguous presented destinations never certify a return.
                result = proof;
            }
            return result;
        }

        private static Proof Read(TMP_Text title, Transform owner, string attempt, string expected)
        {
            var raw = title is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : title == null ? "" : title.text;
            var copy = UiLocalizedText.CatalogLabel(expected);
            return new Proof { attempt = attempt, locale = UiShellRuntimeGateway.Localization.CurrentLocaleCode,
                frame = Time.frameCount, ownerPath = PathOf(owner), titlePath = title == null ? "" : PathOf(title.transform),
                expectedTitle = copy, actualTitle = raw, rendered = Rendered(title), copyMatches = CopyMatches(raw, copy) };
        }

        public static bool CopyMatches(string actual, string expected) => !string.IsNullOrEmpty(expected) &&
            (string.Equals(actual, expected, StringComparison.Ordinal) ||
             string.Equals(actual, V3LocalizedTextBindingView.ShapeForRendering(expected), StringComparison.Ordinal));

        private static string PathOf(Transform transform) => transform == null ? "" :
            transform.parent == null ? transform.name : PathOf(transform.parent) + "/" + transform.name;

        private static bool Rendered(TMP_Text text)
        {
            if (text == null || !text.isActiveAndEnabled || !text.gameObject.activeInHierarchy ||
                string.IsNullOrWhiteSpace(text.text) || text.color.a <= .01f || text.canvasRenderer.cull) return false;
            foreach (var group in text.GetComponentsInParent<CanvasGroup>()) if (group.alpha <= .01f) return false;
            bool glyph = false;
            if (text.textInfo != null)
                for (int i = 0; i < text.textInfo.characterCount; i++)
                    if (text.textInfo.characterInfo[i].isVisible) { glyph = true; break; }
            if (!glyph) return false;
            var canvas = text.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
            var low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new Rect(Mathf.Min(low.x, high.x), Mathf.Min(low.y, high.y), Mathf.Abs(high.x-low.x),
                Mathf.Abs(high.y-low.y)).Overlaps(Screen.safeArea);
        }
    }
}
#endif
