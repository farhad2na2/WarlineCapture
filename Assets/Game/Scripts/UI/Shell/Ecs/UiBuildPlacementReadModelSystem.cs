using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class UiBuildPlacementReadModelSource
{
    private static IBuildingUiCommand buildingUiCommand;

    public static bool HasBuildingUiCommand => buildingUiCommand != null;
    public static IBuildingUiCommand BuildingUiCommand => buildingUiCommand;

    public static void Configure(IBuildingUiCommand command)
    {
        buildingUiCommand = command;
    }

    public static void Clear()
    {
        buildingUiCommand = null;
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct UiBuildPlacementReadModelSystem : ISystem
{
    private EntityQuery boundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        boundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<UiShellStateComponent>(),
            ComponentType.ReadWrite<UiBuildPlacementConfirmationBarComponent>());
        state.RequireForUpdate(boundaryQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity boundary = boundaryQuery.GetSingletonEntity();
        IBuildingUiCommand command = UiBuildPlacementReadModelSource.BuildingUiCommand;
        if (command == null || !command.HasPendingBuildingPlacement)
        {
            UiBuildPlacementConfirmationBarComponent current =
                state.EntityManager.GetComponentData<UiBuildPlacementConfirmationBarComponent>(boundary);
            if (current.Visible == 0)
                return;

            state.EntityManager.SetComponentData(boundary, Hidden());
            return;
        }

        state.EntityManager.SetComponentData(boundary, BuildPlacementBar(command));
    }

    private static UiBuildPlacementConfirmationBarComponent BuildPlacementBar(IBuildingUiCommand buildingUiCommand)
    {
        if (buildingUiCommand == null || !buildingUiCommand.HasPendingBuildingPlacement)
            return Hidden();

        bool canConfirm = buildingUiCommand.CanConfirmBuildingPlacement;
        SplitPlacementStatus(buildingUiCommand.PlacementStatusText, out string title, out string status);
        string safeStatus = string.IsNullOrWhiteSpace(status)
            ? "DRAG TO POSITION"
            : status.ToUpperInvariant();

        return new UiBuildPlacementConfirmationBarComponent
        {
            Visible = 1,
            CanConfirm = canConfirm ? (byte)1 : (byte)0,
            CanCancel = 1,
            CanRotate = 1,
            Title = new FixedString64Bytes(string.IsNullOrWhiteSpace(title) ? "PLACE BUILDING" : $"PLACE {title.ToUpperInvariant()}"),
            Status = new FixedString64Bytes(safeStatus),
            CostText = new FixedString32Bytes(FormatCost(buildingUiCommand.ActivePlacementCost)),
            DurationText = new FixedString32Bytes(FormatDuration(buildingUiCommand.ActivePlacementDurationSeconds)),
            InstructionText = new FixedString128Bytes("DRAG TO POSITION, CONFIRM TO BUILD")
        };
    }

    private static UiBuildPlacementConfirmationBarComponent Hidden()
    {
        return new UiBuildPlacementConfirmationBarComponent
        {
            Visible = 0,
            CanConfirm = 0,
            CanCancel = 0,
            CanRotate = 0,
            Title = default,
            Status = default,
            CostText = default,
            DurationText = default,
            InstructionText = default
        };
    }

    private static void SplitPlacementStatus(string rawStatus, out string title, out string status)
    {
        title = "BUILDING";
        status = rawStatus;
        if (string.IsNullOrWhiteSpace(rawStatus))
            return;

        int separator = rawStatus.IndexOf(':');
        if (separator < 0)
            return;

        title = rawStatus.Substring(0, separator).Trim();
        status = rawStatus.Substring(separator + 1).Trim();
    }

    private static string FormatCost(int cost)
    {
        return cost > 0
            ? cost.ToString("N0", CultureInfo.InvariantCulture)
            : "0";
    }

    private static string FormatDuration(float seconds)
    {
        if (seconds <= 0f)
            return "00:00";

        int totalSeconds = Mathf.CeilToInt(seconds);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
