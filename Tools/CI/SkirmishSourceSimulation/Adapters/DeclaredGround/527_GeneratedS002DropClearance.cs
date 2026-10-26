using System;using System.IO;using System.Linq;using System.Security.Cryptography;
using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Game.Components;using Unity.Collections;using Unity.Mathematics;
namespace Game.Runtime {
 internal sealed class GeneratedS002DropClearance {
  readonly float3[] centers,extents;readonly float globalXMin,globalYMin,globalXMax,globalYMax;
  readonly int sourceWidth=2048,sourceHeight=1024;readonly float sourceCell=1;readonly float3 sourceOrigin=float3.zero;
  internal GeneratedS002DropClearance(string projectionPath,string projectionSha){
   var root=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(projectionPath,projectionSha,8*1024*1024)),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
   if((string)root["kind"]=="OriginalCapturedFixedMapCompilerContext"){
    var native=SourceCapturedFixedMapInputs.Read(root);var frame=native["gridFrame"];sourceWidth=(int)frame["width"];sourceHeight=(int)frame["height"];sourceCell=(float)frame["cellSize"];var o=frame["origin"];sourceOrigin=new float3((float)o["x"],(float)o["y"],(float)o["z"]);var nativeRecord=(JObject)native["dropClearanceBounds"];
    if((string)nativeRecord["authority"]!="OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"||(int)nativeRecord["grid_width"]!=sourceWidth||(int)nativeRecord["grid_height"]!=sourceHeight||(float)nativeRecord["cell_size"]!=sourceCell||(float)nativeRecord["minimum_extent"]!=.25f||(float)nativeRecord["maximum_extent"]!=20f||(int)nativeRecord["source_search_radius_cells"]!=96||(int)nativeRecord["source_padding_cells"]!=2||!math.all(math.isfinite(sourceOrigin))||!(sourceCell>0))throw new InvalidDataException("Qualified original drop source frame/collector changed");var ro=(JArray)nativeRecord["grid_origin"];if(ro.Count!=3||(float)ro[0]!=sourceOrigin.x||(float)ro[1]!=sourceOrigin.y||(float)ro[2]!=sourceOrigin.z)throw new InvalidDataException("Actual drop/grid origin differs");var nativeQuery=(JArray)nativeRecord["query_rect"];if(nativeQuery.Count!=4)throw new InvalidDataException("Exact captured original nativeQuery envelope required");globalXMin=(float)nativeQuery[0];globalYMin=(float)nativeQuery[1];globalXMax=globalXMin+(float)nativeQuery[2];globalYMax=globalYMin+(float)nativeQuery[3];var nativeRows=(JArray)nativeRecord["bounds"];if(nativeRows.Count!=3988)throw new InvalidDataException("Complete actual fixed-map drop bounds required");centers=new float3[nativeRows.Count];extents=new float3[nativeRows.Count];for(int i=0;i<nativeRows.Count;i++){var c=(JArray)nativeRows[i]["center"];var e=(JArray)nativeRows[i]["extents"];if(c.Count!=3||e.Count!=3)throw new InvalidDataException("Actual source bound shape");centers[i]=new float3((float)c[0],(float)c[1],(float)c[2]);extents[i]=new float3((float)e[0],(float)e[1],(float)e[2]);if(!math.all(math.isfinite(centers[i]))||!math.all(math.isfinite(extents[i]))||math.any(extents[i]<0))throw new InvalidDataException("Invalid actual source bound");}return;
   }
   const string sourceSha="8e67cd3436c42f5c69f57a3cba95674050be2e58665e89899ded601e087b4193";
   if((int?)root["schema_version"]!=1||(string)root["kind"]!="GeneratedSourceStaticClearanceProjection"||(string)root["source_sha256"]!=sourceSha)throw new InvalidDataException("Released source clearance projection required");
   string sourcePath=(string)root["source_path"];var info=new FileInfo(sourcePath);if(!info.Exists||info.Length!=86665092||(info.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Pinned regular source artifact required");
   using(var stream=File.OpenRead(sourcePath))using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=sourceSha)throw new InvalidDataException("Source clearance artifact changed");
   var record=(JObject)root["source_record"];if((string)record["authority"]!="OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"||(int)record["grid_width"]!=2048||(int)record["grid_height"]!=1024||(float)record["cell_size"]!=1f||(float)record["minimum_extent"]!=.25f||(float)record["maximum_extent"]!=20f||(int)record["source_search_radius_cells"]!=96||(int)record["source_padding_cells"]!=2)throw new NotSupportedException("Original source clearance domain differs");
   var origin=(JArray)record["grid_origin"];if(origin.Any(x=>(float)x!=0f))throw new NotSupportedException("Source clearance grid origin differs");
   var query=(JArray)record["query_rect"];globalXMin=(float)query[0];globalYMin=(float)query[1];globalXMax=globalXMin+(float)query[2];globalYMax=globalYMin+(float)query[3];
   var rows=(JArray)record["bounds"];if(rows.Count!=48457)throw new InvalidDataException("Complete original accepted source bounds required");centers=new float3[rows.Count];extents=new float3[rows.Count];
   for(int i=0;i<rows.Count;i++){var c=(JArray)rows[i]["center"];var e=(JArray)rows[i]["extents"];centers[i]=new float3((float)c[0],(float)c[1],(float)c[2]);extents[i]=new float3((float)e[0],(float)e[1],(float)e[2]);if(!math.all(math.isfinite(centers[i]))||!math.all(math.isfinite(extents[i]))||math.any(extents[i]<0))throw new InvalidDataException("Invalid source bound");}
  }
  internal void Reserve(SourceProductionGroundWorld world,int2 preferred,ref NativeBitArray reserved){
   var grid=world.Grid;if(grid.Width!=sourceWidth||grid.Height!=sourceHeight||grid.CellSize!=sourceCell||math.any(grid.Origin!=sourceOrigin))throw new NotSupportedException("Clearance artifact/actual grid mismatch");
   float safe=math.max(.01f,grid.CellSize),maximum=20*safe;int padding=96+2;int2 lo=preferred-new int2(padding),hi=preferred+new int2(padding+1);
   float xMin=grid.Origin.x+lo.x*safe,yMin=grid.Origin.z+lo.y*safe,xMax=xMin+(hi.x-lo.x)*safe,yMax=yMin+(hi.y-lo.y)*safe;
   if(xMin<globalXMin||yMin<globalYMin||xMax>globalXMax||yMax>globalYMax)throw new NotSupportedException("Local drop query exceeds complete source bounds domain");
   // EXACT original collector extent/intersection inequalities, in source row order.
   // Global export rows are candidates, never treated as the local query's result.
   for(int i=0;i<centers.Length;i++){float3 c=centers[i],e=extents[i];float extent=math.max(e.x,e.z);if(!math.all(math.isfinite(c))||!math.all(math.isfinite(e))||extent<safe*.25f||extent>maximum)continue;
    float axMin=c.x-e.x,ayMin=c.z-e.z,axMax=axMin+e.x*2f,ayMax=ayMin+e.z*2f;
    if(axMax<xMin||ayMax<yMin||axMin>=xMax||ayMin>=yMax)continue;
    int2 a=GridUtils.WorldToCell(grid,c-e),b=GridUtils.WorldToCell(grid,c+e);world.ReserveRect(ref reserved,a.x-2,a.y-2,b.x+3,b.y+3);
   }
  }
 }
}
