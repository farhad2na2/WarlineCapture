using UnityEngine;

public sealed class CitizenVisualLifecycleReporter : MonoBehaviour
{
    public int CitizenId;
    public bool SuppressNotifyOnDestroy;
    private CitizenPopulationEventSystem _eventSystem;

    public void Bind(CitizenPopulationEventSystem eventSystem)
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
