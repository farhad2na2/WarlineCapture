using Game.Components;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishCapacityLedger
    {
        public static bool TryReserve(
            ref SkirmishCapacitySnapshot capacity,
            SkirmishPopulationCategory category,
            int members,
            int supply)
        {
            if (capacity.SupplyLive + capacity.SupplyReserved + supply > capacity.SupplyCap)
                return false;
            if (category == SkirmishPopulationCategory.Infantry &&
                capacity.InfantryLive + capacity.InfantryReserved + members > capacity.InfantryCap)
                return false;
            if (category == SkirmishPopulationCategory.Ground &&
                capacity.GroundLive + capacity.GroundReserved + members > capacity.GroundCap)
                return false;
            if (category == SkirmishPopulationCategory.Air &&
                capacity.AirLive + capacity.AirReserved + members > capacity.AirCap)
                return false;

            capacity.SupplyReserved += supply;
            AddReserved(ref capacity, category, members);
            return true;
        }

        public static void PromoteLive(
            ref SkirmishCapacitySnapshot capacity,
            SkirmishPopulationCategory category,
            int members,
            int supply)
        {
            capacity.SupplyReserved -= supply;
            capacity.SupplyLive += supply;
            AddReserved(ref capacity, category, -members);
            AddLive(ref capacity, category, members);
        }

        public static bool TryReleaseDeath(
            ref SkirmishCapacitySnapshot capacity,
            SkirmishPopulationCategory category,
            int supply)
        {
            if (category == SkirmishPopulationCategory.Infantry && capacity.InfantryLive <= 0)
                return false;
            if (category == SkirmishPopulationCategory.Ground && capacity.GroundLive <= 0)
                return false;
            if (category == SkirmishPopulationCategory.Air && capacity.AirLive <= 0)
                return false;

            AddLive(ref capacity, category, -1);
            if (capacity.SupplyLive >= supply)
                capacity.SupplyLive -= supply;
            else
                capacity.SupplyLive = 0;
            return true;
        }

        public static void SeedLive(
            ref SkirmishCapacitySnapshot capacity,
            int infantry,
            int ground,
            int air,
            int supply)
        {
            capacity.InfantryLive = infantry;
            capacity.GroundLive = ground;
            capacity.AirLive = air;
            capacity.SupplyLive = supply;
        }

        public static SkirmishCapacitySnapshot ToSnapshot(SkirmishCapacityComponent capacity) =>
            new SkirmishCapacitySnapshot
            {
                InfantryCap = capacity.InfantryCap,
                GroundCap = capacity.GroundCap,
                AirCap = capacity.AirCap,
                SupplyCap = capacity.SupplyCap,
                InfantryLive = capacity.InfantryLive,
                GroundLive = capacity.GroundLive,
                AirLive = capacity.AirLive,
                SupplyLive = capacity.SupplyLive,
                InfantryReserved = capacity.InfantryReserved,
                GroundReserved = capacity.GroundReserved,
                AirReserved = capacity.AirReserved,
                SupplyReserved = capacity.SupplyReserved
            };

        public static SkirmishCapacityComponent FromSnapshot(
            SkirmishCapacityComponent capacity,
            SkirmishCapacitySnapshot snapshot)
        {
            capacity.InfantryLive = snapshot.InfantryLive;
            capacity.GroundLive = snapshot.GroundLive;
            capacity.AirLive = snapshot.AirLive;
            capacity.SupplyLive = snapshot.SupplyLive;
            capacity.InfantryReserved = snapshot.InfantryReserved;
            capacity.GroundReserved = snapshot.GroundReserved;
            capacity.AirReserved = snapshot.AirReserved;
            capacity.SupplyReserved = snapshot.SupplyReserved;
            return capacity;
        }

        private static void AddReserved(ref SkirmishCapacitySnapshot capacity, SkirmishPopulationCategory category, int delta)
        {
            if (category == SkirmishPopulationCategory.Infantry)
                capacity.InfantryReserved += delta;
            else if (category == SkirmishPopulationCategory.Ground)
                capacity.GroundReserved += delta;
            else if (category == SkirmishPopulationCategory.Air)
                capacity.AirReserved += delta;
        }

        private static void AddLive(ref SkirmishCapacitySnapshot capacity, SkirmishPopulationCategory category, int delta)
        {
            if (category == SkirmishPopulationCategory.Infantry)
                capacity.InfantryLive += delta;
            else if (category == SkirmishPopulationCategory.Ground)
                capacity.GroundLive += delta;
            else if (category == SkirmishPopulationCategory.Air)
                capacity.AirLive += delta;
        }
    }

    public struct SkirmishCapacitySnapshot
    {
        public int InfantryCap;
        public int GroundCap;
        public int AirCap;
        public int SupplyCap;
        public int InfantryLive;
        public int GroundLive;
        public int AirLive;
        public int SupplyLive;
        public int InfantryReserved;
        public int GroundReserved;
        public int AirReserved;
        public int SupplyReserved;
    }
}
