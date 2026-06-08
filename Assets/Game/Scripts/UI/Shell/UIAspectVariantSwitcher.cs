using UnityEngine;

public sealed class UIAspectVariantSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject standardRoot;
    [SerializeField] private GameObject wideRoot;
    [SerializeField] private GameObject[] standardOnlyObjects;
    [SerializeField] private GameObject[] wideOnlyObjects;
    [SerializeField] private float wideAspectThreshold = 2.05f;

    private Vector2 _lastSize;

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        RectTransform rect = transform as RectTransform;
        Vector2 currentSize = rect != null ? rect.rect.size : new Vector2(Screen.width, Screen.height);
        if (currentSize.x <= 0f || currentSize.y <= 0f)
            currentSize = new Vector2(Screen.width, Screen.height);

        if (currentSize == _lastSize)
            return;

        Refresh();
    }

    public void Refresh()
    {
        RectTransform rect = transform as RectTransform;
        Vector2 size = rect != null ? rect.rect.size : new Vector2(Screen.width, Screen.height);
        if (size.x <= 0f || size.y <= 0f)
            size = new Vector2(Screen.width, Screen.height);

        _lastSize = size;

        float aspect = size.y > 0f ? size.x / size.y : 0f;
        bool useWide = aspect >= wideAspectThreshold;
        if (standardRoot != null)
            standardRoot.SetActive(!useWide);
        if (wideRoot != null)
            wideRoot.SetActive(useWide);

        SetObjectsActive(standardOnlyObjects, !useWide);
        SetObjectsActive(wideOnlyObjects, useWide);
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }
}
