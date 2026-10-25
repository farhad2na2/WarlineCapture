extern alias ActualCore;
using System;
using System.IO;
using System.Security.Cryptography;
using Unity.Mathematics;
using V3=ActualCore::UnityEngine.Vector3;
using Game.Components;
namespace Game.Runtime.TransportPose {
 public static class PoseMath {
  public const float PositionTolerance=0.0005f, BoundsTolerance=0.001f, QuaternionTolerance=0.00002f, CellBoundaryMargin=0.002f;
  public const string MathSha256="87bdf65c5fc23bbef3afd323cc2da9cb1e2cfb94f3f36eddf29f49c58bc79661";
  public const string CoreSha256="8aa998f99dbe14e2c5dcfa03786094ba9711e2f88d72fcc4d2f410361101f018";
  static bool admitted;
  public static void AdmitInstalledMono() {
   if(Type.GetType("Mono.Runtime")==null)throw new NotSupportedException("Installed-Mono numeric lane required");
   string path=typeof(math).Assembly.Location;using(var stream=File.OpenRead(path))using(var sha=SHA256.Create())
    if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=MathSha256)throw new NotSupportedException("Unknown managed math binary");
   using(var stream=File.OpenRead(typeof(V3).Assembly.Location))using(var sha=SHA256.Create())
    if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=CoreSha256)throw new NotSupportedException("Unknown managed Core binary");
   admitted=true;
  }
  public static void Ready(){if(!admitted)throw new InvalidOperationException("Explicit numeric lane admission required");}
  public static void Finite(float3 v){if(!math.all(math.isfinite(v)))throw new ArgumentException("Nonfinite pose");}
  public static void Finite(float v){if(!math.isfinite(v))throw new ArgumentException("Nonfinite scalar");}
  public static void Unit(quaternion q){if(!math.all(math.isfinite(q.value))||math.abs(math.lengthsq(q.value)-1f)>0.0001f)throw new ArgumentException("Nonunit quaternion");}
  public static quaternion Euler(float3 degrees){Ready();Finite(degrees);return quaternion.EulerZXY(math.radians(degrees));}
  public static float3 EulerDegrees(quaternion q){Ready();Unit(q);float3 e=math.degrees(math.EulerZXY(q));
   // Unity Quaternion.Internal_MakePositive convention; preserve the near-zero window.
   float negativeFlip=-0.0001f*(180f/math.PI),positiveFlip=360f+negativeFlip;
   for(int i=0;i<3;i++){float x=e[i];if(x<negativeFlip)x+=360f;else if(x>positiveFlip)x-=360f;e[i]=x;}return e;
  }
  public static quaternion Look(float3 forward,float3 up){Ready();Finite(forward);Finite(up);
   float f=math.lengthsq(forward),u=math.lengthsq(up);if(f<1e-12f||u<1e-12f||math.lengthsq(math.cross(forward,up))<=1e-6f*f*u)
    throw new NotSupportedException("Degenerate/nearparallel LookRotation outside qualified profile");
   return quaternion.LookRotationSafe(forward,up);
  }
  public static quaternion Slerp(quaternion a,quaternion b,float t){Ready();Unit(a);Unit(b);Finite(t);return math.slerp(a,b,math.clamp(t,0f,1f));}
  public static float3 Lerp(float3 a,float3 b,float t){Ready();Finite(a);Finite(b);Finite(t);
   // Reuse actual managed Vector3.Lerp, not an independently reassociated formula.
   V3 r=V3.Lerp(new V3(a.x,a.y,a.z),new V3(b.x,b.y,b.z),t);return new float3(r.x,r.y,r.z);
  }
  public static float QError(quaternion a,quaternion b)=>math.min(math.cmax(math.abs(a.value-b.value)),math.cmax(math.abs(a.value+b.value)));
  public static int2 Cell(in GridCoordinateFrame grid,float3 p,bool requireUnambiguous){Ready();Finite(p);Finite(grid.CellSize);
   if(grid.CellSize<0.1f||grid.CellSize>100f)throw new NotSupportedException("Grid size outside numeric profile");
   Finite(new float3(grid.OriginX,grid.OriginY,grid.OriginZ));
   if(requireUnambiguous){float2 local=new float2(p.x-grid.OriginX,p.z-grid.OriginZ)/grid.CellSize;float2 edge=math.abs(local-math.round(local))*grid.CellSize;
    if(math.cmin(edge)<=CellBoundaryMargin)throw new NotSupportedException("Grid branch numerically ambiguous at boundary");}
   GridCellValue c=GridCoordinateKernel.WorldToCell(grid,p.x,p.z);return new int2(c.X,c.Z);
  }
 }
 public struct PoseFrame { public int Parent;public string Name;public float3 Position,Scale;public quaternion Rotation;public bool InitialEulerPresent;public float3 InitialEulerDegrees; }
 public struct MeshBounds { public int Node; public bool Present; public float3 Center,Extents; }
 public sealed class PoseGraph {
  public const int MaxNodes=64,MaxDepth=8,MaxRenderers=64;
  readonly PoseFrame[] frames;readonly MeshBounds[] renderers;readonly int[] spins;
  // These proofs belong to the complete initial bundle/source owner, never guessed from later poses.
  public PoseGraph(PoseFrame[] input,MeshBounds[] bounds,int[] spinIndices,bool completeHierarchy,bool completeRenderers,bool staticMeshOnly,bool sourceOwnedTransformsOnly) {
   PoseMath.Ready();if(!completeHierarchy||!completeRenderers||!staticMeshOnly||!sourceOwnedTransformsOnly)throw new NotSupportedException("Missing hierarchy/renderer/writer authority");
   if(input==null||bounds==null||spinIndices==null||input.Length<1||input.Length>MaxNodes||bounds.Length>MaxRenderers||spinIndices.Length>MaxNodes)throw new ArgumentException("Pose counts");
   // All counts and indices/finite values are validated before copied arrays or traversal.
   for(int i=0;i<input.Length;i++){PoseFrame f=input[i];if((i==0&&f.Parent!=-1)||(i>0&&(f.Parent<0||f.Parent>=i))||f.Name==null||f.Name.Length>128)throw new ArgumentException("Topological hierarchy");
    PoseMath.Finite(f.Position);PoseMath.Finite(f.Scale);PoseMath.Unit(f.Rotation);if(f.InitialEulerPresent){PoseMath.Finite(f.InitialEulerDegrees);if(PoseMath.QError(PoseMath.Euler(f.InitialEulerDegrees),f.Rotation)>PoseMath.QuaternionTolerance)throw new ArgumentException("Initial Euler/quaternion mismatch");}
    if(f.Scale.x<=0f||f.Scale.x!=f.Scale.y||f.Scale.x!=f.Scale.z||f.Scale.x>32f||math.cmax(math.abs(f.Position))>4096f)throw new NotSupportedException("Nonuniform/negative/excessive scale or position");
    int depth=0,at=i;float scale=1f;while(at>=0){if(++depth>MaxDepth)throw new NotSupportedException("Hierarchy depth");scale*=input[at].Scale.x;if(scale>32f||scale<1f/32f)throw new NotSupportedException("World scale");at=input[at].Parent;}
   }
   for(int i=0;i<bounds.Length;i++){MeshBounds b=bounds[i];if(b.Node<0||b.Node>=input.Length)throw new ArgumentException("Renderer node");PoseMath.Finite(b.Center);PoseMath.Finite(b.Extents);
    if(math.any(b.Extents<0f)||math.cmax(math.abs(b.Center))>4096||math.cmax(b.Extents)>4096)throw new ArgumentException("Bounds range");}
   for(int i=0;i<spinIndices.Length;i++){int n=spinIndices[i];if(n<0||n>=input.Length)throw new ArgumentException("Spin node");for(int j=0;j<i;j++)if(spinIndices[j]==n)throw new ArgumentException("Duplicate spin node");
    string name=input[n].Name;if(!(name.EndsWith("_X",StringComparison.Ordinal)||name.EndsWith("_Y",StringComparison.Ordinal)||name.EndsWith("_Z",StringComparison.Ordinal)))throw new ArgumentException("Source suffix");}
   int expectedSpin=0;for(int i=0;i<input.Length;i++){string n=input[i].Name;if(n.EndsWith("_X",StringComparison.Ordinal)||n.EndsWith("_Y",StringComparison.Ordinal)||n.EndsWith("_Z",StringComparison.Ordinal)){if(expectedSpin>=spinIndices.Length||spinIndices[expectedSpin++]!=i)throw new ArgumentException("Incomplete/source-order spin domain");}}if(expectedSpin!=spinIndices.Length)throw new ArgumentException("Extra spin domain");
   frames=(PoseFrame[])input.Clone();renderers=(MeshBounds[])bounds.Clone();spins=(int[])spinIndices.Clone();
  }
  public int Count=>frames.Length;
  public PoseFrame Frame(int i){if((uint)i>=(uint)frames.Length)throw new ArgumentOutOfRangeException();return frames[i];}
  public float4x4 WorldMatrix(int i){if((uint)i>=(uint)frames.Length)throw new ArgumentOutOfRangeException();PoseFrame f=frames[i];float4x4 m=float4x4.TRS(f.Position,f.Rotation,f.Scale);
   for(int at=f.Parent;at>=0;at=frames[at].Parent){f=frames[at];m=math.mul(float4x4.TRS(f.Position,f.Rotation,f.Scale),m);}return m;
  }
  public float3 Point(int i,float3 p){PoseMath.Finite(p);float3 v=math.transform(WorldMatrix(i),p);PoseMath.Finite(v);return v;}
  public void SetRoot(float3 position,quaternion rotation){PoseMath.Finite(position);PoseMath.Unit(rotation);if(math.cmax(math.abs(position))>4096)throw new NotSupportedException("Root position range");frames[0].Position=position;frames[0].Rotation=rotation;frames[0].InitialEulerPresent=false;}
  public float3 RootPosition=>frames[0].Position;
  public float3 Forward=>math.rotate(frames[0].Rotation,new float3(0,0,1));
  public float3 BoundsCenter(out float3 worldExtents){bool any=false;float3 lo=default,hi=default;
   for(int i=0;i<renderers.Length;i++){MeshBounds b=renderers[i];if(!b.Present)continue;float4x4 m=WorldMatrix(b.Node);float3 center=math.transform(m,b.Center);
    float3 ext=math.abs(m.c0.xyz)*b.Extents.x+math.abs(m.c1.xyz)*b.Extents.y+math.abs(m.c2.xyz)*b.Extents.z;
    if(!any){lo=center-ext;hi=center+ext;any=true;}else{lo=math.min(lo,center-ext);hi=math.max(hi,center+ext);}}
   worldExtents=any?(hi-lo)*.5f:float3.zero;return any?(lo+hi)*.5f:RootPosition;
  }
  public void SourceSpin(float delta){PoseMath.Finite(delta);if(delta<0f||delta>1f)throw new NotSupportedException("Delta outside finite profile");float degrees=15000f*delta;
   for(int i=0;i<spins.Length;i++){int n=spins[i];string name=frames[n].Name;float3 angles=name.EndsWith("_X",StringComparison.Ordinal)?new float3(degrees,0,0):name.EndsWith("_Y",StringComparison.Ordinal)?new float3(0,degrees,0):new float3(0,0,degrees);
    frames[n].Rotation=math.normalize(math.mul(frames[n].Rotation,PoseMath.Euler(angles)));frames[n].InitialEulerPresent=false;}
  }
  public void SourceDoor(int node,float initialOpenX,float open01){if(node<0)return;if(node>=frames.Length)throw new ArgumentException("Door node");PoseMath.Finite(initialOpenX);PoseMath.Finite(open01);
   float3 e=frames[node].InitialEulerPresent?frames[node].InitialEulerDegrees:PoseMath.EulerDegrees(frames[node].Rotation);frames[node].InitialEulerPresent=false;
   float delta=math.fmod(initialOpenX,360f);if(delta<0)delta+=360f;if(delta>180)delta-=360f;e.x=delta*math.clamp(open01,0,1);frames[node].Rotation=PoseMath.Euler(e);
  }
  public float3 SourcePlaneDoor(int node){if(node<0)return RootPosition-Forward*6f;float3 local=Frame(node).Position;local.x=0;return Point(0,local);}
 }
 public static class SourceTransportGeometry {
  public static float3 Drop(PoseGraph producer,bool instancePresent,float3[] actualLocalSlots,int reservedSlot){if(actualLocalSlots!=null&&actualLocalSlots.Length>256)throw new NotSupportedException("Spawn slot bound");if(!instancePresent)return new float3(0,.5f,0);
   if(producer==null)throw new ArgumentException("Unknown producer pose");float3 p=actualLocalSlots!=null&&reservedSlot>=0&&reservedSlot<actualLocalSlots.Length?producer.Point(0,actualLocalSlots[reservedSlot]):producer.RootPosition+producer.Forward*4f;return new float3(p.x,.5f,p.z);
  }
  public static void AlignHelicopter(PoseGraph transport,float3 committed,ref float3 hover,ref float3 touchdown,ref float3 entry,ref float3 exit){transport.SetRoot(hover,transport.Frame(0).Rotation);float3 ext;float3 anchor=transport.BoundsCenter(out ext);float3 delta=new float3(committed.x-anchor.x,0,committed.z-anchor.z);hover+=delta;touchdown+=delta;entry+=delta;exit+=delta;transport.SetRoot(hover,transport.Frame(0).Rotation);}
  public static bool Arrival(PoseGraph transport,float3 entry,float3 hover,quaternion entryQ,quaternion hoverQ,float phaseStartedAt,float arrivalSeconds,float now){PoseMath.Finite(phaseStartedAt);PoseMath.Finite(arrivalSeconds);PoseMath.Finite(now);float t=math.clamp((now-phaseStartedAt)/math.max(.5f,arrivalSeconds),0f,1f);transport.SetRoot(PoseMath.Lerp(entry,hover,t),PoseMath.Slerp(entryQ,hoverQ,t));return t>=1f;}
  public static float3 HelicopterDropStart(float3 committed,PoseGraph transport){float3 ext;float3 anchor=transport.BoundsCenter(out ext);return new float3(committed.x,anchor.y,committed.z);}
  public static float3 HelicopterDropPoint(float3 start,float3 end,float startedAt,float duration,float now,out bool complete){float t=math.clamp((now-startedAt)/math.max(.01f,duration),0f,1f);complete=t>=1f;return PoseMath.Lerp(start,end,t);}
 }
}
