using Unity.Collections;

public static class Chapter01M01SpritePresenterCatalog
{
    public const string DecorCommandPointEntityId = "decor.command_point";
    public const string DestroyedSmallVfxSpriteId = "vfx.unit.destroyed.small";

    public static bool TryCreatePresenter(string runtimeEntityId, Chapter01TacticalAtlasContract atlasContract, out MissionRuntimeSpritePresenter presenter)
    {
        presenter = default;
        if (string.IsNullOrEmpty(runtimeEntityId))
            return false;

        if (runtimeEntityId != Chapter01M01PlayableRuntime.PlayerSquadEntityId &&
            runtimeEntityId != Chapter01M01PlayableRuntime.EnemyPatrolEntityId &&
            runtimeEntityId != DecorCommandPointEntityId)
        {
            return false;
        }

        if (atlasContract != null)
        {
            if (!atlasContract.TryGetSprite(runtimeEntityId, out _) ||
                !atlasContract.TryGetSprite(DestroyedSmallVfxSpriteId, out _))
            {
                return false;
            }
        }

        FixedString64Bytes spriteId = new(runtimeEntityId);
        FixedString64Bytes destroyedVfxId = new(DestroyedSmallVfxSpriteId);
        presenter = new MissionRuntimeSpritePresenter
        {
            RuntimeEntityId = new FixedString64Bytes(runtimeEntityId),
            ManifestAssetId = new FixedString64Bytes(runtimeEntityId),
            IdleSpriteId = spriteId,
            MoveSpriteId = spriteId,
            AttackSpriteId = spriteId,
            DamagedSpriteId = spriteId,
            DestroyedSpriteId = destroyedVfxId,
            DestructionVfxSpriteId = destroyedVfxId,
            CurrentSpriteId = spriteId,
            CurrentState = (byte)MissionRuntimeSpriteVisualState.Idle,
            RequiresFixedDirectionBakedContactShadow = 1,
            UsesSeparateDestroyedChild = 0,
            FinalAtlasArtReady = 0
        };
        return true;
    }

    public static bool TryCreatePresenter(string runtimeEntityId, out MissionRuntimeSpritePresenter presenter)
    {
        return TryCreatePresenter(runtimeEntityId, null, out presenter);
    }

    public static FixedString64Bytes ResolveSpriteId(in MissionRuntimeSpritePresenter presenter, MissionRuntimeSpriteVisualState state)
    {
        return state switch
        {
            MissionRuntimeSpriteVisualState.Move => presenter.MoveSpriteId,
            MissionRuntimeSpriteVisualState.Attack => presenter.AttackSpriteId,
            MissionRuntimeSpriteVisualState.Damaged => presenter.DamagedSpriteId,
            MissionRuntimeSpriteVisualState.Destroyed => presenter.DestroyedSpriteId,
            _ => presenter.IdleSpriteId
        };
    }
}
