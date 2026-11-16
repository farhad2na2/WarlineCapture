using System;
using System.Collections.Generic;
using Unity.Entities;
namespace Game.Runtime {
 // Numeric state belongs to the already-created canonical building wrapper.
 internal sealed partial class RuntimeBuildingEntity : SourceBuildingCombatUtility.IRuntimeBuildingVisualState {
  public float DestroyedCleanupAt {get;set;}
  int SourceBuildingCombatUtility.IRuntimeBuilding.Id => Id;
  Entity SourceBuildingCombatUtility.IRuntimeBuilding.CombatEntity {get=>CombatEntity;set=>CombatEntity=value;}
  Entity SourceBuildingCombatUtility.IRuntimeBuilding.BlockerEntity {get=>BlockerEntity;set=>BlockerEntity=value;}
  RuntimeInstance SourceBuildingCombatUtility.IRuntimeBuildingVisualState.InstanceObject => Instance;
  IReadOnlyList<RuntimeTransform> SourceBuildingCombatUtility.IRuntimeBuildingVisualState.AliveVisualRootTransforms =>
   throw new NotSupportedException("Rendered alive-root hierarchy is not admitted by the numeric lifetime owner");
 }
 // These callbacks are presentation boundaries, not claims that a visual exists.
 internal sealed class SourceDestroyedVisualBoundary {
  internal readonly struct Context {
   internal readonly Action<RuntimeBuildingEntity> Begin,Cleanup;
   internal Context(Action<RuntimeBuildingEntity> begin,Action<RuntimeBuildingEntity> cleanup){Begin=begin;Cleanup=cleanup;}
  }
  internal void BeginDestroyedVisual(Context context,RuntimeBuildingEntity value)=>context.Begin?.Invoke(value);
  internal void CleanupDestroyedVisual(Context context,RuntimeBuildingEntity value)=>context.Cleanup?.Invoke(value);
 }
}
