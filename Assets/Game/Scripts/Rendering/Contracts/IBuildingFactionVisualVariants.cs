using UnityEngine;

namespace Game.Rendering
{
    /// <summary>Presentation binding consumed by ownership without referencing the rendering implementation.</summary>
    public interface IBuildingFactionVisualVariants
    {
        GameObject PlayerVisualRoot { get; }
        GameObject EnemyVisualRoot { get; }
        GameObject EnemyDestroyedVisualPrefab { get; }
        void SelectEnemy(bool enemy);
    }
}
