using System;
using UnityEngine;

internal sealed class BuildingPlacementCommandSystem
{
    internal readonly struct Context
    {
        public readonly BuildingPlacementStartupSystem StartupSystem;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingPlacementSessionSystem SessionSystem;
        public readonly BuildingPlacementSessionSystem.Context SessionContext;
        public readonly Action<string> LogWarning;

        public Context(
            BuildingPlacementStartupSystem startupSystem,
            BuildingDefinitionSystem definitionSystem,
            BuildingPlacementSessionSystem sessionSystem,
            BuildingPlacementSessionSystem.Context sessionContext,
            Action<string> logWarning)
        {
            StartupSystem = startupSystem;
            DefinitionSystem = definitionSystem;
            SessionSystem = sessionSystem;
            SessionContext = sessionContext;
            LogWarning = logWarning;
        }
    }

    public void BeginSoldierBasePlacement(Context context)
    {
        BeginConfiguredPlacement(
            context,
            context.StartupSystem.SoldierBaseDefinition,
            "BuildingPlacementCommandSystem is missing the Soldier Base spawnable prefab reference.");
    }

    public void BeginSoldierTentPlacement(Context context)
    {
        BeginConfiguredPlacement(
            context,
            context.StartupSystem.SoldierTentDefinition,
            "BuildingPlacementCommandSystem is missing the Soldier Tent spawnable prefab reference.");
    }

    public void BeginFactoryPlacement(Context context)
    {
        BeginConfiguredPlacement(
            context,
            context.StartupSystem.FactoryDefinition,
            "BuildingPlacementCommandSystem is missing the Factory spawnable prefab reference.");
    }

    public bool BeginPlacementForConfiguredSpawnable(Context context, GameObject prefab)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return false;

        if (context.DefinitionSystem == null ||
            !context.DefinitionSystem.TryGetConfiguredDefinition(prefab, out BuildingDefinition definition))
        {
            return false;
        }

        BeginPlacement(context, definition);
        return true;
    }

    public bool ConfirmBuildingPlacement(Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext);
    }

    public void CancelBuildingPlacement(Context context)
    {
        context.SessionSystem?.CancelBuildingPlacement(context.SessionContext);
    }

    public void ExitBuildMode(Context context)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext);
    }

    public void ExitBuildMode(Context context, bool clearBuildingSelection)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext, clearBuildingSelection);
    }

    public void NotifyPlacementUiPointerDown(Context context)
    {
        context.SessionSystem?.NotifyPlacementUiPointerDown(context.SessionContext);
    }

    public void SetActivePlacementCost(Context context, int cost)
    {
        context.SessionSystem?.SetActivePlacementCost(context.SessionContext, cost);
    }

    private static void BeginConfiguredPlacement(Context context, BuildingDefinition definition, string missingPrefabWarning)
    {
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        if (definition == null || definition.Prefab == null)
        {
            context.LogWarning?.Invoke(missingPrefabWarning);
            return;
        }

        BeginPlacement(context, definition);
    }

    private static void BeginPlacement(Context context, BuildingDefinition definition)
    {
        context.SessionSystem?.BeginPlacement(context.SessionContext, definition);
    }
}
