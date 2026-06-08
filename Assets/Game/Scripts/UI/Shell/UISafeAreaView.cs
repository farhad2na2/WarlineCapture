using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class UISafeAreaView : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    private void Awake()
    {
        if (target == null)
            target = (RectTransform)transform;

        ApplySafeArea();
    }

    private void Update()
    {
        ApplySafeArea();
    }

    public void ApplySafeArea()
    {
        if (target == null)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new(Screen.width, Screen.height);
        if (safeArea == _lastSafeArea && screenSize == _lastScreenSize)
            return;

        _lastSafeArea = safeArea;
        _lastScreenSize = screenSize;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Mathf.Max(1, Screen.width);
        anchorMin.y /= Mathf.Max(1, Screen.height);
        anchorMax.x /= Mathf.Max(1, Screen.width);
        anchorMax.y /= Mathf.Max(1, Screen.height);

        target.anchorMin = anchorMin;
        target.anchorMax = anchorMax;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}
