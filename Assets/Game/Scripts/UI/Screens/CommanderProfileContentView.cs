using TMPro;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CommanderProfileContentView : MonoBehaviour
    {
        [SerializeField] private TMP_Text commanderNameLabel;
        [SerializeField] private TMP_Text commanderSubtitleLabel;

        public TMP_Text CommanderNameLabel => commanderNameLabel;
        public TMP_Text CommanderSubtitleLabel => commanderSubtitleLabel;

        public void Configure(TMP_Text nameLabel, TMP_Text subtitleLabel)
        {
            commanderNameLabel = nameLabel;
            commanderSubtitleLabel = subtitleLabel;
        }

        public void Bind(UiShellCommanderProfileModel profile)
        {
            if (commanderNameLabel != null && !string.IsNullOrWhiteSpace(profile.Name))
                commanderNameLabel.text = profile.Name.Trim().ToUpperInvariant();

            if (commanderSubtitleLabel != null && !string.IsNullOrWhiteSpace(profile.Subtitle))
                commanderSubtitleLabel.text = profile.Subtitle.Trim().ToUpperInvariant();
        }
    }
}
