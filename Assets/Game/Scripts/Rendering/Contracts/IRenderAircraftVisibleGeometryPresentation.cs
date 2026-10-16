using Unity.Entities;
using UnityEngine;
namespace Game.Rendering.Contracts
{
    // Actor identity selects exact canonical source metadata; no role knowledge
    // is inferred from a culling box or a convenient neighbouring draw.
    public interface IRenderAircraftVisibleGeometryPresentation
    {
        bool TryReadAircraftVisibleGeometryBounds(EntityManager manager,Entity actor,Entity draw,out Bounds bounds,out bool hasDraw,out string failure);
    }
}
