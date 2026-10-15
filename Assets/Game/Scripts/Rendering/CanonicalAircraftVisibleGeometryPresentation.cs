using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEngine;
namespace Game.Rendering
{
    public static class CanonicalAircraftVisibleGeometryPresentation
    {
        public static bool TryRead(EntityManager em,Entity actor,Entity draw,out Bounds bounds,out bool hasDraw,out string failure)
        {
            bounds=default;hasDraw=false;failure="CanonicalAircraftActorSourceUnavailable";
            if(!em.Exists(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||!em.HasComponent<UnitSourcePrefabKey>(actor)||
                !em.HasComponent<UnitRenderVisualComponent>(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1)return false;
            var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);var visual=em.GetComponentData<UnitRenderVisualComponent>(actor);
            if(role.Category!=SkirmishPopulationCategory.Air||visual.Current!=visual.Desired)return false;
            string key=em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString();
            if(role.Role==SkirmishRoleKind.TransportPlane)
            {
                if(key!="Unit_Veh_Plane_Transport")return false;
                return RigidAircraftVisibleGeometryPresentation.TryRead(em,draw,out bounds,out hasDraw,out failure);
            }
            var catalog=SkirmishCanonicalAircraftGeometryConfig.Load();
            if(catalog==null||!catalog.TryRole(role.Role,key,visual.Current,out var metadata))return false;
            Entity root=Entity.Null;
            if(visual.Current==(byte)UnitRenderVisualKind.Detail&&em.HasComponent<UnitDetailedVisualReference>(actor))root=em.GetComponentData<UnitDetailedVisualReference>(actor).Root;
            if(visual.Current==(byte)UnitRenderVisualKind.Mid&&em.HasComponent<UnitMidLodInstanceReference>(actor))root=em.GetComponentData<UnitMidLodInstanceReference>(actor).Instance;
            if(visual.Current==(byte)UnitRenderVisualKind.Low&&em.HasComponent<UnitLowLodInstanceReference>(actor))root=em.GetComponentData<UnitLowLodInstanceReference>(actor).Instance;
            if(root==Entity.Null)return false;
            var context=new SkirmishAircraftRotorDrawPresentation(em,actor,draw,root,role.Role,visual.Current,metadata);
            return RigidAircraftVisibleGeometryPresentation.TryRead(em,draw,context,out bounds,out hasDraw,out failure);
        }
    }
}
