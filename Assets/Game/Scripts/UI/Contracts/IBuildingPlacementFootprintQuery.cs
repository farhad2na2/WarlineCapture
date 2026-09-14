using UnityEngine;
namespace Game.UI.Contracts
{
    public interface IBuildingPlacementFootprintQuery
    {
        Vector2Int ActivePlacementFootprint { get; }
    }
}
