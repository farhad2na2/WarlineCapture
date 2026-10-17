using System.Collections.Generic;
using Game.Skirmish.Contracts;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Assign whole native groups. Never split a squad or reserve empty cap slots.
    public static class SkirmishEnemyGroupAllocation
    {
        public sealed class Package
        {
            public readonly List<SkirmishEnemyPerceptionBuilder.Group> Reserve = new();
            public readonly List<SkirmishEnemyPerceptionBuilder.Group> Assault = new();
            public readonly List<SkirmishEnemyPerceptionBuilder.Group> Escort = new();
            public SkirmishEnemyPerceptionBuilder.Group Scout;
            public int ReserveSupply;
            public bool Viable => Assault.Count > 0 && Escort.Count > 0;
        }

        public static Package Allocate(SkirmishEnemyPerceptionBuilder.Snapshot facts, int reservePercent)
        {
            var package = new Package();
            var remaining = new List<SkirmishEnemyPerceptionBuilder.Group>(facts.Groups);
            // Keep one damage package and one complementary screen before taking
            // optional reserves/scouts. Stable id is the final deterministic tie.
            remaining.Sort((a,b) => a.Id.CompareTo(b.Id));
            var structure = remaining.Find(g => g.StructureCapable && Healthy(g));
            if (structure != null) { package.Assault.Add(structure); remaining.Remove(structure); }
            var screen = remaining.Find(g => g.MobileCapable && Healthy(g) && !g.StructureCapable)
                ?? remaining.Find(g => g.MobileCapable && Healthy(g));
            if (screen != null) { package.Escort.Add(screen); remaining.Remove(screen); }
            int target = SkirmishEnemyDecisionRules.ReserveTarget(facts.CombatSupply, reservePercent);
            remaining.Sort((a,b) =>
            {
                int distance = math.distancesq(a.Position.xz, facts.Home.xz).CompareTo(
                    math.distancesq(b.Position.xz, facts.Home.xz));
                return distance != 0 ? distance : a.Id.CompareTo(b.Id);
            });
            foreach (var group in remaining)
                if (package.ReserveSupply < target)
                { package.Reserve.Add(group); package.ReserveSupply += group.Supply; }
            remaining.RemoveAll(g => package.Reserve.Contains(g));
            // A scout is optional and never consumes the sole assault/escort.
            package.Scout = remaining.Find(g => g.Role == SkirmishRoleKind.Rifle && Healthy(g));
            if (package.Scout != null) remaining.Remove(package.Scout);
            foreach (var group in remaining)
                if (group.StructureCapable && Healthy(group)) package.Assault.Add(group);
                else if (group.MobileCapable && Healthy(group)) package.Escort.Add(group);
                else { package.Reserve.Add(group); package.ReserveSupply += group.Supply; }
            // A lone structure squad must not be repeatedly fed into the enemy.
            // Insufficient armies defend/regroup while paid replacements arrive.
            if (!package.Viable)
            {
                package.Reserve.AddRange(package.Assault); package.Assault.Clear();
                package.Reserve.AddRange(package.Escort); package.Escort.Clear();
                if (package.Scout != null) { package.Reserve.Add(package.Scout); package.Scout = null; }
                package.ReserveSupply = facts.CombatSupply;
            }
            return package;
        }
        public static bool Healthy(SkirmishEnemyPerceptionBuilder.Group group)
            => group.MaximumHealth <= 0 || group.Health * 100 >= group.MaximumHealth * 35;
    }
}
