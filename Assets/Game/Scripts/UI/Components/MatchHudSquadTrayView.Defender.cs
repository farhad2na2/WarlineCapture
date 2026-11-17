using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSquadTrayView
    {
        // Base-under-fire defender cue. Presentation only: it never selects a squad,
        // never issues an order and never moves the camera.
        private uint _defenderGroupId;
        private RectTransform _defenderHighlight;
        private static readonly Color DefenderHighlightColor = new(1f, .65f, .1f, 1f);

        /// <summary>Outline the given own squad's card (0 clears). Does not scroll.</summary>
        public void SetDefenderHighlight(uint groupId)
        {
            _defenderGroupId = groupId;
            RefreshDefenderHighlight();
        }

        /// <summary>
        /// Player-requested: page the tray so the squad's card is visible. Uses the same
        /// page change as the tray's own scroll edges. Returns false when the card is unknown.
        /// </summary>
        public bool TryRevealGroup(uint groupId)
        {
            if (!_rosterActive || groupId == 0) return false;
            for (int i = 0; i < _rosterModels.Count; i++)
            {
                if (_rosterModels[i].GroupId != groupId) continue;
                int page = i / 5;
                if (page != _logicalPage) ScrollPage(page - _logicalPage);
                RefreshDefenderHighlight();
                return true;
            }
            return false;
        }

        private void RefreshDefenderHighlight()
        {
            Button card = null;
            if (_rosterActive && _defenderGroupId != 0)
                for (int i = 0; i < _rosterModels.Count && i < _rosterCards.Count; i++)
                    if (_rosterModels[i].GroupId == _defenderGroupId) { card = _rosterCards[i].Button; break; }
            if (card == null)
            {
                if (_defenderHighlight != null) _defenderHighlight.gameObject.SetActive(false);
                return;
            }
            if (_defenderHighlight == null)
            {
                var go = new GameObject("DefenderHighlight", typeof(RectTransform));
                _defenderHighlight = (RectTransform)go.transform;
                var outline = go.AddComponent<V3GradientGraphic>();
                outline.Configure(Color.clear, Color.clear, DefenderHighlightColor, 5f);
                outline.raycastTarget = false;
            }
            if (_defenderHighlight.parent != card.transform)
            {
                _defenderHighlight.SetParent(card.transform, false);
                _defenderHighlight.anchorMin = Vector2.zero;
                _defenderHighlight.anchorMax = Vector2.one;
                _defenderHighlight.offsetMin = new Vector2(-3f, -3f);
                _defenderHighlight.offsetMax = new Vector2(3f, 3f);
                _defenderHighlight.SetAsLastSibling();
            }
            // A slow pulse reads as "this one" without competing with the selection check.
            var graphic = _defenderHighlight.GetComponent<V3GradientGraphic>();
            float alpha = .55f + .45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
            graphic.color = new Color(1f, 1f, 1f, alpha);
            _defenderHighlight.gameObject.SetActive(true);
        }
    }
}
