using System;
using Game.Tests.Editor.Operations;

internal static class OperationsP2HostRunner
{
    private static int Main()
    {
        try
        {
            OperationsP2Checks.RunAll();
            Console.WriteLine(OperationsP2Checks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsP2Validation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
