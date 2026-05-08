using System;
using UnityEngine;

[CreateAssetMenu(menuName = "WarlineCapture/Chapter 1 Tactical Scale Contract")]
public sealed class Chapter01TacticalScaleContract : ScriptableObject
{
    [SerializeField] private string contractId = "chapter01.tactical.scale.v1";
    [SerializeField] private string approvedReferenceScene = "Assets/Game/Scenes/DesignTargets/FinalMaps/TacticalProductionBatch_A.unity";
    [SerializeField, Min(1f)] private float groundPixelsPerUnit = 600f;
    [SerializeField, Min(1f)] private float entityPixelsPerUnit = 100f;
    [SerializeField, Min(1)] private int groundMaxTextureSize = 2048;
    [SerializeField, Min(1)] private int entityMaxTextureSize = 1024;
    [SerializeField, Min(0.01f)] private float closeCameraOrthographicSize = 0.597f;
    [SerializeField] private Vector2 tacticalWorldSize = new(3.4f, 1.92f);
    [SerializeField, Min(1)] private int defaultGridWidth = 64;
    [SerializeField, Min(1)] private int defaultGridHeight = 36;
    [SerializeField] private TacticalVisualScaleEntry[] visualScales = Array.Empty<TacticalVisualScaleEntry>();

    public string ContractId => contractId;
    public string ApprovedReferenceScene => approvedReferenceScene;
    public float GroundPixelsPerUnit => groundPixelsPerUnit;
    public float EntityPixelsPerUnit => entityPixelsPerUnit;
    public int GroundMaxTextureSize => groundMaxTextureSize;
    public int EntityMaxTextureSize => entityMaxTextureSize;
    public float CloseCameraOrthographicSize => closeCameraOrthographicSize;
    public Vector2 TacticalWorldSize => tacticalWorldSize;
    public int DefaultGridWidth => defaultGridWidth;
    public int DefaultGridHeight => defaultGridHeight;
    public TacticalVisualScaleEntry[] VisualScales => visualScales;

    public float GetScale(TacticalVisualScaleRole role)
    {
        foreach (TacticalVisualScaleEntry entry in visualScales)
        {
            if (entry.Role == role)
            {
                return entry.DefaultScale;
            }
        }

        return 1f;
    }

    public void ConfigureDefaults()
    {
        contractId = "chapter01.tactical.scale.v1";
        approvedReferenceScene = "Assets/Game/Scenes/DesignTargets/FinalMaps/TacticalProductionBatch_A.unity";
        groundPixelsPerUnit = 600f;
        entityPixelsPerUnit = 100f;
        groundMaxTextureSize = 2048;
        entityMaxTextureSize = 1024;
        closeCameraOrthographicSize = 0.597f;
        tacticalWorldSize = new Vector2(3.4f, 1.92f);
        defaultGridWidth = 64;
        defaultGridHeight = 36;
        visualScales = new[]
        {
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.InfantrySquad, 0.07f, "Approved M01 infantry readability scale from playable prototype review."),
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.BattleTank, 0.085f, "Approved tactical vehicle scale anchor."),
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.Apc, 0.095f, "Approved tactical vehicle scale anchor."),
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.CommandBuilding, 0.14f, "Small command/decor building scale."),
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.TentCluster, 0.13f, "Small camp/tent cluster scale."),
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.FuelRefineryModule, 0.30f, "Large industrial building scale anchor."),
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.Helicopter, 0.18f, "Strategic/tactical validation air-unit scale anchor."),
            new TacticalVisualScaleEntry(TacticalVisualScaleRole.VehicleGarage, 0.18f, "Medium vehicle-support building scale anchor."),
        };
    }
}

[Serializable]
public struct TacticalVisualScaleEntry
{
    [SerializeField] private TacticalVisualScaleRole role;
    [SerializeField, Min(0.001f)] private float defaultScale;
    [SerializeField] private string notes;

    public TacticalVisualScaleRole Role => role;
    public float DefaultScale => defaultScale;
    public string Notes => notes;

    public TacticalVisualScaleEntry(TacticalVisualScaleRole role, float defaultScale, string notes)
    {
        this.role = role;
        this.defaultScale = defaultScale;
        this.notes = notes;
    }
}

public enum TacticalVisualScaleRole
{
    InfantrySquad,
    BattleTank,
    Apc,
    CommandBuilding,
    TentCluster,
    FuelRefineryModule,
    Helicopter,
    VehicleGarage
}
