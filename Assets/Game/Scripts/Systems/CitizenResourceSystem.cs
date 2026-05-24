using System;
using UnityEngine;

internal sealed class CitizenResourceSystem
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

    public bool IsConfigured(Context context)
    {
        return context.GetDollars != null && context.SetDollars != null;
    }

    public bool TrySpendDollars(Context context, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0)
            return true;
        if (!IsConfigured(context))
            return false;

        int current = Mathf.Max(0, context.GetDollars());
        if (current < amount)
            return false;

        context.SetDollars(current - amount);
        return true;
    }
}
