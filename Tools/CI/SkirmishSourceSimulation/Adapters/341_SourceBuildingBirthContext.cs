using Unity.Entities;namespace Game.Runtime{internal readonly struct SourceBuildingBirthContext
    {
        internal readonly struct SourceIdentity { internal Entity Grid => Entity.Null; }
        internal SourceIdentity Source => default;
        internal void StampFresh(EntityManager em, Entity actor, Entity actualGrid, bool pathingBlocker = false)
            => RuntimeActorBirthContext.RequireNoRecordProfile(em, actor);
    }}