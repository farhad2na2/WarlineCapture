using Unity.Collections;

public static class Chapter01M01SpritePresenterCatalog
{
    public const string DecorCommandPointEntityId = "decor.command_point";
    public const string DestroyedSmallVfxSpriteId = "vfx.unit.destroyed.small";
    public const string IdleStateSuffix = ".idle";
    public const string MoveStateSuffix = ".move";
    public const string AttackStateSuffix = ".attack";
    public const string DamagedStateSuffix = ".damaged";
    public const string DeathStateSuffix = ".death";

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

        FixedString64Bytes idleSpriteId = new(ResolveStateSpriteId(runtimeEntityId, MissionRuntimeSpriteVisualState.Idle));
        FixedString64Bytes moveSpriteId = new(ResolveStateSpriteId(runtimeEntityId, MissionRuntimeSpriteVisualState.Move));
        FixedString64Bytes attackSpriteId = new(ResolveStateSpriteId(runtimeEntityId, MissionRuntimeSpriteVisualState.Attack));
        FixedString64Bytes damagedSpriteId = new(ResolveStateSpriteId(runtimeEntityId, MissionRuntimeSpriteVisualState.Damaged));
        bool usesV2SoldierAtlas =
            runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId;
        FixedString64Bytes destroyedSpriteId = new(usesV2SoldierAtlas
            ? ResolveStateSpriteId(runtimeEntityId, MissionRuntimeSpriteVisualState.Destroyed)
            : DestroyedSmallVfxSpriteId);
        FixedString64Bytes destroyedVfxId = new(DestroyedSmallVfxSpriteId);
        presenter = new MissionRuntimeSpritePresenter
        {
            RuntimeEntityId = new FixedString64Bytes(runtimeEntityId),
            ManifestAssetId = new FixedString64Bytes(runtimeEntityId),
            IdleSpriteId = idleSpriteId,
            MoveSpriteId = moveSpriteId,
            AttackSpriteId = attackSpriteId,
            DamagedSpriteId = damagedSpriteId,
            DestroyedSpriteId = destroyedSpriteId,
            DestructionVfxSpriteId = destroyedVfxId,
            CurrentSpriteId = idleSpriteId,
            CurrentState = (byte)MissionRuntimeSpriteVisualState.Idle,
            RequiresFixedDirectionBakedContactShadow = 1,
            UsesSeparateDestroyedChild = 0,
            FinalAtlasArtReady = usesV2SoldierAtlas ? (byte)1 : (byte)0
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

    public static string ResolveStateSpriteId(string runtimeEntityId, MissionRuntimeSpriteVisualState state)
    {
        return state switch
        {
            MissionRuntimeSpriteVisualState.Move => runtimeEntityId + MoveStateSuffix,
            MissionRuntimeSpriteVisualState.Attack => runtimeEntityId + AttackStateSuffix,
            MissionRuntimeSpriteVisualState.Damaged => runtimeEntityId + DamagedStateSuffix,
            MissionRuntimeSpriteVisualState.Destroyed => runtimeEntityId + DeathStateSuffix,
            _ => runtimeEntityId + IdleStateSuffix
        };
    }
}
