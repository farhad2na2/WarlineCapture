using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuDiagnosticsView : MonoBehaviour
    {
        private readonly MenuDiagnosticsUiSystemHelper diagnosticsSystem = new();

        [SerializeField] private Button fpsButton;
        [SerializeField] private TMP_Text fpsText;
        [SerializeField] private GameObject logPanel;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private Button closeButton;

        public Button FpsButton => fpsButton;
        public TMP_Text FpsText => fpsText;
        public GameObject LogPanel => logPanel;
        public TMP_Text LogText => logText;
        public ScrollRect LogScrollRect => logScrollRect;
        public Button CloseButton => closeButton;

        public void Configure(
            Button configuredFpsButton,
            TMP_Text configuredFpsText,
            GameObject configuredLogPanel,
            TMP_Text configuredLogText,
            ScrollRect configuredLogScrollRect,
            Button configuredCloseButton)
        {
            fpsButton = configuredFpsButton;
            fpsText = configuredFpsText;
            logPanel = configuredLogPanel;
            logText = configuredLogText;
            logScrollRect = configuredLogScrollRect;
            closeButton = configuredCloseButton;
        }

        private void Awake()
        {
            diagnosticsSystem.Initialize(this);
        }

        private void OnEnable()
        {
            diagnosticsSystem.Initialize(this);
        }

        private void Update()
        {
            diagnosticsSystem.Update(this, Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            diagnosticsSystem.Shutdown(this);
        }
    }
}
