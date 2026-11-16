"""Instrument exact original enemy calls in an opt-in source-only projection."""


def apply(arguments, output, root):
    owner = root / 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCommanderService.cs'
    economy = root / 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyEconomicCoordinator.cs'
    replacements = {
        owner: {
            'SkirmishEnemyEconomicCoordinator.Reconcile(em,session);':
                'SourceS004EnemyTiming.Measure("reconcile",()=>SkirmishEnemyEconomicCoordinator.Reconcile(em,session));',
            'var facts = SkirmishEnemyPerceptionBuilder.Build(em, session, setup);':
                'var facts = SourceS004EnemyTiming.Measure("perception",()=>SkirmishEnemyPerceptionBuilder.Build(em, session, setup));',
            'var ownLiving = SkirmishEnemyCapabilityService.ReadLivingRoleCounts(em,session);':
                'var ownLiving = SourceS004EnemyTiming.Measure("living_roles",()=>SkirmishEnemyCapabilityService.ReadLivingRoleCounts(em,session));',
            'SkirmishEnemyCapabilityService.RemoveReservedGroundGroups(em,session,facts);':
                'SourceS004EnemyTiming.Measure("remove_reserved",()=>SkirmishEnemyCapabilityService.RemoveReservedGroundGroups(em,session,facts));',
        },
        economy: {
            'SkirmishEnemyConstructionAdapter.Tick(em,session);':
                'SourceS004EnemyTiming.Measure("construction_tick",()=>SkirmishEnemyConstructionAdapter.Tick(em,session));',
        },
    }
    for index, (path, changes) in enumerate(replacements.items()):
        source = path.read_text()
        for old, new in changes.items():
            if source.count(old) != 1:
                raise ValueError('Original enemy timing anchor changed: ' + old)
            source = source.replace(old, new)
        dest = output / ('S004EnemyTimingProjection%d.cs' % index)
        dest.write_text(source)
        matches = [i for i, value in enumerate(arguments) if value == str(path)]
        if len(matches) != 1:
            raise ValueError('Exact original enemy owner occurrence required: ' + str(path))
        arguments[matches[0]] = str(dest)
    # S004's admitted null-policy path uses the already-generated strategy
    # projection rather than the commander. Measure its retained guard and
    # original perception call without changing either operation.
    strategy = output / 'S004EnemyStrategyProjection.cs'
    source = strategy.read_text()
    anchor = 'SourceS004CompiledSetupInput.RequireExact('
    if source.count(anchor) != 1:
        raise ValueError('Exact S004 setup guard required')
    start = source.index(anchor)
    end = source.index(';', start)
    call = source[start:end]
    source = source[:start] + 'SourceS004EnemyTiming.Measure("setup_guard",()=>'+call+')' + source[end:]
    old = 'SkirmishPublicPerception perception = Perceive(em, session, owned);'
    new = 'SkirmishPublicPerception perception = SourceS004EnemyTiming.Measure("fallback_perception",()=>Perceive(em, session, owned));'
    if source.count(old) != 1:
        raise ValueError('Exact fallback perception call required')
    source = source.replace(old, new)
    dest = output / 'S004EnemyTimingProjection2.cs'
    dest.write_text(source)
    matches = [i for i, value in enumerate(arguments) if value == str(strategy)]
    if len(matches) != 1:
        raise ValueError('Exact S004 strategy projection occurrence required')
    arguments[matches[0]] = str(dest)
