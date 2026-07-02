using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public enum AssistantButtonVisualState
    {
        Idle = 0,
        Recommendation = 1,
        Critical = 2,
        Takeover = 3,
        Muted = 4
    }

    [DisallowMultipleComponent]
    public sealed class AssistantButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image stateBackground;
        [SerializeField] private Image waveformIcon;
        [SerializeField] private Image pulseDot;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text cueText;
        [SerializeField] private Sprite[] stateSprites;
        [SerializeField] private AssistantButtonVisualState initialState;

        public Button Button => button;
        public Image StateBackground => stateBackground;
        public Image WaveformIcon => waveformIcon;
        public TMP_Text LabelText => labelText;
        public TMP_Text StateText => stateText;
        public TMP_Text CueText => cueText;
        public AssistantButtonVisualState CurrentState { get; private set; }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            SetState(initialState);
        }

        public void SetState(AssistantButtonVisualState state)
        {
            CurrentState = state;

            int stateIndex = (int)state;
            if (stateBackground != null && stateSprites != null && stateIndex >= 0 && stateIndex < stateSprites.Length)
                stateBackground.sprite = stateSprites[stateIndex];

            AssistantButtonStatePresentation presentation = GetPresentation(state);
            if (labelText != null)
                labelText.text = "ARIA";

            if (stateText != null)
                stateText.text = presentation.StateLabel;

            if (cueText != null)
            {
                cueText.text = presentation.CueLabel;
                cueText.color = presentation.AccentColor;
            }

            if (pulseDot != null)
                pulseDot.color = presentation.AccentColor;

            if (waveformIcon != null)
                waveformIcon.color = presentation.IconColor;
        }

        private static AssistantButtonStatePresentation GetPresentation(AssistantButtonVisualState state)
        {
            return state switch
            {
                AssistantButtonVisualState.Recommendation => new AssistantButtonStatePresentation("NEXT", ">", new Color(0.30f, 1f, 0.66f, 1f), new Color(0.72f, 1f, 0.88f, 1f)),
                AssistantButtonVisualState.Critical => new AssistantButtonStatePresentation("WARN", "!", new Color(1f, 0.30f, 0.28f, 1f), new Color(1f, 0.72f, 0.68f, 1f)),
                AssistantButtonVisualState.Takeover => new AssistantButtonStatePresentation("CTRL", "[]", new Color(1f, 0.70f, 0.22f, 1f), new Color(1f, 0.86f, 0.56f, 1f)),
                AssistantButtonVisualState.Muted => new AssistantButtonStatePresentation("OFF", "/", new Color(0.52f, 0.62f, 0.64f, 1f), new Color(0.48f, 0.58f, 0.60f, 0.72f)),
                _ => new AssistantButtonStatePresentation("IDLE", "~", new Color(0.20f, 0.92f, 1f, 1f), new Color(0.78f, 0.98f, 1f, 1f))
            };
        }

        private readonly struct AssistantButtonStatePresentation
        {
            public AssistantButtonStatePresentation(string stateLabel, string cueLabel, Color accentColor, Color iconColor)
            {
                StateLabel = stateLabel;
                CueLabel = cueLabel;
                AccentColor = accentColor;
                IconColor = iconColor;
            }

            public string StateLabel { get; }
            public string CueLabel { get; }
            public Color AccentColor { get; }
            public Color IconColor { get; }
        }
    }
}
