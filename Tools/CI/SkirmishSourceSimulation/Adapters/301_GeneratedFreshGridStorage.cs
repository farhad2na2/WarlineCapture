using System;using Game.Components;using Unity.Entities;
namespace Game.Runtime{internal static class GeneratedFreshGridStorage{
 internal static void Initialize(EntityManager em,Entity gridEntity){var g=em.GetComponentData<GridConfig>(gridEntity);long size=(long)g.Width*g.Height;if(size<1||size>4194304)throw new NotSupportedException("Source complete finite grid required");
 if(!em.HasComponent<DynamicBlockerComponent>(gridEntity))em.AddComponentData(gridEntity,default(DynamicBlockerComponent));
 if(!em.HasComponent<DynamicOccupancyComponent>(gridEntity))em.AddComponentData(gridEntity,default(DynamicOccupancyComponent));
 if(!em.HasComponent<PathPoolComponent>(gridEntity))em.AddComponentData(gridEntity,default(PathPoolComponent));
 RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(em,gridEntity,(int)size);
 }
}}
