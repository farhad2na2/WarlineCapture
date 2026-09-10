using System;
using UnityEngine;

namespace Game.UI.Contracts
{
    public enum UiMissionGuideAvailability : byte { Friendly, Hostile, Protected, Reference, Unavailable }
    public readonly struct UiMissionGuideClass
    {
        public readonly string Id, NameKey, CategoryKey, AssetKey;
        public readonly UiMissionGuideAvailability Availability;
        public UiMissionGuideClass(string id, string nameKey, string categoryKey, string assetKey, UiMissionGuideAvailability availability)
        { Id = id; NameKey = nameKey; CategoryKey = categoryKey; AssetKey = assetKey; Availability = availability; }
    }
    public readonly struct UiMissionGuideTopic
    {
        public readonly string TitleKey, BodyKey, ExampleKey, MistakeKey, DiagramKey;
        public UiMissionGuideTopic(string title, string body, string example, string mistake, string diagram)
        { TitleKey = title; BodyKey = body; ExampleKey = example; MistakeKey = mistake; DiagramKey = diagram; }
    }
    public sealed class UiMissionGuideCatalog
    {
        private readonly UiMissionGuideTopic[] topics;
        private readonly UiMissionGuideClass[] classes;
        public string Name { get; }
        public string RadioTextKey { get; }
        public ReadOnlySpan<UiMissionGuideTopic> Topics => topics;
        public ReadOnlySpan<UiMissionGuideClass> Classes => classes;
        public UiMissionGuideCatalog(string name, string radioTextKey, UiMissionGuideTopic[] topics, UiMissionGuideClass[] classes)
        { Name = name; RadioTextKey = radioTextKey; this.topics = topics; this.classes = classes; }
    }
    public sealed class UiMissionGuideUnitModel
    {
        public int MaxHealth, AttackDamage, GroundSensorRadius, AirSensorRadius, SoldierTransportCapacity, VehicleTransportCapacity;
        public float Speed, AttackRange;
        public bool CanAttack;
        public Sprite PortraitCardSprite, PortraitSprite;
    }
    public interface IUiMissionGuideSession : IDisposable
    {
        UiMissionGuideCatalog Catalog { get; }
        int ResidentClassCount { get; }
        bool ClassLoadPending { get; }
        bool ClassLoadFailed { get; }
        event Action Changed;
        UiMissionGuideUnitModel RequestUnit(in UiMissionGuideClass card);
        void ReleaseUnit();
    }
    public interface IUiMissionGuideContent
    {
        IUiMissionGuideSession Open(UnityEngine.Object authoredSource);
    }
}
