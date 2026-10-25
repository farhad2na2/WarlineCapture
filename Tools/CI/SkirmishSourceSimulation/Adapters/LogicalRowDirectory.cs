using System;using System.Collections.Generic;
namespace Unity.Entities {
 public sealed partial class EntityManager {
  // Fresh logical profile only. Existing dictionaries remain authoritative.
  const int LogicalRowDirectoryLimit=65536;
  struct LogicalRows { internal Entity Owner;internal Dictionary<Type,object> Data,Buffers; }
  LogicalRows[] logicalRows=Array.Empty<LogicalRows>();
  void IndexLogicalRowContainers(Entity e) {
   if(!LogicalAllocatorAdmitted||(uint)e.Index>=LogicalRowDirectoryLimit)return;
   if(e.Index>=logicalRows.Length){int size=Math.Max(128,logicalRows.Length);while(size<=e.Index)size*=2;Array.Resize(ref logicalRows,size);}
   ref var slot=ref logicalRows[e.Index];
   // Simultaneous older versions retain the exact authoritative dictionary fallback.
   if(slot.Data!=null&&slot.Owner.Version>e.Version)return;
   slot=new LogicalRows{Owner=e,Data=data[e],Buffers=buffers[e]};
  }
  void ForgetLogicalRowContainers(Entity e) {
   if((uint)e.Index<(uint)logicalRows.Length&&logicalRows[e.Index].Owner==e)logicalRows[e.Index]=default;
  }
  bool TryLogicalDataRow(Entity e,out Dictionary<Type,object> row) {
   if(LogicalAllocatorAdmitted&&(uint)e.Index<(uint)logicalRows.Length){ref var slot=ref logicalRows[e.Index];if(slot.Owner==e&&slot.Data!=null){row=slot.Data;return true;}}
   return data.TryGetValue(e,out row);
  }
  bool TryLogicalBufferRow(Entity e,out Dictionary<Type,object> row) {
   if(LogicalAllocatorAdmitted&&(uint)e.Index<(uint)logicalRows.Length){ref var slot=ref logicalRows[e.Index];if(slot.Owner==e&&slot.Buffers!=null){row=slot.Buffers;return true;}}
   return buffers.TryGetValue(e,out row);
  }
 }
}
