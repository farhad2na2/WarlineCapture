using Unity.Entities;
using UnityEngine;

namespace Game.Rendering.Contracts
{
    // Optional independent capability: culling bounds cannot satisfy this query.
    public interface IRenderVisibleGeometryPresentation
    {
        // A non-rendering/disabled entity succeeds with hasDraw=false. An enabled
        // unknown/unsupported draw fails; callers must not silently omit a wing.
        bool TryReadVisibleGeometryBounds(EntityManager manager,Entity entity,out Bounds bounds,out bool hasDraw,out string failure);
    }
}
