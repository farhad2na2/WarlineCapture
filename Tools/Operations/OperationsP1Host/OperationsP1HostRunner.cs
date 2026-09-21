using System;
using Game.Tests.Editor.Operations;

internal static class OperationsP1HostRunner
{
    private static int Main()
    {
        try
        {
            OperationsP1Checks.RunAll();
            Console.WriteLine(OperationsP1Checks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsP1Validation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
