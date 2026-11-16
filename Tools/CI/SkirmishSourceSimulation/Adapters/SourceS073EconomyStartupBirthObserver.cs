using System;
using System.Collections.Generic;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Optional read-only wrapper for observing the original S073 economy startup
// owner. This file is deliberately not wired into the shared runner or manifest.
// Call Observe around the actual owner invocation when a later task authorizes
// wiring; the callback itself remains the original owner and is invoked once.
internal static class SourceS073EconomyStartupBirthObserver
{
    const string OptIn = "WARLINE_S073_ECONOMY_STARTUP_BIRTH_OBSERVER";

    sealed class BankSnapshot
    {
        internal Entity Entity;
        internal byte FactionId;
        internal int Money;
        internal byte MaterialsOnlyConstruction;
        internal float Oil;
        internal float Fuel;
        internal float OilIncomeRate;
        internal float FuelIncomeRate;
        internal float LastSellTime;
        internal float LastLogTime;
        internal bool HasPolicy;
        internal byte PolicyEnabled;
        internal float PolicyIncomeMultiplier;
        internal int PolicyOilSellPrice;
        internal int PolicyFuelSellPrice;
        internal float PolicySellIntervalSeconds;
        internal bool HasMaterials;
        internal byte MaterialsFactionId;
        internal int MaterialsCurrent;
        internal int MaterialsCapacity;
        internal uint MaterialsVersion;
        internal bool HasAttemptOwner;
        internal string AttemptSessionId;
        internal string AttemptStableObjectId;
    }

    static string EntityId(Entity entity) => $"{entity.Index}:{entity.Version}";

    static Dictionary<string, BankSnapshot> Capture(EntityManager em)
    {
        var result = new Dictionary<string, BankSnapshot>();
        using var query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            FactionEconomy economy = em.GetComponentData<FactionEconomy>(entity);
            var row = new BankSnapshot
            {
                Entity = entity,
                FactionId = economy.FactionId,
                Money = economy.Money,
                MaterialsOnlyConstruction = economy.MaterialsOnlyConstruction,
                Oil = economy.Oil,
                Fuel = economy.Fuel,
                OilIncomeRate = economy.OilIncomeRate,
                FuelIncomeRate = economy.FuelIncomeRate,
                LastSellTime = economy.LastSellTime,
                LastLogTime = economy.LastLogTime,
                HasPolicy = em.HasComponent<FactionEconomyPolicy>(entity),
                HasMaterials = em.HasComponent<FactionTacticalMaterialsComponent>(entity),
                HasAttemptOwner = em.HasComponent<SkirmishAttemptOwnedComponent>(entity)
            };
            if (row.HasPolicy)
            {
                FactionEconomyPolicy policy = em.GetComponentData<FactionEconomyPolicy>(entity);
                row.PolicyEnabled = policy.Enabled;
                row.PolicyIncomeMultiplier = policy.IncomeMultiplier;
                row.PolicyOilSellPrice = policy.OilSellPrice;
                row.PolicyFuelSellPrice = policy.FuelSellPrice;
                row.PolicySellIntervalSeconds = policy.SellIntervalSeconds;
            }
            if (row.HasMaterials)
            {
                FactionTacticalMaterialsComponent materials =
                    em.GetComponentData<FactionTacticalMaterialsComponent>(entity);
                row.MaterialsFactionId = materials.FactionId;
                row.MaterialsCurrent = materials.Current;
                row.MaterialsCapacity = materials.Capacity;
                row.MaterialsVersion = materials.Version;
            }
            if (row.HasAttemptOwner)
            {
                SkirmishAttemptOwnedComponent owner =
                    em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                row.AttemptSessionId = owner.SessionId.ToString();
                row.AttemptStableObjectId = owner.StableObjectId.ToString();
            }

            string id = EntityId(entity);
            if (result.ContainsKey(id))
                throw new InvalidOperationException("Duplicate live entity identity in economy snapshot.");
            result.Add(id, row);
        }
        return result;
    }

    static JObject CaptureMatchIdentity(EntityManager em)
    {
        using var matches = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
        if (matches.CalculateEntityCount() != 1)
            throw new InvalidOperationException("S073 economy observation requires exactly one same-world match identity.");
        SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
        return new JObject
        {
            ["session_id"] = match.SessionId.ToString(),
            ["scenario_index"] = match.ScenarioIndex,
            ["seed"] = match.Seed,
            ["phase"] = match.Phase.ToString()
        };
    }

    static JObject ToJson(BankSnapshot row, string birth)
    {
        var result = new JObject
        {
            ["entity_index"] = row.Entity.Index,
            ["entity_version"] = row.Entity.Version,
            ["faction_id"] = row.FactionId,
            ["birth"] = birth,
            ["economy"] = new JObject
            {
                ["money"] = row.Money,
                ["materials_only_construction"] = row.MaterialsOnlyConstruction,
                ["oil"] = row.Oil,
                ["fuel"] = row.Fuel,
                ["oil_income_rate"] = row.OilIncomeRate,
                ["fuel_income_rate"] = row.FuelIncomeRate,
                ["last_sell_time"] = row.LastSellTime,
                ["last_log_time"] = row.LastLogTime
            },
            ["has_policy"] = row.HasPolicy,
            ["has_tactical_materials"] = row.HasMaterials,
            ["has_attempt_owner"] = row.HasAttemptOwner
        };
        if (row.HasPolicy)
            result["policy"] = new JObject
            {
                ["enabled"] = row.PolicyEnabled,
                ["income_multiplier"] = row.PolicyIncomeMultiplier,
                ["oil_sell_price"] = row.PolicyOilSellPrice,
                ["fuel_sell_price"] = row.PolicyFuelSellPrice,
                ["sell_interval_seconds"] = row.PolicySellIntervalSeconds
            };
        if (row.HasMaterials)
            result["tactical_materials"] = new JObject
            {
                ["faction_id"] = row.MaterialsFactionId,
                ["current"] = row.MaterialsCurrent,
                ["capacity"] = row.MaterialsCapacity,
                ["version"] = row.MaterialsVersion
            };
        if (row.HasAttemptOwner)
            result["attempt_owner"] = new JObject
            {
                ["session_id"] = row.AttemptSessionId,
                ["stable_object_id"] = row.AttemptStableObjectId
            };
        return result;
    }

    /// <summary>
    /// Invokes the supplied original startup owner exactly once. When opted in,
    /// returns a same-EntityManager before/after read-only census. When opted
    /// out, invokes the owner and returns null. Exceptions from the owner pass
    /// through unchanged and no ECS data is written by this observer.
    /// </summary>
    internal static JObject Observe(EntityManager em, string owner, Action originalStartup)
    {
        if (originalStartup == null) throw new ArgumentNullException(nameof(originalStartup));
        bool optedIn = string.Equals(Environment.GetEnvironmentVariable(OptIn), "1", StringComparison.Ordinal);
        if (!optedIn)
        {
            originalStartup();
            return null;
        }
        if (em.World == null || !em.World.IsCreated)
            throw new InvalidOperationException("S073 economy birth observation requires a live source world.");

        ulong worldSequenceBefore = em.WorldUnmanaged.SequenceNumber;
        JObject matchBefore = CaptureMatchIdentity(em);
        Dictionary<string, BankSnapshot> before = Capture(em);
        originalStartup();
        Dictionary<string, BankSnapshot> after = Capture(em);
        ulong worldSequenceAfter = em.WorldUnmanaged.SequenceNumber;
        JObject matchAfter = CaptureMatchIdentity(em);
        var rows = new JArray();
        int created = 0, existing = 0;
        foreach (KeyValuePair<string, BankSnapshot> pair in after)
        {
            bool existed = before.TryGetValue(pair.Key, out BankSnapshot prior);
            string birth = existed ? "ExistingEntity" : "CreatedDuringOriginalOwnerInvocation";
            if (existed) existing++; else created++;
            JObject row = ToJson(pair.Value, birth);
            row["entity_identity"] = pair.Key;
            row["faction_id_before"] = existed ? new JValue(prior.FactionId) : JValue.CreateNull();
            row["economy_before"] = existed ? ToJson(prior, "ExistingEntity")["economy"] : null;
            row["faction_id_changed"] = existed && prior.FactionId != pair.Value.FactionId;
            rows.Add(row);
        }

        var removed = new JArray();
        foreach (KeyValuePair<string, BankSnapshot> pair in before)
            if (!after.ContainsKey(pair.Key))
                removed.Add(ToJson(pair.Value, "RemovedDuringOriginalOwnerInvocation"));

        return new JObject
        {
            ["kind"] = "S073EconomyStartupBirthObservation",
            ["schema_version"] = 1,
            ["owner_invocation"] = owner ?? string.Empty,
            ["opt_in_environment_variable"] = OptIn,
            ["scope"] = "SameLiveWorldEntityManagerImmediatelyBeforeAndAfterSuppliedOriginalOwnerInvocation",
            ["source_host_label"] = "RetainedS073LegacySourceHost",
            ["world_sequence_number"] = worldSequenceAfter,
            ["world_sequence_number_before"] = worldSequenceBefore,
            ["world_sequence_number_after"] = worldSequenceAfter,
            ["session_id"] = matchAfter["session_id"].DeepClone(),
            ["scenario_index"] = matchAfter["scenario_index"].DeepClone(),
            ["seed"] = matchAfter["seed"].DeepClone(),
            ["phase"] = matchAfter["phase"].DeepClone(),
            ["match_identity_before"] = matchBefore,
            ["match_identity_after"] = matchAfter,
            ["observer_mutated_ecs"] = false,
            ["original_owner_invoked_once"] = true,
            ["economy_bank_count_before"] = before.Count,
            ["economy_bank_count_after"] = after.Count,
            ["existing_bank_count_after"] = existing,
            ["created_bank_count"] = created,
            ["removed_bank_count"] = removed.Count,
            ["banks_after"] = rows,
            ["banks_removed"] = removed
        };
    }
}
