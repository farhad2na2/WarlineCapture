using UnityEngine;

public class UIScreenSystem : MonoBehaviour
{
    [SerializeField] private UIRoute route;

    public UIRoute Route => route;
    public bool IsVisible => gameObject.activeSelf;

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetRouteForTests(UIRoute value)
    {
        route = value;
    }
}
