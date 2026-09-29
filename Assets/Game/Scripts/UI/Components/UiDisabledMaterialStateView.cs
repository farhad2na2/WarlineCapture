using System;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    internal sealed class UiDisabledMaterialStateView : MonoBehaviour
    {
        [NonSerialized] public Material OriginalMaterial;
        [NonSerialized] public Color OriginalColor;
        [NonSerialized] public UiDisabledVisualReason Reasons;
        [NonSerialized] public bool PreserveAuthoredVisual;
    }

}
