using UnityEngine;

public sealed class CitizenVisualLifecycleReporter : MonoBehaviour
{
    public int CitizenId;
    public bool SuppressNotifyOnDestroy;

    private void OnDestroy()
    {
        if (SuppressNotifyOnDestroy)
            return;

        CitizenPopulationSystem.Instance?.NotifyVisibleCitizenDestroyed(CitizenId);
    }
}
