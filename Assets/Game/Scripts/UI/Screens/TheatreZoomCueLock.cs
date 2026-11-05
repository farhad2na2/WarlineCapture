using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Reapplies the zoom-cue size after LibraryPass, a layout group, and the
    /// localization fitter. Those run in the same frame and were putting the
    /// Farsi label back at about 12px, on top of the district name.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    public sealed class TheatreZoomCueLock : MonoBehaviour
    {
        private void LateUpdate()
        {
            QuickCustomScreenView.HoldZoomCuePaint(GetComponent<TMP_Text>());
        }
    }
}
