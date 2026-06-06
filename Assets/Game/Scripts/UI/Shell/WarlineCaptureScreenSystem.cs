using UnityEngine;

public class WarlineCaptureScreenSystem : MonoBehaviour
{
    [SerializeField] private WarlineCaptureRoute route;

    public WarlineCaptureRoute Route => route;
    public bool IsVisible => gameObject.activeSelf;

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetRouteForTests(WarlineCaptureRoute value)
    {
        route = value;
    }
}
