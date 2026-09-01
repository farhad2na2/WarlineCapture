using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class StoreCommandExchangeV3View : UIScreenView
    {
        [SerializeField] private TMP_Text creditsValue;
        [SerializeField] private TMP_Text commandValue;
        [SerializeField] private TMP_Text offersHeading;
        [SerializeField] private Button[] categoryButtons;
        [SerializeField] private V3GradientGraphic[] categoryGradients;
        [SerializeField] private Button[] offerButtons;
        [SerializeField] private V3GradientGraphic[] offerGradients;
        [SerializeField] private TMP_Text[] offerTitles;
        [SerializeField] private TMP_Text[] offerSubtitles;
        [SerializeField] private TMP_Text[] offerPrices;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailTimer;
        [SerializeField] private RawImage detailArt;
        [SerializeField] private Texture[] detailArtTextures;
        [SerializeField] private TMP_Text[] detailLines;
        [SerializeField] private TMP_Text detailNote;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text purchaseLabel;

        private UnityAction[] _categoryActions = Array.Empty<UnityAction>();
        private UnityAction[] _offerActions = Array.Empty<UnityAction>();
        private int _categoryIndex;
        private int _offerIndex;

        private static readonly string[] CategoryNames =
        {
            "FEATURED", "STARTER PACKS", "RESOURCES", "ARMORY", "COSMETICS", "OPERATIONS"
        };

        private static readonly OfferData[][] Catalog =
        {
            new[]
            {
                new OfferData("RECON STARTER PACK", "2,500 CREDITS + 120 COMMAND", "$4.99", "72H REMAINING", "2,500 CREDITS", "120 COMMAND", "RANGER SQUAD UNLOCK", "BLUE VANGUARD FRAME", "Duplicates grant 40 Ranger Parts."),
                new OfferData("COMMAND READY BUNDLE", "7,500 CREDITS + 200 COMMAND", "$9.99", "WEEKLY OFFER", "7,500 CREDITS", "200 COMMAND", "2 RUSH TICKETS", "FIXED CONTENTS", "No random rewards."),
                new OfferData("BLUE VANGUARD FRAME", "COMMANDER COSMETIC", "250", "ACCOUNT-WIDE", "BLUE VANGUARD FRAME", "COMMANDER PROFILE", "COSMETIC ONLY", "NO COMBAT STATS", "Owned cosmetics cannot be purchased twice."),
                new OfferData("AIRLIFT SUPPORT BUNDLE", "300 COMMAND + SUPPORT PARTS", "$14.99", "WEEKLY OFFER", "300 COMMAND", "HELICOPTER SKIN", "TRANSPORT PARTS x30", "3 RUSH TICKETS", "Fixed listed contents; no random draw.")
            },
            new[]
            {
                new OfferData("RECON STARTER PACK", "LEVELS 1 - 8", "$4.99", "72H REMAINING", "2,500 CREDITS", "120 COMMAND", "RANGER SQUAD UNLOCK", "BLUE VANGUARD FRAME", "Duplicates grant 40 Ranger Parts."),
                new OfferData("BASE BUILDER PACK", "LEVELS 3 - 15", "$9.99", "STARTER OFFER", "8,000 CREDITS", "4 RUSH TICKETS", "GUARD TOWER PARTS x40", "2 AID CONVOYS", "Construction Queue skin included."),
                new OfferData("OPERATION FOUNDER PACK", "OPERATIONS UNLOCK", "$19.99", "STARTER OFFER", "12,000 CREDITS", "350 COMMAND", "3 REPAIR CONVOYS", "FOUNDER BADGE", "Includes fixed district marker set."),
                new OfferData("STARTER CATALOG", "FIXED CONTENT ONLY", "—", "DESIGNED CATALOG", "NO RANDOM LOOT", "NO MATCH RESOURCES", "ACCOUNT ITEMS ONLY", "EARN PATHS PRESERVED", "Select a starter pack for exact contents.")
            },
            new[]
            {
                new OfferData("COMMAND S", "120 COMMAND", "$1.99", "RESOURCE BUNDLE", "120 COMMAND", "ACCOUNT RESOURCE", "NO MATCH MATERIALS", "NO OIL OR FUEL", "Command is never injected into active combat."),
                new OfferData("COMMAND M", "330 COMMAND", "$4.99", "RESOURCE BUNDLE", "330 COMMAND", "ACCOUNT RESOURCE", "NO MATCH MATERIALS", "NO OIL OR FUEL", "Command is never injected into active combat."),
                new OfferData("COMMAND L", "750 COMMAND", "$9.99", "RESOURCE BUNDLE", "750 COMMAND", "ACCOUNT RESOURCE", "NO MATCH MATERIALS", "NO OIL OR FUEL", "Command is never injected into active combat."),
                new OfferData("CREDIT CACHE", "5,000 CREDITS", "$2.99", "RESOURCE BUNDLE", "5,000 CREDITS", "ACCOUNT RESOURCE", "NO MATCH MATERIALS", "NO OIL OR FUEL", "Credits cannot fund an active match directly.")
            },
            new[]
            {
                new OfferData("RANGER PARTS CASE", "RANGER PARTS x40", "180", "ARMORY ITEM", "RANGER PARTS x40", "TARGET: RANGER SQUAD", "FIXED CONTENT", "EARN PATH AVAILABLE", "Parts support unlock progression."),
                new OfferData("APC UPGRADE CASE", "APC ARMOR PARTS x35", "320", "ARMORY ITEM", "APC ARMOR PARTS x35", "1 RUSH TICKET", "FIXED CONTENT", "EARN PATH AVAILABLE", "Opens upgrade detail for exact requirements."),
                new OfferData("SUPPORT DRONE KIT", "DRONE SCAN PARTS x30", "280", "ARMORY ITEM", "DRONE SCAN PARTS x30", "1 INTEL DOSSIER", "FIXED CONTENT", "EARN PATH AVAILABLE", "Opens ability detail for exact requirements."),
                new OfferData("QUEUE RUSH TICKETS", "5 RUSH TICKETS", "150", "ARMORY ITEM", "5 RUSH TICKETS", "PRODUCTION TIMERS", "OPERATION TIMERS", "NO COMBAT COOLDOWNS", "Rush never changes active combat cooldowns.")
            },
            new[]
            {
                new OfferData("BLUE VANGUARD FRAME", "COMMANDER COSMETIC", "250", "ACCOUNT-WIDE", "BLUE VANGUARD FRAME", "COMMANDER PROFILE", "COSMETIC ONLY", "NO COMBAT STATS", "Owned cosmetics cannot be purchased twice."),
                new OfferData("NIGHT OPS SQUAD CARDS", "UNIT CARD SKIN SET", "300", "ACCOUNT-WIDE", "NIGHT OPS CARD SET", "LOADOUT + BATTLE HUD", "COSMETIC ONLY", "NO COMBAT STATS", "Applies only to presentation."),
                new OfferData("IRON GUARD BANNER", "BASE BANNER + BADGE", "220", "ACCOUNT-WIDE", "IRON GUARD BANNER", "PROFILE BADGE", "COSMETIC ONLY", "NO COMBAT STATS", "Applies only to presentation."),
                new OfferData("AMBER COMMAND HUD", "HUD ACCENT THEME", "400", "ACCOUNT-WIDE", "AMBER HUD ACCENT", "APP SHELL THEME", "COSMETIC ONLY", "THEME SERVICE REQUIRED", "Activation remains locked until theme service exists.")
            },
            new[]
            {
                new OfferData("INTEL DOSSIER", "1 OPERATION SUPPLY", "120", "OPERATION SUPPLY", "1 INTEL DOSSIER", "CONSUMED BY ACTION", "NO DIRECT METRIC GRANT", "FIXED CONTENT", "Value applies only through an authored action."),
                new OfferData("AID CONVOY", "2 OPERATION SUPPLIES", "180", "OPERATION SUPPLY", "2 AID CONVOYS", "CONSUMED BY ACTION", "NO DIRECT TRUST GRANT", "FIXED CONTENT", "Value applies only through an authored action."),
                new OfferData("REPAIR CONVOY", "1 OPERATION SUPPLY", "220", "OPERATION SUPPLY", "1 REPAIR CONVOY", "CONSUMED BY ACTION", "NO DIRECT METRIC GRANT", "FIXED CONTENT", "Value applies only through an authored action."),
                new OfferData("READINESS BOOST", "1 ACTION AUTHORITY", "160", "OPERATION SUPPLY", "1 READINESS ACTION", "NEXT OPERATION DAY", "NO ACTIVE-MATCH BOOST", "FIXED CONTENT", "Value applies only through an authored action.")
            }
        };

        public Button PurchaseButton => purchaseButton;
        public Button[] CategoryButtons => categoryButtons;
        public Button[] OfferButtons => offerButtons;

        public void Configure(
            TMP_Text configuredCredits,
            TMP_Text configuredCommand,
            TMP_Text configuredHeading,
            Button[] configuredCategoryButtons,
            V3GradientGraphic[] configuredCategoryGradients,
            Button[] configuredOfferButtons,
            V3GradientGraphic[] configuredOfferGradients,
            TMP_Text[] configuredOfferTitles,
            TMP_Text[] configuredOfferSubtitles,
            TMP_Text[] configuredOfferPrices,
            TMP_Text configuredDetailTitle,
            TMP_Text configuredDetailTimer,
            RawImage configuredDetailArt,
            Texture[] configuredDetailArtTextures,
            TMP_Text[] configuredDetailLines,
            TMP_Text configuredDetailNote,
            Button configuredPurchaseButton,
            TMP_Text configuredPurchaseLabel)
        {
            creditsValue = configuredCredits;
            commandValue = configuredCommand;
            offersHeading = configuredHeading;
            categoryButtons = configuredCategoryButtons;
            categoryGradients = configuredCategoryGradients;
            offerButtons = configuredOfferButtons;
            offerGradients = configuredOfferGradients;
            offerTitles = configuredOfferTitles;
            offerSubtitles = configuredOfferSubtitles;
            offerPrices = configuredOfferPrices;
            detailTitle = configuredDetailTitle;
            detailTimer = configuredDetailTimer;
            detailArt = configuredDetailArt;
            detailArtTextures = configuredDetailArtTextures;
            detailLines = configuredDetailLines;
            detailNote = configuredDetailNote;
            purchaseButton = configuredPurchaseButton;
            purchaseLabel = configuredPurchaseLabel;
        }

        private void Awake()
        {
            SetRouteForTests(UIRoute.CommandExchange);
            WireButtons();
            SelectCategory(0);
        }

        private void OnEnable()
        {
            RefreshResources();
            RefreshPresentation();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < Mathf.Min(categoryButtons?.Length ?? 0, _categoryActions.Length); i++)
                categoryButtons[i]?.onClick.RemoveListener(_categoryActions[i]);
            for (int i = 0; i < Mathf.Min(offerButtons?.Length ?? 0, _offerActions.Length); i++)
                offerButtons[i]?.onClick.RemoveListener(_offerActions[i]);
        }

        private void WireButtons()
        {
            int categoryCount = categoryButtons?.Length ?? 0;
            _categoryActions = new UnityAction[categoryCount];
            for (int i = 0; i < categoryCount; i++)
            {
                int index = i;
                _categoryActions[i] = () => SelectCategory(index);
                categoryButtons[i]?.onClick.AddListener(_categoryActions[i]);
            }
            int offerCount = offerButtons?.Length ?? 0;
            _offerActions = new UnityAction[offerCount];
            for (int i = 0; i < offerCount; i++)
            {
                int index = i;
                _offerActions[i] = () => SelectOffer(index);
                offerButtons[i]?.onClick.AddListener(_offerActions[i]);
            }
            if (purchaseButton != null)
                purchaseButton.interactable = false;
        }

        private void SelectCategory(int index)
        {
            _categoryIndex = Mathf.Clamp(index, 0, Catalog.Length - 1);
            _offerIndex = 0;
            RefreshPresentation();
        }

        private void SelectOffer(int index)
        {
            _offerIndex = Mathf.Clamp(index, 0, Catalog[_categoryIndex].Length - 1);
            RefreshPresentation();
        }

        private void RefreshResources()
        {
            if (!UiShellRuntimeGateway.TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources))
                return;
            if (creditsValue != null && !string.IsNullOrWhiteSpace(resources.CreditsText))
                creditsValue.text = resources.CreditsText;
            if (commandValue != null && !string.IsNullOrWhiteSpace(resources.CommandText))
                commandValue.text = resources.CommandText;
        }

        private void RefreshPresentation()
        {
            if (offersHeading != null)
                offersHeading.text = CategoryNames[_categoryIndex] + " OFFERS";
            for (int i = 0; i < (categoryGradients?.Length ?? 0); i++)
            {
                bool selected = i == _categoryIndex;
                categoryGradients[i]?.ConfigureCorners(
                    selected ? new Color32(18, 129, 210, 255) : new Color32(31, 42, 46, 255),
                    selected ? new Color32(5, 83, 149, 255) : new Color32(20, 30, 33, 255),
                    selected ? new Color32(1, 49, 90, 255) : new Color32(5, 12, 15, 255),
                    selected ? new Color32(3, 64, 112, 255) : new Color32(8, 17, 19, 255),
                    selected ? new Color32(0, 183, 239, 255) : new Color32(70, 82, 86, 255),
                    3f);
            }

            OfferData[] offers = Catalog[_categoryIndex];
            int visibleCount = Mathf.Min(offers.Length, offerButtons?.Length ?? 0);
            for (int i = 0; i < visibleCount; i++)
            {
                OfferData offer = offers[i];
                if (offerTitles != null && i < offerTitles.Length && offerTitles[i] != null)
                    offerTitles[i].text = offer.Name;
                if (offerSubtitles != null && i < offerSubtitles.Length && offerSubtitles[i] != null)
                    offerSubtitles[i].text = offer.Summary;
                if (offerPrices != null && i < offerPrices.Length && offerPrices[i] != null)
                    offerPrices[i].text = offer.Price;
                bool selected = i == _offerIndex;
                if (offerGradients != null && i < offerGradients.Length && offerGradients[i] != null)
                {
                    offerGradients[i].SetBorder(selected ? new Color32(75, 211, 255, 255) : ResolveOfferAccent(i), 3f);
                }
            }

            OfferData selectedOffer = offers[_offerIndex];
            if (detailTitle != null) detailTitle.text = selectedOffer.Name;
            if (detailTimer != null) detailTimer.text = selectedOffer.Timer;
            if (detailArt != null && detailArtTextures != null && _offerIndex < detailArtTextures.Length)
            {
                Texture texture = detailArtTextures[_offerIndex];
                detailArt.texture = texture;
                AspectRatioFitter fitter = detailArt.GetComponent<AspectRatioFitter>();
                if (fitter != null && texture != null && texture.height > 0)
                    fitter.aspectRatio = texture.width / (float)texture.height;
            }
            string[] lines = { selectedOffer.Line1, selectedOffer.Line2, selectedOffer.Line3, selectedOffer.Line4 };
            for (int i = 0; i < Mathf.Min(lines.Length, detailLines?.Length ?? 0); i++)
                if (detailLines[i] != null) detailLines[i].text = lines[i];
            if (detailNote != null) detailNote.text = selectedOffer.Note;
            if (purchaseLabel != null) purchaseLabel.text = "PURCHASE " + selectedOffer.Price;
            if (purchaseButton != null) purchaseButton.interactable = false;
        }

        private static Color ResolveOfferAccent(int index) => index switch
        {
            0 => new Color32(0, 185, 232, 255),
            1 => new Color32(246, 171, 0, 255),
            2 => new Color32(0, 163, 225, 255),
            _ => new Color32(145, 84, 193, 255)
        };

        private readonly struct OfferData
        {
            public readonly string Name;
            public readonly string Summary;
            public readonly string Price;
            public readonly string Timer;
            public readonly string Line1;
            public readonly string Line2;
            public readonly string Line3;
            public readonly string Line4;
            public readonly string Note;

            public OfferData(string name, string summary, string price, string timer, string line1, string line2, string line3, string line4, string note)
            {
                Name = name;
                Summary = summary;
                Price = price;
                Timer = timer;
                Line1 = line1;
                Line2 = line2;
                Line3 = line3;
                Line4 = line4;
                Note = note;
            }
        }
    }
}
