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

    /// <summary>Per-hit health-bar window. Enableable (2026-11-17 allocator fix): disabled at expiry, never removed.</summary>
    public struct RecentDamageHealthBarVisibility : IComponentData, IEnableableComponent
    {
        public float TimeRemaining;
    }
}
