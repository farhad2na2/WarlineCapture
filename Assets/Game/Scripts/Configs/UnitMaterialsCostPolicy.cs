using UnityEngine;

namespace Game.Configs
{
    /// <summary>Shared data-only legacy materials-cost rule for authored and source-host paths.</summary>
    public static class UnitMaterialsCostPolicy
    {
        public static int ResolveLegacyMaterialsCost(int legacyPrice)
        {
            const int creditsPerMaterial = 500;
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0, legacyPrice) / (float)creditsPerMaterial));
        }
    }
}
