namespace Game.Simulation
{
    public enum BuildingReadModelPublishDecision : byte
    {
        Skip,
        Initial,
        Forced,
        Dynamic
    }

    // Decides only when/which read-model phase runs. Owners commit readiness,
    // signatures and phase advancement after their original callbacks succeed.
    public static class BuildingReadModelPublishCadenceKernel
    {
        public static BuildingReadModelPublishDecision Begin(
            float now,
            float publishInterval,
            bool configured,
            ref bool forcePending,
            ref float nextPublishAt)
        {
            bool forcePublish = forcePending;
            if (!forcePublish && configured && now < nextPublishAt)
                return BuildingReadModelPublishDecision.Skip;

            forcePending = false;
            nextPublishAt = now + publishInterval;
            if (!configured)
                return BuildingReadModelPublishDecision.Initial;
            return forcePublish
                ? BuildingReadModelPublishDecision.Forced
                : BuildingReadModelPublishDecision.Dynamic;
        }

        public static byte NextPhase(byte current)
        {
            switch (current)
            {
                case 0: return 1;
                case 1: return 2;
                case 2: return 3;
                default: return 0;
            }
        }
    }
}
