using Unity.Entities;using Unity.Mathematics;
namespace Unity.Transforms{
public struct Parent : IComponentData
    {
        /// <summary>
        /// The parent entity.
        /// </summary>
        /// <remarks>This field must refer to a valid entity. Root level entities should not use <see cref="Entity.Null"/>;
        /// rather, they should not have the <see cref="Parent"/> component at all.</remarks>
        public Entity Value;
    }
}
