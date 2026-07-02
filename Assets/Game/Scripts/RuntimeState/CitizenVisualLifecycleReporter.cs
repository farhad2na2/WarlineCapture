using UnityEngine;

namespace Game.Runtime
{
    public sealed class CitizenVisualLifecycleReporter : MonoBehaviour
    {
        public int CitizenId;
        public bool SuppressNotifyOnDestroy;
        private CitizenPopulationEventCompositionSystemHelper _eventSystem;

        public void Bind(CitizenPopulationEventCompositionSystemHelper eventSystem)
        {
            _eventSystem = eventSystem;
        }

        private void OnDestroy()
        {
            if (SuppressNotifyOnDestroy)
                return;

            _eventSystem?.NotifyVisibleCitizenDestroyed(CitizenId);
        }
    }
}
