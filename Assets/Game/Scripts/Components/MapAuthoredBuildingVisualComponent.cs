using UnityEngine;

namespace Game.Components
{
    [DisallowMultipleComponent]
    public sealed class MapAuthoredBuildingVisualComponent : MonoBehaviour
    {
        [SerializeField] private bool preserveAuthoredTransform = true;
        [SerializeField] private bool preserveAuthoredMaterials = true;
        private bool _hasPresentationWorldCenter;
        private Vector3 _presentationWorldCenter;
        private bool _hasPresentationGeometry;
        private Vector3 _presentationWorldSize;
        private float _presentationYawDegrees;

        public bool PreserveAuthoredTransform => preserveAuthoredTransform;
        public bool PreserveAuthoredMaterials => preserveAuthoredMaterials;
        public bool HasPresentationWorldCenter => _hasPresentationWorldCenter;
        public Vector3 PresentationWorldCenter => _presentationWorldCenter;
        public bool HasPresentationGeometry => _hasPresentationGeometry;
        public Vector3 PresentationWorldSize => _presentationWorldSize;
        public float PresentationYawDegrees => _presentationYawDegrees;

        public void ConfigurePresentationWorldCenter(Vector3 worldCenter)
        {
            _presentationWorldCenter = worldCenter;
            _hasPresentationWorldCenter = true;
        }

        public void ConfigurePresentationGeometry(Vector3 worldCenter, Vector3 worldSize, float yawDegrees)
        {
            ConfigurePresentationWorldCenter(worldCenter);
            _presentationWorldSize = new Vector3(
                Mathf.Max(0f, Mathf.Abs(worldSize.x)),
                Mathf.Max(0f, Mathf.Abs(worldSize.y)),
                Mathf.Max(0f, Mathf.Abs(worldSize.z)));
            _presentationYawDegrees = yawDegrees;
            _hasPresentationGeometry = _presentationWorldSize.x > 0.001f &&
                                       _presentationWorldSize.z > 0.001f;
        }
    }
}
