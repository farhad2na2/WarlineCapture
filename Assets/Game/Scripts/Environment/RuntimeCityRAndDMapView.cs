using Game.Configs;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class RuntimeCityRAndDMapView : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private RuntimeCitySpawnerSystemConfig config;
        [SerializeField] private RuntimeOperationMapVisualRecipe visualRecipe;
        [SerializeField] private RuntimeOperationMapVisualRecipe deterministicFallbackRecipe;
        [SerializeField] private bool deterministicFallbackEnabled = true;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool showDebugOverlay = true;
        [SerializeField, Min(1)] private int visualRecipeEntriesPerFrame = 8;
        [SerializeField, Min(0.1f)] private float visualRecipeFrameBudgetMilliseconds = 6f;

        [Header("Local Grid")]
        [SerializeField, Min(64)] private int gridWidth = 512;
        [SerializeField, Min(64)] private int gridHeight = 512;
        [SerializeField, Min(0.1f)] private float gridCellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = new(-256f, 0f, -256f);
        [SerializeField, Min(1)] private int roadCellSizeInGridCells = 10;

        [Header("Algorithmic Road Layout")]
        [SerializeField, Min(0)] private int algorithmicNorthRadialTrim = 4;
        [SerializeField, Min(0)] private int algorithmicEastRadialTrim = 4;
        [SerializeField, Min(0)] private int algorithmicSouthRadialTrim = 3;
        [SerializeField, Min(0)] private int algorithmicWestRadialTrim = 1;
        [SerializeField, Min(0)] private int algorithmicMaximumOuterStreetLength = 3;

        [Header("Algorithmic Reveal")]
        [SerializeField] private RuntimeOperationMapRevealSettings algorithmicReveal;
        [SerializeField] private List<RuntimeOperationMapCameraPose> algorithmicCameraPoses = new();

        [Header("Presentation")]
        [SerializeField] private Transform generatedRoot;
        [SerializeField] private Camera presentationCamera;
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material roadShoulderMaterial;
        [SerializeField] private Color algorithmicRoadColor = new(0.12f, 0.13f, 0.14f, 1f);
        [SerializeField] private Color algorithmicRoadShoulderColor = new(0.56f, 0.42f, 0.28f, 1f);
        [SerializeField] private Material algorithmicGroundMaterial;
        [SerializeField] private Color algorithmicGroundColor = new(0.46f, 0.32f, 0.20f, 1f);
        [SerializeField] private List<RuntimeOperationMapAlgorithmicDistrictSurfaceSettings> algorithmicDistrictSurfaces = new();
        [SerializeField] private RuntimeOperationMapAlgorithmicAftermathSettings algorithmicAftermath;
        [SerializeField] private TextMesh statusText;

        private RuntimeCityRAndDMapSystem _runtimeSystem;

        public RuntimeCitySpawnerSystemConfig Config => config;
        public RuntimeOperationMapVisualRecipe VisualRecipe => visualRecipe;
        public RuntimeOperationMapVisualRecipe DeterministicFallbackRecipe =>
            deterministicFallbackRecipe;
        public bool DeterministicFallbackEnabled => deterministicFallbackEnabled;
        public bool GenerateOnStart => generateOnStart;
        public bool ShowDebugOverlay => showDebugOverlay;
        public int VisualRecipeEntriesPerFrame => Mathf.Max(1, visualRecipeEntriesPerFrame);
        public float VisualRecipeFrameBudgetMilliseconds => Mathf.Max(0.1f, visualRecipeFrameBudgetMilliseconds);
        public int GridWidth => Mathf.Max(64, gridWidth);
        public int GridHeight => Mathf.Max(64, gridHeight);
        public float GridCellSize => Mathf.Max(0.1f, gridCellSize);
        public Vector3 GridOrigin => gridOrigin;
        public int RoadCellSizeInGridCells => Mathf.Max(1, roadCellSizeInGridCells);
        internal RuntimeCityRoadTerminalPolicy AlgorithmicRoadTerminalPolicy =>
            new(
                algorithmicNorthRadialTrim,
                algorithmicEastRadialTrim,
                algorithmicSouthRadialTrim,
                algorithmicWestRadialTrim,
                algorithmicMaximumOuterStreetLength);
        public RuntimeOperationMapRevealSettings AlgorithmicReveal => algorithmicReveal;
        public IReadOnlyList<RuntimeOperationMapCameraPose> AlgorithmicCameraPoses =>
            algorithmicCameraPoses;
        public Transform GeneratedRoot => generatedRoot;
        public Camera PresentationCamera => presentationCamera;
        public Material RoadMaterial => roadMaterial;
        public Material RoadShoulderMaterial => roadShoulderMaterial;
        public Color AlgorithmicRoadColor => algorithmicRoadColor;
        public Color AlgorithmicRoadShoulderColor => algorithmicRoadShoulderColor;
        public Material AlgorithmicGroundMaterial => algorithmicGroundMaterial;
        public Color AlgorithmicGroundColor => algorithmicGroundColor;
        public IReadOnlyList<RuntimeOperationMapAlgorithmicDistrictSurfaceSettings> AlgorithmicDistrictSurfaces =>
            algorithmicDistrictSurfaces;
        public RuntimeOperationMapAlgorithmicAftermathSettings AlgorithmicAftermath => algorithmicAftermath;

        public void RequestGeneration()
        {
            _runtimeSystem?.RequestGeneration();
        }

        public void RequestCancel()
        {
            _runtimeSystem?.RequestCancel();
        }

        public void RequestRestart()
        {
            _runtimeSystem?.RequestRestart();
        }

        public void RequestClear()
        {
            _runtimeSystem?.RequestClear();
        }

        internal void ApplyPresentation(string status)
        {
            if (statusText == null)
                return;

            bool visible = showDebugOverlay && !string.IsNullOrEmpty(status);
            if (statusText.gameObject.activeSelf != visible)
                statusText.gameObject.SetActive(visible);
            if (visible && !string.Equals(statusText.text, status, System.StringComparison.Ordinal))
                statusText.text = status;
        }

        private void OnEnable()
        {
            BindToDefaultWorld();
        }

        private void Start()
        {
            BindToDefaultWorld();
        }

        private void OnDisable()
        {
            if (_runtimeSystem == null)
                return;

            _runtimeSystem.Unbind(this);
            _runtimeSystem = null;
        }

        private void BindToDefaultWorld()
        {
            if (!Application.isPlaying || _runtimeSystem != null)
                return;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            _runtimeSystem = world.GetOrCreateSystemManaged<RuntimeCityRAndDMapSystem>();
            _runtimeSystem.Bind(this);
        }
    }
}
