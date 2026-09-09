using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string ConstructionKey="Warline.M03.Probe.Construction";
        private static int constructionStep,constructionAt;
        private static readonly HashSet<int> originalBuildings=new();
        private static readonly HashSet<Entity> commandedReinforcements=new();
        private static bool constructionVerified;
        private static int nextConstructionLog;
        private static bool inspectConstruction,constructionHeld,constructionDumped;
        public static void RunConstructionInspection() {inspectConstruction=true; constructionHeld=constructionDumped=false; RunConstructionDefense();}
        public static void RunConstructionDefense()
        {
            constructionStep=constructionAt=nextConstructionLog=0; constructionVerified=false;
            originalBuildings.Clear(); commandedReinforcements.Clear();
            SessionState.SetBool(ConstructionKey,true);
            RunRifleDefense();
        }
        private static void PlayConstructionDefense(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(ConstructionKey,false) || runtime.Phase!=MissionPhaseKind.Engage ||
                runtime.Outcome!=MissionOutcomeKind.None || facts.ElapsedMilliseconds<6000) return;
            if(inspectConstruction && !constructionHeld && facts.ElapsedMilliseconds>=35000) {Time.timeScale=0; constructionHeld=true; Debug.Log("[M03ConstructionProbe] paused for production inspection");}
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if(match==null) return;
            var bootstrap=match.MatchBootstrap;
            var command=bootstrap.BuildingUiCommandContract;
            var buildings=bootstrap.BuildingUiQueryContext.RuntimeBuildings;
            switch(constructionStep)
            {
                case 0:
                    foreach(var building in buildings) originalBuildings.Add(building.Key);
                    BeginPaidPlacement(command,"Building_GuardTower",22000,50);
                    AssertBudget(em,50000,100); command.CancelBuildingPlacement(); AssertBudget(em,50000,100);
                    BeginPaidPlacement(command,"Building_GuardTower",22000,50); NextConstruction(facts); break;
                case 1:
                    if(!ConfirmPaidPlacement(command)) return;
                    AssertBudget(em,28000,50); NextConstruction(facts); break;
                case 2:
                    BeginPaidPlacement(command,"Building_Road_Barrier",6000,15); NextConstruction(facts); break;
                case 3:
                    if(!ConfirmPaidPlacement(command)) return;
                    AssertBudget(em,22000,35); NextConstruction(facts); break;
                case 4:
                    var rifle=AssetDatabase.LoadAssetAtPath<GameObject>(M02EstablishBaseConfigBuilder.RequiredRiflePrefabPath);
                    var result=command.TryRequestCampItem(rifle,0,out string required,true);
                    if(result!=BuildingUiCommandFailure.None) throw new InvalidOperationException("Real rifle order rejected: "+result+" producer="+required);
                    var budget=ReadBudget(em);
                    if(budget.Credits!=12000 || budget.Materials<0) throw new InvalidOperationException("Incorrect rifle transaction or negative reserve: "+budget);
                    Debug.Log($"[M03ConstructionProbe] actual reserve credits={budget.Credits} materials={budget.Materials}; cancel had no charge");
                    NextConstruction(facts); break;
                case 5:
                    var added=buildings.Values.Where(b=>!originalBuildings.Contains(b.Id) && b.Definition!=null).ToArray();
                    if(facts.ElapsedMilliseconds>=nextConstructionLog)
                    {
                        nextConstructionLog=facts.ElapsedMilliseconds+15000;
                        Debug.Log("[M03ConstructionProbe] buildings="+string.Join(";",added.Select(b=>$"id={b.Id} prefab={b.Definition.Prefab.name} position={b.OriginCell} combat={b.CombatEntity} dead={b.IsDestroyed}")));
                        foreach(var producer in buildings.Values.Where(b=>b.PendingProductions?.Count>0 || b.ProducedUnits?.Count>0))
                            Debug.Log($"[M03ConstructionProbe] producer={producer.Definition.Prefab.name} pending={producer.PendingProductions?.Count} produced={producer.ProducedUnits?.Count} time={Time.time} "+string.Join(";",producer.PendingProductions?.Select(p=>$"ready={p.ReadyAt} remaining={p.RemainingQuantity}") ?? Array.Empty<string>()));
                    }
                    var tower=added.SingleOrDefault(b=>b.Definition.Prefab.name=="Building_GuardTower");
                    var barrier=added.SingleOrDefault(b=>b.Definition.Prefab.name=="Building_Road_Barrier");
                    if(tower==null || barrier==null) break;
                    if(!em.Exists(tower.CombatEntity) || !em.HasComponent<BuildingDefenseWeapon>(tower.CombatEntity) ||
                        em.HasComponent<CampaignMissionDormantMapDefenseTag>(tower.CombatEntity))
                        throw new InvalidOperationException("The purchased Tower did not receive its active weapon.");
                    int produced=0;
                    using(var query=em.CreateEntityQuery(typeof(BuildingProducedUnitReadModel)))
                    using(var owners=query.ToEntityArray(Allocator.Temp))
                    foreach(var owner in owners)
                    {
                        var units=em.GetBuffer<BuildingProducedUnitReadModel>(owner,true);
                        if(constructionHeld && !constructionDumped) {constructionDumped=true; Debug.Log("[M03ConstructionProbe] produced="+string.Join(";",units.ToNativeArray(Allocator.Temp).Select(u=>$"{u.Unit} key={u.UnitSourceKey} owner={u.HasOwnerFaction}/{u.OwnerFactionId} alive={Alive(em,u.Unit)}")));}
                        for(int i=0;i<units.Length;i++)
                        {
                            var unit=units[i];
                            if(unit.HasOwnerFaction==0 || unit.OwnerFactionId!=1 || !unit.UnitSourceKey.ToString().Equals("Unit_Chr_Soldier_Male_02_Alt_04",StringComparison.OrdinalIgnoreCase) || !Alive(em,unit.Unit)) continue;
                            produced++;
                            if(commandedReinforcements.Add(unit.Unit)) UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(em,unit.Unit,new Unity.Mathematics.int2(915+produced*3,412));
                        }
                    }
                    if(produced<4) break;
                    constructionVerified=true;
                    Debug.Log($"[M03ConstructionProbe] result=Passed tower={tower.OriginCell} barrier={barrier.OriginCell} activeTowerWeapon=1 realRifles={produced} creditsReserve=12000");
                    NextConstruction(facts); break;
            }
            if(constructionStep<6 && facts.ElapsedMilliseconds-constructionAt>85000)
                throw new InvalidOperationException("Construction/production did not complete. step="+constructionStep+" placement="+command.PlacementStatusText);
        }
        private static void BeginPaidPlacement(IBuildingUiCommand command,string id,int credits,int materials)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/"+id+".prefab");
            var result=command.TryRequestCampItem(prefab,materials,out string required,false);
            if(result!=BuildingUiCommandFailure.None || !command.HasPendingBuildingPlacement || command.ActivePlacementCreditsCost!=credits ||
                command.ActivePlacementCost!=materials || Math.Abs(command.ActivePlacementDurationSeconds-30)>.01f)
                throw new InvalidOperationException("Placement contract mismatch for "+id+": "+result+" "+required);
        }
        private static bool ConfirmPaidPlacement(IBuildingUiCommand command)
        {
            var bar=UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>();
            if(!command.CanConfirmBuildingPlacement || bar==null || !bar.ConfirmButton.interactable) return false;
            bar.ConfirmButton.onClick.Invoke();
            if(command.HasPendingBuildingPlacement) throw new InvalidOperationException("Actual confirmation button did not commit placement.");
            return true;
        }
        private static (int Credits,int Materials) ReadBudget(EntityManager em)
        {
            using var query=em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent));
            using var owners=query.ToEntityArray(Allocator.Temp);
            foreach(var owner in owners) if(FactionIdentity.IsPlayerControlled(em.GetComponentData<FactionEconomy>(owner).FactionId))
                return ((int)em.GetComponentData<FactionEconomy>(owner).Money,em.GetComponentData<FactionTacticalMaterialsComponent>(owner).Current);
            throw new InvalidOperationException("Player budget missing.");
        }
        private static void AssertBudget(EntityManager em,int credits,int materials)
        {if(ReadBudget(em)!=(credits,materials)) throw new InvalidOperationException("Budget mismatch: expected "+(credits,materials)+" actual "+ReadBudget(em));}
        private static void NextConstruction(in CampaignMissionAttemptFactsComponent facts)
        {constructionStep++; constructionAt=facts.ElapsedMilliseconds; Debug.Log("[M03ConstructionProbe] step="+constructionStep);}
    }
}
