using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class WarlineCaptureIso2DCameraController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool enableKeyboardMouseControls = true;
    [SerializeField] private float panSpeed = 4f;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minimumZoom = 2.2f;
    [SerializeField] private float maximumZoom = 5.5f;

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }
    }

    private void Update()
    {
        if (!enableKeyboardMouseControls || targetCamera == null)
        {
            return;
        }

        var horizontal = 0f;
        var vertical = 0f;
        var scroll = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            horizontal += keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? -1f : 0f;
            horizontal += keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f;
            vertical += keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? -1f : 0f;
            vertical += keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f;
        }

        if (Mouse.current != null)
        {
            scroll = Mathf.Clamp(Mouse.current.scroll.ReadValue().y / 120f, -1f, 1f);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        scroll = Input.mouseScrollDelta.y;
#endif

        if (!Mathf.Approximately(horizontal, 0f) || !Mathf.Approximately(vertical, 0f))
        {
            transform.position += new Vector3(horizontal, vertical, 0f).normalized * (panSpeed * Time.deltaTime);
        }

        if (!Mathf.Approximately(scroll, 0f))
        {
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize - scroll * zoomSpeed * Time.deltaTime,
                minimumZoom,
                maximumZoom);
        }
    }

    public void Configure(Camera camera, float minZoom, float maxZoom, float speed, float zoom)
    {
        targetCamera = camera;
        minimumZoom = minZoom;
        maximumZoom = maxZoom;
        panSpeed = speed;
        zoomSpeed = zoom;
    }
}
