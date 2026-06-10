using UnityEngine;

[DisallowMultipleComponent]
public sealed class MatchHudFooterContentView : MonoBehaviour
{
    [SerializeField] private MatchOverlayCommandControlsView commandControls;
    [SerializeField] private BattleHudRuntimeFeedbackView runtimeFeedback;
    [SerializeField] private MatchHudMinimapView minimap;
    [SerializeField] private MatchHudSquadTrayView squadTray;

    public MatchOverlayCommandControlsView CommandControls => commandControls;
    public BattleHudRuntimeFeedbackView RuntimeFeedback => runtimeFeedback;
    public MatchHudMinimapView Minimap => minimap;
    public MatchHudSquadTrayView SquadTray => squadTray;
}
