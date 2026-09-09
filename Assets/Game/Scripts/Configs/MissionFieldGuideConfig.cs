using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Configs
{
    public enum MissionGuideAvailability : byte { Friendly=0, Hostile=1, Protected=2, Reference=3, Unavailable=4 }
    [Serializable]
    public struct MissionGuideClass
    {
        public string Id, NameKey, CategoryKey;
        public AssetReferenceT<UnitGridAuthoringConfig> UnitAsset;
        public MissionGuideAvailability Availability;
        public static int GroundSensorRadius(UnitGridAuthoringConfig unit)=>unit!=null && unit.ThreatDetectionKind==Game.Components.ThreatDetectionKind.Ground ? unit.ThreatDetectionRadiusCells : 0;
        public static int AirSensorRadius(UnitGridAuthoringConfig unit)=>unit!=null && unit.ThreatDetectionKind==Game.Components.ThreatDetectionKind.Air ? unit.ThreatDetectionRadiusCells : 0;
    }
    [Serializable]
    public struct MissionGuideTopic
    {
        public string TitleKey, BodyKey, ExampleKey, MistakeKey, DiagramKey;
    }
    [CreateAssetMenu(menuName="Game/Campaign/Mission Field Guide")]
    public sealed class MissionFieldGuideConfig : ScriptableObject
    {
        [SerializeField] private MissionGuideTopic[] topics=Array.Empty<MissionGuideTopic>();
        [SerializeField] private MissionGuideClass[] classes=Array.Empty<MissionGuideClass>();
        public ReadOnlySpan<MissionGuideTopic> Topics=>topics;
        public ReadOnlySpan<MissionGuideClass> Classes=>classes;
        public void Configure(MissionGuideTopic[] newTopics,MissionGuideClass[] newClasses)
        { topics=newTopics ?? Array.Empty<MissionGuideTopic>(); classes=newClasses ?? Array.Empty<MissionGuideClass>(); }
    }
}
