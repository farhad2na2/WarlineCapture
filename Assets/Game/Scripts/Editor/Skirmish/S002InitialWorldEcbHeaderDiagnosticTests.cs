#if UNITY_EDITOR
using System;using System.Collections.Generic;using System.Reflection;using Game.Runtime;using Unity.Collections.LowLevel.Unsafe;using Unity.Entities;using UnityEngine;
namespace Game.Editor
{
    public static class S002InitialWorldEcbHeaderDiagnosticTests
    {
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);Debug.Log("[InitialWorldEcbHeaders] PASS "+message);}
        // Root-owned checked native execution only. Never executes commands.
        public static unsafe void RunFocusedValidation()
        {
            var prior=World.DefaultGameObjectInjectionWorld;
            using(var world=new World("InitialWorldEcbHeaderDiagnosticTests"))
            {
                var owner=world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
                // A real fixture structural boundary; no jobs are scheduled in this World.
                world.EntityManager.CreateEntity();var first=owner.CreateCommandBuffer();var second=owner.CreateCommandBuffer();
                var empty=S002InitialWorldEcbAuthority.Capture(owner,true);var rows=(List<object>)empty["buffers"];
                Check((bool)empty["supported"]&&rows.Count==2&&first.IsEmpty&&second.IsEmpty,"two actual created empty pending buffers are admitted, not dropped");
                var one=(Dictionary<string,object>)((Dictionary<string,object>)rows[0])["header_diagnostic"];var two=(Dictionary<string,object>)((Dictionary<string,object>)rows[1])["header_diagnostic"];
                Check((int)one["alias_id"]!=(int)two["alias_id"]&&(int)one["recorded_chain_count"]==0&&(int)two["recorded_chain_count"]==0,"distinct empty native data owners remain distinct aliases");
                first.CreateEntity();int version=world.EntityManager.EntityOrderVersion;bool should=first.ShouldPlayback;
                var nonempty=S002InitialWorldEcbAuthority.Capture(owner,true);rows=(List<object>)nonempty["buffers"];one=(Dictionary<string,object>)((Dictionary<string,object>)rows[0])["header_diagnostic"];var headers=(List<object>)one["first_headers"];var header=(Dictionary<string,object>)headers[0];
                Check(!(bool)nonempty["supported"]&&!(bool)one["complete_queue_codec"]&&headers.Count==1&&(string)header["opcode_name"]=="CreateEntity"&&!(bool)header["payload_read"],"actual CreateEntity first header observed while nonempty admission remains false");
                Check(!first.IsEmpty&&second.IsEmpty&&first.ShouldPlayback==should&&world.EntityManager.EntityOrderVersion==version,"observer does not play, clear or change source World/ECB state");
                var field=typeof(EntityCommandBufferSystem).GetField("m_PendingBuffers",BindingFlags.Instance|BindingFlags.NonPublic);var pointer=(UnsafeList<EntityCommandBuffer>*)Pointer.Unbox(field.GetValue(owner));int length=pointer->Length;
                try
                {
                    // Fixture-only alias/default setup. Restore before owner destruction,
                    // so original allocated command buffers are each disposed once.
                    pointer->Add(first);pointer->Add(default(EntityCommandBuffer));
                    var aliases=S002InitialWorldEcbAuthority.Capture(owner,true);rows=(List<object>)aliases["buffers"];one=(Dictionary<string,object>)((Dictionary<string,object>)rows[0])["header_diagnostic"];var alias=(Dictionary<string,object>)((Dictionary<string,object>)rows[length])["header_diagnostic"];var absent=(Dictionary<string,object>)rows[length+1];
                    Check(rows.Count==length+2&&(int)one["alias_id"]==(int)alias["alias_id"]&&!(bool)absent["is_created"]&&(bool)absent["is_empty"],"actual duplicate native data alias and uncreated list slot preserve order/presence");
                }
                finally{pointer->Length=length;}
            }
            Check(ReferenceEquals(prior,World.DefaultGameObjectInjectionWorld),"default World unchanged");Debug.Log("[InitialWorldEcbHeaders] result=Passed controls=6 nativeExecutionByRootOnly=True fullQueueCodec=False");
        }
    }
}
#endif
