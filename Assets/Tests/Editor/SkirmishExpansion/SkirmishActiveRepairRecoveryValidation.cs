#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishActiveRepairRecoveryValidation
    {
        public static void RunFocusedValidation()
        {
            var failures=new List<Exception>();
            RunSuite("Rules8",()=>
            {
            var rules=new VehicleRepairRulesTests();
            rules.FullQuarterCostsTwentyAndTakesTwentySimulationSeconds();rules.SmallMissingHealthProratesWithCeilingCostAndDuration();
            rules.DestructionNoRefundAndCancellationUsesExactWorkBoundary();rules.HealthUpgradeRetainsAcceptedFractionAndDoesNotChargeAgain();
            rules.IncomingDamageIsDetectedIncludingDuringHealthUpgrade();rules.CompletionClampsToMissingHealthWithoutRevival();
            rules.InvalidHealthAndFractionsNeverQuoteOrHeal();rules.ExtremeIntegerHealthDoesNotOverflowQuoteOrDamageMath();
            Debug.Log("[VehicleRepairRules] result=Passed tests=8");
            },failures);
            RunSuite("VehicleFieldRepairNativeTests",VehicleFieldRepairNativeTests.RunFocusedValidation,failures);
            RunSuite("SkirmishNativeRepairCheckpointHelperTests",SkirmishNativeRepairCheckpointHelperTests.RunFocusedValidation,failures);
            RunSuite("SkirmishNativeRepairWireTests",SkirmishNativeRepairWireTests.RunFocusedValidation,failures);
            RunSuite("SkirmishActiveRepairRecoveryIntegrationTests",SkirmishActiveRepairRecoveryIntegrationTests.RunFocusedValidation,failures);
            RunSuite("SkirmishNativeCheckpointCodecTests",SkirmishNativeCheckpointCodecTests.RunFocusedValidation,failures);
            RunSuite("SkirmishNativeCheckpointRecoveryPreparationTests",SkirmishNativeCheckpointRecoveryPreparationTests.RunFocusedValidation,failures);
            RunSuite("SkirmishNativeCheckpointRecoveryCommitTests",SkirmishNativeCheckpointRecoveryCommitTests.RunFocusedValidation,failures);
            RunSuite("SkirmishNativeObjectiveCheckpointValidation",SkirmishNativeObjectiveCheckpointValidation.RunFocusedValidation,failures);
            if(failures.Count!=0)throw new AggregateException("Repair recovery suites failed; independent results retained.",failures);
            Debug.Log("[SkirmishActiveRepairRecoveryValidation] result=Passed tests=55 legacyWire1Preserved=1 normalMissionReadiness=Pending");
        }
        private static void RunSuite(string name,Action run,List<Exception> failures)
        {
            try{run();}
            catch(Exception exception)
            {
                failures.Add(exception);
                Debug.Log("[SkirmishActiveRepairRecoveryValidation] suiteFailed="+name+" failure="+exception);
            }
        }
    }
}
#endif
