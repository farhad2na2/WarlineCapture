using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Selects one cohesive baked Main Menu scene for a stable commander ID.
    /// The full-scene variant keeps character lighting, hand contact, occlusion,
    /// tactical table, and environment authored as one composition.
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

        private void OnEnable()
        {
            ApplyCommander(defaultCommanderId);
        }
    }
}
