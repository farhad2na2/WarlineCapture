using Unity.Entities;
using UnityEngine;

internal sealed class RuntimeResourceSystem
{
    private int _dollars;

    public int CurrentDollars => _dollars;

    public void SetInitialDollars(int dollars)
    {
        _dollars = Mathf.Max(0, dollars);
    }

    public void AddDollars(int amount)
    {
        _dollars += Mathf.Max(0, amount);
    }

    public bool TrySpendDollars(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0)
            return true;
        if (_dollars < amount)
            return false;

        _dollars -= amount;
        return true;
    }

    public CitizenResourceCompositionSystemHelper.Context CreateCitizenResourceContext()
    {
        return new CitizenResourceCompositionSystemHelper.Context(
            () => _dollars,
            value => _dollars = Mathf.Max(0, value));
    }
}
