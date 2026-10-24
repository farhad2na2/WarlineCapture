#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Game.Runtime
{
    // Source-pinned read-only adapter for the protected ECB list. No pointer value
    // leaves this method and no producer handle is polled, completed or reset.
    public static class S002InitialWorldEcbAuthority
    {
        public static unsafe Dictionary<string, object> Capture(EntityCommandBufferSystem owner, bool originalStructuralSyncObserved)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!originalStructuralSyncObserved)
                return new Dictionary<string, object> { ["source_type"] = owner.GetType().FullName,
                    ["supported"] = false, ["reason"] = "Original source structural synchronization is not proven" };
            ref var state = ref owner.World.Unmanaged.ResolveSystemStateRef(owner.SystemHandle);
            var jobField = typeof(SystemState).GetField("m_JobHandle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (jobField == null || jobField.FieldType != typeof(JobHandle))
                throw new InvalidOperationException("Pinned SystemState dependency layout mismatch.");
            // SystemState is a byref-like unmanaged source struct and cannot be
            // boxed. Read only the verified JobHandle field at its runtime layout
            // offset; never invoke Dependency's state-mutating getter.
            int jobOffset = checked((int)Marshal.OffsetOf(typeof(SystemState), "m_JobHandle"));
            if (!typeof(SystemState).IsLayoutSequential || jobOffset < 0 || jobOffset > sizeof(SystemState) - sizeof(JobHandle))
                throw new InvalidOperationException("SystemState dependency field offset is outside the pinned layout.");
            JobHandle dependency;
            fixed (SystemState* pointerToState = &state)
                dependency = *(JobHandle*)((byte*)pointerToState + jobOffset);
            if (!dependency.Equals(default(JobHandle)))
                return new Dictionary<string, object> { ["source_type"] = owner.GetType().FullName,
                    ["supported"] = false, ["reason"] = "ECB system dependency handle exists; no polling/completion permitted" };
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = typeof(EntityCommandBufferSystem).GetField("m_PendingBuffers", flags);
            if (field == null || field.FieldType != typeof(UnsafeList<EntityCommandBuffer>).MakePointerType())
                throw new InvalidOperationException("Pinned ECB pending-list layout does not match installed source.");
            var pointer = (UnsafeList<EntityCommandBuffer>*)Pointer.Unbox(field.GetValue(owner));
            var result = new Dictionary<string, object> { ["source_type"] = owner.GetType().FullName,
                ["list_created"] = pointer != null, ["supported"] = false };
            if (pointer == null) { result["reason"] = "ECB owner has no live pending-list storage"; return result; }
            int count = pointer->Length;
            if (count < 0 || count > 65536 || pointer->Capacity < count)
                throw new InvalidOperationException("ECB pending-list bounds are invalid.");
            var producer = typeof(EntityCommandBufferSystem).GetField("m_ProducerHandle", flags);
            if (producer == null || producer.FieldType != typeof(JobHandle))
                throw new InvalidOperationException("Pinned ECB producer-handle layout does not match installed source.");
            bool hasProducer = !((JobHandle)producer.GetValue(owner)).Equals(default(JobHandle));
            result["producer_handle_is_default"] = !hasProducer;
            result["pending_count"] = count;
            result["pending_capacity"] = pointer->Capacity;
            if (hasProducer) { result["reason"] = "Producer handle exists; no completion or IsCompleted poll permitted"; return result; }
            if(count>256)throw new InvalidOperationException("ECB header diagnostic pending-buffer limit");
            var aliases=new Dictionary<IntPtr,int>();int diagnosticWork=0;
            var rows = new List<object>(); bool allEmpty = true;
            for (int i = 0; i < count; i++)
            {
                var buffer = (*pointer)[i]; bool empty = buffer.IsEmpty;
                var row=new Dictionary<string,object>{["ordinal"]=i,["is_created"]=buffer.IsCreated,["is_empty"]=empty};
                row["header_diagnostic"]=FirstHeaderDiagnostic(buffer,owner.World,aliases,ref diagnosticWork);rows.Add(row);
                allEmpty &= empty;
            }
            if(pointer->Length!=count)throw new InvalidOperationException("ECB pending list changed during header observation");
            result["header_diagnostic_scope"]="first live header per chain in source chain enumeration order; NOT full commands or playback order";
            result["header_diagnostic_work"]=diagnosticWork;result["buffers"] = rows;
            result["supported"] = allEmpty;
            result["reason"] = allEmpty ? "Observed empty pending command domain; future buffers require source constructors" : "Pending commands require an explicit command codec";
            return result;
        }

        // No payload is boxed/read and no source field is written. All pointers are
        // confined to this synchronous method and observer-local identity sets.
        static void HeaderNeed(bool value,string message){if(!value)throw new InvalidOperationException("ECB header diagnostic: "+message);}
        static Type EcbType(string name)=>typeof(EntityCommandBuffer).Assembly.GetType("Unity.Entities."+name,true);
        static int NativeOffset(Type type,string name,Type expected)
        {
            var field=type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            HeaderNeed(type.IsLayoutSequential&&field!=null&&field.FieldType==expected,"field layout mismatch "+type.FullName+"."+name);
            int offset=UnsafeUtility.GetFieldOffset(field),size=UnsafeUtility.SizeOf(type),width=expected.IsPointer?IntPtr.Size:UnsafeUtility.SizeOf(expected);
            HeaderNeed(offset>=0&&width>0&&offset<=size-width,"field offset outside native type");return offset;
        }
        static int PointerOffset(Type type,string name,Type element)=>NativeOffset(type,name,element.MakePointerType());
        static unsafe bool ReadBool(byte* pointer,int offset){byte value=pointer[offset];HeaderNeed(value<=1,"noncanonical native bool");return value!=0;}
        static unsafe Dictionary<string,object> FirstHeaderDiagnostic(EntityCommandBuffer buffer,World world,Dictionary<IntPtr,int> aliases,ref int work)
        {
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            var dataField=typeof(EntityCommandBuffer).GetField("m_Data",flags);Type dataType=EcbType("EntityCommandBufferData"),chainType=EcbType("EntityCommandBufferChain"),chunkType=EcbType("ECBChunk"),basicType=EcbType("BasicCommand");
            HeaderNeed(dataField!=null&&dataField.FieldType==dataType.MakePointerType(),"ECB data field type mismatch");
            byte* data=(byte*)Pointer.Unbox(dataField.GetValue(buffer));
            if(data==null)return new Dictionary<string,object>{{"alias_id",null},{"data_created",false},{"complete_queue_codec",false}};
            IntPtr key=(IntPtr)data;if(!aliases.TryGetValue(key,out int alias)){alias=aliases.Count;aliases.Add(key,alias);}
            int chainSize=UnsafeUtility.SizeOf(chainType),chunkSize=UnsafeUtility.SizeOf(chunkType),basicSize=UnsafeUtility.SizeOf(basicType);
            HeaderNeed(IntPtr.Size==8&&UnsafeUtility.SizeOf<bool>()==1&&basicSize==12&&chainSize==Math.Max(64,JobsUtility.CacheLineSize),"pinned architecture/native size mismatch");
            int main=NativeOffset(dataType,"m_MainThreadChain",chainType),threaded=PointerOffset(dataType,"m_ThreadedChains",chainType),recorded=NativeOffset(dataType,"m_RecordedChainCount",typeof(int));
            HeaderNeed(main==0&&threaded==chainSize&&recorded==chainSize+IntPtr.Size,"data chain prefix mismatch");
            int head=PointerOffset(chainType,"m_Head",chunkType),nextChain=PointerOffset(chainType,"m_NextChain",chainType);
            HeaderNeed(head==IntPtr.Size&&nextChain==5*IntPtr.Size,"chain prefix mismatch");
            int usedOffset=NativeOffset(chunkType,"Used",typeof(int)),sizeOffset=NativeOffset(chunkType,"Size",typeof(int));
            HeaderNeed(usedOffset==0&&sizeOffset==4&&PointerOffset(chunkType,"Next",chunkType)==8&&PointerOffset(chunkType,"Prev",chunkType)==8+IntPtr.Size&&chunkSize==8+2*IntPtr.Size,"chunk layout mismatch");
            Type opcodeType=EcbType("ECBCommand");HeaderNeed(opcodeType.IsEnum&&Enum.GetUnderlyingType(opcodeType)==typeof(int),"opcode type mismatch");
            int opcodeOffset=NativeOffset(basicType,"CommandType",opcodeType),totalOffset=NativeOffset(basicType,"TotalSize",typeof(int)),sortOffset=NativeOffset(basicType,"SortKey",typeof(int));
            HeaderNeed(opcodeOffset==0&&totalOffset==4&&sortOffset==8,"basic header layout mismatch");
            int idOffset=NativeOffset(dataType,"m_CommandBufferID",typeof(int)),entityOffset=NativeOffset(dataType,"m_Entity",typeof(Entity));int id=*(int*)(data+idOffset);Entity deferred=*(Entity*)(data+entityOffset);
            HeaderNeed(deferred.Index<=0&&deferred.Index>=-1000000&&deferred.Version==id,"deferred identity counter mismatch");
            int playbackOffset=NativeOffset(dataType,"m_ShouldPlayback",typeof(bool)),didOffset=NativeOffset(dataType,"m_DidPlayback",typeof(bool)),forceOffset=NativeOffset(dataType,"m_ForceFullDispose",typeof(bool));
            HeaderNeed(didOffset==playbackOffset+1&&forceOffset==didOffset+1,"playback bool native width mismatch");
            HeaderNeed(Enum.GetUnderlyingType(typeof(PlaybackPolicy))==typeof(int)&&UnsafeUtility.SizeOf<PlaybackPolicy>()==4,"playback policy width mismatch");
            int policyOffset=NativeOffset(dataType,"m_PlaybackPolicy",typeof(PlaybackPolicy));int policy=*(int*)(data+policyOffset);string policyName=Enum.GetName(typeof(PlaybackPolicy),policy);HeaderNeed(policyName!=null,"unknown playback policy");
            var originField=typeof(EntityCommandBuffer).GetField("OriginSystemHandle",flags);var systemField=typeof(EntityCommandBuffer).GetField("SystemID",flags);
            HeaderNeed(originField!=null&&originField.FieldType==typeof(SystemHandle)&&systemField!=null&&systemField.FieldType==typeof(int),"origin metadata field mismatch");
            var origin=(SystemHandle)originField.GetValue(buffer);bool present=origin!=SystemHandle.Null;bool live=present&&world.Unmanaged.IsSystemValid(origin);string sourceType=live?TypeManager.GetSystemName(world.Unmanaged.GetSystemTypeIndex(origin)).ToString():null;
            int count=*(int*)(data+recorded);HeaderNeed(count>=0&&count<=64,"recorded chain count limit");var first=new List<object>(count);var seenChains=new HashSet<IntPtr>();var seenHeads=new HashSet<IntPtr>();
            ActionWork(ref work);CollectChainHeaders(data+main,"main",-1,chainSize,head,nextChain,chunkSize,usedOffset,sizeOffset,basicSize,opcodeOffset,totalOffset,sortOffset,opcodeType,first,seenChains,seenHeads,ref work);
            byte* threads=*(byte**)(data+threaded);int threadCount=0;
            if(threads!=null){threadCount=JobsUtility.ThreadIndexCount;HeaderNeed(threadCount>0&&threadCount<=1024,"thread-index count limit");for(int i=0;i<threadCount;i++){ActionWork(ref work);CollectChainHeaders(threads+(long)i*chainSize,"thread",i,chainSize,head,nextChain,chunkSize,usedOffset,sizeOffset,basicSize,opcodeOffset,totalOffset,sortOffset,opcodeType,first,seenChains,seenHeads,ref work);}}
            HeaderNeed(first.Count==count&&*(int*)(data+recorded)==count&&*(int*)(data+idOffset)==id&&((Entity*)(data+entityOffset))->Index==deferred.Index,"recorded chain/identity changed during inspection");
            return new Dictionary<string,object>{{"alias_id",alias},{"data_created",true},{"layout_source","Unity.Entities ef040875dea9 field metadata; native SizeOf/GetFieldOffset verified"},{"command_buffer_id",id},{"deferred_entity_counter_index",deferred.Index},{"should_playback",ReadBool(data,playbackOffset)},{"did_playback",ReadBool(data,didOffset)},{"playback_policy",policyName},{"origin_system_id",(int)systemField.GetValue(buffer)},{"origin_handle_present",present},{"origin_live_in_capture_world",live},{"origin_resolution",!present?"absent":live?"live_in_capture_world":"not_live_in_capture_world"},{"origin_source_type",sourceType},{"recorded_chain_count",count},{"thread_index_count_if_allocated",threadCount},{"first_headers",first},{"chain_enumeration_order","main segments then thread slots ascending and segment links; NOT merged playback order"},{"complete_queue_codec",false},{"payload_read",false}};
        }
        static void ActionWork(ref int work){HeaderNeed(work<32768,"aggregate chain/slot work limit");work++;}
        static unsafe void CollectChainHeaders(byte* chain,string source,int thread,int chainSize,int headOffset,int nextOffset,int chunkSize,int usedOffset,int sizeOffset,int basicSize,int opcodeOffset,int totalOffset,int sortOffset,Type opcodeType,List<object> rows,HashSet<IntPtr> seenChains,HashSet<IntPtr> seenHeads,ref int work)
        {
            int segment=0;
            while(chain!=null)
            {
                ActionWork(ref work);HeaderNeed(seenChains.Add((IntPtr)chain),"cyclic/repeated chain node");byte* chunk=*(byte**)(chain+headOffset);
                if(chunk!=null)
                {
                    HeaderNeed(rows.Count<64&&seenHeads.Add((IntPtr)chunk),"repeated chunk head or first-header limit");int used=*(int*)(chunk+usedOffset),size=*(int*)(chunk+sizeOffset);HeaderNeed(size>0&&size<=16*1024*1024&&used>=basicSize&&used<=size,"first chunk Used/Size bounds");
                    byte* header=chunk+chunkSize;int opcode=*(int*)(header+opcodeOffset),total=*(int*)(header+totalOffset),sort=*(int*)(header+sortOffset);string name=Enum.GetName(opcodeType,opcode);HeaderNeed(name!=null&&total>=basicSize&&(total&7)==0&&total<=used,"first command header bounds/known opcode");
                    Type nativeHeader=CommandHeaderType(name);int minimum=UnsafeUtility.SizeOf(nativeHeader);HeaderNeed(total>=minimum,"command smaller than declared source header");var row=new Dictionary<string,object>{{"chain_enumeration_ordinal",rows.Count},{"source",source},{"thread_slot",thread},{"segment_ordinal",segment},{"opcode",opcode},{"opcode_name",name},{"sort_key",sort},{"total_size",total},{"first_chunk_used",used},{"first_chunk_size",size},{"native_header_type",nativeHeader.FullName},{"payload_read",false}};
                    if(TryComponentTypeOffset(nativeHeader,out int offset)){HeaderNeed(offset<=total-UnsafeUtility.SizeOf<TypeIndex>(),"component type field outside first command");var type=*(TypeIndex*)(header+offset);HeaderNeed(type.Index>0&&type.Index<TypeManager.GetTypeCount(),"unregistered component TypeIndex");row["captured_component_type_index"]=type.Value;var sourceType=TypeManager.GetType(type);HeaderNeed(sourceType!=null,"component source type unresolved");row["component_source_type"]=sourceType.FullName;}
                    rows.Add(row);HeaderNeed(*(int*)(header+opcodeOffset)==opcode&&*(int*)(header+totalOffset)==total&&*(int*)(header+sortOffset)==sort&&*(int*)(chunk+usedOffset)==used,"first header changed during observation");
                }
                chain=*(byte**)(chain+nextOffset);segment++;
            }
        }
        static bool TryComponentTypeOffset(Type header,out int offset)
        {
            offset=0;const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            for(int depth=0;depth<8;depth++)
            {
                var component=header.GetField("ComponentTypeIndex",flags);if(component!=null){offset=checked(offset+NativeOffset(header,"ComponentTypeIndex",typeof(TypeIndex)));return true;}
                var parent=header.GetField("Header",flags);if(parent==null)return false;HeaderNeed(parent.FieldType.IsValueType&&parent.FieldType.Namespace=="Unity.Entities","unknown nested header type");offset=checked(offset+NativeOffset(header,"Header",parent.FieldType));header=parent.FieldType;
            }
            throw new InvalidOperationException("ECB header diagnostic nested header depth limit");
        }
        static Type CommandHeaderType(string opcode)
        {
            string name;
            switch(opcode)
            {
                case "CreateEntity":name="CreateCommand";break;
                case "InstantiateEntity":case "DestroyEntity":name="EntityCommand";break;
                case "AddComponent":case "SetComponent":case "RemoveComponent":case "AppendToBuffer":case "ReplaceComponentLinkedEntityGroup":name="EntityComponentCommand";break;
                case "AddMultipleComponents":case "RemoveMultipleComponents":name="EntityMultipleComponentsCommand";break;
                case "SetEntityEnabled":name="EntityEnabledCommand";break;case "SetComponentEnabled":name="EntityComponentEnabledCommand";break;case "SetName":name="EntityNameCommand";break;
                case "AddBuffer":case "SetBuffer":name="EntityBufferCommand";break;
                case "AddManagedComponentData":case "SetManagedComponentData":name="EntityManagedComponentCommand";break;case "MoveManagedComponentData":name="EntityMoveManagedComponentCommand";break;
                case "AddSharedComponentData":case "SetSharedComponentData":name="EntitySharedComponentCommand";break;
                case "AddUnmanagedSharedComponentData":case "SetUnmanagedSharedComponentData":name="EntityUnmanagedSharedComponentCommand";break;
                case "AddComponentForEntityQuery":case "RemoveComponentForEntityQuery":name="EntityQueryComponentCommand";break;
                case "AddMultipleComponentsForEntityQuery":case "RemoveMultipleComponentsForEntityQuery":name="EntityQueryComponentTypeSetCommand";break;
                case "DestroyForEntityQuery":name="EntityQueryCommand";break;
                case "DestroyMultipleEntities":name="MultipleEntitiesCommand";break;
                case "AddComponentForMultipleEntities":case "RemoveComponentForMultipleEntities":name="MultipleEntitiesComponentCommand";break;
                case "AddMultipleComponentsForMultipleEntities":case "RemoveMultipleComponentsForMultipleEntities":name="MultipleEntitiesAndComponentsCommand";break;
                case "AddComponentObjectForMultipleEntities":case "SetComponentObjectForMultipleEntities":case "AddSharedComponentWithValueForMultipleEntities":case "SetSharedComponentValueForMultipleEntities":name="MultipleEntitiesComponentCommandWithObject";break;
                case "AddSharedComponentWithValueForEntityQuery":case "SetSharedComponentValueForEntityQuery":name="EntityQueryComponentCommandWithObject";break;
                case "AddUnmanagedSharedComponentValueForMultipleEntities":case "SetUnmanagedSharedComponentValueForMultipleEntities":name="MultipleEntitiesCommand_WithUnmanagedSharedComponent";break;
                case "AddUnmanagedSharedComponentValueForEntityQuery":case "SetUnmanagedSharedComponentValueForEntityQuery":name="EntityQueryComponentCommandWithUnmanagedSharedComponent";break;
                case "AddComponentLinkedEntityGroup":case "SetComponentLinkedEntityGroup":name="EntityQueryMaskCommand";break;
                default:throw new InvalidOperationException("ECB header diagnostic unsupported source opcode "+opcode);
            }
            return EcbType(name);
        }
    }
}
#endif
