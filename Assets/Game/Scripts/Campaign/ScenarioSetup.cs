using System;
using UnityEngine;

[Serializable]
public sealed class ScenarioSetup
{
    [SerializeField] private GameModeDefinition gameMode;
    [SerializeField] private MissionConfig mission;
    [SerializeField] private string returnRouteName;

    public GameModeDefinition GameMode => gameMode;
    public MissionConfig Mission => mission;
    public string ReturnRouteName => returnRouteName;
    public string ScenarioSetupId => mission?.ScenarioSetupId ?? string.Empty;
    public string LevelId => mission?.LevelId ?? string.Empty;
    public string IsoMapId => mission?.IsoMapId ?? string.Empty;
    public string MapPreviewArtId => mission?.MapPreviewArtId ?? string.Empty;
    public string MinimapArtId => mission?.MinimapArtId ?? string.Empty;

    public ScenarioSetup(GameModeDefinition gameMode, MissionConfig mission, WarlineCaptureRoute returnRoute)
    {
        this.gameMode = gameMode;
        this.mission = mission;
        returnRouteName = returnRoute.ToString();
    }
}
