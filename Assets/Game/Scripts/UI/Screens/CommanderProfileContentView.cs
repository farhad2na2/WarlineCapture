using TMPro;
using UnityEngine;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CommanderProfileContentView : MonoBehaviour
    {
        [SerializeField] private TMP_Text commanderNameLabel, commanderSubtitleLabel;
        [SerializeField] private TMP_Text levelLabel, xpLabel, recordLabel, progressLabel;
        [SerializeField] private TMP_Text[] statLabels;
        public TMP_Text CommanderNameLabel => commanderNameLabel;
        public TMP_Text CommanderSubtitleLabel => commanderSubtitleLabel;
        public void Configure(TMP_Text name, TMP_Text subtitle) { commanderNameLabel = name; commanderSubtitleLabel = subtitle; }
        public void ConfigureFacts(TMP_Text level, TMP_Text xp, TMP_Text[] stats, TMP_Text record, TMP_Text progress)
        { levelLabel = level; xpLabel = xp; statLabels = stats; recordLabel = record; progressLabel = progress; }
        private void LateUpdate() { if (UiShellRuntimeGateway.TryReadCommanderProfile(out var profile)) Bind(profile); }
        public void Bind(UiShellCommanderProfileModel profile)
        {
            bool fa = GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode;
            // The portrait view owns the player name and its Arabic shaping.
            if (commanderSubtitleLabel != null) UiLocalizedText.Set(commanderSubtitleLabel, fa ? "فرمانده منتخب" : "SELECTED COMMANDER");
            Set(levelLabel, profile.Level.ToString()); Set(xpLabel, profile.Xp.ToString("N0"));
            int total = profile.Victories + profile.Defeats;
            string[] values = { profile.Victories.ToString(), profile.Missions.ToString(), profile.Enemies.ToString("N0"),
                profile.UnitsLost.ToString("N0"), total == 0 ? (fa ? "بدون نبرد" : "NO BATTLES") : Mathf.RoundToInt(100f * profile.Victories / total) + "%" };
            if (statLabels != null) for (int i = 0; i < Mathf.Min(values.Length, statLabels.Length); i++) Set(statLabels[i], values[i]);
            Set(recordLabel, fa ? $"ماموریت‌های تکمیل‌شده: {profile.Missions}\nپیروزی: {profile.Victories}\nشکست: {profile.Defeats}" :
                $"MISSIONS COMPLETED  {profile.Missions}\nVICTORIES  {profile.Victories}\nDEFEATS  {profile.Defeats}");
            Set(progressLabel, fa ? $"ستاره‌های کسب‌شده: {profile.Stars}\nامتیاز تجربه: {profile.Xp:N0}" : $"STARS EARNED  {profile.Stars}\nCOMMANDER XP  {profile.Xp:N0}");
        }
        private static void Set(TMP_Text label, string value) { if (label != null) UiLocalizedText.Set(label, value); }
    }
}
