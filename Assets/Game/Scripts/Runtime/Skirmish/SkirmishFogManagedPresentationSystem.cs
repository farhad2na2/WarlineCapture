using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial class SkirmishFogManagedPresentationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            using var query = EntityManager.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using var contacts = query.ToEntityArray(Allocator.Temp);
            foreach (var contact in contacts)
            {
                if (EntityManager.HasComponent<SkirmishVisualInstanceRecord>(contact))
                    Bind(EntityManager.GetComponentObject<SkirmishVisualInstanceRecord>(contact)?.Instance, contact);
                if (EntityManager.HasComponent<SkirmishNativeBuildingCheckpointRecord>(contact))
                    Bind(EntityManager.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(contact)?.Building?.Instance, contact);
            }
        }
        private void Bind(GameObject instance, Entity contact)
        {
            if (instance == null) return;
            var mask = instance.GetComponent<SkirmishFogManagedVisualMask>();
            if (mask == null) mask = instance.AddComponent<SkirmishFogManagedVisualMask>();
            mask.Bind(World, contact);
        }
    }
}
