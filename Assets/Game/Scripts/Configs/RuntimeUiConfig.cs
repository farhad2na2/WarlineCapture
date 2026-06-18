using UnityEngine;

public enum RuntimeUiMode
{
    Canvas = 0,
    UiToolkit = 1
}

[CreateAssetMenu(menuName = "Game/UI/Runtime UI Config", fileName = "RuntimeUiConfig")]
public sealed class RuntimeUiConfig : ScriptableObject
{
    [SerializeField] private RuntimeUiMode mode = RuntimeUiMode.Canvas;

    public RuntimeUiMode Mode => mode;
    public bool UseUiToolkit => mode == RuntimeUiMode.UiToolkit;
}
