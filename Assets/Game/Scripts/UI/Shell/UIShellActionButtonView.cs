using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIShellActionButtonView : MonoBehaviour
{
    [SerializeField] private UiActionKind actionKind;
    [SerializeField] private int payloadId;
    [SerializeField] private Button button;

    public UiActionKind ActionKind => actionKind;
    public int PayloadId => payloadId;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(EnqueueAction);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(EnqueueAction);
    }

    private void EnqueueAction()
    {
        UiShellRuntimeGateway.TryEnqueueUiAction(actionKind, payloadId);
    }
}
