using UnityEngine;
using Game.Tactical.Contracts;

namespace Game.UI.Contracts
{
    public readonly struct MatchHudCurrentOrderBannerModel
    {
        public MatchHudCurrentOrderBannerModel(
            bool visible,
            TacticalCommandMode commandMode,
            string orderText,
            string descriptionText,
            Sprite iconSprite,
            bool chevronsVisible = true)
        {
            Visible = visible;
            CommandMode = commandMode;
            OrderText = visible ? orderText ?? string.Empty : string.Empty;
            DescriptionText = visible ? descriptionText ?? string.Empty : string.Empty;
            IconSprite = visible ? iconSprite : null;
            ChevronsVisible = visible && chevronsVisible;
        }

        public bool Visible { get; }
        public TacticalCommandMode CommandMode { get; }
        public string OrderText { get; }
        public string DescriptionText { get; }
        public Sprite IconSprite { get; }
        public bool ChevronsVisible { get; }

        public static MatchHudCurrentOrderBannerModel Hidden =>
            new(false, TacticalCommandMode.None, string.Empty, string.Empty, null, false);
    }
}
