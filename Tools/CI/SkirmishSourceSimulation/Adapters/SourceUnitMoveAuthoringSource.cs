using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class SourceUnitMoveAuthoringSource
    {
        internal readonly string RootKey;
        internal readonly string ConfigGuid;
        internal readonly UnitMoveAuthoringProjection Projection;
        internal readonly UnitCombat Combat;
        internal readonly UnitHealth Health;
        internal readonly UnitAttack Attack;
        internal readonly IReadOnlyDictionary<string, string> Pins;

        private SourceUnitMoveAuthoringSource(string rootKey, string configGuid, UnitMoveAuthoringProjection projection,
            UnitCombat combat, UnitHealth health, UnitAttack attack, Dictionary<string, string> pins)
        { RootKey = rootKey; ConfigGuid = configGuid; Projection = projection; Combat = combat; Health = health; Attack = attack; Pins = pins; }

        internal static SourceUnitMoveAuthoringSource Load(JObject descriptor, JObject capturedSource,
            string expectedRootKey, string sourceRepositoryRoot, Func<string, string, int, byte[]> readPinned)
            => LoadCore(descriptor, capturedSource, expectedRootKey, sourceRepositoryRoot, readPinned, false);

        // Computes the current Alt04 authoring value slice from pinned source only.
        // This path intentionally has no retained-capture parameter and never reads
        // the prefab or asks the template importer to allocate entities.
        internal static SourceUnitMoveAuthoringSource LoadCurrentProjection(JObject descriptor,
            string expectedRootKey, string sourceRepositoryRoot, Func<string, string, int, byte[]> readPinned)
            => LoadCore(descriptor, null, expectedRootKey, sourceRepositoryRoot, readPinned, true);

        private static SourceUnitMoveAuthoringSource LoadCore(JObject descriptor, JObject capturedSource,
            string expectedRootKey, string sourceRepositoryRoot, Func<string, string, int, byte[]> readPinned,
            bool sourceOnly)
        {
            if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
                descriptor["sha256"]?.Type != JTokenType.String || readPinned == null ||
                string.IsNullOrWhiteSpace(sourceRepositoryRoot) || !Path.IsPathRooted(sourceRepositoryRoot))
                throw new InvalidDataException("Exact pinned UnitMove source descriptor required");
            string repoRoot = ValidatePath(sourceRepositoryRoot, directory: true).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string descriptorPath = ValidatePath((string)descriptor["path"], directory: false);
            byte[] descriptorBytes = readPinned(descriptorPath, (string)descriptor["sha256"], 64 * 1024);
            JObject input = JObject.Parse(Encoding.UTF8.GetString(descriptorBytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            string[] fields = sourceOnly
                ? new[] { "schema_version", "kind", "root_key", "config_guid", "source_pins", "semantics" }
                : new[] { "schema_version", "kind", "root_key", "config_guid", "values_bits", "movement_values", "captured_source_pins", "source_pins", "semantics" };
            if (input.Count != fields.Length || input.Properties().Any(p => !fields.Contains(p.Name, StringComparer.Ordinal)) ||
                (int?)input["schema_version"] != (sourceOnly ? 1 : 2) ||
                (string)input["kind"] != (sourceOnly ? "StrictCurrentUnitAuthoringProjectionInput" : "StrictUnitMovementAuthoringSourceInput") ||
                (string)input["root_key"] != expectedRootKey || expectedRootKey != "Unit_Chr_Soldier_Male_02_Alt_04")
                throw new InvalidDataException("Exact single-rifle UnitMove source descriptor required");
            string configRel = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset";
            string configMetaRel = configRel + ".meta";
            string prefabRel = "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";
            string authoringRel = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs";
            string authoringVisualsRel = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.BakerVisuals.cs";
            string helperRel = "Assets/Game/Scripts/Components/UnitMoveAuthoringProjection.cs";
            string helperMetaRel = helperRel + ".meta";
            string combatHealthHelperRel = "Assets/Game/Scripts/Components/UnitCombatHealthAuthoringProjection.cs";
            string combatHealthHelperMetaRel = combatHealthHelperRel + ".meta";
            string combatComponentsRel = "Assets/Game/Scripts/Components/UnitCombatComponents.cs";
            string attackHelperRel = "Assets/Game/Scripts/Components/UnitAttackAuthoringProjection.cs";
            string attackHelperMetaRel = attackHelperRel + ".meta";
            string configModelsRel = "Assets/Game/Scripts/Configs/GameplayConfigModels.cs";
            string importerRel = "Tools/CI/SkirmishSourceSimulation/Adapters/GeneratedReferenceTemplateImporter.UnitMoveProjection.cs";
            string sourceAdapterRel = "Tools/CI/SkirmishSourceSimulation/Adapters/SourceUnitMoveAuthoringSource.cs";
            string manifestS002Rel = "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json";
            string manifestS003Rel = "Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json";
            string[] sharedRels = { configRel, configMetaRel, authoringRel, authoringVisualsRel, helperRel, helperMetaRel,
                combatHealthHelperRel, combatHealthHelperMetaRel, combatComponentsRel, attackHelperRel, attackHelperMetaRel, configModelsRel,
                sourceAdapterRel, importerRel, manifestS002Rel, manifestS003Rel };
            string[] expectedRels = sourceOnly ? sharedRels : new[] { configRel, configMetaRel, prefabRel, authoringRel, authoringVisualsRel,
                helperRel, helperMetaRel, combatHealthHelperRel, combatHealthHelperMetaRel, combatComponentsRel, attackHelperRel,
                attackHelperMetaRel, configModelsRel, sourceAdapterRel, importerRel, manifestS002Rel, manifestS003Rel };
            JObject sourcePins = input["source_pins"] as JObject;
            if (sourcePins == null || sourcePins.Count != expectedRels.Length)
                throw new InvalidDataException("Exact UnitMove source pin closure required");
            var verified = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string rel in expectedRels)
            {
                string path = Path.GetFullPath(Path.Combine(repoRoot, rel));
                if (!path.StartsWith(repoRoot, StringComparison.Ordinal)) throw new InvalidDataException("UnitMove source pin escaped repository");
                string digest = (string)sourcePins[rel];
                if (digest == null) throw new InvalidDataException("Missing UnitMove source pin: " + rel);
                path = ValidatePath(path, directory: false);
                readPinned(path, digest, 8 * 1024 * 1024);
                verified.Add(path, digest);
            }

            if (!sourceOnly)
            {
                JObject capturedPins = input["captured_source_pins"] as JObject;
                JObject rawPins = capturedSource?["source_pins"] as JObject;
                if (capturedPins == null || capturedPins.Count != 3 || rawPins == null)
                    throw new InvalidDataException("S003 capture-to-config source binding required");
                foreach (string rel in new[] { configRel, configMetaRel, prefabRel })
                {
                    string capturedHash = (string)capturedPins[rel];
                    string expectedPath = Path.GetFullPath(Path.Combine(repoRoot, rel));
                    var rawMatches = rawPins.Properties().Where(p =>
                    {
                        string raw = p.Name.Replace('\\', Path.DirectorySeparatorChar);
                        string resolved = Path.GetFullPath(Path.IsPathRooted(raw) ? raw : Path.Combine(repoRoot, raw));
                        return string.Equals(resolved, expectedPath, StringComparison.Ordinal);
                    }).ToArray();
                    if (capturedHash == null || rawMatches.Length == 0 ||
                        rawMatches.Any(p => (string)p.Value != capturedHash) ||
                        capturedHash != verified[expectedPath])
                        throw new InvalidDataException("Rifle config source pin differs from the retained bake capture: " + rel);
                }
            }

            string configPath = Path.GetFullPath(Path.Combine(repoRoot, configRel));
            string configMetaPath = Path.GetFullPath(Path.Combine(repoRoot, configMetaRel));
            string prefabPath = sourceOnly ? null : Path.GetFullPath(Path.Combine(repoRoot, prefabRel));
            string authoringPath = Path.GetFullPath(Path.Combine(repoRoot, authoringRel));
            string helperPath = Path.GetFullPath(Path.Combine(repoRoot, helperRel));
            string combatHelperPath = Path.GetFullPath(Path.Combine(repoRoot, combatHealthHelperRel));
            string combatComponentsPath = Path.GetFullPath(Path.Combine(repoRoot, combatComponentsRel));
            string attackHelperPath = Path.GetFullPath(Path.Combine(repoRoot, attackHelperRel));
            string configModelsPath = Path.GetFullPath(Path.Combine(repoRoot, configModelsRel));
            string importerPath = Path.GetFullPath(Path.Combine(repoRoot, importerRel));
            string config = Encoding.UTF8.GetString(readPinned(configPath, verified[configPath], 1024 * 1024));
            string meta = Encoding.UTF8.GetString(readPinned(configMetaPath, verified[configMetaPath], 64 * 1024));
            string prefab = sourceOnly ? null : Encoding.UTF8.GetString(readPinned(prefabPath, verified[prefabPath], 4 * 1024 * 1024));
            string authoring = Encoding.UTF8.GetString(readPinned(authoringPath, verified[authoringPath], 4 * 1024 * 1024));
            string helper = Encoding.UTF8.GetString(readPinned(helperPath, verified[helperPath], 256 * 1024));
            string combatHelper = Encoding.UTF8.GetString(readPinned(combatHelperPath, verified[combatHelperPath], 128 * 1024));
            string combatComponents = Encoding.UTF8.GetString(readPinned(combatComponentsPath, verified[combatComponentsPath], 256 * 1024));
            string attackHelper = Encoding.UTF8.GetString(readPinned(attackHelperPath, verified[attackHelperPath], 128 * 1024));
            string configModels = Encoding.UTF8.GetString(readPinned(configModelsPath, verified[configModelsPath], 2 * 1024 * 1024));
            string importer = Encoding.UTF8.GetString(readPinned(importerPath, verified[importerPath], 256 * 1024));
            if (config.IndexOf("m_Name: Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("Unexpected rifle UnitGridAuthoringConfig asset name");
            string configGuid = MatchOne(meta, @"(?m)^guid: ([0-9a-f]{32})$", "config GUID");
            if ((string)input["config_guid"] != configGuid || (!sourceOnly && Regex.Matches(prefab, "guid: " + Regex.Escape(configGuid)).Count != 1))
                throw new InvalidDataException("Rifle prefab does not bind the pinned config GUID");
            string applyConfig = MethodBody(authoring, "private void ApplyConfigIfAvailable()");
            foreach (string assignment in new[] { "speed = config.Speed;", "walkSpeed = config.WalkSpeed;",
                         "roadSpeedMultiplier = config.RoadSpeedMultiplier;", "arriveDistance = config.ArriveDistance;",
                         "allowIdleWander = config.AllowIdleWander;", "autoCalculateFootprint = config.AutoCalculateFootprint;",
                         "footprintCells = config.FootprintCells;", "usesVehicleMotion = config.UsesVehicleMotion;" })
                if (applyConfig.IndexOf(assignment, StringComparison.Ordinal) < 0) throw new InvalidDataException("ApplyConfigIfAvailable copy semantics changed: " + assignment);

            string configClass = ClassBody(configModels, "public class UnitGridAuthoringConfig : ScriptableObject");
            var combatInputs = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["CanAttack"] = "config != null ? config.CanAttack : canAttack",
                ["AllowAutoEngage"] = "config != null ? config.AllowAutoEngage : allowAutoEngage",
                ["AggroRangeCells"] = "Mathf.Max(0, config != null ? config.AggroRangeCells : aggroRangeCells)",
                ["ChaseBreakDistance"] = "Mathf.Max(0f, config != null ? config.ChaseBreakDistance : chaseBreakDistance)",
                ["MaxHealth"] = "Mathf.Max(1, config != null ? config.MaxHealth : maxHealth)",
            };
            var combatConfigFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["CanAttack"] = "canAttack", ["AllowAutoEngage"] = "allowAutoEngage",
                ["AggroRangeCells"] = "aggroRangeCells", ["ChaseBreakDistance"] = "chaseBreakDistance",
                ["MaxHealth"] = "maxHealth",
            };
            var combatAuthoringFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["CanAttack"] = "canAttack", ["AllowAutoEngage"] = "allowAutoEngage",
                ["AggroRangeCells"] = "aggroRangeCells", ["ChaseBreakDistance"] = "chaseBreakDistance",
                ["MaxHealth"] = "maxHealth",
            };
            foreach (string name in combatInputs.Keys)
            {
                if (PropertyExpression(authoring, name == "CanAttack" ? "ConfiguredCanAttack" :
                        name == "AllowAutoEngage" ? "ConfiguredAllowAutoEngage" : "Configured" + name) != combatInputs[name])
                    throw new InvalidDataException("Current combat authoring getter semantics changed: " + name);
                if (PropertyExpression(configClass, name) != combatConfigFields[name])
                    throw new InvalidDataException("Current UnitGridAuthoringConfig property semantics changed: " + name);
                string applyField = combatAuthoringFields[name];
                string applyProperty = applyField == "aggroRangeCells" ? "AggroRangeCells" :
                    applyField == "allowAutoEngage" ? "AllowAutoEngage" :
                    char.ToUpperInvariant(applyField[0]) + applyField.Substring(1);
                if (applyConfig.IndexOf(applyField + " = config." + applyProperty + ";", StringComparison.Ordinal) < 0)
                    throw new InvalidDataException("ApplyConfigIfAvailable combat input copy changed: " + applyField);
            }
            if (combatHelper.IndexOf("public static UnitCombat FromConfiguredCombatValues(", StringComparison.Ordinal) < 0 ||
                combatHelper.IndexOf("AggroRangeCells = aggroRangeCells", StringComparison.Ordinal) < 0 ||
                combatHelper.IndexOf("ChaseBreakDistance = chaseBreakDistance", StringComparison.Ordinal) < 0 ||
                combatHelper.IndexOf("CanAttack = (byte)(canAttack ? 1 : 0)", StringComparison.Ordinal) < 0 ||
                combatHelper.IndexOf("AutoEngage = (byte)(allowAutoEngage ? 1 : 0)", StringComparison.Ordinal) < 0 ||
                combatHelper.IndexOf("Current = maxHealth, Max = maxHealth", StringComparison.Ordinal) < 0 ||
                combatComponents.IndexOf("public struct" + " UnitCombat : IComponentData", StringComparison.Ordinal) < 0 ||
                combatComponents.IndexOf("public struct" + " UnitHealth : IComponentData", StringComparison.Ordinal) < 0 ||
                importer.IndexOf("Projected rifle root must contain UnitCombat and UnitHealth", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("Shared UnitCombat/UnitHealth projection/importer source binding changed");
            var attackAuthoringInputs = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ConfiguredAttackRange"] = "Mathf.Max(0f, config != null ? config.AttackRange : attackRange)",
                ["ConfiguredAttackCooldownSeconds"] = "Mathf.Max(0.01f, config != null ? config.AttackCooldownSeconds : attackCooldownSeconds)",
                ["ConfiguredAttackDamage"] = "Mathf.Max(0, config != null ? config.AttackDamage : attackDamage)",
            };
            foreach (var pair in attackAuthoringInputs)
                if (PropertyExpression(authoring, pair.Key) != pair.Value)
                    throw new InvalidDataException("Current UnitAttack getter semantics changed: " + pair.Key);
            foreach (var pair in new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["AttackRange"] = "attackRange", ["AttackCooldownSeconds"] = "attackCooldownSeconds",
                ["AttackDamage"] = "attackDamage", ["AttackTraceColor"] = "attackTraceColor",
                ["AttackTraceWidth"] = "attackTraceWidth", ["AttackTraceScrollSpeed"] = "attackTraceScrollSpeed",
                ["AttackTraceDashDensity"] = "attackTraceDashDensity", ["AttackTraceVisibleSeconds"] = "attackTraceVisibleSeconds",
                ["AttackTracerEveryNthShot"] = "Mathf.Max(1, attackTracerEveryNthShot)",
            })
                if (PropertyExpression(configClass, pair.Key) != pair.Value)
                    throw new InvalidDataException("Current UnitGridAuthoringConfig attack property changed: " + pair.Key);
            // UnitGridAuthoring does not expose trace settings as public properties:
            // the Baker reads the ScriptableObject properties directly and falls back
            // to serialized fields. Those exact expressions are checked below.
            if (attackHelper.IndexOf("public static UnitAttack FromConfiguredValues(", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("Range = range", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("CooldownSeconds = cooldownSeconds", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("Damage = damage", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("TraceColor = new float4(traceRed, traceGreen, traceBlue, traceAlpha)", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("TraceWidth = math.max(0.01f, traceWidth)", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("TraceScrollSpeed = math.max(0.1f, traceScrollSpeed)", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("TraceDashDensity = math.max(1f, traceDashDensity)", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("TraceVisibleSeconds = math.max(0.01f, traceVisibleSeconds)", StringComparison.Ordinal) < 0 ||
                attackHelper.IndexOf("TracerEveryNthShot = math.max(1, tracerEveryNthShot)", StringComparison.Ordinal) < 0 ||
                combatComponents.IndexOf("public struct UnitAttack : IComponentData", StringComparison.Ordinal) < 0 ||
                importer.IndexOf("Projected rifle root must contain UnitAttack", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("Shared UnitAttack projection/importer source binding changed");
            string bake = MethodBody(authoring, "public override void Bake(UnitGridAuthoring authoring)");
            string compactBake = Regex.Replace(bake, @"\s+", string.Empty);
            string exactMovementCall = "UnitMoveAuthoringProjection.FromAuthoredMovement(authoring.speed,authoring.walkSpeed,authoring.roadSpeedMultiplier,authoring.arriveDistance,authoring.footprintCells.x,authoring.footprintCells.y,authoring.autoCalculateFootprint,authoring.UsesVehicleMotion,authoring.allowIdleWander,hasModelBounds,modelBounds.size.x,modelBounds.size.z)";
            if (bake.IndexOf("UnitMoveAuthoringProjection.FromAuthoredMovement(", StringComparison.Ordinal) < 0 ||
                compactBake.IndexOf("boolhasModelBounds=TryGetModelLocalBounds(authoring,outmodelBounds);", StringComparison.Ordinal) < 0 ||
                compactBake.IndexOf(exactMovementCall, StringComparison.Ordinal) < 0 ||
                importer.IndexOf("Complete baked rifle movement components required", StringComparison.Ordinal) < 0 ||
                helper.IndexOf("math.min(math.max(0.01f, walkSpeed), math.max(0.01f, speed))", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("Shared movement projection/baker/importer binding changed");
            string exactAttackCall = "UnitAttackAuthoringProjection.FromConfiguredValues(authoring.ConfiguredAttackRange,authoring.ConfiguredAttackCooldownSeconds,authoring.ConfiguredAttackDamage,traceColor.r,traceColor.g,traceColor.b,traceColor.a,traceWidth,traceScrollSpeed,traceDashDensity,traceVisibleSeconds,authoring.config!=null?authoring.config.AttackTracerEveryNthShot:authoring.attackTracerEveryNthShot)";
            if (compactBake.IndexOf(exactAttackCall, StringComparison.Ordinal) < 0 ||
                compactBake.IndexOf(exactAttackCall, compactBake.IndexOf(exactAttackCall, StringComparison.Ordinal) + exactAttackCall.Length, StringComparison.Ordinal) >= 0 ||
                compactBake.IndexOf("ColortraceColor=authoring.config!=null?authoring.config.AttackTraceColor:authoring.attackTraceColor;", StringComparison.Ordinal) < 0 ||
                compactBake.IndexOf("floattraceWidth=authoring.config!=null?authoring.config.AttackTraceWidth:authoring.attackTraceWidth;", StringComparison.Ordinal) < 0 ||
                compactBake.IndexOf("floattraceScrollSpeed=authoring.config!=null?authoring.config.AttackTraceScrollSpeed:authoring.attackTraceScrollSpeed;", StringComparison.Ordinal) < 0 ||
                compactBake.IndexOf("floattraceDashDensity=authoring.config!=null?authoring.config.AttackTraceDashDensity:authoring.attackTraceDashDensity;", StringComparison.Ordinal) < 0 ||
                compactBake.IndexOf("floattraceVisibleSeconds=authoring.config!=null?authoring.config.AttackTraceVisibleSeconds:authoring.attackTraceVisibleSeconds;", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("UnitAttack Baker input selection/evaluation order changed");

            float speed = ConfigFloat(config, "speed");
            float walkSpeed = ConfigFloat(config, "walkSpeed");
            float road = ConfigFloat(config, "roadSpeedMultiplier");
            float arrive = ConfigFloat(config, "arriveDistance");
            if (!sourceOnly)
            {
                JObject bits = input["values_bits"] as JObject;
                if (bits == null || bits.Count != 4 || Bits(speed) != (string)bits["speed"] ||
                    Bits(walkSpeed) != (string)bits["walkSpeed"] || Bits(road) != (string)bits["roadSpeedMultiplier"] ||
                    Bits(arrive) != (string)bits["arriveDistance"])
                    throw new InvalidDataException("Descriptor values differ from exact serialized UnitGridAuthoringConfig fields");
            }

            JObject movementValues = input["movement_values"] as JObject;
            if (!sourceOnly && (movementValues == null || movementValues.Count != 5 ||
                (bool?)movementValues["allowIdleWander"] != ConfigBool(config, "allowIdleWander") ||
                (bool?)movementValues["usesVehicleMotion"] != ConfigBool(config, "usesVehicleMotion") ||
                (bool?)movementValues["autoCalculateFootprint"] != ConfigBool(config, "autoCalculateFootprint") ||
                (int?)movementValues["footprintX"] != ConfigInt(config, "footprintCells", "x") ||
                (int?)movementValues["footprintY"] != ConfigInt(config, "footprintCells", "y") ||
                ConfigBool(config, "usesVehicleMotion")))
                throw new InvalidDataException("Descriptor movement fields differ or the bounded rifle projection now requires model bounds");

            int aggroRangeCells = ConfigInt(config, "aggroRangeCells");
            float chaseBreakDistance = ConfigFloat(config, "chaseBreakDistance");
            bool canAttack = ConfigBool(config, "canAttack");
            bool allowAutoEngage = ConfigBool(config, "allowAutoEngage");
            int maxHealth = ConfigInt(config, "maxHealth");
            RequireNoGetterClamp(aggroRangeCells, combatInputs["AggroRangeCells"], "AggroRangeCells");
            RequireNoGetterClamp(chaseBreakDistance, combatInputs["ChaseBreakDistance"], "ChaseBreakDistance");
            RequireNoGetterClamp(maxHealth, combatInputs["MaxHealth"], "MaxHealth");
            UnitCombat expectedCombat = UnitCombatHealthAuthoringProjection.FromConfiguredCombatValues(
                aggroRangeCells, chaseBreakDistance, canAttack, allowAutoEngage);
            UnitHealth expectedHealth = UnitCombatHealthAuthoringProjection.FromConfiguredMaxHealth(maxHealth);
            float attackRange = ConfigFloat(config, "attackRange");
            float attackCooldown = ConfigFloat(config, "attackCooldownSeconds");
            int attackDamage = ConfigInt(config, "attackDamage");
            float traceRed = ConfigColor(config, "attackTraceColor", "r");
            float traceGreen = ConfigColor(config, "attackTraceColor", "g");
            float traceBlue = ConfigColor(config, "attackTraceColor", "b");
            float traceAlpha = ConfigColor(config, "attackTraceColor", "a");
            float traceWidth = ConfigFloat(config, "attackTraceWidth");
            float traceScrollSpeed = ConfigFloat(config, "attackTraceScrollSpeed");
            float traceDashDensity = ConfigFloat(config, "attackTraceDashDensity");
            float traceVisibleSeconds = ConfigFloat(config, "attackTraceVisibleSeconds");
            int tracerEveryNthShot = ConfigInt(config, "attackTracerEveryNthShot");
            RequireNoGetterClamp(attackRange, attackAuthoringInputs["ConfiguredAttackRange"], "AttackRange");
            RequireNoGetterClamp(attackCooldown, attackAuthoringInputs["ConfiguredAttackCooldownSeconds"], "AttackCooldownSeconds");
            RequireNoGetterClamp(attackDamage, attackAuthoringInputs["ConfiguredAttackDamage"], "AttackDamage");
            if (tracerEveryNthShot < 1)
                throw new InvalidDataException("Current rifle tracer cadence would be changed by its config getter clamp");
            UnitAttack expectedAttack = UnitAttackAuthoringProjection.FromConfiguredValues(
                attackRange, attackCooldown, attackDamage, traceRed, traceGreen, traceBlue, traceAlpha,
                traceWidth, traceScrollSpeed, traceDashDensity, traceVisibleSeconds, tracerEveryNthShot);

            return new SourceUnitMoveAuthoringSource(expectedRootKey, configGuid,
                UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(speed, walkSpeed, road, arrive,
                    ConfigInt(config, "footprintCells", "x"), ConfigInt(config, "footprintCells", "y"),
                    ConfigBool(config, "autoCalculateFootprint"), ConfigBool(config, "usesVehicleMotion"),
                    ConfigBool(config, "allowIdleWander")), expectedCombat, expectedHealth, expectedAttack, verified);
        }

        private static string ValidatePath(string rawPath, bool directory)
        {
            if (string.IsNullOrWhiteSpace(rawPath) || !Path.IsPathRooted(rawPath))
                throw new InvalidDataException("Absolute UnitMove source path required");
            string fullPath = Path.GetFullPath(rawPath);
            string root = Path.GetPathRoot(fullPath);
            string cursor = root;
            string remainder = fullPath.Substring(root.Length);
            foreach (string part in remainder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
            {
                cursor = Path.Combine(cursor, part);
                if (!Directory.Exists(cursor) && !File.Exists(cursor))
                    throw new FileNotFoundException("UnitMove source path component is missing", cursor);
                if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Symlink/reparse point in UnitMove source path: " + cursor);
            }
            if (directory ? !Directory.Exists(fullPath) : !File.Exists(fullPath))
                throw new InvalidDataException(directory ? "UnitMove repository root directory required" : "UnitMove descriptor/source file required");
            return fullPath;
        }

        private static string MatchOne(string text, string pattern, string label)
        {
            MatchCollection matches = Regex.Matches(text, pattern);
            if (matches.Count != 1) throw new InvalidDataException("Expected exactly one " + label);
            return matches[0].Groups[1].Value;
        }

        private static string MethodBody(string source, string signature)
        {
            int start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) throw new InvalidDataException("Missing authoring source method: " + signature);
            int open = source.IndexOf('{', start + signature.Length);
            if (open < 0) throw new InvalidDataException("Missing method body: " + signature);
            int depth = 0;
            for (int i = open; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}' && --depth == 0) return source.Substring(open, i - open + 1);
            }
            throw new InvalidDataException("Unbalanced method body: " + signature);
        }

        private static string ClassBody(string source, string declaration)
        {
            int start = source.IndexOf(declaration, StringComparison.Ordinal);
            if (start < 0) throw new InvalidDataException("Missing source class: " + declaration);
            if (source.IndexOf(declaration, start + declaration.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidDataException("Ambiguous source class: " + declaration);
            int open = source.IndexOf('{', start + declaration.Length);
            if (open < 0) throw new InvalidDataException("Missing source class body: " + declaration);
            int depth = 0;
            for (int i = open; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}' && --depth == 0) return source.Substring(open + 1, i - open - 1);
            }
            throw new InvalidDataException("Unbalanced source class: " + declaration);
        }

        private static string PropertyExpression(string source, string name)
        {
            MatchCollection matches = Regex.Matches(source,
                @"(?m)^\s*public\s+[^;\r\n]+?\s+" + Regex.Escape(name) + @"\s*=>\s*([^;\r\n]+);");
            if (matches.Count != 1) throw new InvalidDataException("Expected exactly one source property: " + name);
            return matches[0].Groups[1].Value.Trim();
        }

        private static void RequireNoGetterClamp(int value, string getterExpression, string label)
        {
            int threshold = GetterMaxThreshold<int>(getterExpression, label);
            if (value < threshold)
                throw new InvalidDataException("Current config value would be changed by the pinned getter clamp: " + label);
        }

        private static void RequireNoGetterClamp(float value, string getterExpression, string label)
        {
            float threshold = GetterMaxThreshold<float>(getterExpression, label);
            if (value < threshold)
                throw new InvalidDataException("Current config value would be changed by the pinned getter clamp: " + label);
        }

        private static T GetterMaxThreshold<T>(string getterExpression, string label) where T : struct
        {
            Match match = Regex.Match(getterExpression,
                @"^Mathf\.Max\(([-+]?(?:\d+(?:\.\d*)?|\.\d+)[fF]?),\s*config\s*!=\s*null\s*\?");
            if (!match.Success) throw new InvalidDataException("Expected pinned Mathf.Max getter clamp for " + label);
            string thresholdText = match.Groups[1].Value.TrimEnd('f', 'F');
            object value = typeof(T) == typeof(int)
                ? (object)int.Parse(thresholdText, CultureInfo.InvariantCulture)
                : float.Parse(thresholdText, CultureInfo.InvariantCulture);
            return (T)value;
        }

        private static float ConfigFloat(string text, string name) =>
            float.Parse(MatchOne(text, @"(?m)^\s*" + Regex.Escape(name) + @":\s*([-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)\s*$", name), CultureInfo.InvariantCulture);

        private static float ConfigColor(string text, string name, string channel) =>
            float.Parse(MatchOne(text, @"(?m)^\s*" + Regex.Escape(name) + @":\s*\{[^\r\n}]*\b" + Regex.Escape(channel) + @":\s*([-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)[^\r\n}]*\}\s*$", name + "." + channel), CultureInfo.InvariantCulture);

        private static bool ConfigBool(string text, string name) =>
            MatchOne(text, @"(?m)^\s*" + Regex.Escape(name) + @":\s*([01])\s*$", name) == "1";

        private static int ConfigInt(string text, string parent, string child) =>
            int.Parse(MatchOne(text, @"(?m)^\s*" + Regex.Escape(parent) + @":\s*\{[^\r\n]*?\b" + Regex.Escape(child) + @":\s*(-?\d+)\s*,?[^\r\n]*\}\s*$", parent + "." + child), CultureInfo.InvariantCulture);

        private static int ConfigInt(string text, string name) =>
            int.Parse(MatchOne(text, @"(?m)^\s*" + Regex.Escape(name) + @":\s*(-?\d+)\s*$", name), CultureInfo.InvariantCulture);

        private static string Bits(float value) =>
            unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0)).ToString("x8", CultureInfo.InvariantCulture);
    }
}
