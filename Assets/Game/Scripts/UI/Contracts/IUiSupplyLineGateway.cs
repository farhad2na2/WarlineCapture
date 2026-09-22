namespace Game.UI.Contracts
{
    public interface IUiSupplyLineGateway
    {
        bool TryReadSupplyLine(out int storedFuel,out int civilianReserve,out bool canAllocate);
        bool TryAllocateSupplyLineReserve();
    }
}
