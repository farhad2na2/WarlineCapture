using Unity.Entities;
using Unity.Rendering;

// Per-instance material property for the health bar shader.
// Expected shader property reference: "_Fill" in range [0..1].

namespace Game.Components
{
    [MaterialProperty("_Fill")]
    public struct HealthBarFill : IComponentData
    {
        public float Value;
    }

    public struct RecentDamageHealthBarVisibility : IComponentData
    {
        public float TimeRemaining;
    }
}
