#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishObjectiveApproachTowerTests
    {
        public static void AttachSyntheticDefensePads(SkirmishMapLayoutConfig layout)
        {
            var data=new SerializedObject(layout);var anchors=data.FindProperty("anchors");var pads=data.FindProperty("pads");
            for(int i=0;i<2;i++)
            {
                string role=i==0?"defense.approach.a":"defense.approach.b";float u=.65f,v=i==0?.25f:.75f;
                float x=layout.OriginX+u*layout.WorldWidthMetres,z=layout.OriginZ+v*layout.WorldDepthMetres;
                int n=anchors.arraySize;anchors.arraySize++;var a=anchors.GetArrayElementAtIndex(n);
                a.FindPropertyRelative("AnchorId").stringValue=role;a.FindPropertyRelative("RoleId").stringValue=role;
                a.FindPropertyRelative("WorldX").floatValue=x;a.FindPropertyRelative("WorldZ").floatValue=z;
                a.FindPropertyRelative("NormalizedU").floatValue=u;a.FindPropertyRelative("NormalizedV").floatValue=v;
                n=pads.arraySize;pads.arraySize++;var p=pads.GetArrayElementAtIndex(n);
                p.FindPropertyRelative("PadId").stringValue="pad."+role;p.FindPropertyRelative("AnchorId").stringValue=role;
                p.FindPropertyRelative("RoleId").stringValue=role;p.FindPropertyRelative("Kind").intValue=(int)SkirmishLegalPadKind.DefenseApproach;
                p.FindPropertyRelative("FactionId").intValue=2;p.FindPropertyRelative("CenterX").floatValue=x;p.FindPropertyRelative("CenterZ").floatValue=z;
                p.FindPropertyRelative("WidthMetres").floatValue=layout.WorldWidthMetres*.03f;p.FindPropertyRelative("DepthMetres").floatValue=layout.WorldDepthMetres*.03f;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void AttachSyntheticBays(SkirmishMapLayoutConfig layout)
        {
            var data=new SerializedObject(layout);var anchors=data.FindProperty("anchors");var pads=data.FindProperty("pads");
            for(int i=0;i<3;i++)
            {
                string role="convoy.origin."+(i+1);float u=.2f+i*.2f,v=.65f;
                float x=layout.OriginX+u*layout.WorldWidthMetres,z=layout.OriginZ+v*layout.WorldDepthMetres;
                int n=anchors.arraySize;anchors.arraySize++;var a=anchors.GetArrayElementAtIndex(n);
                a.FindPropertyRelative("AnchorId").stringValue=role;a.FindPropertyRelative("RoleId").stringValue=role;
                a.FindPropertyRelative("NormalizedU").floatValue=u;a.FindPropertyRelative("NormalizedV").floatValue=v;
                a.FindPropertyRelative("WorldX").floatValue=x;a.FindPropertyRelative("WorldZ").floatValue=z;
                n=pads.arraySize;pads.arraySize++;var p=pads.GetArrayElementAtIndex(n);
                p.FindPropertyRelative("PadId").stringValue=role;p.FindPropertyRelative("AnchorId").stringValue=role;p.FindPropertyRelative("RoleId").stringValue=role;
                p.FindPropertyRelative("Kind").intValue=(int)SkirmishLegalPadKind.ObjectiveTruckSpawn;p.FindPropertyRelative("FactionId").intValue=1;
                p.FindPropertyRelative("CenterX").floatValue=x;p.FindPropertyRelative("CenterZ").floatValue=z;
                p.FindPropertyRelative("WidthMetres").floatValue=layout.WorldWidthMetres*.05f;p.FindPropertyRelative("DepthMetres").floatValue=layout.WorldDepthMetres*.05f;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void WithDefinition(SkirmishSetupMatrixRow row,Action<SkirmishScenarioDefinitionConfig> assertion)
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition(row.CatalogId,(definition,owned)=>
            {
                var p=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName).ToAuthoredSet();
                var objective=UnityEngine.Object.Instantiate(p.ObjectiveBa);owned.Add(objective);
                var editor=new SerializedObject(objective);editor.FindProperty("kind").intValue=(int)row.ObjectiveKind;
                editor.FindProperty("objectiveId").stringValue=row.ObjectiveKind==SkirmishObjectiveKind.Breakthrough?"BT":"CE";editor.ApplyModifiedPropertiesWithoutUndo();
                var army=row.ArmyProfile==SkirmishArmyProfileId.GroundManeuver?p.ArmyGround:row.ArmyProfile==SkirmishArmyProfileId.AirMobile?p.ArmyAir:p.ArmyCombined;
                var roles=row.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort?Resources.Load<SkirmishRoleCatalogConfig>("SkirmishConvoyRoleCatalog"):p.Roles;
                Assert.IsNotNull(roles);var source=definition.CampaignMap;
                definition.Configure(row.CatalogId,objective,army,row.StartProfile==SkirmishStartPackageId.FieldBase?p.StartField:p.StartEstablished,
                    p.Economy,p.Upgrades,p.Intel,roles,definition.MapLayout,featureIds:new[]{"ground","intel","transport","advanced_ground","offensive_air","objective_bt","objective_ce"});
                definition.BindCampaignMap(source);AttachSyntheticDefensePads(definition.MapLayout);
                if(row.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort)AttachSyntheticBays(definition.MapLayout);
                assertion(definition);
            });
        }
        [Test]public void AllSixtyObjectiveTuplesAndThreeSizesKeepExactCanonicalBudgetsAndApproachBindings()
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            var rows=matrix.Where(r=>r.ObjectiveKind==SkirmishObjectiveKind.Breakthrough||r.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort).ToArray();
            Assert.AreEqual(180,rows.Length);
            foreach(var row in rows)WithDefinition(row,definition=>
            {
                Assert.IsTrue(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,SkirmishDifficultyId.Regular,row.SizeId,1,
                    new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var budget,out var reasons),string.Join(";",reasons));
                Assert.IsTrue(budget.ExactCanonicalQuantities,row.CatalogId+"/"+row.SizeId+":"+string.Join(";",budget.CanonicalBudgetErrors));
                Assert.AreEqual(row.PlayerStartingStructures,budget.Structures.Count(s=>s.FactionId==1));
                Assert.AreEqual(row.EnemyStartingStructures,budget.Structures.Count(s=>s.FactionId==2));
                Assert.AreEqual(budget.Structures.Length,budget.Structures.Select(s=>s.FactionId+"/"+s.StructureId).Distinct().Count());
                foreach(string id in new[]{SkirmishStructureIds.DefenseApproachA,SkirmishStructureIds.DefenseApproachB})
                {
                    var structure=budget.Structures.Single(s=>s.StructureId==id);Assert.AreEqual(2,structure.FactionId);
                    Assert.AreEqual("Building_GuardTower",SkirmishStructureIds.VisualKey(id));
                    Assert.IsTrue(SkirmishMapLayoutBuilder.TryPadForStructure(definition.MapLayout,structure,out var pad));
                    Assert.AreEqual(SkirmishLegalPadKind.DefenseApproach,pad.Kind);Assert.AreEqual(structure.SpawnAnchorId,pad.AnchorId);
                }
                foreach(byte side in new byte[]{1,2})
                    Assert.AreEqual(side==1?row.PlayerLogisticsSupport:row.EnemyLogisticsSupport,
                        budget.Forces.Where(f=>f.FactionId==side&&(f.RoleKind==SkirmishRoleKind.Tanker||f.RoleKind==SkirmishRoleKind.LogisticsTruck)).Sum(f=>f.Quantity));
                Assert.AreEqual(row.PlayerObjectiveTrucks,budget.Forces.Where(f=>f.FactionId==1&&f.RoleKind==SkirmishRoleKind.ConvoyLogic).Sum(f=>f.Quantity));
            });
        }
        [Test]public void ApproachCannotFallBackToBaseOrServiceOrWrongFaction()
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));var row=matrix.First(r=>r.ObjectiveKind==SkirmishObjectiveKind.Breakthrough);
            WithDefinition(row,definition=>
            {
                var a=new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.DefenseApproachA,SpawnAnchorId="defense.approach.a"};
                Assert.IsTrue(SkirmishMapLayoutBuilder.TryPadForStructure(definition.MapLayout,a,out _));
                a.FactionId=1;Assert.IsFalse(SkirmishMapLayoutBuilder.TryPadForStructure(definition.MapLayout,a,out _));
                a.FactionId=2;a.SpawnAnchorId="base.enemy";Assert.IsFalse(SkirmishMapLayoutBuilder.TryPadForStructure(definition.MapLayout,a,out _));
                a.SpawnAnchorId="defense.approach.a";var data=new SerializedObject(definition.MapLayout);var pads=data.FindProperty("pads");
                for(int i=0;i<pads.arraySize;i++)if(pads.GetArrayElementAtIndex(i).FindPropertyRelative("RoleId").stringValue=="defense.approach.a")
                    pads.GetArrayElementAtIndex(i).FindPropertyRelative("Kind").intValue=(int)SkirmishLegalPadKind.Service;
                data.ApplyModifiedPropertiesWithoutUndo();Assert.IsFalse(SkirmishMapLayoutBuilder.TryPadForStructure(definition.MapLayout,a,out _));
            });
        }
        [Test]public void ProtectedBaIdentitiesRemainUnchanged()
            =>new SkirmishEnemyAirCombined13Tests().ProtectedThreeKeepIndependentGoldenIdentities();
        public static void RunFocusedValidation()
        {
            var t=new SkirmishObjectiveApproachTowerTests();t.AllSixtyObjectiveTuplesAndThreeSizesKeepExactCanonicalBudgetsAndApproachBindings();
            t.ApproachCannotFallBackToBaseOrServiceOrWrongFaction();t.ProtectedBaIdentitiesRemainUnchanged();
            Debug.Log("[SkirmishObjectiveApproachTowerTests] result=Passed controls=3 objectiveTuples=60 sizeVectors=180 protectedIdentities=9 sourceFitCertified=0");
        }
    }
}
#endif
