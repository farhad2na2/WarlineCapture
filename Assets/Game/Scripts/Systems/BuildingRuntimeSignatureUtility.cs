using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal static class BuildingRuntimeSignatureUtility
    {
        public static int AddEntity(int hash, Entity entity)
        {
            hash = (hash * 31) + entity.Index;
            return (hash * 31) + entity.Version;
        }

        public static int AddUnityObject(int hash, UnityEngine.Object unityObject) =>
            (hash * 31) + (unityObject != null ? unityObject.GetEntityId().GetHashCode() : 0);

        public static int AddFloat(int hash, float value) =>
            (hash * 31) + Mathf.RoundToInt(value * 1000f);
    }
}
