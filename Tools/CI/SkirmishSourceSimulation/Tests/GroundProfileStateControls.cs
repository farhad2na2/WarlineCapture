using System;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;

internal static class GroundProfileStateControls
{
 static void Main()
 {
  var setup=new SkirmishResolvedSetup{ArmyProfileId=SkirmishArmyProfileId.GroundManeuver,Seed=1};
  SourceMetadataCompatibility.RequireGroundProfileState(setup);
  setup.Seed=104731;SourceMetadataCompatibility.RequireGroundProfileState(setup);
  setup.Seed=87321;SourceMetadataCompatibility.RequireGroundProfileState(setup);
  var cached=SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
  string old=cached.AllowedRoleIds[0];
  try {cached.AllowedRoleIds[0]="altered";bool rejected=false;try{SourceMetadataCompatibility.RequireGroundProfileState(setup);}catch(System.IO.InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Changed allowed role accepted");}
  finally {cached.AllowedRoleIds[0]=old;}
  old=cached.ExcludedRoleIds[0];
  try {cached.ExcludedRoleIds[0]="altered";bool rejected=false;try{SourceMetadataCompatibility.RequireGroundProfileState(setup);}catch(System.IO.InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Changed excluded role accepted");}
  finally {cached.ExcludedRoleIds[0]=old;}
  SourceMetadataCompatibility.RequireGroundProfileState(setup);
  Console.WriteLine("[GroundProfileStateControls] result=Passed seeds=3 mutations=2");
 }
}
