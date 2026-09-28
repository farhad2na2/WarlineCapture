using System;
using UnityEngine;
namespace Game.Configs
{
    [CreateAssetMenu(menuName="Game/Support/Mission Policies")]
    public sealed class SupportMissionPolicyConfig : ScriptableObject
    {
        public SupportMissionPolicyEntry[] Missions = Array.Empty<SupportMissionPolicyEntry>();
        public bool TryResolve(string id, out SupportMissionPolicyEntry policy)
        {
            foreach(var entry in Missions) if(entry.MissionId==id || id.StartsWith("saga."+entry.MissionId.ToLowerInvariant().Replace("-", ".")+".", StringComparison.Ordinal)) { policy=entry; return true; }
            policy=default; return false;
        }
    }
    [Serializable]
    public struct SupportMissionPolicyEntry
    { public string MissionId; public byte AllowedMask; public Game.Components.SupportAbilityKind LessonKind; public int PopulationCeiling; }
}
