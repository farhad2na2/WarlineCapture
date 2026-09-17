using UnityEngine;
namespace Game.UI.Contracts
{
    public interface IBuildingPlacementViewportQuery
    {
        Vector2 GetPlacementViewportCenter(bool isValid);
    }
}
