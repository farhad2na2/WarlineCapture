#if UNITY_EDITOR
using System;
using Game.Configs;
using UnityEngine;

namespace Game.Editor
{
    // Development availability for the unchanged normal Menu/input path. Mirrors
    // the existing S005 Editor-memory availability scope; never changes shipped
    // assets, PublicationStatus, acceptance evidence, entitlement or readiness.
    public sealed class SkirmishGenericMissionAvailabilityScope:IDisposable
    {
        private SkirmishBattleCatalogConfig candidate;
        private IDisposable libraryScope,authoredScope;
        public static SkirmishGenericMissionAvailabilityScope Open(SkirmishExpansionCatalogConfig exactAuthored,string catalogId,int explicitIndex)
        {
            if(exactAuthored==null||!exactAuthored.TryGetDefinition(catalogId,out var definition)||definition==null||
                !SkirmishMissionDispatch.Canonical(catalogId)||SkirmishMissionDispatch.TryGetLegacyIndex(catalogId,out _)||explicitIndex<=7||
                definition.CampaignMap==null||definition.MapLayout==null)
                throw new ArgumentException("Explicit authored generic definition/source/layout and unreserved test index required.");
            var shipped=SkirmishBattleCatalogConfig.Load();if(shipped==null)throw new InvalidOperationException("Actual library catalog missing.");
            var rows=new SkirmishBattleCatalogEntry[shipped.Entries.Count];int found=0;
            for(int i=0;i<rows.Length;i++)
            {
                rows[i]=shipped.Entries[i];
                if(rows[i].ScenarioId!=catalogId)
                {
                    if(rows[i].IsPlayable&&rows[i].PlayableScenarioIndex==explicitIndex)throw new ArgumentException("Test dispatch index collides with another available mission.");
                    continue;
                }
                found++;rows[i].Status=SkirmishBattleCatalogStatus.Playable;rows[i].PlayableScenarioIndex=explicitIndex;
                rows[i].DefinitionId=definition.DefinitionId;rows[i].ContentVersion=definition.ContentVersion;
            }
            if(found!=1)throw new ArgumentException("Exactly one existing logical catalog entry required; no invented missions.");
            var scope=new SkirmishGenericMissionAvailabilityScope();
            try
            {
                scope.candidate=ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();scope.candidate.hideFlags=HideFlags.HideAndDontSave;scope.candidate.Configure(rows);
                scope.authoredScope=SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(exactAuthored);
                scope.libraryScope=SkirmishBattleCatalogConfig.UseEditorValidationCatalog(scope.candidate);
                Debug.Log("[SkirmishCandidateAvailability] catalog="+catalogId+" index="+explicitIndex+
                    " scope=EditorMemoryOnly ordinaryMenu=1 publicationModified=0 readinessGranted=0 shippedCatalogModified=0");
                return scope;
            }
            catch{scope.Dispose();throw;}
        }
        public void Dispose()
        {
            libraryScope?.Dispose();libraryScope=null;authoredScope?.Dispose();authoredScope=null;
            if(candidate!=null)UnityEngine.Object.DestroyImmediate(candidate);candidate=null;
        }
    }
}
#endif
