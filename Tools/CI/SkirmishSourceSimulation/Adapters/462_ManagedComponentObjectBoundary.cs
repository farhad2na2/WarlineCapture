using System;
using Unity.Entities;

namespace Unity.Entities
{
    // Finite host equivalent of Unity's managed-component insertion API. The
    // object remains attached to the same EntityManager row and participates in
    // its normal query membership and GetComponentObject readback.
    public sealed partial class EntityManager
    {
        public void AddComponentObject<T>(Entity entity, T componentObject)
            where T : class, IComponentData
        {
            if (componentObject == null) throw new ArgumentNullException(nameof(componentObject));
            AddComponentData(entity, componentObject);
        }
    }
}
