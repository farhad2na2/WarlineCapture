using System;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public static class UnitMoveTargetDiagnosticValidationRunner
{
    public static void Run()
    {
        try
        {
            using World world = new("UnitMoveTargetDiagnosticValidationRunner");
            SystemHandle system = world.CreateSystem<UnitMoveTargetDiagnosticSystem>();
            Require(system != SystemHandle.Null, "UnitMoveTargetDiagnosticSystem was not created as an ISystem.");
            system.Update(world.Unmanaged);

            string sourcePath = Path.Combine(
                Application.dataPath,
                "Game/Scripts/Systems/UnitMoveTargetDiagnosticSystem.cs");
            string source = File.ReadAllText(sourcePath);
            Require(source.Contains("struct UnitMoveTargetDiagnosticSystem : ISystem", StringComparison.Ordinal),
                "UnitMoveTargetDiagnosticSystem is not declared as an ISystem struct.");
            Require(!source.Contains("UnitMoveTargetDiagnosticSystem : SystemBase", StringComparison.Ordinal),
                "UnitMoveTargetDiagnosticSystem still declares SystemBase inheritance.");
            Require(source.Contains("NativeParallelHashMap<Entity, int2>", StringComparison.Ordinal),
                "Move target cache did not migrate to a native container.");

            Debug.Log("[UnitMoveTargetDiagnosticValidation] result=Passed tests=1");
            Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitMoveTargetDiagnosticValidation] result=Failed");
            Exit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
