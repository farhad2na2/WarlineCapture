using System;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class CitizenResourceCompositionSystemHelper
    {
        public readonly struct Context
        {
            public readonly Func<int> GetDollars;
            public readonly Action<int> SetDollars;

            public Context(Func<int> getDollars, Action<int> setDollars)
            {
                GetDollars = getDollars;
                SetDollars = setDollars;
            }
        }

        public static bool IsConfigured(CitizenResourceCompositionSystemHelper system, Context context)
        {
            return system != null
                ? system.IsConfigured(context)
                : IsConfiguredState(context);
        }

        public bool IsConfigured(Context context)
        {
            return IsConfiguredState(context);
        }

        public static bool TrySpendDollars(CitizenResourceCompositionSystemHelper system, Context context, int amount)
        {
            return system != null
                ? system.TrySpendDollars(context, amount)
                : TrySpendDollarsState(context, amount);
        }

        public bool TrySpendDollars(Context context, int amount)
        {
            return TrySpendDollarsState(context, amount);
        }

        private static bool IsConfiguredState(Context context)
        {
            return context.GetDollars != null && context.SetDollars != null;
        }

        private static bool TrySpendDollarsState(Context context, int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount <= 0)
                return true;
            if (!IsConfiguredState(context))
                return false;

            int current = Mathf.Max(0, context.GetDollars());
            if (current < amount)
                return false;

            context.SetDollars(current - amount);
            return true;
        }
    }
}
