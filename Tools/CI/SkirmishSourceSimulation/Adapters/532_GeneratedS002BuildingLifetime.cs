namespace Game.Runtime { internal sealed partial class AutonomousWorld {
 SourceBuildingLifetimeOwner actualBuildingLifetime;
 internal void InitializeActualBuildingLifetime(string repositoryRoot){if(actualBuildingLifetime!=null)throw new System.InvalidOperationException("Source lifetime binds once");actualBuildingLifetime=new SourceBuildingLifetimeOwner(this,repositoryRoot,SourceBuildingLifetimePresentation.NonRendered);}
 void AdvanceActualBuildingLifetime(){if(actualBuildingLifetime==null)throw new System.InvalidOperationException("Source lifetime owner missing");actualBuildingLifetime.Advance(SimulationSeconds,(float)SimulationSeconds);}
}}
