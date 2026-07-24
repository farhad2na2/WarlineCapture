using UnityEngine;

namespace Game.Authoring
{
    public sealed class DenseCityProtectedAutobahnReplacementTileMarker : MonoBehaviour
    {
        [SerializeField] private int column;
        [SerializeField] private int row;

        public Vector2Int Cell => new(column, row);

        public void Configure(Vector2Int cell)
        {
            column = cell.x;
            row = cell.y;
        }
    }
}
