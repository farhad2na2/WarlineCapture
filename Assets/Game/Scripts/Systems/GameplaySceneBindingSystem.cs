using Game.Scripts.UI;
using Unity.Entities;
using UnityEngine;

public sealed class GameplaySceneBindingSystem
{
    private readonly MatchOverlayCommandInputSystem _matchOverlayCommandInputSystem = new();

    public void BindRuntimeGridBlockerDebugViews(RuntimeGridBlockerSystem runtimeGridBlockers)
    {
        foreach (GridAuthoring grid in Resources.FindObjectsOfTypeAll<GridAuthoring>())
        {
            if (grid == null || !grid.gameObject.scene.IsValid())
                continue;

            grid.BindRuntimeGridBlockers(runtimeGridBlockers);
        }
    }

    public void BindGameplayUiRuntimeDependencies(
        World world,
        SelectionUiCommandSystem selectionUiCommandSystem)
    {
        foreach (WarlineCaptureShellContentPresenterView presenter in Resources.FindObjectsOfTypeAll<WarlineCaptureShellContentPresenterView>())
        {
            if (IsLoadedSceneObject(presenter))
                presenter.BindGameplayRuntimeDependencies(selectionUiCommandSystem);
        }

        foreach (MatchOverlayCommandControlsView controls in Resources.FindObjectsOfTypeAll<MatchOverlayCommandControlsView>())
        {
            if (IsLoadedSceneObject(controls) &&
                controls.GetComponentInParent<WarlineCaptureShellContentPresenterView>(true) == null)
            {
                _matchOverlayCommandInputSystem.Bind(controls, selectionUiCommandSystem);
            }
        }

        foreach (AssistantRuntimeBinding binding in Resources.FindObjectsOfTypeAll<AssistantRuntimeBinding>())
        {
            if (!IsLoadedSceneObject(binding))
                continue;

            WarlineCaptureRouter router = binding.GetComponentInParent<WarlineCaptureRouter>(true);
            WarlineCaptureMatchResultFlow resultFlow = binding.GetComponentInParent<WarlineCaptureMatchResultFlow>(true);
            MatchObjectivePanelController objectivePanel = binding.GetComponentInParent<MatchObjectivePanelController>(true);
            BattleHudGameplayBridge bridge = binding.GetComponentInParent<BattleHudGameplayBridge>(true);
            if (bridge == null)
                bridge = FindLoadedSceneComponent<BattleHudGameplayBridge>();

            binding.BindRuntimeDependencies(
                world,
                bridge,
                router,
                resultFlow,
                objectivePanel);
        }
    }

    private T FindLoadedSceneComponent<T>() where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (IsLoadedSceneObject(component))
                return component;
        }

        return null;
    }

    private bool IsLoadedSceneObject(Component component)
    {
        return component != null &&
            component.gameObject != null &&
            component.gameObject.scene.IsValid() &&
            component.gameObject.scene.isLoaded;
    }
}
