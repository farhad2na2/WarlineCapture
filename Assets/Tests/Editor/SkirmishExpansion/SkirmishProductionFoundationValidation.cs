using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishProductionFoundationValidation
    {
        public static void RunFocusedValidation()
        {
            SkirmishAuthoredDefinitionLookupTests.RunFocusedValidation();
            SkirmishConfigOnlyBatchBuilderTests.RunFocusedValidation();
            global::RtsSelectionInputSystemTests.RunSelectModeUiDragValidation();
            Debug.Log("[SkirmishProductionFoundationValidation] result=Passed");
        }
    }
}
