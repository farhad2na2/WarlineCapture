using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class MissionLaunchActionSystem : MonoBehaviour
{
    [SerializeField] private string missionId = "saga.ch01.m05.breach_assault";
    [SerializeField] private WarlineCaptureRoute returnRoute = WarlineCaptureRoute.SagaMap;
    [SerializeField] private bool launchExistingGameplay;
    [SerializeField] private bool useActiveMissionWhenAvailable;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        ActiveMissionSession activeMissionSession = new();
        string selectedMissionId = useActiveMissionWhenAvailable && activeMissionSession.HasActiveMission
            ? activeMissionSession.ActiveMission.MissionId
            : missionId;
        activeMissionSession.BeginMission(selectedMissionId, returnRoute);

        if (launchExistingGameplay)
            WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter(this);
    }
}
