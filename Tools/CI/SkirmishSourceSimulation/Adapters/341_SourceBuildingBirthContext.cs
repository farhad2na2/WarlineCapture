using System;using Unity.Entities;using Unity.Collections;namespace Game.Runtime{internal readonly struct SourceBuildingBirthContext
    {
        readonly bool legacy;internal SourceBuildingBirthContext(bool legacy){this.legacy=legacy;}
        internal bool IsLegacy=>legacy;
        internal readonly struct SourceIdentity { internal Entity Grid => Entity.Null; }
        internal SourceIdentity Source => default;
        internal static void RequireLegacyNoProvider(EntityManager em){using var q=em.CreateEntityQuery(new EntityQueryDesc{Options=EntityQueryOptions.IncludePrefab|EntityQueryOptions.IncludeDisabledEntities});foreach(var e in q.ToEntityArray(Allocator.Temp).ToArray())if(em.HasComponentNamed(e,"Game.Runtime.RuntimeActorBirthSourceProvider"))throw new NotSupportedException("Legacy birth provider unsupported");}
        internal void StampFresh(EntityManager em, Entity actor, Entity actualGrid, bool pathingBlocker = false){if(!legacy){RuntimeActorBirthContext.RequireNoRecordProfile(em,actor);return;}if(!em.Exists(actor))return;RequireLegacyNoProvider(em);const string name="Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord";foreach(var type in em.ComponentTypes(actor))if(type.FullName==name)em.RemoveComponent(actor,type);em.RemoveInitialNativeFactNamed(actor,name);if(em.HasComponentNamed(actor,name))throw new NotSupportedException("Inherited birth record removal failed");}
    }}