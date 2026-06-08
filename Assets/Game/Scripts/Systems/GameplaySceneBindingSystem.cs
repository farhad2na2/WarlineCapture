using Game.Scripts.UI;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class GameplaySceneBindingSystem
{
    public void BindRuntimeGridBlockerDebugViews(RuntimeGridBlockerSystem runtimeGridBlockers)
    {
        IReadOnlyList<GridAuthoring> grids = GridAuthoring.Instances;
        for (int i = 0; i < grids.Count; i++)
        {
            GridAuthoring grid = grids[i];
            if (grid == null || !grid.gameObject.scene.IsValid())
                continue;

            grid.BindRuntimeGridBlockers(runtimeGridBlockers);
        }
    }

    public void BindGameplayUiRuntimeDependencies(
        World world,
        SelectionUiCommandSystem selectionUiCommandSystem,
        MainMenuPlayUI mainMenuPlayUi = null)
    {
        IReadOnlyList<WarlineCaptureShellContentSystem> contentSystems = WarlineCaptureShellContentSystem.Instances;
        for (int i = 0; i < contentSystems.Count; i++)
        {
            WarlineCaptureShellContentSystem contentSystem = contentSystems[i];
            if (IsLoadedSceneObject(contentSystem))
                contentSystem.BindGameplayRuntimeDependencies(selectionUiCommandSystem, mainMenuPlayUi);
        }
    }

    private bool IsLoadedSceneObject(Component component)
    {
        return component != null &&
            component.gameObject != null &&
            component.gameObject.scene.IsValid() &&
            component.gameObject.scene.isLoaded;
    }
}
