using System;
using Game.Tests.Editor.Operations;

internal static class OperationsO001PlayerReadyHostRunner
{
    private static int Main()
    {
        try
        {
            OperationsO001PlayerReadyChecks.RunAll();
            Console.WriteLine(OperationsO001PlayerReadyChecks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsO001PlayerShellValidation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
