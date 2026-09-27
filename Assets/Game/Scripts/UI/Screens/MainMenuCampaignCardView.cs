using System;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// The campaign card shows the brief comic of the furthest unlocked mission.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuCampaignCardView : MonoBehaviour
    {
        [Serializable]
        public struct MissionPlate
        {
            public string missionId;
            public Sprite plate;
        }

        [SerializeField] private Image art;
        [SerializeField] private MissionPlate[] plates = Array.Empty<MissionPlate>();
        private Sprite _fallback;
        private ushort _appliedMask = ushort.MaxValue;

        private void Awake()
        {
            if (art != null)
                _fallback = art.sprite;
        }

        private void OnEnable()
        {
            _appliedMask = ushort.MaxValue;
            ApplyLatestUnlocked();
        }

        private void LateUpdate() => ApplyLatestUnlocked();

        private void ApplyLatestUnlocked()
        {
            if (art == null)
                return;
            if (!UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign) || !campaign.IsValid)
                return;
            if (campaign.AvailableMissionMask == _appliedMask)
                return;

            _appliedMask = campaign.AvailableMissionMask;
            int count = Mathf.Min(CampaignMissionSequence.RegisteredMissionCount, 16);
            for (int index = count - 1; index >= 0; index--)
            {
                if ((_appliedMask & (1 << index)) == 0)
                    continue;
                Sprite plate = FindPlate(CampaignMissionSequence.IdAt(index));
                if (plate == null)
                    continue;
                art.sprite = plate;
                return;
            }

            if (_fallback != null)
                art.sprite = _fallback;
        }

        private Sprite FindPlate(string missionId)
        {
            if (string.IsNullOrEmpty(missionId) || plates == null)
                return null;
            for (int i = 0; i < plates.Length; i++)
                if (plates[i].plate != null && plates[i].missionId == missionId)
                    return plates[i].plate;
            return null;
        }
    }
}
