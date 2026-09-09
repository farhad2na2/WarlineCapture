using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string CommandsKey="Warline.M03.Probe.Commands";
        private static int commandsStep,commandsClass,commandsFrame;
        private static double commandsNext,commandsDeadline;
        private static readonly List<Entity> commandsUnits=new();
        private static readonly List<float3> commandsInitialPositions=new();
        private static float3 commandsInitialPosition;
        private static double commandsMovementLogAt;
        public static void RunCommandValidation()=>RunChecked(()=>
        {
            M03RadarWarningConfigBuilder.Build(); M03RadarWarningUiBuilder.Build();
            commandsStep=commandsClass=commandsFrame=0; commandsNext=0;
            commandsDeadline=0;
            SessionState.SetBool(CommandsKey,true); Run();
        });
        private static bool AdvanceCommandValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(CommandsKey,false)) return false;
            if(commandsDeadline==0) commandsDeadline=EditorApplication.timeSinceStartup+150;
            if(EditorApplication.timeSinceStartup>commandsDeadline) throw new TimeoutException($"Live command check class={commandsClass} step={commandsStep}");
            if(runtime.Phase==MissionPhaseKind.FindSquad) UiShellRuntimeGateway.TryRequestMissionDefenseAction(Game.UI.Contracts.UiMissionDefenseAction.SkipCameraTour);
            if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<1500 || Time.frameCount-commandsFrame<3 || EditorApplication.timeSinceStartup<commandsNext) return true;
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var commands=match.MatchBootstrap.SelectionUiCommand;
            var input=(RtsSelectionInputCompositionSystemHelper)typeof(SelectionUiCommandUiSystemHelper)
                .GetField("_inputSystem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(commands);
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            switch(commandsStep)
            {
                case 0:
                    if(commandsClass==0) M03RadarWarningRuntimeGridProbe.Capture(em,Output);
                    if(!(commandsClass==0 ? commands.RequestSelectAllSoldiers() : commands.RequestSelectAllVehicles()))
                        throw new InvalidOperationException("Normal select-all class command was rejected.");
                    NextCommand(); break;
                case 1:
                    commandsUnits.Clear();
                    commandsInitialPositions.Clear();
                    using(var selected=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitHealth),typeof(Faction)))
                    using(var units=selected.ToEntityArray(Allocator.Temp))
                    foreach(var unit in units)
                    {
                        if(em.GetComponentData<Faction>(unit).Id!=1) throw new InvalidOperationException("Class selection included a non-player actor.");
                        commandsUnits.Add(unit);
                        commandsInitialPositions.Add(em.GetComponentData<LocalTransform>(unit).Position);
                        Debug.Log($"[M03CommandProbe] selection class={commandsClass} actor={unit} source={(em.HasComponent<UnitSourcePrefabKey>(unit) ? em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString() : em.GetName(unit))} role={(em.HasComponent<CampaignMissionUnitRoleComponent>(unit) ? em.GetComponentData<CampaignMissionUnitRoleComponent>(unit).MissionRoleId.ToString() : "none")}");
                    }
                    if(commandsUnits.Count!=(commandsClass==0 ? 8 : 1)) throw new InvalidOperationException($"Expected {(commandsClass==0 ? 8 : 1)} selected mission actors, got {commandsUnits.Count}.");
                    if(commandsClass==1 && (RadarPingRequestSystem.FindSensor(em,root)!=commandsUnits[0] ||
                        em.GetComponentData<UnitCombat>(commandsUnits[0]).CanAttack!=0))
                        throw new InvalidOperationException("The real unarmed Ground Radar Tank advertises an attack action.");
                    commandsInitialPosition=em.GetComponentData<LocalTransform>(commandsUnits[0]).Position;
                    Debug.Log($"[M03CommandProbe] class={commandsClass} first={commandsUnits[0]} initialPosition={commandsInitialPosition}");
                    if(commandsClass==1) {ClickCommand(controls.AttackButton); commandsStep=5; NextCommand(); break;}
                    ClickCommand(controls.MoveButton); NextCommand(); break;
                case 6:
                    if(input.TryGetActiveCommandMode(out var attackMode) && attackMode==Game.Tactical.Contracts.TacticalCommandMode.Attack)
                        throw new InvalidOperationException("The unarmed sensor entered attack-target mode.");
                    if(em.HasComponent<EngageTarget>(commandsUnits[0])) throw new InvalidOperationException("The unarmed sensor received an attack order.");
                    ClickCommand(controls.MoveButton); commandsStep=1; NextCommand(); break;
                case 2:
                    if(!input.TryGetActiveCommandMode(out var mode) || mode!=Game.Tactical.Contracts.TacticalCommandMode.Move)
                        throw new InvalidOperationException("The actual Move button did not enter target mode.");
                    int2 cell=commandsClass==0 ? new int2(840,432) : new int2(945,430);
                    using(var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent)))
                    {
                    var grid=maps.GetSingleton<OperationMapMetadataComponent>().Blob.Value.Grid;
                    var world=new Vector3(grid.Origin.x+(cell.x+.5f)*grid.CellSize,commandsInitialPosition.y,grid.Origin.z+(cell.y+.5f)*grid.CellSize);
                    if(!input.QueueMoveCommandRequest(match.MatchBootstrap.WorldCamera.WorldToScreenPoint(world),cell,world,Time.frameCount))
                        throw new InvalidOperationException("Normal resolved target input rejected the Move request.");
                    }
                    commandsMovementLogAt=EditorApplication.timeSinceStartup+3; NextCommand(); break;
                case 3:
                    if(EditorApplication.timeSinceStartup>=commandsMovementLogAt)
                    {
                        commandsMovementLogAt=EditorApplication.timeSinceStartup+10;
                        foreach(var unit in commandsUnits)
                        {Debug.Log($"[M03CommandProbe] actor={unit} position={em.GetComponentData<LocalTransform>(unit).Position}"); LogMovement(em,unit);}
                    }
                    for(int i=0;i<commandsUnits.Count;i++)
                        if(math.distance(commandsInitialPositions[i],em.GetComponentData<LocalTransform>(commandsUnits[i]).Position)<.25f) return true;
                    var readModel=match.MatchBootstrap.SelectionUiReadModel;
                    Debug.Log($"[M03CommandProbe] beforeHold selected={readModel.HasAnySelectedUnits} focused={readModel.HasFocusedUnit} canHold={readModel.FocusedUnitCanHold} reason={readModel.FocusedUnitHoldDisabledReason}");
                    ClickCommand(controls.HoldButton); NextCommand(); break;
                case 4:
                    foreach(var unit in commandsUnits)
                    {
                        if(!em.HasComponent<HoldPositionOrderTag>(unit) || em.HasComponent<UnitPathRequest>(unit) || em.HasComponent<UnitPathFollow>(unit))
                            throw new InvalidOperationException($"The actual Hold button failed actor={unit} selected={em.HasComponent<SelectedUnitTag>(unit)} hold={em.HasComponent<HoldPositionOrderTag>(unit)} request={em.HasComponent<UnitPathRequest>(unit)} follow={em.HasComponent<UnitPathFollow>(unit)} auto={em.GetComponentData<UnitCombat>(unit).AutoEngage}.");
                        if(commandsClass==0 && em.GetComponentData<UnitCombat>(unit).AutoEngage!=1)
                            throw new InvalidOperationException("Rifle Hold did not enable defensive fire.");
                    }
                    ClickCommand(controls.StopButton); NextCommand(); break;
                case 5:
                    foreach(var unit in commandsUnits)
                        if(em.HasComponent<HoldPositionOrderTag>(unit) || em.HasComponent<UnitPathRequest>(unit) || em.HasComponent<UnitPathFollow>(unit) ||
                            commandsClass==0 && em.GetComponentData<UnitCombat>(unit).AutoEngage!=0)
                            throw new InvalidOperationException("The actual Stop button did not cancel orders/auto-engagement.");
                    Debug.Log($"[M03CommandProbe] class={(commandsClass==0 ? "Rifle" : "Ground Radar Tank")} selected={commandsUnits.Count} actualMove=Passed actualHold=Passed actualStop=Passed");
                    if(++commandsClass==2)
                    {Complete(true,"live class selection; eight rifles and the unarmed Ground Radar Tank: actual Move/Hold/Stop buttons and resolved target input; observed movement/cancellation; sensor attack unavailable"); return true;}
                    commandsStep=0; commandsNext=EditorApplication.timeSinceStartup+.5; break;
            }
            return true;
        }
        private static void ClickCommand(Button button)
        {
            if(button==null || !button.isActiveAndEnabled || !button.interactable) throw new InvalidOperationException("Required live command button is unavailable: "+button?.name);
            button.onClick.Invoke();
        }
        private static void NextCommand()
        {commandsStep++; commandsFrame=Time.frameCount; commandsNext=EditorApplication.timeSinceStartup+.5; commandsDeadline=commandsNext+30;}
    }
}
