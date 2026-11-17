using Game.Configs;
using Unity.Mathematics;

namespace Game.Rendering
{
    internal static class CombatCameraShakeHelper
    {
        internal const float LargeExplosionTrauma = 0.55f;
        internal const float SmallExplosionTrauma = 0.22f;
        internal const float FinalKillTrauma = 0.8f;
        internal const float FirstContactTrauma = 0.3f;
        internal const float MainBaseHitMinimumTrauma = 0.12f;
        internal const float MainBaseHitMaximumTrauma = 0.4f;
        internal const float MainBaseHitCooldownSeconds = 1.5f;
        internal const float DecayPerSecond = 1.6f;
        internal const float MinimumFalloffRadius = 60f;
        internal const float FalloffRadiusPerCameraHeight = 3.5f;
        internal const float OffsetPerCameraHeight = 0.015f;
        internal const float MinimumMaxOffset = 0.2f;
        internal const float MaximumMaxOffset = 0.6f;

        internal static float ResolveEventTrauma(uint eventHash) => eventHash switch
        {
            AudioEventIds.GameplayExplosionLargeHash => LargeExplosionTrauma,
            AudioEventIds.GameplayExplosionSmallHash => SmallExplosionTrauma,
            _ => 0f
        };

        internal static float Attenuate(float trauma, float distance, float cameraHeight)
        {
            float radius = math.max(MinimumFalloffRadius, cameraHeight * FalloffRadiusPerCameraHeight);
            float falloff = math.saturate(1f - distance / radius);
            return trauma * falloff * falloff;
        }

        internal static float Decay(float trauma, float unscaledDeltaTime) =>
            math.max(0f, trauma - math.max(0f, unscaledDeltaTime) * DecayPerSecond);

        internal static float2 EvaluateOffset(float trauma, float time, float cameraHeight)
        {
            float shake = math.saturate(trauma);
            shake *= shake;
            float maxOffset = math.clamp(cameraHeight * OffsetPerCameraHeight, MinimumMaxOffset, MaximumMaxOffset);
            float x = (math.sin(time * 37.1f) + 0.5f * math.sin(time * 61.7f + 1.3f)) / 1.5f;
            float y = (math.sin(time * 43.3f + 2.1f) + 0.5f * math.sin(time * 71.9f + 0.7f)) / 1.5f;
            return new float2(x, y) * (shake * maxOffset);
        }

        /// <summary>True when exactly one side is the player (faction 1) and the other is hostile (faction 2+).</summary>
        internal static bool IsPlayerEnemyExchange(byte factionA, byte factionB) =>
            (factionA == 1 && factionB >= 2) || (factionB == 1 && factionA >= 2);

        /// <summary>
        /// Trauma for one observed hit. First contact is a fixed kick; a main-base hit scales with the share of
        /// base health removed. Returns 0 for every other hit so ordinary fire stays quiet.
        /// </summary>
        internal static float ResolveContactTrauma(bool firstContact, bool mainBaseHit, int damage, int maxHealth)
        {
            float trauma = firstContact ? FirstContactTrauma : 0f;
            if (mainBaseHit && damage > 0)
            {
                float share = maxHealth > 0 ? math.saturate((float)damage / maxHealth) : 0f;
                float baseTrauma = math.clamp(MainBaseHitMinimumTrauma + share * 6f,
                    MainBaseHitMinimumTrauma, MainBaseHitMaximumTrauma);
                trauma = math.max(trauma, baseTrauma);
            }

            return trauma;
        }
    }
}
