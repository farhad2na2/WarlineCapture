using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SelectionRectangleView : MonoBehaviour, ISelectionRectangleView
{
    [SerializeField] private Color selectionFill = new(0.2f, 1f, 0.2f, 0.15f);
    [SerializeField] private Color selectionBorder = new(0.2f, 1f, 0.2f, 0.95f);
    [SerializeField, Min(1f)] private float borderThickness = 2f;

    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RtsSelectionInputStateSystem _inputStateSystem = new();
    private Texture2D _pixel;

    public void ApplyConfig(RTSSelectionSystemConfig config)
    {
        if (config == null)
            return;

        selectionFill = config.SelectionFill;
        selectionBorder = config.SelectionBorder;
    }

    public void Draw()
    {
        if (!_runtimeGameplayStateSystem.PlayRequested)
            return;

        if (!_inputStateSystem.TryRead(out _, out RtsSelectionInputStateComponent state))
            return;

        bool canDrawSelectionRect = _runtimeGameplayStateSystem.SelectionModeActive ||
                                    (TacticalCommandMode)state.ActiveCommandMode == TacticalCommandMode.Board;
        if (!canDrawSelectionRect)
            return;

        if (state.HasLiveSelectionRect == 0)
            return;

        EnsurePixel();
        DrawRectangle(ToGuiRect(state.LastLiveSelectionRect));
    }

    private void OnDestroy()
    {
        if (_pixel == null)
            return;

        Destroy(_pixel);
        _pixel = null;
    }

    private void EnsurePixel()
    {
        if (_pixel != null)
            return;

        _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _pixel.SetPixel(0, 0, Color.white);
        _pixel.Apply();
    }

    private void DrawRectangle(Rect rect)
    {
        DrawRect(rect, selectionFill);
        DrawBorder(rect, borderThickness, selectionBorder);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, _pixel);
        GUI.color = previous;
    }

    private void DrawBorder(Rect rect, float thickness, Color color)
    {
        DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    }

    private static Rect ToGuiRect(float4 screenRect)
    {
        var rect = Rect.MinMaxRect(screenRect.x, screenRect.y, screenRect.z, screenRect.w);
        rect.y = Screen.height - rect.yMax;
        return rect;
    }
}
