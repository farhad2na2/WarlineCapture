using Unity.Entities;using Unity.Mathematics;
namespace Unity.Transforms{
public struct PostTransformMatrix : IComponentData
    {
        /// <summary>
        /// The post-transform scale matrix
        /// </summary>
        public float4x4 Value;
    }
}
