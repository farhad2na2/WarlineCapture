using UnityEngine;

namespace Game.Rendering
{
    /// <summary>Owner-selected building art; gameplay remains owned by the shared definition.</summary>
    [DisallowMultipleComponent]
    public sealed class BuildingFactionVisualVariants : MonoBehaviour, IBuildingFactionVisualVariants
    {
        [SerializeField] private GameObject playerVisualRoot;
        [SerializeField] private GameObject enemyVisualRoot;
        [SerializeField] private GameObject enemyDestroyedVisualPrefab;

        public GameObject PlayerVisualRoot => playerVisualRoot;
        public GameObject EnemyVisualRoot => enemyVisualRoot;
        public GameObject EnemyDestroyedVisualPrefab => enemyDestroyedVisualPrefab;

        public void SelectEnemy(bool enemy)
        {
            if (playerVisualRoot != null && playerVisualRoot.activeSelf == enemy)
                playerVisualRoot.SetActive(!enemy);
            if (enemyVisualRoot != null && enemyVisualRoot.activeSelf != enemy)
                enemyVisualRoot.SetActive(enemy);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(GameObject player, GameObject enemy, GameObject enemyRuins)
        {
            playerVisualRoot = player;
            enemyVisualRoot = enemy;
            enemyDestroyedVisualPrefab = enemyRuins;
            SelectEnemy(false);
        }
#endif
    }
}
