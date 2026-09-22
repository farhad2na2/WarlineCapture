using System;
using Game.Tests.Editor.Operations;

internal static class OperationsAriaPlayModeCaptureHostRunner
{
    private static int Main()
    {
        try
        {
            OperationsAriaPlayModeCaptureChecks.RunAll();
            Console.WriteLine(OperationsAriaPlayModeCaptureChecks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsAriaPlayModeCaptureValidation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
