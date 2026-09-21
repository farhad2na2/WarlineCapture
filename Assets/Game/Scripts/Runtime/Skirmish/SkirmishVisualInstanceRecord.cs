using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class SkirmishVisualInstanceRecord : IComponentData
    {
        public GameObject Instance;
        public string PrefabKey;
    }

    public sealed class SkirmishVisualPrefabCatalogRecord : IComponentData
    {
        public SkirmishVisualPrefabCatalog Catalog;
    }
}
