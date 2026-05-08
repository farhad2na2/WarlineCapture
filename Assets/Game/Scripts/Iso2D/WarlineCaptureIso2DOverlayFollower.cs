using UnityEngine;

public sealed class WarlineCaptureIso2DOverlayFollower : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private SpriteRenderer overlayRenderer;
    [SerializeField] private int sortingOrder = 5000;

    public Transform Target => target;
    public Vector3 Offset => offset;

    private void Reset()
    {
        overlayRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (overlayRenderer == null)
        {
            overlayRenderer = GetComponent<SpriteRenderer>();
        }

        ApplyFollow();
    }

    private void LateUpdate()
    {
        ApplyFollow();
    }

    public void Configure(Transform followTarget, Vector3 followOffset, SpriteRenderer renderer, int order)
    {
        target = followTarget;
        offset = followOffset;
        overlayRenderer = renderer;
        sortingOrder = order;
        ApplyFollow();
    }

    public void ApplyFollow()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }

        if (overlayRenderer != null)
        {
            overlayRenderer.sortingOrder = sortingOrder;
        }
    }
}
