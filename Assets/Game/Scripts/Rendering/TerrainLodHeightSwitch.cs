using UnityEngine;

namespace Game.Rendering
{
    public sealed class TerrainLodHeightSwitch : MonoBehaviour
    {
        private static Camera[] s_cameraScratch = new Camera[8];

        [SerializeField] private Transform lod0Root;
        [SerializeField] private Transform lod1Root;
        [SerializeField] private Transform lod2Root;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float lod1CameraHeight = 70f;
        [SerializeField] private float lod2CameraHeight = 130f;
        [SerializeField] private bool useCameraDistance = true;
        [SerializeField] private float lod1CameraDistance = 950f;
        [SerializeField] private float lod2CameraDistance = 1500f;
        [SerializeField] private int currentLod;

        private int _activeLod = -1;
        private Camera _resolvedCamera;

        public Transform Lod0Root
        {
            get => lod0Root;
            set => lod0Root = value;
        }

        public Transform Lod1Root
        {
            get => lod1Root;
            set => lod1Root = value;
        }

        public Transform Lod2Root
        {
            get => lod2Root;
            set => lod2Root = value;
        }

        public float Lod1CameraHeight
        {
            get => lod1CameraHeight;
            set => lod1CameraHeight = value;
        }

        public float Lod2CameraHeight
        {
            get => lod2CameraHeight;
            set => lod2CameraHeight = value;
        }

        public int CurrentLod => currentLod;

        private void OnEnable()
        {
            Apply(force: true);
        }

        private void Update()
        {
            Apply(force: false);
        }

        private void OnValidate()
        {
            if (lod2CameraHeight < lod1CameraHeight)
                lod2CameraHeight = lod1CameraHeight;
            if (lod2CameraDistance < lod1CameraDistance)
                lod2CameraDistance = lod1CameraDistance;

            Apply(force: true);
        }

        private void Apply(bool force)
        {
            Camera cameraToUse = ResolveCamera();
            float cameraHeight = cameraToUse != null ? cameraToUse.transform.position.y : 0f;
            float cameraDistance = cameraToUse != null ? Vector3.Distance(cameraToUse.transform.position, transform.position) : 0f;
            int heightLod = cameraHeight >= lod2CameraHeight ? 2 : cameraHeight >= lod1CameraHeight ? 1 : 0;
            int distanceLod = useCameraDistance
                ? cameraDistance >= lod2CameraDistance ? 2 : cameraDistance >= lod1CameraDistance ? 1 : 0
                : 0;
            int nextLod = Mathf.Max(heightLod, distanceLod);
            if (!force && nextLod == _activeLod)
                return;

            _activeLod = nextLod;
            currentLod = nextLod;
            SetActive(lod0Root, nextLod == 0);
            SetActive(lod1Root, nextLod == 1);
            SetActive(lod2Root, nextLod == 2);
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
                return targetCamera;

            if (_resolvedCamera != null && _resolvedCamera.isActiveAndEnabled)
                return _resolvedCamera;

            int cameraCount = Camera.allCamerasCount;
            if (s_cameraScratch.Length < cameraCount)
                s_cameraScratch = new Camera[Mathf.NextPowerOfTwo(cameraCount)];

            Camera.GetAllCameras(s_cameraScratch);
            for (int i = 0; i < cameraCount; i++)
            {
                Camera candidate = s_cameraScratch[i];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.cameraType != CameraType.Game)
                    continue;
                if (candidate.name.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                _resolvedCamera = candidate;
                return _resolvedCamera;
            }

            return null;
        }

        private static void SetActive(Transform root, bool active)
        {
            if (root != null && root.gameObject.activeSelf != active)
                root.gameObject.SetActive(active);
        }
    }
}
