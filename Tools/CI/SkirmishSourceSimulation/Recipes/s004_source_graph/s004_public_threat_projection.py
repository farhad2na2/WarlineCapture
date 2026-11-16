"""Generate a scoped source-only S004 public-threat projection from pinned owners."""
from pathlib import Path


def apply(arguments, output, root):
    catalog = root / 'Tools/CI/SkirmishSourceSimulation/Adapters/223_PublicCommandCatalog.cs'
    threat = root / 'Tools/CI/SkirmishSourceSimulation/Adapters/481_SourceAriaPublicHomeThreat.cs'
    source = catalog.read_text()
    start = ' var id=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;'
    end = '\n return view;}\n}\n}'
    if source.count(start) != 1 or source.count(end) != 1:
        raise ValueError('Original public target projection changed')
    before, rest = source.split(start)
    body, after = rest.split(end)
    body = start + body
    if body.count('view.Targets.Add(') != 1:
        raise ValueError('Original public target append changed')
    helper = body.replace('view.Targets.Add(', 'targets.Add(')
    derived = (before + ' view.Targets.AddRange(ReadVisibleTargets(em,session));' + end[:-3] +
               '\n internal static List<TargetControl> ReadVisibleTargets(EntityManager em,Entity session){' +
               'var targets=new List<TargetControl>();' + helper + 'return targets;}\n}\n}')
    dest_catalog = output / 'S004PublicTargetProjection.cs'
    dest_catalog.write_text(derived)

    original = threat.read_text()
    anchor = '   SourceGroundMissionFamily.RequireSession(em,session);\n'
    if original.count(anchor) != 1 or original.count('foreach(var target in publicCatalog.Targets)') != 1:
        raise ValueError('Original public threat projection changed')
    replacement = '''   SourceGroundMissionFamily.RequireSession(em,session);
   return ReadTargets(em,session,publicCatalog.Targets);
  }
  internal static SourceAriaHomeThreat? ReadS004(EntityManager em,Entity session){
   if(em==null||!em.Exists(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||!em.HasComponent<SkirmishMatchState>(session)||!em.HasComponent<SkirmishLaunchDefinitionRecord>(session))throw new NotSupportedException("Actual S004 session required");
   var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);var match=em.GetComponentData<SkirmishMatchState>(session);var launch=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
   if(setup==null||setup.CatalogId!="S004"||state.IsLegacy!=0||state.SessionId.IsEmpty||state.SessionId!=match.SessionId||state.CatalogId.ToString()!=setup.CatalogId||state.DefinitionId.ToString()!=setup.DefinitionId||state.ContentVersion!=setup.ContentVersion||state.SetupHash!=setup.SetupHash||state.Seed!=setup.Seed||match.Seed!=setup.Seed||state.SizeId!=setup.SizeId||state.DifficultyId!=setup.DifficultyId||state.ObjectiveKind!=setup.ObjectiveKind||launch==null||!launch.MatchesIdentity(setup,state)||match.ScenarioIndex!=6||match.Phase!=SkirmishPhase.Playing||match.Outcome!=SkirmishOutcome.None||state.Phase!=SkirmishSessionPhase.Playing||!PublicCommandCatalog.ActiveSession(em,session))throw new NotSupportedException("Active exact S004 identity required");
   return ReadTargets(em,session,PublicCommandCatalog.ReadVisibleTargets(em,session));
  }
  static SourceAriaHomeThreat? ReadTargets(EntityManager em,Entity session,System.Collections.Generic.IReadOnlyList<PublicCommandCatalog.TargetControl> targets){
'''
    derived = original.replace(anchor, replacement).replace('foreach(var target in publicCatalog.Targets)', 'foreach(var target in targets)')
    dest_threat = output / 'S004PublicHomeThreatProjection.cs'
    dest_threat.write_text(derived)
    for src, dst in ((catalog, dest_catalog), (threat, dest_threat)):
        matches = [i for i, value in enumerate(arguments) if value == str(src)]
        if len(matches) != 1:
            raise ValueError('Exact original source occurrence required: ' + str(src))
        arguments[matches[0]] = str(dst)
