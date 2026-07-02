using System;
using UnityEngine;

namespace Game.UI.Contracts
{
    public interface IMatchHudSquadTrayView
    {
        void Bind(Action<MatchHudSquadTraySlot> cardClicked);

        void ClearActiveSlot();

        bool ContainsScreenPoint(Vector2 screenPosition);

        void FlashDisabled(MatchHudSquadTraySlot slot);

        void SetSelectedSlot(MatchHudSquadTraySlot selectedSlot);

        bool TryGetPortraitSprite(MatchHudSquadTraySlot slot, out Sprite sprite);

        void Unbind();
    }
}
