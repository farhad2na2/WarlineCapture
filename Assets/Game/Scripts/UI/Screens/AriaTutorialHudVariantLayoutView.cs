using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>Compatibility component for old POP13 prefabs; guidance is now owned by the permanent HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class AriaTutorialHudVariantLayoutView : MonoBehaviour
    {
        public void RefreshLayout() { }
        public void RestoreLayout() { }
    }
}
