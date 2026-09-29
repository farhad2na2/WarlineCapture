using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Configs;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Selects the saved onboarding portrait and presents the player-entered identity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuCommanderVariantView : MonoBehaviour
    {
        [Serializable]
        public sealed class CommanderVariant
        {
            [SerializeField] private string commanderId;
            [SerializeField] private Sprite sprite;

            public string CommanderId => commanderId;
            public Sprite Sprite => sprite;

            public CommanderVariant(string id, Sprite bakedSceneSprite)
            {
                commanderId = id;
                sprite = bakedSceneSprite;
            }
        }

        [SerializeField] private Image target;
        [SerializeField] private TMP_Text identityName;
        [SerializeField] private TMP_FontAsset identityFont, identityPersianFont;
        [SerializeField] private CommanderVariant[] variants = Array.Empty<CommanderVariant>();
        [SerializeField] private string defaultCommanderId = "field_commander_01";

        public Image Target => target;
        public CommanderVariant[] Variants => variants;
        public string DefaultCommanderId => defaultCommanderId;

        public void Configure(Image targetImage, CommanderVariant[] commanderVariants, string defaultId)
        {
            target = targetImage;
            variants = commanderVariants ?? Array.Empty<CommanderVariant>();
            defaultCommanderId = defaultId;
            ApplyCommander(defaultCommanderId);
        }

        public bool ApplyCommander(string commanderId)
        {
            if (target == null || variants == null)
                return false;

            for (int i = 0; i < variants.Length; i++)
            {
                CommanderVariant variant = variants[i];
                if (variant == null || variant.Sprite == null ||
                    !string.Equals(variant.CommanderId, commanderId, StringComparison.Ordinal))
                    continue;

                target.sprite = variant.Sprite;
                target.enabled = true;
                return true;
            }

            target.enabled = false;
            return false;
        }

        public void ConfigureIdentity(TMP_Text label)
        {
            identityName=label; identityFont=label.font;
            identityPersianFont=Resources.Load<GameLocalizationCatalog>("Localization/V3UiLocalizationCatalog")?.FindLocale("fa-IR")?.FontAsset as TMP_FontAsset;
        }
        private void OnEnable() => RefreshIdentity();
        private void LateUpdate() => RefreshIdentity();
        public void RefreshIdentity()
        {
            if (!UiShellRuntimeGateway.TryReadCommanderProfile(out var profile)) return;
            ApplyCommander(profile.PortraitClass);
            if (identityName != null)
            {
                // Shape the player's raw name independently of the UI language.
                var binding=identityName.GetComponent<V3LocalizedTextBindingView>();
                if(binding!=null) binding.enabled=false;
                bool arabic=false;
                foreach(char letter in profile.Name) arabic |= letter is >= '\u0600' and <= '\u06ff';
                var font=arabic?identityPersianFont:identityFont;
                if(font!=null && identityName.font!=font) { identityName.font=font; identityName.fontSharedMaterial=font.material; }
                identityName.isRightToLeftText=arabic;
                identityName.alignment=arabic?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
                string rendered=arabic?V3LocalizedTextBindingView.ShapeForRendering(profile.Name):profile.Name;
                if(identityName.text!=rendered) identityName.text=rendered;
            }
        }
    }
}
