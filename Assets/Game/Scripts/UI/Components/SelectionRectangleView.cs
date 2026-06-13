using UnityEngine;

[DisallowMultipleComponent]
public sealed class SelectionRectangleView : MonoBehaviour, ISelectionRectangleView
{
    [SerializeField] private Color selectionFill = new(0.2f, 1f, 0.2f, 0.15f);
    [SerializeField] private Color selectionBorder = new(0.2f, 1f, 0.2f, 0.95f);
    [SerializeField, Min(1f)] private float borderThickness = 2f;

    private ISelectionRectangleState _state;
    private Texture2D _pixel;

    public void BindState(ISelectionRectangleState state)
    {
        _state = state;
    }

    public void ApplyStyle(Color configuredSelectionFill, Color configuredSelectionBorder)
    {
        selectionFill = configuredSelectionFill;
        selectionBorder = configuredSelectionBorder;
    }

    public void Draw()
    {
        if (_state == null || !_state.TryRead(out SelectionRectangleStateModel state))
            return;

        EnsurePixel();
        DrawRectangle(state.ScreenRect);
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

}
