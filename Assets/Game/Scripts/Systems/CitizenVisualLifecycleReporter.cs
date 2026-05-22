using UnityEngine;

public sealed class CitizenVisualLifecycleReporter : MonoBehaviour
{
    public int CitizenId;
    public bool SuppressNotifyOnDestroy;
    private CitizenPopulationSystem _citizenPopulationSystem;

    public void Bind(CitizenPopulationSystem citizenPopulationSystem)
    {
        _citizenPopulationSystem = citizenPopulationSystem;
    }

    private void OnDestroy()
    {
        if (SuppressNotifyOnDestroy)
            return;

        _citizenPopulationSystem?.NotifyVisibleCitizenDestroyed(CitizenId);
    }
}
