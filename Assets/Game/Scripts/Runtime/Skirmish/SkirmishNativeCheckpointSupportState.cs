using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Native logical owners, Smoke and Strike flights are reconstructed without copying Campaign progress,
    // baked blob pointers or presentation handles. Air payloads remain fail-closed.
    internal static class SkirmishNativeCheckpointSupportState
    {
        [Serializable] internal sealed class Record
        {
            public string Id, AircraftSourceKey;
            public byte Kind, Faction;
            public uint CatalogRevision;
            public SkirmishNativeCheckpointParts.Part[] Parts;
        }

        internal static Record[] Capture(EntityManager em, Entity session,
            Dictionary<Entity,string> identities, HashSet<string> used)
        {
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var records=new List<Record>();var entities=new List<Entity>();
            using var rootsQuery=em.CreateEntityQuery(typeof(SkirmishSupportOwnerComponent),typeof(SupportSessionComponent));
            using var roots=rootsQuery.ToEntityArray(Allocator.Temp);
            foreach(var root in roots)
            {
                var owner=em.GetComponentData<SkirmishSupportOwnerComponent>(root);
                if(owner.Session!=session && !owner.SessionId.Equals(native.SessionId))continue;
                var state=em.GetComponentData<SupportSessionComponent>(root);
                if(owner.Session!=session || !owner.SessionId.Equals(native.SessionId) ||
                    !state.SessionToken.Equals(native.SessionId) || state.AttemptOrdinal!=1 || state.TestEncounter!=0 ||
                    state.FactionId<1 || state.FactionId>2 || !em.HasComponent<SupportCatalogComponent>(root))
                    throw new InvalidOperationException("NativeSupportOwnerInvalid");
                var catalog=em.GetComponentData<SupportCatalogComponent>(root);
                if(!catalog.Blob.IsCreated || state.CatalogRevision!=catalog.Revision)
                    throw new InvalidOperationException("NativeSupportCatalogInvalid");
                string id="support/root/"+state.FactionId;
                if(!used.Add(id))throw new InvalidOperationException("NativeSupportOwnerAmbiguous");
                identities.Add(root,id);entities.Add(root);
                records.Add(new Record {Id=id,Kind=1,Faction=state.FactionId,CatalogRevision=catalog.Revision});
            }
            using var zonesQuery=em.CreateEntityQuery(typeof(SupportSmokeZoneComponent));
            using var zones=zonesQuery.ToEntityArray(Allocator.Temp);
            foreach(var entity in zones)
            {
                var zone=em.GetComponentData<SupportSmokeZoneComponent>(entity);
                if(!identities.TryGetValue(zone.Root,out string rootId) || !rootId.StartsWith("support/root/",StringComparison.Ordinal))
                    throw new InvalidOperationException("NativeSmokeOwnerMissing");
                var state=em.GetComponentData<SupportSessionComponent>(zone.Root);
                if(!zone.SessionToken.Equals(state.SessionToken) || zone.AttemptOrdinal!=state.AttemptOrdinal)
                    throw new InvalidOperationException("NativeSmokeAttemptMismatch");
                string id="support/smoke/"+records.Count;
                if(!used.Add(id))throw new InvalidOperationException("NativeSmokeAliasDuplicate");
                identities.Add(entity,id);entities.Add(entity);
                records.Add(new Record {Id=id,Kind=2,Faction=state.FactionId});
            }
            using var flightsQuery=em.CreateEntityQuery(typeof(SupportFlightComponent));
            using var flights=flightsQuery.ToEntityArray(Allocator.Temp);
            foreach(var entity in flights)
            {
                var flight=em.GetComponentData<SupportFlightComponent>(entity);
                if(flight.Request.Kind!=SupportAbilityKind.Strike || flight.Payload!=Entity.Null ||
                    !identities.TryGetValue(flight.Root,out string rootId) || !rootId.StartsWith("support/root/",StringComparison.Ordinal) ||
                    !em.HasComponent<SupportFlightCleanupComponent>(entity) || !em.HasBuffer<SupportFuelReservationElement>(entity))
                    throw new InvalidOperationException("NativeUnsupportedSupportFlight");
                var catalog=em.GetComponentData<SupportCatalogComponent>(flight.Root);
                if(!SupportTargetValidationUtilitySystemHelper.TryDefinition(catalog,SupportAbilityKind.Strike,out var definition))
                    throw new InvalidOperationException("NativeStrikeDefinitionMissing");
                string id="support/flight/"+records.Count;
                if(!used.Add(id))throw new InvalidOperationException("NativeStrikeAliasDuplicate");
                identities.Add(entity,id);entities.Add(entity);
                records.Add(new Record {Id=id,Kind=3,Faction=flight.FactionId,AircraftSourceKey=definition.AircraftSourceKey.ToString()});
            }
            string Identity(Entity entity)=>identities.TryGetValue(entity,out string id)?id:null;
            for(int i=0;i<records.Count;i++)records[i].Parts=SkirmishNativeCheckpointParts.Capture(em,entities[i],Identity);
            if(!PublishAliases(records.ToArray(),native.SessionId.ToString(),new HashSet<string>(identities.ValuesWithoutSupport(),StringComparer.Ordinal)))
                throw new InvalidOperationException("NativeSupportStateInvalid");
            return records.ToArray();
        }

        private static IEnumerable<string> ValuesWithoutSupport(this Dictionary<Entity,string> identities)
        {foreach(var id in identities.Values)if(!id.StartsWith("support/",StringComparison.Ordinal))yield return id;}

        internal static bool PublishAliases(Record[] records,string sessionId,HashSet<string> ids)
        {
            if(records==null || records.Length>256)return false;
            var roots=new Dictionary<byte,SupportSessionComponent>();
            foreach(var record in records)
                if(record==null || record.Kind<1 || record.Kind>3 || record.Faction<1 || record.Faction>2 ||
                    string.IsNullOrEmpty(record.Id) || !ids.Add(record.Id))return false;
            foreach(var record in records)
            {
                if(!SkirmishNativeCheckpointParts.Validate(record.Parts,ids))return false;
                if(record.Kind==1)
                {
                    if(record.Id!="support/root/"+record.Faction || !Only(record.Parts,
                        typeof(SkirmishSupportOwnerComponent),typeof(SupportSessionComponent),typeof(SupportMissionPolicyComponent),
                        typeof(SupportInputStateComponent),typeof(SupportPreviewComponent),typeof(SupportProposalComponent),
                        typeof(SupportFuelScopeComponent),typeof(SupportFuelAvailabilityComponent),typeof(SupportAirRouteComponent),typeof(SupportAbilityStateElement),
                        typeof(SupportGroundRegionElement),typeof(SupportRequestElement),typeof(SupportReceiptElement)) ||
                        !SkirmishNativeCheckpointParts.TryReadComponent(record.Parts,out SkirmishSupportOwnerComponent owner) ||
                        owner.SessionId.ToString()!=sessionId || !Reference<SkirmishSupportOwnerComponent>(record.Parts,"$session") ||
                        !SkirmishNativeCheckpointParts.TryReadComponent(record.Parts,out SupportSessionComponent state) ||
                        state.SessionToken.ToString()!=sessionId || state.AttemptOrdinal!=1 || state.TestEncounter!=0 ||
                        state.FactionId!=record.Faction || state.CatalogRevision!=record.CatalogRevision ||
                        !double.IsFinite(state.SimulationSeconds) || state.SimulationSeconds<0 || state.Active>1 ||
                        !roots.TryAdd(record.Faction,state) ||
                        !SkirmishNativeCheckpointParts.TryReadBuffer(record.Parts,out SupportAbilityStateElement[] abilities) || abilities.Length>4 ||
                        !SkirmishNativeCheckpointParts.TryReadBuffer(record.Parts,out SupportRequestElement[] requests) || requests.Length!=0)
                        return false;
                    var kinds=new HashSet<SupportAbilityKind>();
                    foreach(var ability in abilities)
                        if(ability.Kind<SupportAbilityKind.Smoke || ability.Kind>SupportAbilityKind.Supply ||
                            !kinds.Add(ability.Kind) || ability.ChargesRemaining<0 || ability.Enabled>1 ||
                            !double.IsFinite(ability.CooldownUntil) || ability.CooldownUntil<0)return false;
                }
                else if(record.Kind==2 && (!record.Id.StartsWith("support/smoke/",StringComparison.Ordinal) ||
                    !Only(record.Parts,typeof(SupportSmokeZoneComponent)) || record.CatalogRevision!=0))
                    return false;
            }
            foreach(var record in records)if(record.Kind==2)
            {
                if(!roots.TryGetValue(record.Faction,out var state) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(record.Parts,out SupportSmokeZoneComponent zone) ||
                    !Reference<SupportSmokeZoneComponent>(record.Parts,"support/root/"+record.Faction) ||
                    zone.SessionToken.ToString()!=sessionId || zone.AttemptOrdinal!=1 ||
                    !math.all(math.isfinite(zone.Center)) || !float.IsFinite(zone.RadiusSquared) || zone.RadiusSquared<=0 ||
                    !double.IsFinite(zone.ExpiresAt) || zone.ExpiresAt<state.SimulationSeconds || zone.DirectDamagePermille>1000)
                    return false;
            }
            foreach(var record in records)if(record.Kind==3)
            {
                if(!record.Id.StartsWith("support/flight/",StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(record.AircraftSourceKey) || record.AircraftSourceKey.Length>63 || record.CatalogRevision!=0 ||
                    !roots.TryGetValue(record.Faction,out var state) ||
                    !Only(record.Parts,typeof(SupportFlightComponent),typeof(SupportFlightCleanupComponent),
                        typeof(SupportFuelReservationElement),typeof(Unity.Transforms.LocalTransform)) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(record.Parts,out SupportFlightComponent flight) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(record.Parts,out SupportFlightCleanupComponent cleanup) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(record.Parts,out Unity.Transforms.LocalTransform transform) ||
                    !SkirmishNativeCheckpointParts.TryReadBuffer(record.Parts,out SupportFuelReservationElement[] fuel) ||
                    flight.Request.Kind!=SupportAbilityKind.Strike || flight.Request.SessionToken.ToString()!=sessionId ||
                    flight.Request.AttemptOrdinal!=1 || flight.Request.RequestId==0 || flight.FactionId!=record.Faction ||
                    flight.Phase!=SupportExecutionPhase.Approaching && flight.Phase!=SupportExecutionPhase.Released ||
                    !At<SupportFlightComponent>(record.Parts,0,"support/root/"+record.Faction) || !At<SupportFlightComponent>(record.Parts,1,"") ||
                    !At<SupportFlightCleanupComponent>(record.Parts,0,"support/root/"+record.Faction) || !At<SupportFlightCleanupComponent>(record.Parts,1,"") ||
                    cleanup.Request.RequestId!=flight.Request.RequestId || cleanup.Request.Source!=flight.Request.Source ||
                    !SupportTargetValidationUtilitySystemHelper.SameAction(cleanup.Request,flight.Request) ||
                    !SameReference<SupportFlightComponent,SupportFlightCleanupComponent>(record.Parts,2) ||
                    !cleanup.Request.SessionToken.Equals(flight.Request.SessionToken) || cleanup.Request.AttemptOrdinal!=1 ||
                    cleanup.Released!=(flight.Phase==SupportExecutionPhase.Released?1:0) ||
                    !double.IsFinite(flight.StartedAt) || flight.StartedAt<0 || flight.StartedAt>state.SimulationSeconds ||
                    !double.IsFinite(flight.ReleasedAt) || flight.ReleasedAt<0 || flight.ReleasedAt>state.SimulationSeconds ||
                    !float.IsFinite(flight.ApproachSeconds) || flight.ApproachSeconds<=0 || !float.IsFinite(flight.ExitSeconds) || flight.ExitSeconds<=0 ||
                    !math.all(math.isfinite(flight.Entry)) || !math.all(math.isfinite(flight.Release)) || !math.all(math.isfinite(flight.Exit)) ||
                    !math.all(math.isfinite(transform.Position)) || !math.all(math.isfinite(transform.Rotation.value)) ||
                    !float.IsFinite(transform.Scale) || transform.Scale<=0 || flight.RouteVersion==0 || flight.Damage<=0 || fuel.Length==0 || fuel.Length>65536)
                    return false;
                Record rootRecord=null;
                foreach(var root in records)if(root.Kind==1 && root.Faction==record.Faction){rootRecord=root;break;}
                if(rootRecord==null || !SkirmishNativeCheckpointParts.TryReadBuffer(rootRecord.Parts,out SupportReceiptElement[] receipts))return false;
                int receiptIndex=-1;
                for(int i=0;i<receipts.Length;i++)if(receipts[i].RequestId==flight.Request.RequestId && receipts[i].Source==flight.Request.Source)
                {if(receiptIndex>=0)return false;receiptIndex=i;}
                if(receiptIndex<0 || !At<SupportReceiptElement>(rootRecord.Parts,receiptIndex,record.Id))return false;
                var receipt=receipts[receiptIndex];float reserved=0;
                foreach(var reservation in fuel)reserved+=reservation.Amount;
                if(receipt.Kind!=SupportAbilityKind.Strike || receipt.ReservationReleased!=0 || receipt.Reason!=SupportRejectionReason.None ||
                    receipt.Phase!=(flight.Phase==SupportExecutionPhase.Approaching?SupportExecutionPhase.Approaching:SupportExecutionPhase.Resolved) ||
                    receipt.EffectApplied!=(flight.Phase==SupportExecutionPhase.Approaching?0:1) ||
                    receipt.ReservedFuel!=(flight.Phase==SupportExecutionPhase.Approaching?reserved:0) ||
                    receipt.SpentFuel!=(flight.Phase==SupportExecutionPhase.Approaching?0:reserved))return false;
                foreach(var reservation in fuel)
                    if(!float.IsFinite(reservation.Amount) || reservation.Amount<=0 || reservation.RequestId!=flight.Request.RequestId ||
                        reservation.Status!=(flight.Phase==SupportExecutionPhase.Approaching?0:1))return false;
            }
            return true;
        }

        private static bool Only(SkirmishNativeCheckpointParts.Part[] parts,params Type[] types)
        {
            foreach(var part in parts)
            {bool found=false;foreach(var type in types)if(part.Type==type.FullName){found=true;break;}if(!found)return false;}
            return true;
        }
        private static bool At<T>(SkirmishNativeCheckpointParts.Part[] parts,int index,string id)
        {foreach(var part in parts)if(part.Type==typeof(T).FullName)return part.References.Length>index && part.References[index].ObjectId==id;return false;}
        private static bool SameReference<T,U>(SkirmishNativeCheckpointParts.Part[] parts,int index)
        {
            string first=null,second=null;
            foreach(var part in parts) {
                if(part.Type==typeof(T).FullName && part.References.Length>index)first=part.References[index].ObjectId;
                if(part.Type==typeof(U).FullName && part.References.Length>index)second=part.References[index].ObjectId;
            }
            return !string.IsNullOrEmpty(first) && first==second;
        }
        private static bool Reference<T>(SkirmishNativeCheckpointParts.Part[] parts,string id)
        {foreach(var part in parts)if(part.Type==typeof(T).FullName)return part.References.Length==1 && part.References[0].ObjectId==id;return false;}

        internal static void Rebuild(EntityManager em,Entity session,Record[] records,
            SkirmishNativeCheckpointTransientState.Bindings bindings)
        {
            if(records.Length==0)return;
            // Rebind current baked configuration only; the restored owner has no Campaign runtime/progress.
            using var query=em.CreateEntityQuery(typeof(SupportCatalogComponent));
            using var candidates=query.ToEntityArray(Allocator.Temp);
            foreach(var record in records)
            {
                Entity entity;
                if(record.Kind==3)
                {
                    var prefab=SupportAirValidationUtilitySystemHelper.ResolveAircraftPrefab(em,new FixedString64Bytes(record.AircraftSourceKey));
                    if(prefab==Entity.Null)throw new ArgumentException("Native Strike aircraft source is missing or ambiguous.");
                    entity=SupportAirValidationUtilitySystemHelper.CreateFlightPresentation(em,prefab,float3.zero,SupportAbilityKind.Strike);
                }
                else entity=em.CreateEntity();
                bindings.Created.Add(entity);
                em.AddComponentData(entity,new SkirmishNativeTransientOwner {
                    SessionId=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId });
                bindings.Identities.Add(record.Id,entity);
                if(record.Kind!=1)continue;
                Entity template=Entity.Null;
                foreach(var candidate in candidates)
                {
                    var catalog=em.GetComponentData<SupportCatalogComponent>(candidate);
                    if(!catalog.Blob.IsCreated || catalog.Revision!=record.CatalogRevision || em.HasComponent<SkirmishSupportOwnerComponent>(candidate))continue;
                    if(template!=Entity.Null)throw new ArgumentException("Native Support configuration is ambiguous.");
                    template=candidate;
                }
                if(template==Entity.Null)throw new ArgumentException("Native Support configuration is missing.");
                var fresh=em.GetComponentData<SupportCatalogComponent>(template);fresh.OwnsBlob=0;
                em.AddComponentData(entity,fresh);
                if(em.HasComponent<SupportSmokeVisualConfigComponent>(template))
                    em.AddComponentData(entity,em.GetComponentData<SupportSmokeVisualConfigComponent>(template));
                if(em.HasComponent<SupportPayloadBindingsComponent>(template))
                    em.AddComponentData(entity,em.GetComponentData<SupportPayloadBindingsComponent>(template));
            }
            foreach(var record in records)if(record.Kind==3)
            {
                var root=bindings.Identities["support/root/"+record.Faction];
                if(!SupportTargetValidationUtilitySystemHelper.TryDefinition(em.GetComponentData<SupportCatalogComponent>(root),SupportAbilityKind.Strike,out var definition) ||
                    definition.AircraftSourceKey.ToString()!=record.AircraftSourceKey)
                    throw new ArgumentException("Native Strike aircraft source differs from current configuration.");
            }

        }
    }
}
