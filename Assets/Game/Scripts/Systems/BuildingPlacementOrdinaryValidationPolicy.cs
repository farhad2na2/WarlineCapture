namespace Game.Runtime {
internal interface IOrdinaryPlacementValidationSource {
 bool AuthoredOverlap();bool WaterOverlap();bool RoadOverlap();bool ValidRect();
}
internal static class BuildingPlacementOrdinaryValidationPolicy {
 internal static bool IsValid<T>(ref T source) where T:struct,IOrdinaryPlacementValidationSource =>
  !source.AuthoredOverlap()&&!source.WaterOverlap()&&!source.RoadOverlap()&&source.ValidRect();
}}
