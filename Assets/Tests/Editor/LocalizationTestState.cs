using Game.Configs;
using UnityEngine;

/// <summary>Restores the shipped localization catalog so locale state cannot leak between EditMode tests.</summary>
public static class LocalizationTestState
{
    public static void UseShippedCatalog(string localeCode = GameLocalization.EnglishLocaleCode)
    {
        GameLocalization.Initialize(
            Resources.Load<GameLocalizationCatalog>(GameLocalization.CatalogResourcePath),
            localeCode,
            persist: false);
    }
}
