using System;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Composition
{
    internal sealed partial class CampaignMissionMenuBootstrapRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void BindGuideContent() => UiShellRuntimeGateway.BindMissionGuideContent(new GuideContent());

        // Each visible guide owns exactly one residency session. Closing it releases the pending or loaded class.
        private sealed class GuideContent : IUiMissionGuideContent
        {
            public IUiMissionGuideSession Open(UnityEngine.Object authoredSource) =>
                authoredSource is MissionFieldGuideConfig config ? new GuideSession(config) : null;
        }

        private sealed class GuideSession : IUiMissionGuideSession
        {
            private AsyncOperationHandle<UnitGridAuthoringConfig> handle;
            private string key;
            private bool disposed;
            private UiMissionGuideUnitModel unit;
            public UiMissionGuideCatalog Catalog { get; }
            public int ResidentClassCount => handle.IsValid() ? 1 : 0;
            public bool ClassLoadPending => handle.IsValid() && !handle.IsDone;
            public bool ClassLoadFailed => handle.IsValid() && handle.IsDone && handle.Status != AsyncOperationStatus.Succeeded;
            public event Action Changed;

            internal GuideSession(MissionFieldGuideConfig config)
            {
                var topics = new UiMissionGuideTopic[config.Topics.Length];
                var classes = new UiMissionGuideClass[config.Classes.Length];
                for (int i = 0; i < topics.Length; i++)
                {
                    var source = config.Topics[i];
                    topics[i] = new UiMissionGuideTopic(source.TitleKey, source.BodyKey, source.ExampleKey, source.MistakeKey, source.DiagramKey);
                }
                for (int i = 0; i < classes.Length; i++)
                {
                    var source = config.Classes[i];
                    string assetKey = source.UnitAsset != null && source.UnitAsset.RuntimeKeyIsValid() ? source.UnitAsset.AssetGUID : null;
                    classes[i] = new UiMissionGuideClass(source.Id, source.NameKey, source.CategoryKey, assetKey, (UiMissionGuideAvailability)source.Availability);
                }
                Catalog = new UiMissionGuideCatalog(config.name, M03RadarWarningCopyCatalog.Comms[0].Key, topics, classes);
            }

            public UiMissionGuideUnitModel RequestUnit(in UiMissionGuideClass card)
            {
                if (disposed) return null;
                if (card.AssetKey != key)
                {
                    ReleaseUnit();
                    key = card.AssetKey;
                    if (!string.IsNullOrEmpty(key))
                    {
                        handle = Addressables.LoadAssetAsync<UnitGridAuthoringConfig>(key);
                        handle.Completed += Ready;
                    }
                }
                return unit;
            }

            private void Ready(AsyncOperationHandle<UnitGridAuthoringConfig> completed)
            {
                if (disposed || !handle.IsValid() || !completed.Equals(handle)) return;
                if (completed.Status == AsyncOperationStatus.Succeeded && completed.Result != null)
                {
                    var source = completed.Result;
                    unit = new UiMissionGuideUnitModel
                    {
                        MaxHealth = source.MaxHealth, Speed = source.Speed, CanAttack = source.CanAttack,
                        AttackDamage = source.AttackDamage, AttackRange = source.AttackRange,
                        GroundSensorRadius = MissionGuideClass.GroundSensorRadius(source), AirSensorRadius = MissionGuideClass.AirSensorRadius(source),
                        SoldierTransportCapacity = source.SoldierTransportCapacity, VehicleTransportCapacity = source.VehicleTransportCapacity,
                        PortraitCardSprite = source.PortraitCardSprite, PortraitSprite = source.PortraitSprite
                    };
                }
                Changed?.Invoke();
            }

            public void ReleaseUnit()
            {
                if (handle.IsValid()) { handle.Completed -= Ready; Addressables.Release(handle); }
                handle = default; key = null; unit = null;
            }
            public void Dispose() { if (disposed) return; disposed = true; Changed = null; ReleaseUnit(); }
        }
    }
}
