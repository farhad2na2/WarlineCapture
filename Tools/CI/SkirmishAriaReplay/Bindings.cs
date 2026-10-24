using System;
// Metadata/container bindings only. Real UnityEngine managed Vector2/Vector3/Mathf
// are referenced directly. No World, ECB, device/input or gameplay implementation.
namespace Unity.Entities {
 public interface IComponentData{}public interface ISystem{}
 public struct Entity {public int Index,Version;}
 public sealed class PresentationSystemGroup{}
 [AttributeUsage(AttributeTargets.Struct)]public sealed class UpdateInGroupAttribute:Attribute{public UpdateInGroupAttribute(Type t){}}
 [AttributeUsage(AttributeTargets.Struct)]public sealed class UpdateBeforeAttribute:Attribute{public UpdateBeforeAttribute(Type t){}}
}
namespace Unity.Burst{[AttributeUsage(AttributeTargets.Struct|AttributeTargets.Method)]public sealed class BurstCompileAttribute:Attribute{}}
namespace Unity.Collections {
 // Only uint is admitted. Capacity127 follows actual FixedList512Bytes<uint>
 // (two-byte length header); copy-on-write preserves value-copy mutation semantics.
 // No binary-layout, Burst or native collection safety equivalence is claimed.
 public struct FixedList512Bytes<T>where T:struct {
  T[] data;public int Capacity {get{if(typeof(T)!=typeof(uint))throw new NotSupportedException("Only planner uint fixed list");return 127;}}
  public int Length=>data==null?0:data.Length;
  public T this[int i]=>data[i];
  public void Add(T value){if(Length>=Capacity)throw new ArgumentException("FixedList capacity exceeded");var next=new T[Length+1];if(data!=null)Array.Copy(data,next,data.Length);next[next.Length-1]=value;data=next;}
  public void Clear(){data=null;}
 }
}
namespace Game.UI.Shell.Ecs {public sealed class AriaPlayDecisionSystem{}}
// Existing focused validation logging only; planner never references Debug.
namespace UnityEngine{public static class Debug{public static void Log(object value)=>Console.Error.WriteLine(value);}}
