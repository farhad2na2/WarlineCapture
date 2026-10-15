using Game.Skirmish.Contracts;

namespace Game.Configs
{
    // Mechanical capability scope only. Source fit, normal-input acceptance and
    // publication are independent gates. Legacy layout bindings have no generic
    // LayoutBindingFingerprint and retain their existing policy/hash contract.
    public static class SkirmishGenericGroundEnemyRollout
    {
        public static bool Supports(SkirmishResolvedSetup setup)
        {
            if (setup == null || setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                setup.ArmyProfileId != SkirmishArmyProfileId.GroundManeuver ||
                !setup.MeasuredLayoutBound || string.IsNullOrEmpty(setup.LayoutBindingFingerprint) ||
                setup.Forces == null || setup.Forces.Length == 0 ||
                !((setup.StartPackageId == SkirmishStartPackageId.FieldBase && setup.Readiness == SkirmishReadinessStage.Field) ||
                  (setup.StartPackageId == SkirmishStartPackageId.EstablishedBase && setup.Readiness == SkirmishReadinessStage.Established)))
                return false;
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            foreach (var force in setup.Forces)
            {
                if (force.FactionId != setup.PlayerFaction && force.FactionId != setup.EnemyFaction) return false;
                if (force.Quantity <= 0 || !army.Allows(SkirmishRoleIds.ToId(force.RoleKind))) return false;
                // Starting aircraft support is deliberately limited to the existing
                // transport owner and its exact authored-original provenance.
                if (force.RoleKind == SkirmishRoleKind.Drone) return false;
            }
            return true;
        }

        public static bool IsAuthoredStartingTransport(SkirmishResolvedSetup setup, string stableId,
            SkirmishRoleKind role, int faction)
        {
            if (!Supports(setup) || faction != setup.EnemyFaction || role != SkirmishRoleKind.TransportHeli || string.IsNullOrEmpty(stableId)) return false;
            int matches = 0;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                var force = setup.Forces[i];
                if (force.FactionId != faction || force.RoleKind != role || force.RuntimePrefabKey != SkirmishRoleCatalogConfig.RuntimePrefabKey(role)) continue;
                for (int member = 0; member < force.Quantity; member++)
                    if (stableId == force.RoleId + "." + force.FactionId + "." + i + "." + member) matches++;
            }
            return matches == 1;
        }

        public static bool IsReplenishableCombatRole(SkirmishResolvedSetup setup, SkirmishRoleKind role)
        {
            if (!Supports(setup) || !SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId).Allows(SkirmishRoleIds.ToId(role)) ||
                !SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,role,out var overlay)) return false;
            return overlay.Damage > 0 &&
                (SkirmishRoleIds.Category(role) == SkirmishPopulationCategory.Infantry || SkirmishRoleIds.Category(role) == SkirmishPopulationCategory.Ground);
        }
    }
}
