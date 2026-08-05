using Game.Components;
using UnityEngine;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class OperationMapAuthoredVehicleOwnershipAuthoring : MonoBehaviour
    {
        [SerializeField] private byte factionId = FactionIdentity.NeutralFactionId;

        public byte FactionId => factionId;

#if UNITY_EDITOR
        public void ConfigureForEditor(byte ownerFactionId)
        {
            factionId = ownerFactionId;
        }
#endif
    }
}
