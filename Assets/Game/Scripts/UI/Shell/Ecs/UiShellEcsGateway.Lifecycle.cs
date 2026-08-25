using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static void ResetWorldBoundQueries(World nextWorld)
        {
            if (cachedWorld != null && cachedWorld.IsCreated)
            {
                if (hasBoundaryQuery) boundaryQuery.Dispose();
                if (hasFocusedSelectionQuery) focusedSelectionQuery.Dispose();
                if (hasSelectionInputQuery) selectionInputQuery.Dispose();
                if (hasSelectedUnitsQuery) selectedUnitsQuery.Dispose();
                if (hasMinimapMarkerQuery) minimapMarkerQuery.Dispose();
                if (hasGridConfigQuery) gridConfigQuery.Dispose();
                if (hasResourceStorageQuery) resourceStorageQuery.Dispose();
                if (hasFactionEconomyQuery) factionEconomyQuery.Dispose();
                if (hasAssistantMatchStartQuery) assistantMatchStartQuery.Dispose();
                if (hasMissionRootQuery) missionRootQuery.Dispose();
            }

            cachedWorld = nextWorld;
            boundaryQuery = default;
            focusedSelectionQuery = default;
            selectionInputQuery = default;
            selectedUnitsQuery = default;
            minimapMarkerQuery = default;
            gridConfigQuery = default;
            resourceStorageQuery = default;
            factionEconomyQuery = default;
            assistantMatchStartQuery = default;
            missionRootQuery = default;
            hasBoundaryQuery = false;
            hasFocusedSelectionQuery = false;
            hasSelectionInputQuery = false;
            hasSelectedUnitsQuery = false;
            hasMinimapMarkerQuery = false;
            hasGridConfigQuery = false;
            hasResourceStorageQuery = false;
            hasFactionEconomyQuery = false;
            hasAssistantMatchStartQuery = false;
            hasMissionRootQuery = false;
        }
    }
}
