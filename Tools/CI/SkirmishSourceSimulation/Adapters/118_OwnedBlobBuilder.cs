using System;using Unity.Collections;namespace Unity.Entities {
 public struct BlobBuilderArray<T>{readonly T[] values;internal BlobBuilderArray(T[] data){values=data;}public int Length=>values.Length;public ref T this[int index]=>ref values[index];}
 // Bounded managed ownership for original blob-construction algorithms. No raw native ABI is inferred.
 public sealed class BlobBuilder:IDisposable{sealed class Root<T>where T:struct{internal T Value;}object root;long allocated;bool disposed;
  public BlobBuilder(Allocator allocator){}
  public ref T ConstructRoot<T>()where T:struct{if(disposed)throw new ObjectDisposedException(nameof(BlobBuilder));if(root==null)root=new Root<T>();if(!(root is Root<T> typed))throw new NotSupportedException("One typed blob root");return ref typed.Value;}
  public BlobBuilderArray<T> Allocate<T>(ref BlobArray<T> target,int length){if(disposed||length<0||length>4194304)throw new NotSupportedException("Finite blob allocation bound");long bytes=checked((long)length*Math.Max(1,System.Runtime.InteropServices.Marshal.SizeOf(typeof(T))));allocated=checked(allocated+bytes);if(allocated>268435456)throw new NotSupportedException("256MiB blob allocation bound");var data=new T[length];target=new BlobArray<T>(data);return new BlobBuilderArray<T>(data);}
  public BlobAssetReference<T>CreateBlobAssetReference<T>(Allocator allocator)where T:struct{if(disposed||!(root is Root<T> typed))throw new NotSupportedException("Owned typed blob publication required");return new BlobAssetReference<T>(typed.Value);}
  public void Dispose(){disposed=true;root=null;}
 }
}
