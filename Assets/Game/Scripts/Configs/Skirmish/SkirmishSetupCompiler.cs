using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishSetupCompiler
    {
        public static bool TryCompile(
            SkirmishScenarioDefinitionConfig definition,
            SkirmishDifficultyId difficulty,
            SkirmishSizeId size,
            int seed,
            SkirmishContentManifest contentManifest,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            out SkirmishResolvedSetup setup,
            out List<SkirmishCompileReason> reasons)
        {
            reasons = new List<SkirmishCompileReason>();
            setup = null;
            if (definition == null)
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingDefinition, "definition", "Definition is null."));
                return false;
            }

            ValidateIdentity(definition, reasons);
            if (difficulty == SkirmishDifficultyId.None)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedDifficulty, "difficulty", "Difficulty is required."));
            if (size == SkirmishSizeId.None)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedSize, "size", "Size is required."));
            if (definition.ObjectiveConfig == null)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "objectiveConfig", "Objective config is missing."));
            if (definition.ArmyProfileConfig == null)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "armyProfileConfig", "Army profile is missing."));
            if (definition.StartPackageConfig == null)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "startPackageConfig", "Start package is missing."));
            if (definition.EconomyConfig == null)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "economyConfig", "Economy config is missing."));
            if (definition.RoleCatalog == null)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "roleCatalog", "Role catalog is missing."));
            if (definition.MapLayout == null)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "mapLayout", "Map layout is missing."));
            if (definition.MapLayout != null && definition.MapLayout.OperationMapId != definition.OperationMapId)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedMap, "operationMapId", definition.OperationMapId));
            if (size != SkirmishSizeId.Standard && size != SkirmishSizeId.War && size != SkirmishSizeId.LargeWar)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedSize, "size", SkirmishIdParsing.ToCode(size)));
            RejectMissingCapabilities(definition, contentManifest, reasons);

            if (reasons.Count > 0)
                return false;

            if (!SkirmishSetupMatrixTable.TryFind(matrix, definition.CatalogId, size, out SkirmishSetupMatrixRow vector))
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "catalog_id+size_id", "No matrix vector."));
                return false;
            }

            CompareIdentity(definition, vector, reasons);
            if (reasons.Count > 0)
                return false;

            setup = BuildSetup(definition, difficulty, size, seed, vector, reasons);
            if (reasons.Count > 0)
            {
                setup = null;
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        // Read-only authoring diagnostics. A returned roster is NOT a compiled setup,
        // source-fit proof, native identity, launch permission or readiness declaration.
        public static bool TryInspectStaticAuthoringBudget(SkirmishScenarioDefinitionConfig definition,
            SkirmishDifficultyId difficulty, SkirmishSizeId size, int seed, SkirmishContentManifest manifest,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix, out SkirmishStaticAuthoringBudget budget,
            out List<SkirmishCompileReason> reasons)
        {
            budget=null;reasons=new List<SkirmishCompileReason>();
            if(matrix==null||definition==null||definition.ObjectiveConfig==null||definition.ArmyProfileConfig==null||
                definition.StartPackageConfig==null||definition.EconomyConfig==null||definition.RoleCatalog==null||definition.MapLayout==null)
            {reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference,"staticAuthoringBudget","Actual typed definition/catalog/layout required."));return false;}
            ValidateIdentity(definition,reasons);RejectMissingCapabilities(definition,manifest,reasons);
            if(difficulty==SkirmishDifficultyId.None||(size!=SkirmishSizeId.Standard&&size!=SkirmishSizeId.War&&size!=SkirmishSizeId.LargeWar)||!SkirmishSetupMatrixTable.TryFind(matrix,definition.CatalogId,size,out var vector))
            {reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch,"staticAuthoringBudget","Exact canonical difficulty/size vector required."));return false;}
            CompareIdentity(definition,vector,reasons);if(reasons.Count!=0)return false;
            bool normal=TryCompile(definition,difficulty,size,seed,manifest,matrix,out var accepted,out var normalReasons);
            // Reuse the actual budget owner even when its later source/proof gates reject.
            // The rejected internal object and its hash/payload never leave this method.
            var observed=new List<SkirmishCompileReason>();var resolved=accepted??BuildSetup(definition,difficulty,size,seed,vector,observed);
            reasons.AddRange(normalReasons);
            var forces=new List<SkirmishResolvedForceEntry>(resolved.Forces);var errors=new List<string>();
            Tally(forces,1,out int pi,out int pg,out int pa,out int pc,out int ps);
            Tally(forces,2,out int ei,out int eg,out int ea,out int ec,out int es);
            if(pi!=vector.PlayerInfantry||pg!=vector.PlayerGround||pa!=vector.PlayerAir||pc!=vector.PlayerCombat||ps!=vector.PlayerSupply||
                ei!=vector.EnemyInfantry||eg!=vector.EnemyGround||ea!=vector.EnemyAir||ec!=vector.EnemyCombat||es!=vector.EnemySupply)
                errors.Add("Actual typed combat roster does not match canonical totals/Supply.");
            int playerSupport=0,enemySupport=0,playerTrucks=0,enemyTrucks=0,playerStructures=0,enemyStructures=0;
            foreach(var force in forces)
            {
                if(force.FactionId!=1&&force.FactionId!=2||force.Quantity<=0||string.IsNullOrEmpty(force.RuntimePrefabKey)||string.IsNullOrEmpty(force.RoleId))
                    errors.Add("Actual force identity/count/native prefab binding missing.");
                if(force.RoleId==SkirmishRoleIds.Tanker||force.RoleId==SkirmishRoleIds.LogisticsTruck)
                {if(force.FactionId==1)playerSupport+=force.Quantity;else if(force.FactionId==2)enemySupport+=force.Quantity;}
                if(force.RoleKind==SkirmishRoleKind.ConvoyLogic)
                {if(force.FactionId==1)playerTrucks+=force.Quantity;else if(force.FactionId==2)enemyTrucks+=force.Quantity;}
            }
            foreach(var structure in resolved.Structures){if(structure.FactionId==1)playerStructures++;else if(structure.FactionId==2)enemyStructures++;else errors.Add("Invalid actual structure faction.");}
            if(playerSupport!=vector.PlayerLogisticsSupport||enemySupport!=vector.EnemyLogisticsSupport||
                playerTrucks!=vector.PlayerObjectiveTrucks||enemyTrucks!=vector.EnemyObjectiveTrucks||
                playerStructures!=vector.PlayerStartingStructures||enemyStructures!=vector.EnemyStartingStructures)
                errors.Add("Actual support/objective-truck/structure roster does not match canonical counts.");
            budget=new SkirmishStaticAuthoringBudget(definition.CatalogId,definition.DefinitionId,size,
                resolved.Forces,resolved.Structures,normal,errors.ToArray());
            return true;
        }
        public static bool TryCaptureMissionLogicalAuthoringInputs(SkirmishScenarioDefinitionConfig definition,
            SkirmishDifficultyId difficulty,SkirmishSizeId size,int seed,SkirmishContentManifest manifest,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,out SkirmishMissionLogicalAuthoringInputs inputs,
            out List<SkirmishCompileReason> reasons)
        {
            inputs=null;
            bool valid=TryCompile(definition,difficulty,size,seed,manifest,matrix,out var setup,out reasons);
            if(definition==null||definition.CampaignMap==null||definition.ObjectiveConfig==null||
                !SkirmishMissionLogicalBindingCompiler.RequiresBinding(new SkirmishResolvedSetup {CatalogId=definition.CatalogId,ObjectiveKind=definition.ObjectiveConfig.Kind}))return false;
            if(!valid)
            {
                if(reasons.Count==0)return false;
                foreach(var reason in reasons)if(reason.Field!="missionLogicalBinding")return false;
                if(!SkirmishSetupMatrixTable.TryFind(matrix,definition.CatalogId,size,out var vector))return false;
                var observed=new List<SkirmishCompileReason>();
                setup=BuildSetup(definition,difficulty,size,seed,vector,observed);
                foreach(var reason in observed)if(reason.Field!="missionLogicalBinding")return false;
            }
            // A failed normal compile remains failed. Return ONLY measured logical
            // authoring inputs, never its rejected setup/native hash or launch data.
            if(setup==null||string.IsNullOrEmpty(setup.LayoutBindingFingerprint))return false;
            inputs=SkirmishMissionLogicalAuthoringInputs.Capture(definition,setup);return true;
        }
#endif
        public static bool TryCompileCatalog(
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            SkirmishContentManifest manifest,
            out List<SkirmishResolvedSetup> compiled,
            out List<SkirmishCompileReason> reasons)
        {
            compiled = new List<SkirmishResolvedSetup>();
            reasons = new List<SkirmishCompileReason>();
            for (int i = 0; i < matrix.Count; i++)
            {
                SkirmishSetupMatrixRow vector = matrix[i];
                if (!SkirmishExpansionCatalogFactory.TryGetDefinition(authored, vector.CatalogId, out SkirmishScenarioDefinitionConfig definition))
                {
                    reasons.Add(new SkirmishCompileReason(
                        SkirmishReasonCode.MissingDefinition,
                        "catalog_id",
                        vector.CatalogId + "/" + SkirmishIdParsing.ToCode(vector.SizeId)));
                    continue;
                }

                if (TryCompile(
                    definition,
                    SkirmishDifficultyId.Regular,
                    vector.SizeId,
                    0,
                    manifest,
                    matrix,
                    out SkirmishResolvedSetup setup,
                    out List<SkirmishCompileReason> compileReasons))
                {
                    compiled.Add(setup);
                }
                else
                {
                    reasons.AddRange(compileReasons);
                }
            }

            return compiled.Count > 0;
        }

        // Initial capability rollout: Combined Arms field starts with no authored air.
        // Air Mobile and Established starts retain their existing strategy/save identity
        // until the corresponding strategic orchestration and regressions are qualified.
        // This is an immutable initial-setup decision, never a catalog/seed or live R2 switch.
        public static bool SupportsGroundCommanderRollout(SkirmishResolvedSetup setup) =>
            setup != null && setup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault &&
            setup.ArmyProfileId == SkirmishArmyProfileId.CombinedArms &&
            setup.StartPackageId == SkirmishStartPackageId.FieldBase &&
            setup.Readiness == SkirmishReadinessStage.Field &&
            setup.PlayerAir == 0 && setup.EnemyAir == 0;

        private static void ValidateIdentity(SkirmishScenarioDefinitionConfig definition, List<SkirmishCompileReason> reasons)
        {
            if (!new SkirmishCatalogId(definition.CatalogId).IsValid)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InvalidIdentity, "catalogId", definition.CatalogId));
            if (!new SkirmishDefinitionId(definition.DefinitionId).IsValid)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InvalidIdentity, "definitionId", definition.DefinitionId));
            if (!new SkirmishScenarioSetupId(definition.ScenarioSetupId).IsValid)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InvalidIdentity, "scenarioSetupId", definition.ScenarioSetupId));
        }

        private static void CompareIdentity(
            SkirmishScenarioDefinitionConfig definition,
            SkirmishSetupMatrixRow vector,
            List<SkirmishCompileReason> reasons)
        {
            if (definition.ArmyProfileConfig.Kind != vector.ArmyProfile)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "army_profile", vector.ArmyProfile.ToString()));
            if (definition.StartPackageConfig.Kind != vector.StartProfile)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "start_profile", vector.StartProfile.ToString()));
            if (definition.ObjectiveConfig.Kind != vector.ObjectiveKind)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "logic_id", vector.ObjectiveKind.ToString()));
        }

        private static void RejectMissingCapabilities(
            SkirmishScenarioDefinitionConfig definition,
            SkirmishContentManifest manifest,
            List<SkirmishCompileReason> reasons)
        {
            if (manifest == null || manifest.RequiredFeatureIds == null)
                return;
            string[] required = definition.RequiredFeatureIds;
            if (required == null)
                return;
            for (int i = 0; i < required.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < manifest.RequiredFeatureIds.Length; j++)
                {
                    if (manifest.RequiredFeatureIds[j] == required[i])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedCapability, "requiredFeatureIds", required[i]));
            }
        }

        private static SkirmishResolvedSetup BuildSetup(
            SkirmishScenarioDefinitionConfig definition,
            SkirmishDifficultyId difficulty,
            SkirmishSizeId size,
            int seed,
            SkirmishSetupMatrixRow vector,
            List<SkirmishCompileReason> reasons)
        {
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = definition.CatalogId,
                DefinitionId = definition.DefinitionId,
                ScenarioSetupId = definition.ScenarioSetupId,
                OperationMapId = definition.OperationMapId,
                LayoutId = definition.MapLayoutId,
                ContentVersion = definition.ContentVersion,
                ContentHash = definition.ContentHash,
                Seed = seed,
                DifficultyId = difficulty,
                SizeId = size,
                ArmyProfileId = definition.ArmyProfileConfig.Kind,
                StartPackageId = definition.StartPackageConfig.Kind,
                ObjectiveKind = definition.ObjectiveConfig.Kind,
                Readiness = definition.StartPackageConfig.Readiness,
                DeadlineSeconds = definition.ObjectiveConfig.DeadlineSeconds(size),
                PlayerFaction = definition.PlayerFaction,
                EnemyFaction = definition.EnemyFaction
            };

            definition.EconomyConfig.Stocks(
                definition.StartPackageConfig.Kind,
                size,
                out setup.MaterialsEach,
                out setup.OilEach,
                out setup.UsableFuelEach,
                out setup.MaterialsCapacityEach,
                out setup.OilCapacityEach,
                out setup.FuelCapacityEach);

            CompareInt(vector.Readiness, (int)setup.Readiness, "readiness", reasons);
            CompareInt(vector.DeadlineSeconds, setup.DeadlineSeconds, "deadline_seconds", reasons);
            CompareInt(vector.MaterialsEach, setup.MaterialsEach, "materials_each", reasons);
            CompareInt(vector.OilEach, setup.OilEach, "oil_each", reasons);
            CompareInt(vector.UsableFuelEach, setup.UsableFuelEach, "usable_fuel_each", reasons);
            CompareInt(vector.MaterialsCapacityEach, setup.MaterialsCapacityEach, "materials_capacity_each", reasons);
            CompareInt(vector.OilCapacityEach, setup.OilCapacityEach, "oil_capacity_each", reasons);
            CompareInt(vector.FuelCapacityEach, setup.FuelCapacityEach, "fuel_capacity_each", reasons);

            var forces = new List<SkirmishResolvedForceEntry>();
            AddForce(forces, definition, 1, SkirmishRoleIds.Rifle, vector.PlayerRifle, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.Gunner, vector.PlayerGunner, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.Rocketeer, vector.PlayerRocketeer, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.Car, vector.PlayerCar, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.ApcArmored, vector.PlayerApc, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.Tank, vector.PlayerTank, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.AntiAir, vector.PlayerAa, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.TransportHeli, vector.PlayerTransportHeli, "staging.player", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.Rifle, vector.EnemyRifle, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.Gunner, vector.EnemyGunner, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.Rocketeer, vector.EnemyRocketeer, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.Car, vector.EnemyCar, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.ApcArmored, vector.EnemyApc, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.Tank, vector.EnemyTank, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.AntiAir, vector.EnemyAa, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.TransportHeli, vector.EnemyTransportHeli, "staging.enemy", reasons);

            // The matrix's support counts are real starting actors, separate
            // from combat Supply. One Tanker and the remaining Oil trucks are
            // the authored extraction/conversion/delivery chain for each side.
            AddForce(forces, definition, 1, SkirmishRoleIds.Tanker, vector.PlayerLogisticsSupport > 0 ? 1 : 0, "staging.player", reasons);
            AddForce(forces, definition, 1, SkirmishRoleIds.LogisticsTruck, System.Math.Max(0, vector.PlayerLogisticsSupport - 1), "staging.player", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.Tanker, vector.EnemyLogisticsSupport > 0 ? 1 : 0, "staging.enemy", reasons);
            AddForce(forces, definition, 2, SkirmishRoleIds.LogisticsTruck, System.Math.Max(0, vector.EnemyLogisticsSupport - 1), "staging.enemy", reasons);

            if (setup.ObjectiveKind == SkirmishObjectiveKind.ConvoyEscort)
            {
                var objectiveTruck = SkirmishConvoyTruckDefinition.Load();
                if (objectiveTruck == null || vector.EnemyObjectiveTrucks != 0 ||
                    !objectiveTruck.TryAppendForces(definition.MapLayout, vector.PlayerObjectiveTrucks, vector.ObjectiveSupportExtraPlayer,
                        forces, out var bays, out string truckError))
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "objectiveTruck", "Explicit pinned Cargo Truck definition and three measured bays required."));
                else
                {
                    setup.ObjectiveActorDefinitionFingerprint = objectiveTruck.Fingerprint();
                    setup.ObjectiveTruckBays = bays;
                }
            }

            Tally(forces, 1, out setup.PlayerInfantry, out setup.PlayerGround, out setup.PlayerAir, out setup.PlayerCombat, out setup.PlayerSupply);
            Tally(forces, 2, out setup.EnemyInfantry, out setup.EnemyGround, out setup.EnemyAir, out setup.EnemyCombat, out setup.EnemySupply);
            setup.PlayerLogisticsSupport = vector.PlayerLogisticsSupport;
            setup.EnemyLogisticsSupport = vector.EnemyLogisticsSupport;
            setup.PlayerObjectiveTrucks = vector.PlayerObjectiveTrucks;
            setup.EnemyObjectiveTrucks = vector.EnemyObjectiveTrucks;
            setup.PlayerStartingStructures = vector.PlayerStartingStructures;
            setup.EnemyStartingStructures = vector.EnemyStartingStructures;
            setup.PlayerDesignatedRifles = vector.PlayerDesignatedRifles;
            setup.EnemyDesignatedRifles = vector.EnemyDesignatedRifles;
            setup.InfantryQueuesEach = vector.InfantryQueuesEach;
            setup.VehicleQueuesEach = vector.VehicleQueuesEach;
            setup.LogisticsQueuesEach = vector.LogisticsQueuesEach;
            setup.RefineryModulesEach = vector.RefineryModulesEach;
            setup.InfantryCapEach = vector.InfantryCapEach;
            setup.GroundCapEach = vector.GroundCapEach;
            setup.TacticalAirCapEach = vector.TacticalAirCapEach;
            setup.SupplyCapEach = vector.SupplyCapEach;
            setup.LogisticsSupportCapEach = vector.LogisticsSupportCapEach;
            setup.DeliveryCarrierCapEach = vector.DeliveryCarrierCapEach;
            setup.ObjectiveSupportExtraPlayer = vector.ObjectiveSupportExtraPlayer;
            setup.PlayerBuiltStructureCapEach = vector.PlayerBuiltStructureCapEach;
            setup.BarrierSegmentCapEach = vector.BarrierSegmentCapEach;

            CompareInt(vector.PlayerInfantry, setup.PlayerInfantry, "player_infantry", reasons);
            CompareInt(vector.PlayerGround, setup.PlayerGround, "player_ground", reasons);
            CompareInt(vector.PlayerAir, setup.PlayerAir, "player_air", reasons);
            CompareInt(vector.PlayerCombat, setup.PlayerCombat, "player_combat", reasons);
            CompareInt(vector.PlayerSupply, setup.PlayerSupply, "player_supply", reasons);
            CompareInt(vector.EnemyInfantry, setup.EnemyInfantry, "enemy_infantry", reasons);
            CompareInt(vector.EnemyGround, setup.EnemyGround, "enemy_ground", reasons);
            CompareInt(vector.EnemyAir, setup.EnemyAir, "enemy_air", reasons);
            CompareInt(vector.EnemyCombat, setup.EnemyCombat, "enemy_combat", reasons);
            CompareInt(vector.EnemySupply, setup.EnemySupply, "enemy_supply", reasons);

            if (setup.PlayerInfantry > setup.InfantryCapEach)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientCapacity, "infantry_cap_each", setup.PlayerInfantry.ToString(CultureInfo.InvariantCulture)));
            if (setup.PlayerGround > setup.GroundCapEach)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientCapacity, "ground_cap_each", setup.PlayerGround.ToString(CultureInfo.InvariantCulture)));
            if (setup.PlayerAir > setup.TacticalAirCapEach)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientCapacity, "tactical_air_cap_each", setup.PlayerAir.ToString(CultureInfo.InvariantCulture)));
            if (setup.PlayerSupply > setup.SupplyCapEach)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientCapacity, "supply_cap_each", setup.PlayerSupply.ToString(CultureInfo.InvariantCulture)));

            if (definition.IntelConfig != null)
            {
                setup.SharedFog = definition.IntelConfig.SharedFog;
                setup.DevelopmentFullVision = definition.IntelConfig.DevelopmentFullVision;
                setup.LastSeenExpireSeconds = definition.IntelConfig.LastSeenExpireSeconds;
            }
            else
            {
                setup.SharedFog = true;
                setup.DevelopmentFullVision = true;
                setup.LastSeenExpireSeconds = 20;
            }

            SkirmishLegacyLayoutBaselineContract.TryGetReviewedLayoutFingerprint(setup.CatalogId, out string reviewedLegacyLayoutFingerprint);
            bool preserveLegacyBinding = SkirmishMeasuredLayoutBinder.PreservesPackagedLegacyBinding(setup, definition, reviewedLegacyLayoutFingerprint);
            setup.Structures = BuildStructures(definition, vector, preserveLegacyBinding, reasons);
            setup.TitleKey = definition.TitleKey ?? string.Empty;
            setup.BriefingKey = definition.BriefingKey ?? string.Empty;
            setup.ObjectiveKey = definition.ObjectiveKey ?? string.Empty;
            setup.ResultVictoryKey = definition.ResultVictoryKey ?? string.Empty;
            setup.ResultDefeatKey = definition.ResultDefeatKey ?? string.Empty;
            setup.Forces = forces.ToArray();
            setup.PlayerBaseObjectId = "obj." + definition.CatalogId.ToLowerInvariant() + ".base.player";
            setup.EnemyBaseObjectId = "obj." + definition.CatalogId.ToLowerInvariant() + ".base.enemy";
            setup.RoleOverlays = SkirmishRoleOverlayCatalog.CreateForArmy(definition.ArmyProfileConfig, preserveLegacyBinding);
            if (preserveLegacyBinding)
            {
                SkirmishMapLayoutBuilder.BindRegularStandard(setup, definition.MapLayout, reasons);
            }
            else if (SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string bindingFingerprint))
            {
                setup.LayoutBindingFingerprint = bindingFingerprint;
            }
            // Failed generic binding adds reasons; outer TryCompile discards setup.
            if (SupportsGroundCommanderRollout(setup) || (!preserveLegacyBinding && (SkirmishGenericGroundEnemyRollout.Supports(setup) || SkirmishGenericAirCombinedEnemyRollout.Supports(setup))))
            {
                var policyCatalog = SkirmishExpansionCatalogConfig.Load();
                SkirmishDifficultyConfig difficultyConfig = policyCatalog != null &&
                    policyCatalog.TryGetDefinition(setup.CatalogId, out var packagedDefinition) && packagedDefinition == definition
                    ? policyCatalog.GetDifficulty(setup.DifficultyId) : null;
                if (!SkirmishEnemyPolicyCompiler.TryCompile(setup, definition.MapLayout, out var policy, difficultyConfig))
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "enemyPolicy", "Enemy policy could not resolve existing configuration/routes."));
                else
                {
                    setup.EnemyPolicy = policy;
                    setup.ContentHash = (setup.ContentHash ?? string.Empty) + "/enemy-policy-v1/" + policy.Fingerprint;
                }
            }
            if (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault || definition.MissionLogicalBinding != null)
            {
                if (!SkirmishMissionLogicalBindingCompiler.TryCompile(definition,setup,out var logicalBinding,out string logicalError))
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference,"missionLogicalBinding",logicalError));
                else setup.MissionLogicalBindingFingerprint = logicalBinding.Fingerprint;
            }
            if (SkirmishEnemyObjectivePolicyScope.Supports(setup))
            {
                if (!SkirmishEnemyPolicyCompiler.TryCompile(setup,definition.MapLayout,out var objectivePolicy))
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference,"enemyObjectivePolicy","Exact non-BA enemy policy inputs unavailable."));
                else
                {
                    setup.EnemyPolicy=objectivePolicy;
                    setup.ContentHash=(setup.ContentHash??string.Empty)+"/enemy-objective-policy-v1/"+objectivePolicy.Fingerprint;
                }
            }
            setup.SetupHash = ComputeHash(setup, seed);
            setup.Report = BuildReport(setup);
            return setup;
        }

        private static void AddForce(
            List<SkirmishResolvedForceEntry> forces,
            SkirmishScenarioDefinitionConfig definition,
            byte faction,
            string roleId,
            int quantity,
            string anchorRole,
            List<SkirmishCompileReason> reasons)
        {
            if (quantity <= 0)
                return;
            if (!definition.ArmyProfileConfig.Allows(roleId))
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedRole, "armyProfile", roleId));
                return;
            }

            if (!definition.RoleCatalog.TryGet(roleId, out SkirmishRoleBindingConfig binding))
            {
                if (!SkirmishRoleIds.TryParse(roleId, out SkirmishRoleKind kind))
                {
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedRole, "roleId", roleId));
                    return;
                }

                binding = new SkirmishRoleBindingConfig
                {
                    RoleId = roleId,
                    Kind = kind,
                    RuntimePrefabKey = string.Empty,
                    SourceConfigPath = string.Empty
                };
            }

            if (!definition.MapLayout.TryGetAnchor(anchorRole, out SkirmishLayoutAnchorConfig anchor))
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, "mapLayout", anchorRole));
                return;
            }

            forces.Add(new SkirmishResolvedForceEntry
            {
                FactionId = faction,
                RoleId = roleId,
                RoleKind = binding.Kind,
                RuntimePrefabKey = binding.RuntimePrefabKey,
                SourceConfigPath = binding.SourceConfigPath,
                Quantity = quantity,
                SupplyCost = SkirmishRoleIds.SupplyCost(binding.Kind) * quantity,
                SpawnAnchorId = anchor.AnchorId,
                ObjectiveRoleId = string.Empty
            });
        }

        private static void Tally(
            List<SkirmishResolvedForceEntry> forces,
            byte faction,
            out int infantry,
            out int ground,
            out int air,
            out int combat,
            out int supply)
        {
            infantry = 0;
            ground = 0;
            air = 0;
            combat = 0;
            supply = 0;
            for (int i = 0; i < forces.Count; i++)
            {
                if (forces[i].FactionId != faction)
                    continue;
                SkirmishPopulationCategory category = SkirmishRoleIds.Category(forces[i].RoleKind);
                if (category == SkirmishPopulationCategory.Infantry)
                    infantry += forces[i].Quantity;
                else if (category == SkirmishPopulationCategory.Ground)
                    ground += forces[i].Quantity;
                else if (category == SkirmishPopulationCategory.Air)
                    air += forces[i].Quantity;
                if (category == SkirmishPopulationCategory.Infantry ||
                    category == SkirmishPopulationCategory.Ground ||
                    category == SkirmishPopulationCategory.Air)
                {
                    combat += forces[i].Quantity;
                    supply += forces[i].SupplyCost;
                }
            }
        }

        private static SkirmishResolvedStructureEntry[] BuildStructures(
            SkirmishScenarioDefinitionConfig definition,
            SkirmishSetupMatrixRow vector,
            bool preserveLegacyBinding,
            List<SkirmishCompileReason> reasons)
        {
            string playerAnchor = definition.MapLayout.TryGetAnchor("base.player", out SkirmishLayoutAnchorConfig player)
                ? player.AnchorId
                : "anchor.skirmish.db.base_player";
            string enemyAnchor = definition.MapLayout.TryGetAnchor("base.enemy", out SkirmishLayoutAnchorConfig enemy)
                ? enemy.AnchorId
                : "anchor.skirmish.db.base_enemy";
            string playerStaging = definition.MapLayout.TryGetAnchor("staging.player", out SkirmishLayoutAnchorConfig playerYard)
                ? playerYard.AnchorId
                : "anchor.skirmish.db.staging_player";
            string enemyStaging = definition.MapLayout.TryGetAnchor("staging.enemy", out SkirmishLayoutAnchorConfig enemyYard)
                ? enemyYard.AnchorId
                : "anchor.skirmish.db.staging_enemy";
            var structures = new List<SkirmishResolvedStructureEntry>
            {
                Structure(1, SkirmishStructureIds.Barracks, playerAnchor, SkirmishObjectiveIds.BasePlayer, true),
                Structure(2, SkirmishStructureIds.Barracks, enemyAnchor, SkirmishObjectiveIds.BaseEnemy, true),
                Structure(1, SkirmishStructureIds.GroundStaging, playerStaging, string.Empty, false),
                Structure(2, SkirmishStructureIds.GroundStaging, enemyStaging, string.Empty, false)
            };

            // Field backbone (7 per side): designated Barracks, Ground Staging, and the
            // supply yard (pump, refinery, bladder, fabrication depot) plus one watchtower.
            // Established adds the approach tower and Satellite Dish. A functional Helipad
            // preserves its legacy grant; generic G also grants its allowed transport heli.
            // A second refinery is the War/Large module.
            // Queue capacity stays on the starting Barracks; it is not a second building.
            AddMirrored(structures, definition, SkirmishStructureIds.OilPump, "economy.player", "economy.enemy", -12f, -8f);
            AddMirrored(structures, definition, SkirmishStructureIds.Refinery, "economy.player", "economy.enemy", 12f, -8f);
            AddMirrored(structures, definition, SkirmishStructureIds.FuelBladder, "economy.player", "economy.enemy", -12f, 10f);
            AddMirrored(structures, definition, SkirmishStructureIds.FabricationDepot, "economy.player", "economy.enemy", 12f, 10f);
            AddMirrored(structures, definition, SkirmishStructureIds.Watchtower, "service.player", "service.enemy", -10f, 0f);
            if (IsEstablished(definition))
            {
                AddMirrored(structures, definition, SkirmishStructureIds.WatchtowerApproach, "service.player", "service.enemy", 10f, 0f);
                AddMirrored(structures, definition, SkirmishStructureIds.SatelliteDish, "service.player", "service.enemy", 0f, 14f);
            }

            if (GrantsEstablishedHelipad(definition, preserveLegacyBinding))
            {
                string playerAir = definition.MapLayout.TryGetAnchor("air.player", out SkirmishLayoutAnchorConfig playerPad)
                    ? playerPad.AnchorId
                    : "anchor.skirmish.db.air_player";
                string enemyAir = definition.MapLayout.TryGetAnchor("air.enemy", out SkirmishLayoutAnchorConfig enemyPad)
                    ? enemyPad.AnchorId
                    : "anchor.skirmish.db.air_enemy";
                structures.Add(Structure(1, SkirmishStructureIds.Helipad, playerAir, string.Empty, false));
                structures.Add(Structure(2, SkirmishStructureIds.Helipad, enemyAir, string.Empty, false));
            }

            if (vector.RefineryModulesEach >= 2)
                AddMirrored(structures, definition, SkirmishStructureIds.RefineryModule, "economy.player", "economy.enemy", 0f, 24f);

            // MATCH_SETUP canonical BT/CE defenders have two EXTRA approach
            // towers. Established's mirrored service tower is independent.
            if(definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.Breakthrough||
               definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.ConvoyEscort)
            {
                foreach(var pair in new[]{
                    (SkirmishStructureIds.DefenseApproachA,"defense.approach.a"),
                    (SkirmishStructureIds.DefenseApproachB,"defense.approach.b")})
                {
                    string anchor=definition.MapLayout.TryGetAnchor(pair.Item2,out var actual)?actual.AnchorId:pair.Item2;
                    structures.Add(Structure(2,pair.Item1,anchor,string.Empty,false));
                }
            }
            ApplyAuthoredFacilityOffsets(definition,vector,preserveLegacyBinding,structures,reasons);
            return structures.ToArray();
        }

        // Placement recipe data only. Roster creation, prices/capacities and canonical
        // size conditions above remain the sole gameplay quantity owners.
        private static void ApplyAuthoredFacilityOffsets(SkirmishScenarioDefinitionConfig definition,
            SkirmishSetupMatrixRow vector,bool preserveLegacyBinding,List<SkirmishResolvedStructureEntry> structures,
            List<SkirmishCompileReason> reasons)
        {
            var layout=definition.MapLayout;
            if(layout==null||!layout.HasFacilityPlacements)return;
            if(preserveLegacyBinding||!layout.HasFactionCompounds)
            {reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor,"facilityPlacements","Explicit generic faction-zone placement authority required."));return;}
            var seen=new HashSet<string>(System.StringComparer.Ordinal);
            foreach(var placement in layout.FacilityPlacements)
            {
                string key=placement.FactionId.ToString()+":"+placement.StructureId;
                int index=structures.FindIndex(v=>v.FactionId==placement.FactionId&&v.StructureId==placement.StructureId);
                bool dormantModule=index<0&&placement.StructureId==SkirmishStructureIds.RefineryModule&&vector.RefineryModulesEach==1;
                var structure=index>=0?structures[index]:new SkirmishResolvedStructureEntry{FactionId=placement.FactionId,StructureId=placement.StructureId};
                bool valid=placement.FactionId>=1&&placement.FactionId<=2&&!string.IsNullOrEmpty(placement.StructureId)&&
                    !string.IsNullOrEmpty(placement.PadId)&&seen.Add(key)&&(index>=0||dormantModule)&&
                    !float.IsNaN(placement.AcrossOffsetMetres)&&!float.IsInfinity(placement.AcrossOffsetMetres)&&
                    !float.IsNaN(placement.ForwardOffsetMetres)&&!float.IsInfinity(placement.ForwardOffsetMetres)&&
                    SkirmishMapLayoutBuilder.TryPadForStructure(layout,structure,out var pad)&&pad.PadId==placement.PadId&&
                    layout.TryGetFactionCompound(placement.FactionId,out var zone)&&System.Array.IndexOf(zone.FacilityPadIds,placement.PadId)>=0;
                if(!valid)
                {reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor,"facilityPlacements/"+key,"Duplicate, foreign, nonfinite or nonmember typed placement override."));continue;}
                if(index<0)continue; // Canonical Standard has no War/Large refinery module to add.
                structure.AcrossOffsetMetres=placement.AcrossOffsetMetres;
                structure.ForwardOffsetMetres=placement.ForwardOffsetMetres;
                structures[index]=structure;
            }
        }

        private static bool IsEstablished(SkirmishScenarioDefinitionConfig definition)
        {
            return definition.StartPackageConfig != null &&
                   definition.StartPackageConfig.Kind == SkirmishStartPackageId.EstablishedBase;
        }

        private static void AddMirrored(
            List<SkirmishResolvedStructureEntry> structures,
            SkirmishScenarioDefinitionConfig definition,
            string structureId,
            string playerRole,
            string enemyRole,
            float acrossMetres,
            float forwardMetres)
        {
            string playerAnchor = definition.MapLayout != null &&
                                  definition.MapLayout.TryGetAnchor(playerRole, out SkirmishLayoutAnchorConfig player)
                ? player.AnchorId
                : playerRole;
            string enemyAnchor = definition.MapLayout != null &&
                                 definition.MapLayout.TryGetAnchor(enemyRole, out SkirmishLayoutAnchorConfig enemy)
                ? enemy.AnchorId
                : enemyRole;
            structures.Add(Structure(1, structureId, playerAnchor, string.Empty, false, acrossMetres, forwardMetres));
            structures.Add(Structure(2, structureId, enemyAnchor, string.Empty, false, -acrossMetres, forwardMetres));
        }

        private static bool GrantsEstablishedHelipad(SkirmishScenarioDefinitionConfig definition, bool preserveLegacyBinding)
        {
            return definition.StartPackageConfig != null &&
                   definition.StartPackageConfig.Kind == SkirmishStartPackageId.EstablishedBase &&
                   definition.ArmyProfileConfig != null &&
                   (definition.ArmyProfileConfig.AllowsOffensiveAir ||
                    (!preserveLegacyBinding && definition.ArmyProfileConfig.Kind == SkirmishArmyProfileId.GroundManeuver &&
                     definition.ArmyProfileConfig.Allows(SkirmishRoleIds.TransportHeli)));
        }

        private static SkirmishResolvedStructureEntry Structure(
            byte faction, string id, string anchor, string role, bool designated) =>
            Structure(faction, id, anchor, role, designated, 0f, 0f);

        private static SkirmishResolvedStructureEntry Structure(
            byte faction,
            string id,
            string anchor,
            string role,
            bool designated,
            float acrossOffsetMetres,
            float forwardOffsetMetres) =>
            new SkirmishResolvedStructureEntry
            {
                FactionId = faction,
                StructureId = id,
                SpawnAnchorId = anchor,
                ObjectiveRoleId = role,
                DesignatedBase = designated,
                AcrossOffsetMetres = acrossOffsetMetres,
                ForwardOffsetMetres = forwardOffsetMetres
            };

        private static void CompareInt(int expected, int actual, string field, List<SkirmishCompileReason> reasons)
        {
            if (expected == actual)
                return;
            reasons.Add(new SkirmishCompileReason(
                SkirmishReasonCode.MatrixMismatch,
                field,
                "expected=" + expected.ToString(CultureInfo.InvariantCulture) +
                " actual=" + actual.ToString(CultureInfo.InvariantCulture)));
        }

        public static uint ComputeHash(SkirmishResolvedSetup setup, int seed)
        {
            return ComputeHashCore(setup, seed, true);
        }

        public static uint ComputePolicylessHash(SkirmishResolvedSetup setup, int seed) => ComputeHashCore(setup, seed, false);

        private static uint ComputeHashCore(SkirmishResolvedSetup setup, int seed, bool includePolicy)
        {
            var builder = new StringBuilder();
            builder.Append(setup.CatalogId).Append('|')
                .Append(setup.DefinitionId).Append('|')
                .Append(setup.SizeId).Append('|')
                .Append(setup.DifficultyId).Append('|')
                .Append(seed.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(setup.PlayerInfantry).Append('|')
                .Append(setup.PlayerGround).Append('|')
                .Append(setup.PlayerTankCount()).Append('|')
                .Append(setup.MaterialsEach).Append('|')
                .Append(setup.DeadlineSeconds);
            if (!string.IsNullOrEmpty(setup.LayoutBindingFingerprint))
                builder.Append("|layout-binding-v1|").Append(setup.LayoutBindingFingerprint);
            // Only new generic G bindings receive the complete roster semantics.
            // Exact certified legacy bindings retain their historical hash text.
            if (setup.ArmyProfileId == SkirmishArmyProfileId.GroundManeuver && !string.IsNullOrEmpty(setup.LayoutBindingFingerprint))
            {
                builder.Append("|ground-roster-v1|");
                if (setup.RoleOverlays != null) foreach (var role in setup.RoleOverlays)
                {
                    builder.Append(role.RoleId).Append(':').Append((int)role.RoleKind).Append(':')
                        .Append(role.MaxHealth).Append(':').Append(role.Damage).Append(':')
                        .Append(role.RangeWorld.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                        .Append(role.SupplyCost).Append(':').Append(role.MaterialsCost).Append(':').Append(role.FuelCost).Append(':')
                        .Append((int)role.Producer).Append(':').Append((int)role.TargetDomains).Append(':')
                        .Append(role.SquadMembers).Append(';');
                }
            }
            if (!string.IsNullOrEmpty(setup.ObjectiveActorDefinitionFingerprint))
                builder.Append("|objective-actor-v1|").Append(setup.ObjectiveActorDefinitionFingerprint).Append('|').Append(UnityEngine.JsonUtility.ToJson(new ConvoyBayHash { Bays = setup.ObjectiveTruckBays }));
            if (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault || !string.IsNullOrEmpty(setup.MissionLogicalBindingFingerprint))
                builder.Append("|mission-logical-v1|").Append(setup.MissionLogicalBindingFingerprint);
            if (includePolicy && setup.EnemyPolicy != null) builder.Append("|enemy-policy-v1|").Append(setup.EnemyPolicy.Fingerprint);
            unchecked
            {
                uint hash = 2166136261;
                string text = builder.ToString();
                for (int i = 0; i < text.Length; i++)
                    hash = (hash ^ text[i]) * 16777619;
                return hash;
            }
        }

        [System.Serializable] private sealed class ConvoyBayHash { public SkirmishResolvedConvoyBay[] Bays; }

        private static string BuildReport(SkirmishResolvedSetup setup)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "catalog={0} definition={1} size={2} difficulty={3} seed={4} hash={5:X8} infantry={6}/{7} ground={8}/{9} supply={10}/{11} structures={12}/{13} deadline={14}",
                setup.CatalogId,
                setup.DefinitionId,
                SkirmishIdParsing.ToCode(setup.SizeId),
                SkirmishIdParsing.ToCode(setup.DifficultyId),
                setup.Seed,
                setup.SetupHash,
                setup.PlayerInfantry,
                setup.EnemyInfantry,
                setup.PlayerGround,
                setup.EnemyGround,
                setup.PlayerSupply,
                setup.EnemySupply,
                setup.PlayerStartingStructures,
                setup.EnemyStartingStructures,
                setup.DeadlineSeconds);
        }
    }

    public static class SkirmishResolvedSetupExtensions
    {
        public static int PlayerTankCount(this SkirmishResolvedSetup setup)
        {
            int count = 0;
            if (setup?.Forces == null)
                return 0;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                if (setup.Forces[i].FactionId == 1 && setup.Forces[i].RoleKind == SkirmishRoleKind.Tank)
                    count += setup.Forces[i].Quantity;
            }

            return count;
        }
    }
}
