#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime.Pathfinding;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public sealed partial class SkirmishOfflineDataExporter
    {
        readonly Dictionary<string,OfflineMap> surfaceCache=new(StringComparer.Ordinal);
        OfflineMap CaptureMap(OperationMapDefinition source)
        {
            string path=AssetDatabase.GetAssetPath(source),key=AssetDatabase.AssetPathToGUID(path);
            if(maps.TryGetValue(key,out var previous))return previous;
            Track(source);
            var map=new OfflineMap {metadata=new OfflineMapMetadata {bounds=source.Bounds,source_binding=source.SourceBinding,grid=source.GridMetadata,surface=source.SurfaceMetadata,navigation=source.NavigationMetadata},key=key,definition_path=path,operation_map_id=source.OperationMapId,
                source_scene_guid=source.SourceSceneReference?.AssetGUID,source_identity_hash=source.SourceIdentityHash,content_hash=source.ContentHash,generated_metadata_hash=source.GeneratedMetadataHash,grid_content_hash=source.GridMetadata.ContentHash,surface_content_hash=source.SurfaceMetadata.ContentHash,surface_metadata_runtime_hash=source.SurfaceMetadata.RuntimeBlobHash,
                grid_path=AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid),
                surface_path=AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference?.AssetGUID ?? "")};
            map.source_scene_path=AssetDatabase.GUIDToAssetPath(map.source_scene_guid ?? "");
            var members=campaign.Where(r=>r.reasons.Length==0 && !string.IsNullOrEmpty(r.scene_guid) && r.scene_guid==map.source_scene_guid).ToArray();
            map.campaign_physical_source_member=members.Any(r=>
            {
                var actual=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(r.map_path);
                return actual!=null && actual.GridMetadata.AssetGuid==source.GridMetadata.AssetGuid && actual.MapSurfaceDataReference?.AssetGUID==source.MapSurfaceDataReference?.AssetGUID;
            });
            map.campaign_mission_ids=members.Select(r=>r.mission_id).ToArray();map.campaign_definition_paths=members.Select(r=>r.map_path).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            var reasons=new List<string>();
            if(members.Length==0)reasons.Add("Source scene GUID is not referenced by a current Menu Campaign mission operation map");
            else if(!map.campaign_physical_source_member)reasons.Add("Source scene matches Campaign but grid/surface GUID bindings differ from all current Campaign consumers");
            if(!source.TryValidateMetadata(out string metadataError))reasons.Add("Map metadata: "+metadataError);
            Pin(map.source_scene_path);Pin(map.grid_path);Pin(map.surface_path);
            if(string.IsNullOrEmpty(map.source_scene_path)||ReadPin(map.source_scene_path).status!="complete")reasons.Add("Source scene payload missing/empty/LFS pointer");
            var grid=AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(map.grid_path);
            if(grid==null || ReadPin(map.grid_path).status!="complete")reasons.Add("Actual grid asset payload unavailable");
            else
            {
                Track(grid);map.width=grid.Width;map.height=grid.Height;map.cell_size=grid.CellSize;
                map.origin_x=grid.Origin.x;map.origin_y=grid.Origin.y;map.origin_z=grid.Origin.z;
                long cells=(long)grid.Width*grid.Height;
                if(grid.Width<=0 || grid.Height<=0 || !float.IsFinite(grid.CellSize) || grid.CellSize<=0 || cells>MaximumCells)reasons.Add("Grid exceeds explicit export cell limit or has invalid dimensions; no crop emitted");
                else
                {
                    map.authored_grid_available=true;
                    string gridKey=AssetDatabase.AssetPathToGUID(map.grid_path);
                    map.walkable_artifact=WriteArtifact(gridKey+".walkable.u8",(int)cells,1,"u8 row-major x+z*width; authored GridAuthoringConfig only",writer=>writer.Write(BuildAuthoredWalkable(grid)));
                }
                if(grid.Width!=source.GridMetadata.Dimensions.x || grid.Height!=source.GridMetadata.Dimensions.y || grid.CellSize!=source.GridMetadata.CellSize || grid.Origin!=source.GridMetadata.Origin)reasons.Add("Grid asset coordinate frame disagrees with operation map metadata");
            }
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(map.surface_path);
            if(surface==null || ReadPin(map.surface_path).status!="complete")reasons.Add("Actual surface asset payload unavailable");
            else
            {
                Track(surface);map.surface_payload_version=surface.PayloadVersion;map.surface_payload_encoding=surface.PayloadEncoding;map.surface_generated_flat_equivalent=surface.GeneratedFlatEquivalent;map.surface_width=surface.Dimensions.x;map.surface_height=surface.Dimensions.y;map.surface_origin_x=surface.GridOrigin.x;map.surface_origin_y=surface.GridOrigin.y;map.surface_origin_z=surface.GridOrigin.z;map.surface_cell_size=surface.CellSize;map.runtime_blob_hash=surface.ComputeRuntimeBlobHash().ToString();
                map.surface_grid_matches=map.authored_grid_available && surface.Dimensions.x==map.width && surface.Dimensions.y==map.height && surface.CellSize==map.cell_size && surface.GridOrigin.x==map.origin_x && surface.GridOrigin.y==map.origin_y && surface.GridOrigin.z==map.origin_z;
                if(!map.surface_grid_matches)reasons.Add("Decoded surface/grid coordinate frames disagree or grid unavailable");
                if(surface.Dimensions.x<=0 || surface.Dimensions.y<=0 || (long)surface.Dimensions.x*surface.Dimensions.y>MaximumCells)reasons.Add("Surface exceeds explicit export cell limit; no crop emitted");
                else if(surfaceCache.TryGetValue(map.surface_path,out var cached))CopySurface(cached,map);
                else if(!surface.TryCreateRuntimeBlobAsset(Allocator.Persistent,out var blob))reasons.Add("Native TryCreateRuntimeBlobAsset rejected actual payload; no flat fallback");
                else
                {
                    try
                    {
                        ref var data=ref blob.Value;
                        if((long)data.Cells.Length*8>MaximumArtifactBytes || (long)data.Samples.Length*47>MaximumArtifactBytes || (long)data.Connections.Length*19>MaximumArtifactBytes || (long)data.CompactSamples.Length*12>MaximumArtifactBytes)
                            reasons.Add("Full decoded surface tables exceed explicit artifact bound; no truncated tables emitted");
                        else
                        {
                        map.surface_width=data.Dimensions.x;map.surface_height=data.Dimensions.y;map.surface_origin_x=data.GridOrigin.x;map.surface_origin_y=data.GridOrigin.y;map.surface_origin_z=data.GridOrigin.z;map.surface_cell_size=data.CellSize;
                        map.surface_grid_matches=map.authored_grid_available && data.Dimensions.x==map.width && data.Dimensions.y==map.height && data.CellSize==map.cell_size && data.GridOrigin.x==map.origin_x && data.GridOrigin.y==map.origin_y && data.GridOrigin.z==map.origin_z;
                        map.decoded_surface_available=true;map.runtime_encoding=(int)data.RuntimeEncoding;map.compact_min_height=data.CompactMinHeight;map.compact_height_step=data.CompactHeightStep;
                        string surfaceKey=AssetDatabase.AssetPathToGUID(map.surface_path);
                        // BinaryWriter defines little-endian scalar encoding, with no native padding.
                        map.cells_artifact=WriteArtifact(surfaceKey+".cells.bin",data.Cells.Length,8,"le:i32 first_surface_index,u16 surface_count,u16 inline_surface_index",w=> {for(int i=0;i<blob.Value.Cells.Length;i++){var v=blob.Value.Cells[i];w.Write(v.FirstSurfaceIndex);w.Write(v.SurfaceCount);w.Write(v.InlineSurfaceIndex);}});
                        map.samples_artifact=WriteArtifact(surfaceKey+".samples.bin",data.Samples.Length,47,"le:i32 cell_x,i32 cell_z,i32 surface_id,i32 layer_id,f32 height,f32 normal_x,f32 normal_y,f32 normal_z,f32 slope,u8 type,u16 movement_mask,u16 flags,i32 first_connection,u16 connection_count",w=>{for(int i=0;i<blob.Value.Samples.Length;i++){var v=blob.Value.Samples[i];w.Write(v.Cell.x);w.Write(v.Cell.y);w.Write(v.SurfaceId);w.Write(v.LayerId);w.Write(v.Height);w.Write(v.Normal.x);w.Write(v.Normal.y);w.Write(v.Normal.z);w.Write(v.SlopeDegrees);w.Write((byte)v.SurfaceType);w.Write((ushort)v.MovementMask);w.Write((ushort)v.Flags);w.Write(v.FirstConnectionIndex);w.Write(v.ConnectionCount);}});
                        map.connections_artifact=WriteArtifact(surfaceKey+".connections.bin",data.Connections.Length,19,"le:i32 from_surface,i32 to_surface,i32 direction_x,i32 direction_z,u8 type,u16 movement_mask",w=>{for(int i=0;i<blob.Value.Connections.Length;i++){var v=blob.Value.Connections[i];w.Write(v.FromSurfaceId);w.Write(v.ToSurfaceId);w.Write(v.Direction.x);w.Write(v.Direction.y);w.Write((byte)v.ConnectionType);w.Write((ushort)v.MovementMask);}});
                        map.compact_samples_artifact=WriteArtifact(surfaceKey+".compact.bin",data.CompactSamples.Length,12,"le:u16 packed_height,i16 layer_id,u16 movement_mask,u16 flags,i8 normal_x,i8 normal_y,i8 normal_z,u8 type",w=>{for(int i=0;i<blob.Value.CompactSamples.Length;i++){var v=blob.Value.CompactSamples[i];w.Write(v.PackedHeight);w.Write(v.LayerId);w.Write((ushort)v.MovementMask);w.Write((ushort)v.Flags);w.Write(v.NormalX);w.Write(v.NormalY);w.Write(v.NormalZ);w.Write((byte)v.SurfaceType);}});
                        var component=new MapSurfaceComponent {SurfaceBlob=blob,HasSurfaceData=1,GridOrigin=data.GridOrigin,CellSize=data.CellSize,Dimensions=data.Dimensions};
                        int count=checked(data.Dimensions.x*data.Dimensions.y);
                        // Native single-cell admission, not connected-surface/edge traversal or runtime blockers.
                        map.infantry_surface_artifact=WriteArtifact(surfaceKey+".infantry-admission.u8",count,1,"u8 row-major surface dimensions; MapSurfaceTraversalValidation.CanTraverse Infantry",w=>{var traversal=new MapSurfaceTraversalValidation();for(int z=0;z<component.Dimensions.y;z++)for(int x=0;x<component.Dimensions.x;x++)w.Write((byte)(traversal.CanTraverse(component,1,new Unity.Mathematics.int2(x,z),MapSurfaceMovementMask.Infantry)?1:0));});
                        surfaceCache.Add(map.surface_path,map);
                        }
                    }
                    finally {blob.Dispose();}
                }
                if(map.runtime_blob_hash!=source.SurfaceMetadata.RuntimeBlobHash)reasons.Add("Actual runtime surface hash disagrees with map metadata");
                if(source.SurfaceMetadata.AssetGuid!=AssetDatabase.AssetPathToGUID(map.surface_path))reasons.Add("Map surface metadata GUID disagrees with actual reference");
            }
            var anchors=new List<OfflinePoint>();foreach(var anchor in source.Anchors)anchors.Add(Point(map,anchor.AnchorId,anchor.Kind.ToString(),anchor.Position.x,anchor.Position.z,"OperationMapDefinition typed anchor"));map.map_anchors=anchors.ToArray();
            map.reasons=reasons.ToArray();maps.Add(key,map);return map;
        }
        static void CopySurface(OfflineMap from,OfflineMap to)
        {to.decoded_surface_available=from.decoded_surface_available;to.runtime_encoding=from.runtime_encoding;to.compact_min_height=from.compact_min_height;to.compact_height_step=from.compact_height_step;to.cells_artifact=from.cells_artifact;to.samples_artifact=from.samples_artifact;to.connections_artifact=from.connections_artifact;to.compact_samples_artifact=from.compact_samples_artifact;to.infantry_surface_artifact=from.infantry_surface_artifact;}
        public static byte[] BuildAuthoredWalkable(GridAuthoringConfig grid)
        {
            if(grid==null || grid.Width<=0 || grid.Height<=0 || (long)grid.Width*grid.Height>MaximumCells)throw new ArgumentException("Bounded complete grid required");
            var result=new byte[checked(grid.Width*grid.Height)];Array.Fill(result,(byte)1);
            foreach(var cell in grid.BlockedCells ?? Array.Empty<Vector2Int>())if((uint)cell.x<(uint)grid.Width && (uint)cell.y<(uint)grid.Height)result[cell.x+cell.y*grid.Width]=0;
            return result;
        }
        string WriteArtifact(string name,int count,int stride,string encoding,Action<BinaryWriter> write)
        {
            if(count<0 || stride<=0 || (long)count*stride>MaximumArtifactBytes)throw new InvalidOperationException("Artifact bound exceeded; no truncated export");
            string relative="data/"+name; if(artifacts.Any(a=>a.path==relative))return relative;
            Directory.CreateDirectory(Path.Combine(output,"data"));string path=Path.Combine(output,relative);
            using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write))using(var writer=new BinaryWriter(stream))write(writer);
            long bytes=new FileInfo(path).Length;if(bytes!=(long)count*stride)throw new InvalidOperationException("Binary schema stride mismatch: "+name);
            artifacts.Add(new OfflineArtifact {path=relative,bytes=bytes,count=count,stride=stride,encoding=encoding,sha256=HashFile(path)});return relative;
        }
    }
}
#endif
