using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class SkirmishAircraftRootProvenance
    {
        // Callable from an explicit checked preparation, or once from the owned
        // normal startup observer after a real World exists. Never streams or
        // instantiates a source just to fabricate a positive baked observation.
        public static void Collect(){CollectExistingWorld(World.DefaultGameObjectInjectionWorld);}
        public static string CollectExistingWorld(World world)
        {
            var catalog=AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>("Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishRoleCatalog_V1.asset");
            var registry=AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>("Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset");
            if(catalog==null||registry==null||EditorUtility.IsDirty(catalog)||EditorUtility.IsDirty(registry))throw new InvalidOperationException("Clean actual catalog/registry required");
            var airKinds=new[]{SkirmishRoleKind.TransportHeli,SkirmishRoleKind.AttackHeliLight,SkirmishRoleKind.AttackHeli,SkirmishRoleKind.Drone,SkirmishRoleKind.Fighter,SkirmishRoleKind.Strike,SkirmishRoleKind.TransportPlane};
            var bindings=catalog.Roles.Where(r=>airKinds.Contains(r.Kind)).ToArray();if(bindings.Length!=7||bindings.Select(r=>r.Kind).Distinct().Count()!=7)throw new InvalidOperationException("Exactly seven canonical air roles required");
            var keys=new HashSet<string>(bindings.Select(r=>r.RuntimePrefabKey),StringComparer.Ordinal);
            var sources=new JArray();
            foreach(var binding in bindings)
            {
                var prefabs=registry.UnitSpawnPrefabs.Where(p=>p!=null&&p.name==binding.RuntimePrefabKey).ToArray();if(prefabs.Length!=1)throw new InvalidOperationException("Unique canonical source required");
                var prefab=prefabs[0];var authoring=prefab.GetComponent<UnitGridAuthoring>();if(authoring==null)throw new InvalidOperationException("Actual authoring missing");
                var property=new SerializedObject(authoring).FindProperty("modelRoot");var explicitRoot=property.objectReferenceValue as Transform;
                var resolved=explicitRoot!=null?explicitRoot:prefab.transform.Find("Model");
                var row=new JObject{["role"]=binding.RoleId,["runtimePrefabKey"]=binding.RuntimePrefabKey,["prefab"]=AssetDatabase.GetAssetPath(prefab),["dependencyHash"]=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(prefab)).ToString(),
                    ["explicitRoot"]=ObjectFact(explicitRoot),["resolvedRoot"]=ObjectFact(resolved),["resolvedRootOwnedByPrefab"]=resolved!=null&&resolved.IsChildOf(prefab.transform),
                    ["allPrefabRenderers"]=Renderers(prefab.transform),["resolvedRootRenderers"]=resolved!=null?Renderers(resolved):new JArray(),["allPrefabBladeCandidates"]=Blades(prefab.transform),["resolvedRootBladeCandidates"]=resolved!=null?Blades(resolved):new JArray(),["midBladeCandidates"]=authoring.MidLodPrefab!=null?Blades(authoring.MidLodPrefab.transform):new JArray(),["lowBladeCandidates"]=authoring.LowLodPrefab!=null?Blades(authoring.LowLodPrefab.transform):new JArray(),["midSource"]=ObjectFact(authoring.MidLodPrefab),["midRenderers"]=authoring.MidLodPrefab!=null?Renderers(authoring.MidLodPrefab.transform):new JArray(),["lowSource"]=ObjectFact(authoring.LowLodPrefab),["lowRenderers"]=authoring.LowLodPrefab!=null?Renderers(authoring.LowLodPrefab.transform):new JArray()};
                row["importerDependencyGraphs"]=new JArray(
                    DependencyGraph("Detail",prefab),
                    DependencyGraph("Mid",authoring.MidLodPrefab),
                    DependencyGraph("Low",authoring.LowLodPrefab));
                sources.Add(row);
            }
            var baked=new JArray();bool nativeAvailable=false;
            if(world!=null&&world.IsCreated)
            {
                var em=world.EntityManager;em.CompleteAllTrackedJobs();using var query=em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));using var owners=query.ToEntityArray(Allocator.Temp);
                nativeAvailable=owners.Length>0;
                foreach(var owner in owners)foreach(var entry in em.GetBuffer<UnitPrefabRegistryEntry>(owner,true))
                {
                    var actor=entry.Prefab;if(!em.Exists(actor)||!em.HasComponent<UnitSourcePrefabKey>(actor))continue;
                    string key=em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString();if(!keys.Contains(key))continue;
                    Entity root=em.HasComponent<UnitDetailedVisualReference>(actor)?em.GetComponentData<UnitDetailedVisualReference>(actor).Root:Entity.Null;
                    var graph=new HashSet<Entity>();var queue=new Queue<Entity>();if(root!=Entity.Null)queue.Enqueue(root);
                    while(queue.Count>0)
                    {
                        var node=queue.Dequeue();if(!graph.Add(node))continue;if(graph.Count>4096||!em.Exists(node))throw new InvalidOperationException("Actual baked root graph invalid");
                        if(em.HasBuffer<Child>(node))foreach(var child in em.GetBuffer<Child>(node,true))queue.Enqueue(child.Value);
                        if(em.HasBuffer<LinkedEntityGroup>(node))foreach(var child in em.GetBuffer<LinkedEntityGroup>(node,true))queue.Enqueue(child.Value);
                    }
                    var blades=new JArray();if(em.HasBuffer<UnitHelicopterBladeReference>(actor))foreach(var blade in em.GetBuffer<UnitHelicopterBladeReference>(actor,true))blades.Add(new JObject{["entity"]=EntityFact(blade.Blade),["exists"]=em.Exists(blade.Blade),["insideDetailedRootGraph"]=graph.Contains(blade.Blade),["axis"]=blade.Axis,["node"]=NodeFact(em,blade.Blade),["descendants"]=GraphFacts(em,blade.Blade),["parentChain"]=ParentFacts(em,blade.Blade),["insideActorLinkedEntityGroup"]=InsideLinked(em,actor,blade.Blade)});
                    baked.Add(new JObject{["key"]=key,["prefabActor"]=EntityFact(actor),["sourceTemplate"]=em.HasComponent<Prefab>(actor),["detailRoot"]=EntityFact(root),["detailGraphNodes"]=graph.Count,["detailedGraph"]=GraphFacts(em,root),["actorLinkedGraph"]=GraphFacts(em,actor),["modelInstanceGraph"]=GraphFacts(em,em.HasComponent<UnitModelInstanceReference>(actor)?em.GetComponentData<UnitModelInstanceReference>(actor).Instance:Entity.Null),["midPrefabGraph"]=GraphFacts(em,em.HasComponent<UnitMidLodPrefabReference>(actor)?em.GetComponentData<UnitMidLodPrefabReference>(actor).Prefab:Entity.Null),["lowPrefabGraph"]=GraphFacts(em,em.HasComponent<UnitLowLodPrefabReference>(actor)?em.GetComponentData<UnitLowLodPrefabReference>(actor).Prefab:Entity.Null),["midInstanceGraph"]=GraphFacts(em,em.HasComponent<UnitMidLodInstanceReference>(actor)?em.GetComponentData<UnitMidLodInstanceReference>(actor).Instance:Entity.Null),["lowInstanceGraph"]=GraphFacts(em,em.HasComponent<UnitLowLodInstanceReference>(actor)?em.GetComponentData<UnitLowLodInstanceReference>(actor).Instance:Entity.Null),["bladeReferenceBufferPresent"]=em.HasBuffer<UnitHelicopterBladeReference>(actor),["bladeReferenceCount"]=blades.Count,["noBladeReferencesObserved"]=blades.Count==0,["bladeReferences"]=blades,["runtimeActorOrDepartureAcceptance"]=false});
                }
            }
            var report=new JObject{["scope"]="ReadOnlyAllSevenCanonicalAircraftRootBladeDrawBindings",["expectedRoles"]=7,["sourceRoleCount"]=sources.Count,["bakedRowCount"]=baked.Count,["sources"]=sources,["bakedRegistryAvailable"]=nativeAvailable,["bakedRows"]=baked,["worldSequence"]=world!=null&&world.IsCreated?(long)world.SequenceNumber:0,["worldName"]=world!=null&&world.IsCreated?world.Name:null,["worldOwnedOrCreatedByCollector"]=false,["cameraAccepted"]=false,["sourceFitCertified"]=false};
            string folder=Path.GetFullPath("Logs/SkirmishProduction/AircraftRootProvenance");Directory.CreateDirectory(folder);string output=Path.Combine(folder,Guid.NewGuid().ToString("N")+".json");
            using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream))writer.Write(report.ToString());
            Debug.Log("[AircraftRootProvenance] result=Collected output="+output+" bakedRegistryAvailable="+(nativeAvailable?1:0)+" cameraAccepted=0");
            return output;
        }
        // Actual Unity importer graph, not a YAML GUID approximation. Byte pins
        // describe this collection only; they do not certify runtime geometry.
        static JObject DependencyGraph(string lod,GameObject source)
        {
            string path=source!=null?AssetDatabase.GetAssetPath(source):null;
            var row=new JObject{["lod"]=lod,["sourcePresent"]=source!=null,["rootAssetPath"]=path,["captured"]=false,["dependencies"]=new JArray()};
            if(source==null){row["status"]="SourceLodAbsent";return row;}
            try
            {
                if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Persistent source asset path required");
                string before=AssetDatabase.GetAssetDependencyHash(path).ToString();
                var paths=AssetDatabase.GetDependencies(path,true).Distinct(StringComparer.Ordinal).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
                if(!paths.Contains(path,StringComparer.Ordinal))throw new InvalidOperationException("Importer graph omitted its root");
                var pins=new JArray();foreach(string dependency in paths)pins.Add(DependencyPin(dependency));
                string after=AssetDatabase.GetAssetDependencyHash(path).ToString();
                row["dependencyHashBefore"]=before;row["dependencyHashAfter"]=after;row["dependencyCount"]=paths.Length;row["dependencies"]=pins;
                row["captured"]=before==after;row["status"]=before==after?"Collected":"DependencyHashChangedDuringRead";
            }
            catch(Exception error){row["status"]="CollectionFailed";row["error"]=error.ToString();}
            return row;
        }
        static JObject DependencyPin(string assetPath)
        {
            var row=new JObject{["assetPath"]=assetPath,["guid"]=AssetDatabase.AssetPathToGUID(assetPath),["sourceKind"]="VirtualOrBuiltin",["resolvedPhysicalPath"]=null};
            string physical=null;
            if(assetPath.StartsWith("Assets/",StringComparison.Ordinal))
            {
                row["sourceKind"]="Assets";physical=Path.GetFullPath(assetPath);
            }
            else if(assetPath.StartsWith("Packages/",StringComparison.Ordinal))
            {
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
                row["sourceKind"]="Package";
                if(package!=null)
                {
                    row["packageName"]=package.name;row["packageVersion"]=package.version;row["packageSource"]=package.source.ToString();row["packageAssetPath"]=package.assetPath;
                    string prefix=package.assetPath.TrimEnd('/')+"/";
                    if(assetPath.StartsWith(prefix,StringComparison.Ordinal)&&!string.IsNullOrEmpty(package.resolvedPath))
                        physical=Path.GetFullPath(Path.Combine(package.resolvedPath,assetPath.Substring(prefix.Length)));
                }
            }
            row["resolvedPhysicalPath"]=physical;
            row["source"]=FilePin(physical);row["meta"]=FilePin(physical!=null?physical+".meta":null);
            return row;
        }
        static JObject FilePin(string path)
        {
            var row=new JObject{["physicalPath"]=path,["exists"]=path!=null&&File.Exists(path),["sha256"]=null};
            if(path==null){row["status"]="VirtualOrUnresolved";return row;}
            if(!File.Exists(path)){row["status"]="PhysicalFileAbsent";return row;}
            try
            {
                using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
                using(var hash=System.Security.Cryptography.SHA256.Create())
                {row["length"]=stream.Length;row["sha256"]=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
                row["status"]="HashedRawBytes";
            }
            catch(Exception error){row["status"]="ReadFailed";row["error"]=error.ToString();}
            return row;
        }
        static bool InsideLinked(EntityManager em,Entity actor,Entity child){if(!em.HasBuffer<LinkedEntityGroup>(actor))return false;foreach(var row in em.GetBuffer<LinkedEntityGroup>(actor,true))if(row.Value==child)return true;return false;}
        static JArray ParentFacts(EntityManager em,Entity child){var rows=new JArray();var seen=new HashSet<Entity>();while(child!=Entity.Null&&em.Exists(child)){if(!seen.Add(child)||seen.Count>128)throw new InvalidOperationException("Provenance parent chain invalid");rows.Add(NodeFact(em,child));child=em.HasComponent<Parent>(child)?em.GetComponentData<Parent>(child).Value:Entity.Null;}return rows;}
        static JArray GraphFacts(EntityManager em,Entity root)
        {
            var rows=new JArray();var seen=new HashSet<Entity>();var queue=new Queue<Entity>();if(root!=Entity.Null)queue.Enqueue(root);
            while(queue.Count>0){var e=queue.Dequeue();if(!seen.Add(e))continue;if(seen.Count>4096)throw new InvalidOperationException("Provenance hierarchy exceeds bounded graph");rows.Add(NodeFact(em,e));if(!em.Exists(e))continue;if(em.HasBuffer<Child>(e))foreach(var c in em.GetBuffer<Child>(e,true))queue.Enqueue(c.Value);if(em.HasBuffer<LinkedEntityGroup>(e))foreach(var c in em.GetBuffer<LinkedEntityGroup>(e,true))queue.Enqueue(c.Value);}
            return rows;
        }
        static JObject NodeFact(EntityManager em,Entity e)
        {
            var row=new JObject{["entity"]=EntityFact(e),["exists"]=em.Exists(e)};if(!em.Exists(e))return row;
            using(var componentTypes=em.GetComponentTypes(e,Allocator.Temp))
            {
                var managedTypes=componentTypes.Select(t=>t.GetManagedType()).ToArray();
                row["componentTypeCensusComplete"]=managedTypes.All(t=>t!=null);
                row["componentTypes"]=new JArray(managedTypes.Select(t=>t!=null?t.AssemblyQualifiedName:null).OrderBy(n=>n,StringComparer.Ordinal));
                row["componentNames"]=new JArray(managedTypes.Select(t=>t!=null?t.FullName:null).OrderBy(n=>n,StringComparer.Ordinal));
            }
            row["postTransformMatrixPresent"]=em.HasComponent<PostTransformMatrix>(e);
            if(em.HasComponent<PostTransformMatrix>(e))
            {
                var post=em.GetComponentData<PostTransformMatrix>(e).Value;
                row["postTransformMatrixColumns"]=new JArray(new JArray(post.c0.x,post.c0.y,post.c0.z,post.c0.w),new JArray(post.c1.x,post.c1.y,post.c1.z,post.c1.w),new JArray(post.c2.x,post.c2.y,post.c2.z,post.c2.w),new JArray(post.c3.x,post.c3.y,post.c3.z,post.c3.w));
            }
            var children=new JArray();if(em.HasBuffer<Child>(e))foreach(var child in em.GetBuffer<Child>(e,true))children.Add(EntityFact(child.Value));row["childEntities"]=children;
            var linked=new JArray();if(em.HasBuffer<LinkedEntityGroup>(e))foreach(var child in em.GetBuffer<LinkedEntityGroup>(e,true))linked.Add(EntityFact(child.Value));row["linkedEntityGroup"]=linked;
            row["name"]=em.GetName(e).ToString();row["prefab"]=em.HasComponent<Prefab>(e);row["disabled"]=em.HasComponent<Disabled>(e);row["parent"]=em.HasComponent<Parent>(e)?EntityFact(em.GetComponentData<Parent>(e).Value):null;
            if(em.HasComponent<LocalTransform>(e)){var t=em.GetComponentData<LocalTransform>(e);row["localTransform"]=new JObject{["position"]=new JArray(t.Position.x,t.Position.y,t.Position.z),["rotation"]=new JArray(t.Rotation.value.x,t.Rotation.value.y,t.Rotation.value.z,t.Rotation.value.w),["scale"]=t.Scale};}
            if(em.HasComponent<LocalToWorld>(e)){var m=em.GetComponentData<LocalToWorld>(e).Value;row["localToWorldColumns"]=new JArray(new JArray(m.c0.x,m.c0.y,m.c0.z,m.c0.w),new JArray(m.c1.x,m.c1.y,m.c1.z,m.c1.w),new JArray(m.c2.x,m.c2.y,m.c2.z,m.c2.w),new JArray(m.c3.x,m.c3.y,m.c3.z,m.c3.w));}
            row["hasMaterialMeshInfo"]=em.HasComponent<MaterialMeshInfo>(e);if(!em.HasComponent<MaterialMeshInfo>(e))return row;
            var info=em.GetComponentData<MaterialMeshInfo>(e);row["materialMeshInfoEnabled"]=em.IsComponentEnabled<MaterialMeshInfo>(e);row["meshIndexOrRegisteredId"]=info.Mesh;row["materialIndexOrRegisteredId"]=info.Material;row["hasRange"]=info.HasMaterialMeshIndexRange;
            var draws=new JArray();row["draws"]=draws;if(!em.HasComponent<RenderMeshArray>(e)){row["bindingError"]="RenderMeshArrayMissing";return row;}var array=em.GetSharedComponentManaged<RenderMeshArray>(e);
            if(info.HasMaterialMeshIndexRange){var range=info.MaterialMeshIndexRange;var indices=array.MaterialMeshIndices;if(indices==null||range.start<0||range.length<=0||range.end>indices.Length){row["bindingError"]="InvalidMaterialMeshRange";return row;}for(int i=range.start;i<range.end;i++){var item=indices[i];if(array.MeshReferences==null||array.MaterialReferences==null||item.MeshIndex<0||item.MaterialIndex<0||item.MeshIndex>=array.MeshReferences.Length||item.MaterialIndex>=array.MaterialReferences.Length){row["bindingError"]="InvalidMaterialMeshReference";return row;}draws.Add(DrawFact(array.MeshReferences[item.MeshIndex].Value,array.MaterialReferences[item.MaterialIndex].Value,item.SubMeshIndex));}}
            else if(info.Mesh>=0||info.Material>=0)row["bindingError"]="RuntimeRegisteredBindingUnresolved";
            else draws.Add(DrawFact(array.GetMesh(info),array.GetMaterial(info),info.SubMesh));return row;
        }
        static JObject DrawFact(Mesh mesh,Material material,int submesh)=>new JObject{["mesh"]=ObjectFact(mesh),["material"]=ObjectFact(material),["shader"]=ObjectFact(material!=null?material.shader:null),["submesh"]=submesh,["submeshCount"]=mesh!=null?mesh.subMeshCount:0,["vertexCount"]=mesh!=null?mesh.vertexCount:0,["indexedGeometryMeasured"]=false};
        static JObject EntityFact(Entity entity)=>new JObject{["index"]=entity.Index,["version"]=entity.Version};
        static JObject ObjectFact(UnityEngine.Object value)
        {
            string guid=null;long id=0;if(value!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out guid,out id);
            return new JObject{["exists"]=value!=null,["name"]=value!=null?value.name:null,["assetPath"]=value!=null?AssetDatabase.GetAssetPath(value):null,["guid"]=guid,["localFileId"]=id,["globalObjectId"]=value!=null?GlobalObjectId.GetGlobalObjectIdSlow(value).ToString():null};
        }
        static JArray Renderers(Transform root)=>new JArray(root.GetComponentsInChildren<Renderer>(true).Select(r=>new JObject{["rendererType"]=r.GetType().FullName,["mesh"]=ObjectFact(r is SkinnedMeshRenderer skin?skin.sharedMesh:r.GetComponent<MeshFilter>()!=null?r.GetComponent<MeshFilter>().sharedMesh:null),["materials"]=new JArray(r.sharedMaterials.Select(m=>new JObject{["material"]=ObjectFact(m),["shader"]=ObjectFact(m!=null?m.shader:null)})),["renderer"]=ObjectFact(r),["transform"]=ObjectFact(r.transform),["enabled"]=r.enabled,["activeSelf"]=r.gameObject.activeSelf,["parent"]=ObjectFact(r.transform.parent)}));
        static JArray Blades(Transform root)=>new JArray(root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Blade",StringComparison.Ordinal)&&(t.name.EndsWith("_X",StringComparison.Ordinal)||t.name.EndsWith("_Y",StringComparison.Ordinal)||t.name.EndsWith("_Z",StringComparison.Ordinal))).Select(t=>ObjectFact(t)));
    }
}
