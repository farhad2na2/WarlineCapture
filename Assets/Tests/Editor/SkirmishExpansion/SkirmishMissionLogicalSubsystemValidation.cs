#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishMissionLogicalSubsystemValidation
    {
        private const string LegacyPath="Design/AgentReports/SkirmishExpansion/ProductionFoundation/Validation/legacy-layout-and-setup-baselines-20261014.json";
        private const string LegacySha="0d03f125c9fc77a13a74f82e1109e7bc5950bf3837325081b7a04cc32e558837";
        [Serializable] private sealed class Baseline { public Row[] Rows; }
        [Serializable] private sealed class Row
        {
            public string CatalogId,DefinitionId,LayoutId,LayoutSemanticSha256,Difficulty,Size,SetupHashHex,PolicylessHashHex;
            public int Seed;
        }
        private static void VerifyImmutableLegacyTwelve()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));byte[] bytes=File.ReadAllBytes(Path.Combine(root,LegacyPath));
            using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=LegacySha)
                throw new InvalidOperationException("Reviewed pre-integration legacy baseline bytes changed.");
            var baseline=JsonUtility.FromJson<Baseline>(System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\ufeff'));
            if(baseline?.Rows==null||baseline.Rows.Length!=12)throw new InvalidOperationException("Twelve immutable setup rows required.");
            var catalog=SkirmishExpansionCatalogConfig.Load();
            if(catalog==null||!SkirmishSetupMatrixTable.TryLoad(root,out var matrix,out _))throw new InvalidOperationException("Actual legacy catalog/matrix unavailable.");
            var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in baseline.Rows)
            {
                if(!keys.Add(row.CatalogId+"/"+row.Size)||!catalog.TryGetDefinition(row.CatalogId,out var definition)||
                    !Enum.TryParse(row.Size,out SkirmishSizeId size)||!Enum.TryParse(row.Difficulty,out SkirmishDifficultyId difficulty)||
                    !SkirmishSetupCompiler.TryCompile(definition,difficulty,size,row.Seed,
                        new SkirmishContentManifest {RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out _))
                    throw new InvalidOperationException("Actual preserved setup failed: "+row.CatalogId+"/"+row.Size);
                if(setup.DefinitionId!=row.DefinitionId||setup.LayoutId!=row.LayoutId||
                    SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout)!=row.LayoutSemanticSha256||
                    setup.SetupHash.ToString("X8")!=row.SetupHashHex||SkirmishSetupCompiler.ComputePolicylessHash(setup,row.Seed).ToString("X8")!=row.PolicylessHashHex)
                    throw new InvalidOperationException("Actual legacy identity drift: "+row.CatalogId+"/"+row.Size);
            }
            Debug.Log("[SkirmishLogicalLegacyIdentity] result=Passed configurations=12 immutableBeforePin=1");
        }
        public static void RunFocusedValidation()
        {
            VerifyImmutableLegacyTwelve();
            SkirmishMeasuredLayoutBinderTests.RunFocusedValidation();
            SkirmishObjectiveAreaConfigTests.RunFocusedValidation();
            SkirmishNativeObjectiveCollectorTests.RunFocusedValidation();
            SkirmishConvoyTruckRuntimeTests.RunFocusedValidation();
            SkirmishNativeObjectiveTickTests.RunFocusedValidation();
            SkirmishNativeObjectiveCheckpointValidation.RunFocusedValidation();
            SkirmishMissionLogicalBindingValidation.RunFocusedValidation();
            SkirmishNativeObjectivePartialFailureCheckpointTests.RunFocusedValidation();
            SkirmishGenericMissionDispatchTests.RunFocusedValidation();
            VerifyImmutableLegacyTwelve();
            Debug.Log("[SkirmishMissionLogicalSubsystemValidation] result=Passed focusedTests=77 legacyConfigurations=12 nativeMissionReadiness=Pending");
        }
    }
}
#endif
