using Unity.Mathematics;

namespace Game.Runtime
{
    public readonly struct BuildingResourceProductionSystemHelper
    {
        public readonly struct State
        {
            public readonly int OilStorageCapacity;
            public readonly int FuelStorageCapacity;
            public readonly float OilBarrelsPerDay;
            public readonly float FuelBarrelsPerDay;
            public readonly float StoredOilBarrels;
            public readonly float StoredFuelBarrels;

            public State(
                int oilStorageCapacity,
                int fuelStorageCapacity,
                float oilBarrelsPerDay,
                float fuelBarrelsPerDay,
                float storedOilBarrels,
                float storedFuelBarrels)
            {
                OilStorageCapacity = oilStorageCapacity;
                FuelStorageCapacity = fuelStorageCapacity;
                OilBarrelsPerDay = oilBarrelsPerDay;
                FuelBarrelsPerDay = fuelBarrelsPerDay;
                StoredOilBarrels = storedOilBarrels;
                StoredFuelBarrels = storedFuelBarrels;
            }
        }

        public readonly struct Result
        {
            public readonly float StoredOilBarrels;
            public readonly float StoredFuelBarrels;
            public readonly float OilExtractedBarrels;
            public readonly float FuelProducedBarrels;

            public Result(
                float storedOilBarrels,
                float storedFuelBarrels,
                float oilExtractedBarrels,
                float fuelProducedBarrels)
            {
                StoredOilBarrels = storedOilBarrels;
                StoredFuelBarrels = storedFuelBarrels;
                OilExtractedBarrels = oilExtractedBarrels;
                FuelProducedBarrels = fuelProducedBarrels;
            }
        }

        public static Result Tick(
            State state,
            float secondsPerDay,
            float deltaTime,
            float oilBarrelsPerFuelBarrel)
        {
            secondsPerDay = math.max(1f, secondsPerDay);
            deltaTime = math.max(0f, deltaTime);
            oilBarrelsPerFuelBarrel = math.max(0.001f, oilBarrelsPerFuelBarrel);

            int oilCapacity = math.max(0, state.OilStorageCapacity);
            int fuelCapacity = math.max(0, state.FuelStorageCapacity);
            float oilBarrelsPerDay = math.max(0f, state.OilBarrelsPerDay);
            float fuelBarrelsPerDay = math.max(0f, state.FuelBarrelsPerDay);
            float storedOil = state.StoredOilBarrels;
            float storedFuel = state.StoredFuelBarrels;
            float oilExtracted = 0f;
            float fuelProduced = 0f;

            if (oilCapacity > 0 && oilBarrelsPerDay > 0f)
            {
                if (storedOil >= oilCapacity)
                {
                    storedOil = oilCapacity;
                }
                else
                {
                    float previousOil = storedOil;
                    float barrelsPerSecond = oilBarrelsPerDay / secondsPerDay;
                    storedOil = math.min(oilCapacity, storedOil + barrelsPerSecond * deltaTime);
                    oilExtracted = storedOil - previousOil;
                }
            }

            if (fuelBarrelsPerDay <= 0f)
                return new Result(storedOil, storedFuel, oilExtracted, fuelProduced);

            float maxFuelFromOil = storedOil / oilBarrelsPerFuelBarrel;
            if (maxFuelFromOil <= 0f)
                return new Result(storedOil, storedFuel, oilExtracted, fuelProduced);

            float desiredFuel = (fuelBarrelsPerDay / secondsPerDay) * deltaTime;
            fuelProduced = math.min(desiredFuel, maxFuelFromOil);
            if (fuelCapacity > 0)
                fuelProduced = math.min(fuelProduced, math.max(0f, fuelCapacity - storedFuel));

            if (fuelProduced <= 0f)
                return new Result(storedOil, storedFuel, oilExtracted, 0f);

            storedOil = math.max(0f, storedOil - fuelProduced * oilBarrelsPerFuelBarrel);
            if (fuelCapacity > 0)
                storedFuel = math.min(fuelCapacity, storedFuel + fuelProduced);

            return new Result(storedOil, storedFuel, oilExtracted, fuelProduced);
        }
    }
}
