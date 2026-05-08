using UnityEngine;

public sealed class WarlineCaptureIso2DSorting : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private int baseSortingOrder = 1000;
    [SerializeField] private int orderPrecision = 100;
    [SerializeField] private int orderOffset;
    [SerializeField] private bool sortContinuously = true;

    public int CurrentSortingOrder => targetRenderer != null ? targetRenderer.sortingOrder : 0;

    private void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        ApplySorting();
    }

    private void LateUpdate()
    {
        if (sortContinuously)
        {
            ApplySorting();
        }
    }

    public void Configure(SpriteRenderer renderer, int baseOrder, int precision, int offset, bool continuous)
    {
        targetRenderer = renderer;
        baseSortingOrder = baseOrder;
        orderPrecision = precision;
        orderOffset = offset;
        sortContinuously = continuous;
        ApplySorting();
    }

    public void ApplySorting()
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.sortingOrder = baseSortingOrder + Mathf.RoundToInt(-transform.position.y * orderPrecision) + orderOffset;
    }
}
