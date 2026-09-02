using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DefaultExecutionOrder(1900)]
    [DisallowMultipleComponent]
    public sealed class MatchHudFullMapFilterView : MonoBehaviour
    {
        [SerializeField] private Toggle friendliesToggle;
        [SerializeField] private Toggle enemiesToggle;
        [SerializeField] private Toggle objectivesToggle;
        [SerializeField] private Toggle routesToggle;
        [SerializeField] private Toggle viewportToggle;
        [SerializeField] private GameObject previewFriendlies;
        [SerializeField] private GameObject previewEnemies;
        [SerializeField] private GameObject previewObjectives;
        [SerializeField] private GameObject previewRoutes;
        [SerializeField] private RectTransform runtimeMarkerRoot;
        [SerializeField] private RectTransform viewportRect;
        [SerializeField] private Sprite friendlyMarkerSprite;
        [SerializeField] private Sprite enemyMarkerSprite;
        [SerializeField] private Sprite objectiveMarkerSprite;

        public Toggle FriendliesToggle => friendliesToggle;
        public Toggle EnemiesToggle => enemiesToggle;
        public Toggle ObjectivesToggle => objectivesToggle;
        public Toggle RoutesToggle => routesToggle;
        public Toggle ViewportToggle => viewportToggle;
        public GameObject PreviewFriendlies => previewFriendlies;
        public GameObject PreviewEnemies => previewEnemies;
        public GameObject PreviewObjectives => previewObjectives;
        public GameObject PreviewRoutes => previewRoutes;

        public void Configure(
            Toggle configuredFriendlies,
            Toggle configuredEnemies,
            Toggle configuredObjectives,
            Toggle configuredRoutes,
            Toggle configuredViewport,
            GameObject configuredPreviewFriendlies,
            GameObject configuredPreviewEnemies,
            GameObject configuredPreviewObjectives,
            GameObject configuredPreviewRoutes,
            RectTransform configuredRuntimeMarkerRoot,
            RectTransform configuredViewportRect,
            Sprite configuredFriendlyMarkerSprite,
            Sprite configuredEnemyMarkerSprite,
            Sprite configuredObjectiveMarkerSprite)
        {
            Unbind();
            friendliesToggle = configuredFriendlies;
            enemiesToggle = configuredEnemies;
            objectivesToggle = configuredObjectives;
            routesToggle = configuredRoutes;
            viewportToggle = configuredViewport;
            previewFriendlies = configuredPreviewFriendlies;
            previewEnemies = configuredPreviewEnemies;
            previewObjectives = configuredPreviewObjectives;
            previewRoutes = configuredPreviewRoutes;
            runtimeMarkerRoot = configuredRuntimeMarkerRoot;
            viewportRect = configuredViewportRect;
            friendlyMarkerSprite = configuredFriendlyMarkerSprite;
            enemyMarkerSprite = configuredEnemyMarkerSprite;
            objectiveMarkerSprite = configuredObjectiveMarkerSprite;
            Bind();
            RefreshFilters();
        }

        public void RefreshFilters()
        {
            SetVisible(previewFriendlies, friendliesToggle == null || friendliesToggle.isOn);
            SetVisible(previewEnemies, enemiesToggle == null || enemiesToggle.isOn);
            SetVisible(previewObjectives, objectivesToggle == null || objectivesToggle.isOn);
            SetVisible(previewRoutes, routesToggle == null || routesToggle.isOn);
            if (viewportRect != null)
                SetVisible(viewportRect.gameObject, viewportToggle == null || viewportToggle.isOn);
            RefreshRuntimeMarkers();
        }

        private void OnEnable()
        {
            Bind();
            RefreshFilters();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void LateUpdate()
        {
            RefreshRuntimeMarkers();
        }

        private void Bind()
        {
            Bind(friendliesToggle);
            Bind(enemiesToggle);
            Bind(objectivesToggle);
            Bind(routesToggle);
            Bind(viewportToggle);
        }

        private void Unbind()
        {
            Unbind(friendliesToggle);
            Unbind(enemiesToggle);
            Unbind(objectivesToggle);
            Unbind(routesToggle);
            Unbind(viewportToggle);
        }

        private void Bind(Toggle toggle)
        {
            if (toggle == null)
                return;
            toggle.onValueChanged.RemoveListener(OnFilterChanged);
            toggle.onValueChanged.AddListener(OnFilterChanged);
        }

        private void Unbind(Toggle toggle)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnFilterChanged);
        }

        private void OnFilterChanged(bool _)
        {
            RefreshFilters();
        }

        private void RefreshRuntimeMarkers()
        {
            if (runtimeMarkerRoot == null)
                return;

            bool showFriendlies = friendliesToggle == null || friendliesToggle.isOn;
            bool showEnemies = enemiesToggle == null || enemiesToggle.isOn;
            bool showObjectives = objectivesToggle == null || objectivesToggle.isOn;
            for (int i = 0; i < runtimeMarkerRoot.childCount; i++)
            {
                Transform child = runtimeMarkerRoot.GetChild(i);
                Image image = child != null ? child.GetComponent<Image>() : null;
                if (image == null || image.sprite == null)
                    continue;

                bool visible = image.sprite == friendlyMarkerSprite
                    ? showFriendlies
                    : image.sprite == enemyMarkerSprite
                        ? showEnemies
                        : image.sprite == objectiveMarkerSprite
                            ? showObjectives
                            : child.gameObject.activeSelf;
                if (child.gameObject.activeSelf != visible)
                    child.gameObject.SetActive(visible);
            }
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null && target.activeSelf != visible)
                target.SetActive(visible);
        }
    }
}
