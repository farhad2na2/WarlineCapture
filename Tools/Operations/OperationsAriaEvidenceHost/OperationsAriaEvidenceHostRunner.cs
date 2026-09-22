using System;
using Game.Tests.Editor.Operations;

internal static class OperationsAriaEvidenceHostRunner
{
    private static int Main()
    {
        try
        {
            OperationsAriaEvidenceChecks.RunAll();
            Console.WriteLine(OperationsAriaEvidenceChecks.PassMarker);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[OperationsAriaEvidenceValidation] result=Failed");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
