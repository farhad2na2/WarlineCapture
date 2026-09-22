using System;
using Game.Tests.Editor.Operations;

internal static class OperationsMobileReadyHostRunner
{
    private static int Main()
    {
        try
        {
            OperationsMobileReadyChecks.RunAll();
            Console.WriteLine(OperationsMobileReadyChecks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsMobileReadyValidation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
