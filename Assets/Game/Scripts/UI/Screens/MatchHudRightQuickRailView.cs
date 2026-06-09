using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudRightQuickRailView : MonoBehaviour
{
    [SerializeField] private Button buildButton;

    public Button BuildButton => buildButton;
}
