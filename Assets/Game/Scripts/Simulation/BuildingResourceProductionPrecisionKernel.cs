namespace Game.Simulation
{
    // Explicit precision boundaries of the existing managed production path.
    // Inputs/local division are float32; compound expression arithmetic is widened.
    public static class BuildingResourceProductionPrecisionKernel
    {
        public static float AccumulateOil(float storedOil, float barrelsPerSecond, float deltaTime, float oilCapacity)
        {
            double updated = (double)storedOil + (double)barrelsPerSecond * deltaTime;
            return (float)((double)oilCapacity < updated ? oilCapacity : updated);
        }

        public static float DesiredFuel(float fuelBarrelsPerDay, float secondsPerDay, float deltaTime)
        {
            return (float)(((double)fuelBarrelsPerDay / secondsPerDay) * deltaTime);
        }
        public static float SubtractConversionOil(float storedOil, float fuelProduced, float oilBarrelsPerFuelBarrel)
        {
            return (float)((double)storedOil - (double)fuelProduced * oilBarrelsPerFuelBarrel);
        }
    }
}
