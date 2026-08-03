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

        public bool PreserveAuthoredTransform => preserveAuthoredTransform;
        public bool PreserveAuthoredMaterials => preserveAuthoredMaterials;
        public bool HasPresentationWorldCenter => _hasPresentationWorldCenter;
        public Vector3 PresentationWorldCenter => _presentationWorldCenter;

        public void ConfigurePresentationWorldCenter(Vector3 worldCenter)
        {
            _presentationWorldCenter = worldCenter;
            _hasPresentationWorldCenter = true;
        }
    }
}
