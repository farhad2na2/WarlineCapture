using UnityEngine;

public class UIScreenView : MonoBehaviour
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
