using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    // Value projection of actual compiled Forces; ForceIndex is its ORIGINAL
    // array index, never an index into a filtered list. No native actor is invented.
    public struct SkirmishOriginalForceDeclaration
    {
        public int ForceIndex, Quantity;
        public byte Faction;
        public string RoleId, NativePrefabKey;
        public SkirmishRoleKind Role;
    }
    [Serializable]
    public struct SkirmishOriginalObjectiveBinding
    {
        public string StableActorId;
        public SkirmishObjectiveRoleKind ObjectiveRole;
        public int ForceIndex, MemberIndex;
    }
    public static class SkirmishOriginalObjectiveBindingPlan
    {
        // Matches the existing CreateForceMember stable-ID formula exactly, using
        // invariant decimal text. The first twelve members of actual rifle force
        // entries are the first three authored squads; new recruits never enter it.
        public static SkirmishOriginalObjectiveBinding[] Build(SkirmishObjectiveKind kind,
            int designatedRifles, int objectiveTrucks, IReadOnlyList<SkirmishOriginalForceDeclaration> forces)
        {
            if(kind!=SkirmishObjectiveKind.Breakthrough&&kind!=SkirmishObjectiveKind.ConvoyEscort)
                throw new ArgumentException("Original unit bindings apply only to BT/CE.");
            bool bt=kind==SkirmishObjectiveKind.Breakthrough;
            int required=bt?12:3;
            if(forces==null||(bt?(designatedRifles!=12||objectiveTrucks!=0):(designatedRifles!=0||objectiveTrucks!=3)))
                throw new ArgumentException("Canonical original objective counts required.");
            var selected=new List<SkirmishOriginalObjectiveBinding>();
            var indices=new HashSet<int>();int previous=-1,totalObjectiveActors=0;
            foreach(var force in forces)
            {
                if(force.ForceIndex<0||force.ForceIndex<=previous||!indices.Add(force.ForceIndex)||force.Quantity<0||
                    string.IsNullOrWhiteSpace(force.RoleId)||force.Faction<1||force.Faction>2)
                    throw new ArgumentException("Original full-array force order/identity invalid.");
                previous=force.ForceIndex;
                if(!bt&&force.Role==SkirmishRoleKind.ConvoyLogic)
                {
                    if(force.Faction!=1)throw new ArgumentException("Only three player objective trucks are authorized.");
                    totalObjectiveActors=checked(totalObjectiveActors+force.Quantity);
                }
                if(force.Faction!=1||force.Role!=(bt?SkirmishRoleKind.Rifle:SkirmishRoleKind.ConvoyLogic))continue;
                if(force.RoleId!=(bt?SkirmishRoleIds.Rifle:SkirmishRoleIds.ConvoyLogic)||string.IsNullOrWhiteSpace(force.NativePrefabKey))
                    throw new ArgumentException("Actual canonical role/native prefab binding missing.");
                for(int member=0;member<force.Quantity&&selected.Count<required;member++)
                    selected.Add(new SkirmishOriginalObjectiveBinding{ForceIndex=force.ForceIndex,MemberIndex=member,
                        StableActorId=StableId(force.RoleId,force.Faction,force.ForceIndex,member),
                        ObjectiveRole=bt?SkirmishObjectiveRoleKind.BreakthroughDesignated:SkirmishObjectiveRoleKind.ConvoyTruck});
            }
            if(selected.Count!=required||(!bt&&totalObjectiveActors!=3))throw new ArgumentException("Compiled exact original rifle/truck actors missing; no substitute is allowed.");
            return selected.ToArray();
        }
        public static string StableId(string roleId,byte faction,int forceIndex,int memberIndex) => roleId+"."+
            faction.ToString(CultureInfo.InvariantCulture)+"."+forceIndex.ToString(CultureInfo.InvariantCulture)+"."+memberIndex.ToString(CultureInfo.InvariantCulture);
    }
}
