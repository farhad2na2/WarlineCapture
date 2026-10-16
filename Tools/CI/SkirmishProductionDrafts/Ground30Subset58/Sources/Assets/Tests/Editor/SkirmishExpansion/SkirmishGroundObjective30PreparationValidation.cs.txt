#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Tests.Editor
{
    public static class SkirmishGroundObjective30PreparationValidation
    {
        public static void Run()
        {
            var failures=new List<Exception>();
            Collect("GroundAuthoring30",Game.Editor.SkirmishGroundSourceFitSurvey.RunGroundObjective30AuthoringControls,failures);
            Collect("StaticBudget5",SkirmishStaticAuthoringBudgetTests.RunFocusedValidation,failures);
            Collect("CollectionControls3",Game.Editor.SkirmishGroundSourceFitSurvey.RunGroundObjective30CollectionControls,failures);
            if(failures.Count!=0)throw new AggregateException("Ground objective authoring preparation failed; all independent failures retained",failures);
            Debug.Log("[SkirmishGroundObjective30Preparation] result=Passed candidates=30 negativeControls=60 budget=5 collector=3 sourceFitCertified=0 normalMissionReadiness=Pending");
        }
        static void Collect(string name,Action action,List<Exception> failures)
        {
            try{action();}
            catch(Exception error){Debug.Log("[SkirmishGroundObjective30Preparation] suiteFailed="+name+" error="+error);failures.Add(new InvalidOperationException(name,error));}
        }
    }
}
#endif
