using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Runtime;
using Game.Skirmish.Contracts;

// Derives captured gameplay component values from real prefab defaults and the
// resolved authored roster. Native actor snapshots are comparison targets only.
static class StartupActorState
{
    internal sealed record Force(string Id, string Key, byte Role, byte Category, int Supply, byte Faction, JsonElement Overlay);
    internal sealed class Inputs
    {
        internal readonly Dictionary<string, JsonElement> Templates = new(StringComparer.OrdinalIgnoreCase);
        internal readonly List<Force> Forces = new();
        internal Inputs(JsonElement root)
        {
            var registry = root.GetProperty("registry_templates");
            SpawnJson.Need(registry.GetProperty("lookup_comparer").GetString() == "OrdinalIgnoreCase_LastObservedEntryWins", "Exact native registry rule required");
            var entries = registry.GetProperty("registry_entries").EnumerateArray().ToArray();
            foreach (var entry in entries)
            {
                string key = entry.GetProperty("key").GetString();
                SpawnJson.Need(!string.IsNullOrEmpty(key), "Native registry key missing");
                Templates[key] = entry;
            }
            foreach (var entry in entries)
            {
                var chosen = Templates[entry.GetProperty("key").GetString()];
                bool selected = chosen.GetProperty("owner_query_index").GetInt32() == entry.GetProperty("owner_query_index").GetInt32() && chosen.GetProperty("buffer_index").GetInt32() == entry.GetProperty("buffer_index").GetInt32();
                SpawnJson.Need(selected == entry.GetProperty("selected_by_native_lookup").GetBoolean(), "Native registry selection assertion differs");
            }
            var setup = root.GetProperty("resolved_setup");
            SpawnJson.Need(setup.GetProperty("CatalogId").GetString() == "S002" && setup.GetProperty("Seed").GetInt32() == root.GetProperty("seed").GetInt32(), "Exact resolved S002 setup required");
            var overlays = setup.GetProperty("RoleOverlays").EnumerateArray().ToArray();
            int index = 0;
            foreach (var force in setup.GetProperty("Forces").EnumerateArray())
            {
                byte role = force.GetProperty("RoleKind").GetByte(); string roleId = force.GetProperty("RoleId").GetString();
                SpawnJson.Need(SkirmishRoleIds.TryParse(roleId, out var parsed) && (byte)parsed == role, "Authored role identity differs");
                var matches = overlays.Where(o => o.GetProperty("RoleKind").GetByte() == role).ToArray();
                SpawnJson.Need(matches.Length == 1 && matches[0].GetProperty("RoleId").GetString() == roleId, "Unique authored role overlay required");
                int quantity = Math.Max(1, force.GetProperty("Quantity").GetInt32());
                SpawnJson.Need(quantity <= 512 && Forces.Count + quantity <= 512, "Bounded full roster required");
                byte faction = force.GetProperty("FactionId").GetByte();
                string key = force.GetProperty("RuntimePrefabKey").GetString();
                SpawnJson.Need(Templates.ContainsKey(key), "Actual baked template absent for " + key);
                for (int member = 0; member < quantity; member++) Forces.Add(new Force(roleId + "." + faction + "." + index + "." + member, key, role, (byte)SkirmishRoleIds.Category(parsed), force.GetProperty("SupplyCost").GetInt32() / quantity, faction, matches[0]));
                index++;
            }
        }
    }
    static JsonElement Element(JsonNode node) => JsonSerializer.SerializeToElement(node);
    static JsonObject Clone(JsonElement value) => JsonNode.Parse(value.GetRawText()).AsObject();
    static bool Has(JsonObject values, string name) => values[name] != null;
    static int Int(JsonObject values, string name, string field) => values[name][field].GetValue<int>();
    static float Float(JsonObject values, string name, string field) => values[name][field].GetValue<float>();
    internal static object Derive(Inputs input, int ordinal, JsonElement transaction, JsonElement final, int x, int z, float worldX, float worldZ, float? groundedY)
    {
        SpawnJson.Need(ordinal < input.Forces.Count, "Unexpected allocator actor beyond authored roster");
        Force force = input.Forces[ordinal];
        SpawnJson.Need(transaction.GetProperty("stable_id").GetString() == force.Id && final.GetProperty("source_key").GetString() == force.Key && final.GetProperty("faction").GetByte() == force.Faction, "Actual actor differs from authored roster order/key/faction");
        JsonElement template = input.Templates[force.Key];
        JsonElement before = transaction.GetProperty("pre_placement_actor");
        SpawnJson.Expect(template.GetProperty("components"), before.GetProperty("components"), "instantiated baked defaults " + force.Id);
        SpawnJson.Expect(template.GetProperty("entity_enabled"), before.GetProperty("entity_enabled"), "instantiated entity enablement " + force.Id);
        SpawnJson.Expect(template.GetProperty("entity_enabled"), final.GetProperty("entity_enabled"), "derived entity enablement " + force.Id);
        var expectedEnablement = template.GetProperty("enableable_components");
        foreach (var flag in expectedEnablement.EnumerateObject())
        {
            SpawnJson.Expect(flag.Value, before.GetProperty("enableable_components").GetProperty(flag.Name), "instantiated component enablement " + force.Id + "." + flag.Name);
            SpawnJson.Expect(flag.Value, final.GetProperty("enableable_components").GetProperty(flag.Name), "derived component enablement " + force.Id + "." + flag.Name);
        }
        SpawnJson.Need(expectedEnablement.EnumerateObject().Count() == before.GetProperty("enableable_components").EnumerateObject().Count() && expectedEnablement.EnumerateObject().Count() == final.GetProperty("enableable_components").EnumerateObject().Count(), "Unmodeled actor enableable component");
        var values = Clone(template.GetProperty("components"));
        var overlay = force.Overlay;
        var state = new SkirmishActorInitializationState
        {
            IsStructure = false, SharedActor = true, CombatSuppressed = true, Category = force.Category,
            HasHealth = Has(values, "UnitHealth"), HasUnitAttack = Has(values, "UnitAttack"),
            HasUnitCombat = Has(values, "UnitCombat"), HasAirLauncher = Has(values, "AirMissileLauncherComponent"),
            HasGroundLauncher = Has(values, "GroundMissileLauncherComponent")
        };
        if (state.HasAirLauncher)
            state.AirMinRange = Float(values, "AirMissileLauncherComponent", "MinRange");
        if (state.HasGroundLauncher)
            state.GroundMinRange = Float(values, "GroundMissileLauncherComponent", "MinRange");
        var applied = new SkirmishActorOverlayValues { MaxHealth = overlay.GetProperty("MaxHealth").GetInt32(), Damage = overlay.GetProperty("Damage").GetInt32(), RangeWorld = overlay.GetProperty("RangeWorld").GetSingle(), TargetDomains = overlay.GetProperty("TargetDomains").GetByte() };
        SkirmishActorInitializationKernel.ApplyHealth(ref state, applied);
        SkirmishActorInitializationKernel.BindWeapons(ref state, applied, NativeStartupConstants.ShotIntervalSeconds);
        values["UnitHealth"] = new JsonObject { ["Current"] = state.HealthCurrent, ["Max"] = state.HealthMax };
        if (state.HasUnitAttack)
        {
            values["UnitAttack"]["Damage"] = state.AttackDamage;
            values["UnitAttack"]["Range"] = state.AttackRange;
            values["UnitAttack"]["CooldownSeconds"] = state.AttackCooldown;
        }
        if (state.HasUnitCombat)
        {
            values["UnitCombat"]["CanAttack"] = state.CanAttack;
            values["UnitCombat"]["AutoEngage"] = state.AutoEngage;
        }
        if (state.HasAirLauncher)
        {
            var v = values["AirMissileLauncherComponent"];
            v["AirTargetDamage"] = state.AirDamage; v["BaseDetectionRange"] = state.AirBaseDetectionRange;
            v["MaxDetectionRange"] = state.AirMaxDetectionRange; v["MinRange"] = state.AirMinRange;
            v["MaxSupportRangeBonus"] = state.AirSupportRangeBonus;
        }
        if (state.HasGroundLauncher)
        {
            var v = values["GroundMissileLauncherComponent"];
            v["Damage"] = state.GroundDamage; v["MaxRange"] = state.GroundMaxRange; v["MinRange"] = state.GroundMinRange;
        }
        values["UnitGrid"] = new JsonObject { ["Cell"] = new JsonObject { ["x"] = x, ["y"] = z } };
        // Native FromPosition resets scale/rotation. The geometry batch computes
        // Y before either native transform is read as an assertion.
        values.Remove("LocalTransform");
        SpawnJson.Expect(Element(values), final.GetProperty("components"), "derived actor components " + force.Id);
        var role = new JsonObject { ["Role"] = force.Role, ["Category"] = force.Category, ["SupplyCost"] = force.Supply };
        var policy = new JsonObject { ["AllowedTargets"] = state.AllowedTargets, ["Domain"] = state.Domain, ["Visible"] = state.Visible };
        var overlayComponent = new JsonObject { ["Role"] = force.Role, ["MaxHealth"] = applied.MaxHealth, ["Damage"] = applied.Damage, ["RangeWorld"] = applied.RangeWorld, ["Producer"] = overlay.GetProperty("Producer").GetByte(), ["TargetDomains"] = applied.TargetDomains, ["Applied"] = 1 };
        SpawnJson.Expect(Element(role), final.GetProperty("role"), "derived role " + force.Id);
        SpawnJson.Expect(Element(policy), final.GetProperty("target_policy"), "derived combat target policy " + force.Id);
        SpawnJson.Expect(Element(overlayComponent), final.GetProperty("overlay"), "derived overlay " + force.Id);
        var transform = final.GetProperty("components").GetProperty("LocalTransform");
        var placedTransform = transaction.GetProperty("placement_result").GetProperty("actor").GetProperty("components").GetProperty("LocalTransform");
        if (groundedY.HasValue)
        {
            float nativeY = transform.GetProperty("Position").GetProperty("y").GetSingle();
            SpawnJson.Need(BitConverter.SingleToInt32Bits(groundedY.Value) == BitConverter.SingleToInt32Bits(nativeY),
                "Derived grounded Y differs from native assertion at " + force.Id + " derived=" + groundedY.Value.ToString("R") +
                " native=" + nativeY.ToString("R") + " derivedBits=" + BitConverter.SingleToInt32Bits(groundedY.Value).ToString("x8") +
                " nativeBits=" + BitConverter.SingleToInt32Bits(nativeY).ToString("x8"));
            float placedY = placedTransform.GetProperty("Position").GetProperty("y").GetSingle();
            SpawnJson.Need(BitConverter.SingleToInt32Bits(groundedY.Value) == BitConverter.SingleToInt32Bits(placedY),
                "Derived grounded Y differs from immediate-placement assertion at " + force.Id);
        }
        SpawnJson.Need(transform.GetProperty("Position").GetProperty("x").GetSingle() == worldX && transform.GetProperty("Position").GetProperty("z").GetSingle() == worldZ && transform.GetProperty("Scale").GetSingle() == 1, "Derived transform X/Z/scale differs");
        var rotation = transform.GetProperty("Rotation").GetProperty("value");
        SpawnJson.Need(rotation.GetProperty("x").GetSingle() == 0 && rotation.GetProperty("y").GetSingle() == 0 && rotation.GetProperty("z").GetSingle() == 0 && rotation.GetProperty("w").GetSingle() == 1, "Derived transform rotation differs");
        SpawnJson.Need(placedTransform.GetProperty("Position").GetProperty("x").GetSingle() == worldX && placedTransform.GetProperty("Position").GetProperty("z").GetSingle() == worldZ && placedTransform.GetProperty("Scale").GetSingle() == 1, "Derived immediate-placement transform X/Z/scale differs");
        var placedRotation = placedTransform.GetProperty("Rotation").GetProperty("value");
        SpawnJson.Need(placedRotation.GetProperty("x").GetSingle() == 0 && placedRotation.GetProperty("y").GetSingle() == 0 && placedRotation.GetProperty("z").GetSingle() == 0 && placedRotation.GetProperty("w").GetSingle() == 1, "Derived immediate-placement transform rotation differs");
        var types = final.GetProperty("component_types").EnumerateArray().Select(x => x.GetString()).ToHashSet();
        SpawnJson.Need(types.Contains("Game.Components.SkirmishSharedActorTag") && !types.Contains("Game.Components.CampaignMissionCombatSuppressedTag"), "Shared actor suppression release differs");
        if (groundedY.HasValue)
            values["LocalTransform"] = new JsonObject
            {
                ["Position"] = new JsonObject { ["x"] = (double)worldX, ["y"] = (double)groundedY.Value, ["z"] = (double)worldZ },
                ["Scale"] = 1, ["Rotation"] = new JsonObject { ["value"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["z"] = 0, ["w"] = 1 } }
            };
        return new { id = force.Id, source_key = force.Key, faction = force.Faction, entity_enabled = template.GetProperty("entity_enabled").GetBoolean(), enableable_components = expectedEnablement.Clone(), components = values, role, target_policy = policy, overlay = overlayComponent, transform = new { x = worldX, z = worldZ, scale = 1, rotation = new[] { 0, 0, 0, 1 }, grounded_y = groundedY }, component_scope = "Captured serialized gameplay defaults plus authored overlay fields; unexported ECS values remain pending", full_actor_state = false };
    }
}
