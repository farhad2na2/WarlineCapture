using System;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    internal sealed class UiDisabledSelectableVisualStateView : MonoBehaviour
    {
        [NonSerialized] public ColorBlock OriginalColors;
        [NonSerialized] public UiDisabledVisualReason Reasons;
    }

}
