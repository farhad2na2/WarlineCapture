#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishProtectedExpandedCounterpartControls
    {
        static void Need(bool value,string error){if(!value)throw new InvalidOperationException(error);}
        public static void RunFocusedValidation()
        {
            VerifySourceBoundClassification();
            VerifyPracticeThroughNormalStartAndOldSave();
            VerifyExplicitSelectionAndDriftRefusal();
            SkirmishGroundSourceFitSurvey.RunProtectedExpandedCounterpartAuthoringControls();
            Debug.Log("[ProtectedExpandedCounterpartControls] result=Passed methods=4 practiceNormalStarts=3 sourceBoundExpandedIds=116 canonicalBudgetVectors=9 actualExpandedLaunches=0 baselineCredit=0");
        }

        public static void VerifySourceBoundClassification()
        {
            int sourceBound=0;
            for(int n=1;n<=120;n++)
            {
                string id="S"+n.ToString("D3");bool coded=id=="S002"||id=="S003"||id=="S004"||id=="S005";
                Need(SkirmishMissionDispatch.RequiresSourceBoundDefinition(id)==!coded,"Expanded source-bound classification differs: "+id);
                var setup=new SkirmishResolvedSetup{CatalogId=id,ObjectiveKind=Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault};
                Need(SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)==!coded,"BA logical provider gate differs: "+id);
                if(!coded)sourceBound++;
            }
            Need(sourceBound==116&&!SkirmishMissionDispatch.RequiresSourceBoundDefinition("S121"),"Exact expanded source-bound scope required");
            foreach(var pair in new[]{("S001",0),("S025",1),("S073",3),("S002",4),("S003",5),("S004",6),("S005",7)})
                Need(SkirmishMissionDispatch.TryGetLegacyIndex(pair.Item1,out int index)&&index==pair.Item2,"Protected transport binding changed");
        }

        public static void VerifyPracticeThroughNormalStartAndOldSave()
        {
            foreach(var pair in new[]{("S001",0), ("S025",1), ("S073",3)})
            {
                string path=Path.Combine(Path.GetTempPath(),"warline-counterpart-store-"+Guid.NewGuid().ToString("N"));
                try
                {
                    var save=new SaveService(new JsonSaveRepository(path));var store=new QuickCustomGameConfigStore(save);
                    var config=store.Current;config.ScenarioIndex=pair.Item2;config.MapSeed=104729+pair.Item2;
                    config.SkirmishSelection=new UiSkirmishLibrarySelection{Kind=UiSkirmishLibraryLaunchKind.PracticePreset,CatalogId=pair.Item1};
                    store.Apply(config);config=store.Current;config.MapSeed++;store.Apply(config);
                    Need(store.Current.SkirmishSelection.Kind==UiSkirmishLibraryLaunchKind.PracticePreset&&store.Current.SkirmishSelection.CatalogId==pair.Item1,
                        "Ordinary seed adjustment lost selected practice identity");
                    using var world=new World("Protected practice normal Start control");var em=world.EntityManager;
                    var shell=em.CreateEntity(typeof(UiShellRootComponent),typeof(UiShellStateComponent));em.AddBuffer<UiShellRouteRequestComponent>(shell);
                    Need(new MatchLaunchCommand(store).QueueMatchRoute(em),"Existing normal Start refused practice");
                    Need(SkirmishLaunchProjection.TryGet(em,out var session,out var match)&&match.ScenarioIndex==pair.Item2&&
                        match.Seed==config.MapSeed&&!em.HasComponent<SkirmishExpandedSessionComponent>(session)&&
                        em.HasComponent<SkirmishLaunchSnapshot>(session),"Practice acquired expanded identity or lost actual native preset snapshot");
                    var actual=em.GetComponentObject<SkirmishLaunchSnapshot>(session).Configuration;
                    Need(actual.Difficulty==AIDifficultySetting.Normal&&!actual.FogOfWar&&actual.IntelReveal,"Existing legacy actual settings changed");
                    var raw=File.ReadAllText(Path.Combine(path,SaveService.QuickGameFileName));
                    Need(!raw.Contains("SkirmishSelection")&&!raw.Contains("ExpandedDefinition"),"Process-only counterpart selection changed old QuickGame save schema");
                    Need(new QuickCustomGameConfigStore(save).Current.SkirmishSelection.Kind==UiSkirmishLibraryLaunchKind.Unspecified,
                        "Reopening an old practice config inferred an expanded selection");
                }
                finally{if(Directory.Exists(path))Directory.Delete(path,true);}
            }
        }

        public static void VerifyExplicitSelectionAndDriftRefusal()
        {
            var shipped=Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName);
            Need(shipped!=null&&ReferenceEquals(shipped,SkirmishBattleCatalogConfig.Load()),"Actual unscoped main catalog required");
            var originalJson=JsonUtility.ToJson(shipped);var rows=shipped.Entries.ToArray();
            var authored=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            var actualVersion=authored.Definitions.First(d=>d!=null).ContentVersion;
            Need(actualVersion>0,"Actual authored content version required by synthetic selection fixture");
            int at=System.Array.FindIndex(rows,r=>r.ScenarioId=="S001");Need(at>=0,"Actual canonical row missing");
            rows[at].DefinitionId="skirmish.s001";rows[at].ContentVersion=actualVersion;
            var candidate=ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();candidate.Configure(rows);
            string temp=Path.Combine(Path.GetTempPath(),"warline-counterpart-selection-"+Guid.NewGuid().ToString("N"));
            var root=new GameObject("Counterpart ordinary setup selection",typeof(RectTransform));
            string pref="Warline.Skirmish.LastScenarioId";bool hadPref=PlayerPrefs.HasKey(pref);string oldPref=PlayerPrefs.GetString(pref);
            try
            {
                using(var scope=SkirmishBattleCatalogConfig.UseEditorValidationCatalog(candidate))
                {
                    Need(SkirmishMissionDispatch.TryGetSelectedExpanded(candidate,"S001","skirmish.s001",actualVersion,0,out _),"Explicit actual-index counterpart selection refused");
                    Need(SkirmishMissionDispatch.TryGetGenericByIndex(candidate,0,out var exact)&&exact.ScenarioId=="S001","Source-bound counterpart unavailable by its unchanged transport token");
                    var store=new QuickCustomGameConfigStore(new SaveService(new JsonSaveRepository(temp)));
                    var view=root.AddComponent<QuickCustomScreenView>();var fields=new SerializedObject(view);
                    fields.FindProperty("baseAssaultPreset").boolValue=true;fields.ApplyModifiedPropertiesWithoutUndo();
                    view.BindRuntimeDependencies(store,new MatchLaunchCommand(store));view.Bind(store.Current);view.SelectScenarioId("S001");
                    var selected=store.Current.SkirmishSelection;
                    Need(selected.Kind==UiSkirmishLibraryLaunchKind.ExpandedDefinition&&selected.CatalogId=="S001"&&
                        selected.DefinitionId=="skirmish.s001"&&selected.ContentVersion==actualVersion,"Public existing card selection lost explicit expanded identity");
                    var changed=store.Current;changed.MapSeed=155922;store.Apply(changed);
                    Need(store.Current.SkirmishSelection.ContentVersion==actualVersion,"Seed edit lost expanded version");
                    using var world=new World("Counterpart stale selection refusal");
                    var damaged=store.Current.SkirmishSelection;damaged.Kind=UiSkirmishLibraryLaunchKind.Unspecified;
                    Need(!SkirmishLaunchProjection.TryQueueSelected(world.EntityManager,store.CurrentConfig,damaged),"Partial expanded descriptor downgraded to practice");
                    var stale=store.Current.SkirmishSelection;stale.ContentVersion++;
                    Need(!SkirmishLaunchProjection.TryQueueSelected(world.EntityManager,store.CurrentConfig,stale),"Changed selected definition version queued");
                    var duplicate=rows.Concat(new[]{rows[at]}).ToArray();candidate.Configure(duplicate);
                    Need(!SkirmishMissionDispatch.TryGetSelectedExpanded(candidate,"S001","skirmish.s001",actualVersion,0,out _),"Ambiguous same-ID candidate admitted");
                    candidate.Configure(rows);
                }
                using(var world=new World("Counterpart scope-loss refusal"))
                {
                    var selected=new UiSkirmishLibrarySelection{Kind=UiSkirmishLibraryLaunchKind.ExpandedDefinition,CatalogId="S001",DefinitionId="skirmish.s001",ContentVersion=actualVersion};
                    Need(!SkirmishLaunchProjection.TryQueueSelected(world.EntityManager,QuickGameConfig.Defaults,selected),"Scope loss silently queued practice instead");
                    using var native=world.EntityManager.CreateEntityQuery(typeof(SkirmishMatchState));Need(native.IsEmptyIgnoreFilter,"Rejected selection created a native session");
                }
                // Reproduce a cached expanded briefing at the same ID and seed;
                // the explicit practice card must clear it rather than retain
                // the stale expanded roster/rules. This is cache fixture data.
                var shown=root.GetComponent<QuickCustomScreenView>();
                var cached=typeof(QuickCustomScreenView).GetField("_briefingSetup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                cached.SetValue(shown,new SkirmishResolvedSetup());
                shown.SelectScenarioId("S001");
                Need(shown.ReadConfigFromControls().SkirmishSelection.Kind==UiSkirmishLibraryLaunchKind.PracticePreset&&cached.GetValue(shown)==null,
                    "Same-ID practice selection retained expanded briefing facts");
                Need(ReferenceEquals(shipped,SkirmishBattleCatalogConfig.Load())&&JsonUtility.ToJson(shipped)==originalJson&&!EditorUtility.IsDirty(shipped),"Isolated same-ID catalog changed main library");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(candidate);
                if(hadPref)PlayerPrefs.SetString(pref,oldPref);else PlayerPrefs.DeleteKey(pref);PlayerPrefs.Save();
                if(Directory.Exists(temp))Directory.Delete(temp,true);
            }
        }
    }
}
#endif
