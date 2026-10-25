using System;using Game.Components;using Game.Skirmish.Contracts;using Newtonsoft.Json.Linq;
namespace Game.Runtime {
internal sealed partial class AutonomousWorld {
 private SourceAriaS002Controller sourceS002Aria;
 internal Game.Simulation.GroundMissionContext SourceAriaGroundContext { get; private set; }
 internal void InitializeSourceS002Aria(SourceAriaRecruitmentBoundary actualPaid){
  if(disposed||sourceS002Aria!=null||Session==Unity.Entities.Entity.Null||!Entities.Exists(Session)||StorageBank==null)throw new InvalidOperationException("Bind unified ARIA once after actual world/public owners");
  var s=Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);if(s.Phase!=SkirmishSessionPhase.Playing||s.SpawnComplete==0)throw new InvalidOperationException("Actual S002 source Playing startup required");
  var canonical=ActualCanonicalCatalog??throw new InvalidOperationException("Actual pinned canonical catalog was not initialized");
  var match=Entities.GetComponentData<SkirmishMatchState>(Session);
  if(!GroundMissionContextFactory.TryCreateLogicalPublicData(ActualDefinition,Setup,ActualCompileManifest,ActualCompileMatrix,canonical,s,match.ScenarioIndex,ActualDefinitionScenarioIndex,out var context,out string reason))throw new InvalidOperationException("Actual compiled ground context rejected: "+reason);
  SourceAriaGroundContext=context;
  sourceS002Aria=new SourceAriaS002Controller(Entities,Session,Army,WorldId,actualPaid,ActualDefinitionScenarioIndex,context,sourceNumericCamera);
 }
 // Existing scheduler invokes this after actual fog/objective/HUD publication.
 // World clock and paid queue source time remain distinct from ARIA unscaled time.
 internal JObject StepSourceS002Aria(){
  if(disposed||sourceS002Aria==null)throw new InvalidOperationException("Actual full S002 source strategy not initialized");
  var row=sourceS002Aria.Step(Tick,UnityEngine.Time.frameCount,UnityEngine.Time.unscaledTime,UnityEngine.Time.time);
  row["world_id"]=WorldId;row["tick"]=Tick;row["simulation_seconds"]=SimulationSeconds;row["source_world_elapsed"]=Entities.WorldUnmanaged.Time.ElapsedTime;row["source_aria_unscaled_time"]=UnityEngine.Time.unscaledTime;row["source_production_scaled_time"]=UnityEngine.Time.time;return row;
 }
 internal void StopSourceS002Aria(){sourceS002Aria?.Stop();}
}
}
