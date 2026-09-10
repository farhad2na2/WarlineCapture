using Unity.Entities;
namespace Game.Components
{
    /// <summary>Root-space planted-foot correction for each GPU animation index.</summary>
    public struct UnitAnimationGroundOffset : IBufferElementData { public float Value; }
}
