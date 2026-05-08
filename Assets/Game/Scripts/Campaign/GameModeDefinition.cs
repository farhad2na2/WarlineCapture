using System;
using UnityEngine;

[Serializable]
public sealed class GameModeDefinition
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private WarlineCaptureRoute sourceRoute;

    public string Id => id;
    public string DisplayName => displayName;
    public WarlineCaptureRoute SourceRoute => sourceRoute;

    public GameModeDefinition(string id, string displayName, WarlineCaptureRoute sourceRoute)
    {
        this.id = id;
        this.displayName = displayName;
        this.sourceRoute = sourceRoute;
    }
}
