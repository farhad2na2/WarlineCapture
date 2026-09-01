using UnityEngine;

namespace Game.UI.Runtime
{
    [CreateAssetMenu(menuName = "Game/UI/V3 Art Catalog", fileName = "V3UiArtCatalog")]
    public sealed class V3UiArtCatalog : ScriptableObject
    {
        [Header("Core chrome")]
        [SerializeField] private Sprite panel;
        [SerializeField] private Sprite button;
        [SerializeField] private Sprite focusOverlay;

        [Header("Core actions")]
        [SerializeField] private Sprite attackIcon;
        [SerializeField] private Sprite settingsIcon;
        [SerializeField] private Sprite settingsAudioIcon;
        [SerializeField] private Sprite settingsVideoIcon;
        [SerializeField] private Sprite settingsAccessibilityIcon;
        [SerializeField] private Sprite resetIcon;

        [Header("Canonical resources")]
        [SerializeField] private Sprite creditsIcon;
        [SerializeField] private Sprite commandIcon;
        [SerializeField] private Sprite materialsIcon;
        [SerializeField] private Sprite oilIcon;
        [SerializeField] private Sprite fuelIcon;
        [SerializeField] private Sprite rushIcon;

        public Sprite Panel => panel;
        public Sprite Button => button;
        public Sprite FocusOverlay => focusOverlay;
        public Sprite AttackIcon => attackIcon;
        public Sprite SettingsIcon => settingsIcon;
        public Sprite SettingsAudioIcon => settingsAudioIcon;
        public Sprite SettingsVideoIcon => settingsVideoIcon;
        public Sprite SettingsAccessibilityIcon => settingsAccessibilityIcon;
        public Sprite ResetIcon => resetIcon;
        public Sprite CreditsIcon => creditsIcon;
        public Sprite CommandIcon => commandIcon;
        public Sprite MaterialsIcon => materialsIcon;
        public Sprite OilIcon => oilIcon;
        public Sprite FuelIcon => fuelIcon;
        public Sprite RushIcon => rushIcon;
    }
}
