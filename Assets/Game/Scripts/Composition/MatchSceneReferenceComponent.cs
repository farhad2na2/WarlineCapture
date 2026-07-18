using Unity.Entities;

namespace Game.Composition
{
    public struct MatchSceneReferenceComponent : IComponentData
    {
        public UnityObjectRef<MatchSceneView> View;
    }
}
