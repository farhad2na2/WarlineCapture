using System;
using Game.Tests.Editor.Operations;

internal static class OperationsP3HostRunner
{
    private static int Main()
    {
        try
        {
            OperationsP3Checks.RunAll();
            Console.WriteLine(OperationsP3Checks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsP3Validation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
