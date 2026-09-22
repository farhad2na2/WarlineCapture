using System;

namespace Game.Operations.Contracts
{
    /// <summary>
    /// Shipping match buttons ask this gate before the Campaign command path.
    /// Composition installs the handler while an Operations shared launch is active.
    /// </summary>
    public static class OperationsShippingInputGate
    {
        public static Func<string, bool> TryConsume;

        public static bool Consume(string shippingControl)
        {
            Func<string, bool> gate = TryConsume;
            return gate != null && gate(shippingControl);
        }
    }
}
