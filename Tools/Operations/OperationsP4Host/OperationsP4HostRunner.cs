using System;
using Game.Tests.Editor.Operations;

internal static class OperationsP4HostRunner
{
    private static int Main()
    {
        try
        {
            OperationsP4Checks.RunAll();
            Console.WriteLine(OperationsP4Checks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsP4Validation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
