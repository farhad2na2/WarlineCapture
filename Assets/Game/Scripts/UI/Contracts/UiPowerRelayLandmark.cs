using UnityEngine;
namespace Game.UI.Contracts
{
    public interface IUiPowerRelayLandmarkGateway
    {
        bool TryReadPowerRelayShelter(out Vector3 center,out bool sheltered);
    }
}
