namespace Game.UI.Contracts
{
    public interface ISelectionRectangleView
    {
        void ApplyStyle(UnityEngine.Color selectionFill, UnityEngine.Color selectionBorder);
        void Draw();
    }
}
