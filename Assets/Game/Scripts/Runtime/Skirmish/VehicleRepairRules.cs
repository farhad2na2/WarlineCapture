using System;

namespace Game.Runtime
{
    public struct VehicleRepairPlan
    {
        public long FractionNumerator,FractionDenominator,DurationTicks;
        public int Materials;
    }
    // Shared exact integer accounting. No entity, map, money or health mutation.
    public static class VehicleRepairRules
    {
        public static bool TryQuote(int current,int maximum,out VehicleRepairPlan plan)
        {
            plan=default;if(maximum<=0||current<=0||current>=maximum)return false;
            long numerator=Math.Min((long)(maximum-current)*4,maximum),denominator=(long)maximum*4;
            plan=new VehicleRepairPlan{FractionNumerator=numerator,FractionDenominator=denominator,
                Materials=(int)((80*numerator+denominator-1)/denominator),
                DurationTicks=Math.Max(1,(80*TimeSpan.TicksPerSecond*numerator+denominator-1)/denominator)};
            return true;
        }
        public static int CompletionHeal(long numerator,long denominator,int current,int maximum)
        {
            if(numerator<=0||denominator<=0||numerator>denominator/4||current<=0||maximum<=current)return 0;
            return (int)Math.Min(maximum-current,(long)maximum*numerator/denominator);
        }
        public static int Refund(int paid,long completedTicks,bool destroyed)
            =>destroyed||paid<=0?0:completedTicks==0?paid:(int)((long)paid*3/4);
        public static bool TookDamage(int oldHealth,int oldMaximum,int health,int maximum)
        {
            if(oldMaximum<=0||maximum<=0||health<=0)return true;
            // Existing vehicle-health research preserves percentage with nearest
            // integer rounding; a max-health change is not itself incoming damage.
            long expected=((long)oldHealth*maximum+oldMaximum/2)/oldMaximum;
            return health<expected;
        }
    }
}
