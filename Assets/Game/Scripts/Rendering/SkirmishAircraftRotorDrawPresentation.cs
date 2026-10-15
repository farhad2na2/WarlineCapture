using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Rendering.Contracts;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Rendering
{
    // One actual draw context; no global mutable actor cache or runtime mesh reads.
    public sealed class SkirmishAircraftRotorDrawPresentation : IRigidAircraftGeometryDrawLookup, IRigidAircraftWorldDrawLookup
    {
        readonly EntityManager em;
        readonly Entity actor,renderer,root;
        readonly SkirmishRoleKind role;
        readonly byte visual;
        readonly SkirmishCanonicalAircraftGeometryConfig.Entry geometry;
        readonly SkirmishAircraftRotorGeometryConfig rotorOverride;
        public SkirmishAircraftRotorDrawPresentation(EntityManager manager,Entity owner,Entity draw,Entity visualRoot,
            SkirmishRoleKind nativeRole,byte currentVisual,SkirmishCanonicalAircraftGeometryConfig.Entry indexedGeometry,
            SkirmishAircraftRotorGeometryConfig explicitRotorMetadata=null)
        {em=manager;actor=owner;renderer=draw;root=visualRoot;role=nativeRole;visual=currentVisual;geometry=indexedGeometry;rotorOverride=explicitRotorMetadata;}
        public bool TryRead(Mesh mesh,int subMesh,out SkirmishRigidAircraftGeometryConfig.DrawSnapshot draw)
        {draw=default;return geometry!=null && geometry.TryRead(mesh,subMesh,out draw);}
        public bool TryReadWorld(Mesh mesh,int subMesh,in SkirmishRigidAircraftGeometryConfig.DrawSnapshot draw,
            Matrix4x4 rendererToWorld,out Bounds bounds,out string failure)
        {
            bounds=default;failure="RotorDrawOwnerOrHierarchyUnavailable";
            if(!em.Exists(actor)||!em.Exists(renderer)||!em.Exists(root)||!em.HasBuffer<UnitHelicopterBladeReference>(actor))return false;
            if(!em.HasComponent<UnitRenderVisualComponent>(actor))return false;
            var state=em.GetComponentData<UnitRenderVisualComponent>(actor);
            Entity currentRoot=visual==1&&em.HasComponent<UnitDetailedVisualReference>(actor)?em.GetComponentData<UnitDetailedVisualReference>(actor).Root:
                visual==2&&em.HasComponent<UnitMidLodInstanceReference>(actor)?em.GetComponentData<UnitMidLodInstanceReference>(actor).Instance:
                visual==3&&em.HasComponent<UnitLowLodInstanceReference>(actor)?em.GetComponentData<UnitLowLodInstanceReference>(actor).Instance:Entity.Null;
            if(state.Current!=visual||state.Desired!=visual||currentRoot!=root)return false;
            var blades=em.GetBuffer<UnitHelicopterBladeReference>(actor,true);
            int expected=role==SkirmishRoleKind.Drone?1:SkirmishAircraftRotorGeometryConfig.HasRotors(role)?2:0;
            if(blades.Length!=expected)return false;
            var axes=new Dictionary<Entity,byte>();
            foreach(var b in blades)
            {
                if(b.Axis>2||b.Blade==Entity.Null||!em.Exists(b.Blade)||!axes.TryAdd(b.Blade,b.Axis))return false;
            }
            if(visual!=1 && expected>0)
            {
                // Actual normal model spawn creates independent Mid/Low instances.
                // The actor's baked buffer still names Detail blades only; its
                // presence excludes the legacy hierarchy-scanning spin path.
                if(!em.HasComponent<UnitDetailedVisualReference>(actor))return false;
                Entity detail=em.GetComponentData<UnitDetailedVisualReference>(actor).Root;
                foreach(var b in blades)
                    if(!OwnedDescendant(b.Blade,detail)||OwnedDescendant(b.Blade,root))
                    {failure="FixedLodRequiresDisjointOwnedDetailBladeBuffer";return false;}
            }
            Entity node=renderer,blade=Entity.Null;
            Matrix4x4 meshToBlade=Matrix4x4.identity,meshToNode=Matrix4x4.identity;
            var ancestry=new List<(Entity node,Matrix4x4 rendererToNode)>();
            var seen=new HashSet<Entity>();bool reachedRoot=false;
            while(node!=Entity.Null && node!=actor)
            {
                if(!seen.Add(node)||seen.Count>128||!em.Exists(node)||em.HasComponent<PostTransformMatrix>(node)||
                    !em.HasComponent<LocalTransform>(node)||em.HasComponent<Disabled>(node)||em.HasComponent<Prefab>(node))return false;
                var local=em.GetComponentData<LocalTransform>(node);
                if(!math.all(math.isfinite(local.Position))||!math.all(math.isfinite(local.Rotation.value))||
                    !float.IsFinite(local.Scale)||local.Scale<=0)return false;
                ancestry.Add((node,meshToNode));
                if(axes.ContainsKey(node))
                {
                    if(blade!=Entity.Null){failure="NestedAircraftSpinnersUnsupported";return false;}
                    blade=node;meshToBlade=meshToNode;
                }
                meshToNode=Matrix4x4.TRS(local.Position,local.Rotation,Vector3.one*local.Scale)*meshToNode;
                if(node==root)reachedRoot=true;
                if(!em.HasComponent<Parent>(node))return false;
                node=em.GetComponentData<Parent>(node).Value;
            }
            if(node!=actor||!reachedRoot){failure="AircraftDrawOutsideCurrentOwnedRoot";return false;}
            var metadata=rotorOverride!=null?rotorOverride:SkirmishAircraftRotorGeometryConfig.Load();
            if(SkirmishAircraftRotorGeometryConfig.HasRotors(role)&&(metadata==null||!metadata.Prepare()))
            {failure="MeasuredRotorMetadataUnavailable";return false;}
            if(blade==Entity.Null)
            {
                if(metadata!=null&&metadata.HasRotorDraw(role,visual,mesh,subMesh,draw.GeometrySha256))
                {
                    if(visual==1||!em.HasComponent<LocalToWorld>(root))
                    {failure="MeasuredRotorDrawMissingActualBladeReference";return false;}
                    Matrix4x4 rootWorld=em.GetComponentData<LocalToWorld>(root).Value;
                    if(!SkirmishAircraftRotorGeometryConfig.Affine(rootWorld)||
                        !metadata.TryReadFixedLod(role,visual,mesh,subMesh,draw.GeometrySha256,rootWorld.inverse*rendererToWorld,out var fixedBinding))
                    {failure="FixedLodSourceRendererRelationUnavailable";return false;}
                    int matches=0;Entity pivot=Entity.Null;
                    foreach(var parent in ancestry)
                    {
                        if(!em.HasComponent<LocalToWorld>(parent.node)||!SkirmishAircraftRotorGeometryConfig.Approximately(parent.rendererToNode,fixedBinding.MeshToBlade))continue;
                        Matrix4x4 world=em.GetComponentData<LocalToWorld>(parent.node).Value;
                        if(!SkirmishAircraftRotorGeometryConfig.Approximately(rootWorld.inverse*world,fixedBinding.BladeToVisualRoot))continue;
                        pivot=parent.node;matches++;
                    }
                    if(matches!=1){failure="FixedLodMeasuredPivotAncestryAmbiguous";return false;}
                    if(!SourceBound(fixedBinding)){failure="RotorIndexedGeometrySourceDependencyMismatch";return false;}
                    var fixedExtent=new SkirmishRigidRotorExtent(fixedBinding.Axis,fixedBinding.MinimumAxial,fixedBinding.MaximumAxial,fixedBinding.MaximumRadius);
                    Matrix4x4 pivotWorld=em.GetComponentData<LocalToWorld>(pivot).Value;
                    // Conservative full rotation also contains this verified fixed
                    // pose. This does not claim Mid/Low animation is active.
                    if(!SkirmishRigidRotorSweep.TryWorldEnvelope(fixedExtent,pivotWorld,out bounds))return false;
                    failure=null;return true;
                }
                // Ordinary body/door/gun parts use the actual current renderer
                // transform and measured indexed bounds. No static pose is assumed.
                bool found=false;
                for(int i=0;i<8;i++)
                {
                    Vector3 p=rendererToWorld.MultiplyPoint3x4(draw.LocalBounds.center+Vector3.Scale(draw.LocalBounds.extents,
                        new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))return false;
                    if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                }
                failure=null;return found;
            }
            // Spin uses q=q*localAxisDelta. The complete cylinder is invariant to
            // that local-axis angle, so the live blade L2W is the correct outer B.
            // Descendant matrices must still match their measured fixed relation.
            if(metadata==null||!em.HasComponent<LocalToWorld>(blade)||
                !metadata.TryRead(role,visual,mesh,subMesh,axes[blade],meshToBlade,draw.GeometrySha256,out var binding))
            {failure="MeasuredRotorPivotOrFixedDescendantRelationUnavailable";return false;}
            if(!SourceBound(binding)){failure="RotorIndexedGeometrySourceDependencyMismatch";return false;}
            Matrix4x4 bladeToWorld=em.GetComponentData<LocalToWorld>(blade).Value;
            Matrix4x4 expectedWorld=bladeToWorld*meshToBlade;
            if(!SkirmishAircraftRotorGeometryConfig.Approximately(expectedWorld,rendererToWorld))
            {failure="RotorCurrentTransformPublicationMismatch";return false;}
            var extent=new SkirmishRigidRotorExtent(binding.Axis,binding.MinimumAxial,binding.MaximumAxial,binding.MaximumRadius);
            if(!SkirmishRigidRotorSweep.TryWorldEnvelope(extent,bladeToWorld,out bounds))
            {failure="MeasuredRotorWorldEnvelopeInvalid";return false;}
            failure=null;return true;
        }
        bool SourceBound(in SkirmishAircraftRotorGeometryConfig.Snapshot binding)
        {foreach(var source in geometry.Sources)if(source.VisualKind==visual&&source.Path==binding.SourcePath&&source.DependencyHash==binding.DependencyHash)return true;return false;}
        bool OwnedDescendant(Entity child,Entity expectedRoot)
        {
            if(expectedRoot==Entity.Null||!em.Exists(expectedRoot))return false;var seen=new HashSet<Entity>();bool reached=false;
            while(child!=actor)
            {
                if(child==Entity.Null||!seen.Add(child)||seen.Count>128||!em.Exists(child)||em.HasComponent<Prefab>(child)||!em.HasComponent<Parent>(child))return false;
                if(child==expectedRoot)reached=true;child=em.GetComponentData<Parent>(child).Value;
            }
            return reached;
        }
    }
}
