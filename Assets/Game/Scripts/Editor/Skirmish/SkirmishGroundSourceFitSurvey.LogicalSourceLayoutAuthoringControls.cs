#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Parameterized idle-Editor control, not an executeMethod route. Deliberately
        // unrun by the portable proposal checks; no scene/World or asset writes.
        internal static void CheckLogicalSourceLayoutAuthoringInIdleEditor(JObject mission,
            OperationMapDefinition source,SkirmishMapLayoutConfig supplied,JObject sourcePins,string layoutFingerprint)
        {
            Require(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,"Idle authoring control required");
            string before=JsonUtility.ToJson(supplied),sourceBefore=JsonUtility.ToJson(source);
            Require(TryCreateGroundBaLogicalSourceLayoutInput(source,supplied,sourcePins,layoutFingerprint,out var input,out string error),error);
            GroundObjective30CandidateOwner owner=null;
            try
            {
                Require(TryCreateGroundBa10Candidate(mission,source,input,out owner,out error),error);
                input.Dispose(); // Candidate owns its independent clone, not this input.
                Require(owner.Definition.MapLayout==owner.Layout&&owner.Definition.CampaignMap==source&&owner.AreaDraft==null&&
                    owner.Definition.MissionLogicalBinding==null&&!owner.Layout.WorldBinding.HasMeasuredDeployments&&
                    !owner.Layout.WorldBinding.BindAllConfigurations,"Unqualified independent candidate required");
                var sourceJson=JObject.Parse(before);var cloneJson=JObject.Parse(JsonUtility.ToJson(owner.Layout));
                sourceJson.Remove("layoutId");cloneJson.Remove("layoutId");
                Require(JToken.DeepEquals(sourceJson,cloneJson),"Every full typed recipe field must roundtrip except candidate identity");
                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out error),error);
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                        budget!=null&&budget.ExactCanonicalQuantities&&!budget.NormalCompilerPassed,
                        "Actual canonical diagnostic budget without normal compiler acceptance required: "+string.Join(";",reasons));
                }
                Require(JsonUtility.ToJson(supplied)==before&&JsonUtility.ToJson(source)==sourceBefore,"Caller layout or Campaign source mutated");
            }
            finally {owner?.Dispose();input.Dispose();}
        }
    }
}
#endif
