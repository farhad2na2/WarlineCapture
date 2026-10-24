using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Simulation
{
    public readonly struct SkirmishBootstrapForce
    {
        public readonly string RoleId;
        public readonly byte Faction;
        public readonly SkirmishRoleKind Role;
        public readonly int Quantity;
        public readonly int SupplyCost;
        public readonly string SourceKey;

        public SkirmishBootstrapForce(string roleId, byte faction, SkirmishRoleKind role, int quantity, int supplyCost, string sourceKey)
        {
            RoleId = roleId;
            Faction = faction;
            Role = role;
            Quantity = quantity;
            SupplyCost = supplyCost;
            SourceKey = sourceKey;
        }
    }

    public readonly struct SkirmishBootstrapMember
    {
        public readonly int ForceIndex;
        public readonly int MemberIndex;
        public readonly int PerMemberSupply;
        public readonly string SourceKey;
        public readonly string StableId;
        public readonly byte Faction;
        public readonly SkirmishRoleKind Role;
        public readonly SkirmishPopulationCategory Category;

        internal SkirmishBootstrapMember(int forceIndex, int memberIndex, int perMemberSupply,
            string sourceKey, string stableId, byte faction, SkirmishRoleKind role)
        {
            ForceIndex = forceIndex;
            MemberIndex = memberIndex;
            PerMemberSupply = perMemberSupply;
            SourceKey = sourceKey;
            StableId = stableId;
            Faction = faction;
            Role = role;
            Category = SkirmishRoleIds.Category(role);
        }
    }

    public struct SkirmishBootstrapPopulation
    {
        public int Infantry;
        public int Ground;
        public int Air;
        public int Supply;
    }

    // Shared mission-opening owner. Asset creation/placement and physical group writes
    // remain adapters. Only actual creation + binding feedback advances this state.
    public sealed class SkirmishMissionBootstrapKernel
    {
        public const int MaximumForceEntries = 4096;
        public const int MaximumOpeningActors = 1048576;
        private readonly SkirmishBootstrapForce[] forces;
        private int forceIndex;
        private int memberIndex;
        private bool pending;
        private bool aborted;
        private SkirmishBootstrapMember current;
        private uint openGroupId;
        private byte openFaction;
        private SkirmishRoleKind openRole;
        private int openFill;
        private SkirmishBootstrapPopulation player;
        private SkirmishBootstrapPopulation enemy;

        public SkirmishMissionBootstrapKernel(IReadOnlyList<SkirmishBootstrapForce> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Count > MaximumForceEntries) throw new ArgumentOutOfRangeException(nameof(source), "Finite bootstrap work bound");
            forces = new SkirmishBootstrapForce[source.Count];
            long actors = 0;
            for (int index = 0; index < source.Count; index++)
            {
                forces[index] = source[index];
                actors += Quantity(source[index]);
                if (actors > MaximumOpeningActors) throw new ArgumentOutOfRangeException(nameof(source), "Finite bootstrap actor work bound");
            }
        }

        private static int Quantity(SkirmishBootstrapForce force) => force.Quantity < 1 ? 1 : force.Quantity;
        public static string StableActorId(string roleId, byte faction, int forceIndex, int memberIndex)
            => roleId + "." + faction + "." + forceIndex + "." + memberIndex;
        public bool StartingSetBound { get; private set; } = true;
        public int CompletedMembers { get; private set; }
        public bool Completed => !aborted && !pending && forceIndex == forces.Length;
        public bool Aborted => aborted;
        public uint OpenGroupId => openGroupId;
        public SkirmishBootstrapPopulation Player => player;
        public SkirmishBootstrapPopulation Enemy => enemy;

        public bool TryBeginMember(out SkirmishBootstrapMember member)
        {
            if (aborted) throw new InvalidOperationException("Aborted bootstrap cannot continue");
            if (pending) throw new InvalidOperationException("Actual actor creation/binding feedback required before next member");
            member = default;
            if (forceIndex == forces.Length) return false;
            var force = forces[forceIndex];
            int quantity = Quantity(force);
            string sourceKey = force.SourceKey ?? string.Empty;
            if (string.IsNullOrEmpty(sourceKey)) StartingSetBound = false;
            current = new SkirmishBootstrapMember(forceIndex, memberIndex, force.SupplyCost / quantity,
                sourceKey, StableActorId(force.RoleId, force.Faction, forceIndex, memberIndex), force.Faction, force.Role);
            pending = true;
            member = current;
            return true;
        }

        public bool RequiresNewGroup
        {
            get
            {
                RequirePending();
                bool infantry = current.Category == SkirmishPopulationCategory.Infantry;
                return openGroupId == 0 || current.Faction != openFaction || current.Role != openRole ||
                    !infantry || openFill >= SkirmishRoleIds.InfantrySquadMembers;
            }
        }

        public void RecordCreatedAndBound(uint actualGroupId)
        {
            RequirePending();
            bool newGroup = RequiresNewGroup;
            if (!newGroup && actualGroupId != openGroupId)
                throw new InvalidOperationException("Existing source group binding changed");
            if (newGroup)
            {
                openGroupId = actualGroupId;
                openFaction = current.Faction;
                openRole = current.Role;
                openFill = 0;
            }
            openFill++;
            if (current.Faction == 1) AddPopulation(ref player, current);
            else if (current.Faction == 2) AddPopulation(ref enemy, current);
            CompletedMembers++;
            memberIndex++;
            if (memberIndex == Quantity(forces[forceIndex])) { forceIndex++; memberIndex = 0; }
            pending = false;
        }

        private static void AddPopulation(ref SkirmishBootstrapPopulation population, SkirmishBootstrapMember member)
        {
            if (member.Category == SkirmishPopulationCategory.Infantry) population.Infantry++;
            else if (member.Category == SkirmishPopulationCategory.Ground) population.Ground++;
            else if (member.Category == SkirmishPopulationCategory.Air) population.Air++;
            else return;
            population.Supply += member.PerMemberSupply;
        }

        private void RequirePending()
        {
            if (aborted || !pending) throw new InvalidOperationException("Current live bootstrap member required");
        }
        public void Abort() { aborted = true; }
    }
}
