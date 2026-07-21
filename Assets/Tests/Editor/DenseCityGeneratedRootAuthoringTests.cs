using Game.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityGeneratedRootAuthoringTests
{
    private const string ValidHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Test]
    public void GeneratedRoot_ValidatesClosedRoleAndDeterministicIdentity()
    {
        GameObject owner = new("GeneratedRoot");
        try
        {
            DenseCityGeneratedRootAuthoring authoring =
                owner.AddComponent<DenseCityGeneratedRootAuthoring>();
            SerializedObject serialized = new(authoring);
            serialized.FindProperty("role").enumValueIndex =
                (int)DenseCityGeneratedRootRole.EntityPresentationSource;
            serialized.FindProperty("generationId").stringValue = "opmap.skirmish.desert_base_01:city:42";
            serialized.FindProperty("generatorSchema").stringValue = "dense-city-v1";
            serialized.FindProperty("generatorSchemaVersion").intValue = 1;
            serialized.FindProperty("deterministicSeed").intValue = 42;
            serialized.FindProperty("deterministicGenerationHash").stringValue = ValidHash;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(authoring.TryValidate(out string error), Is.True, error);
            Assert.That(authoring.DeterministicSeed, Is.EqualTo(42));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void GeneratedRoot_RejectsUnknownRoleAndMalformedHash()
    {
        GameObject owner = new("GeneratedRoot");
        try
        {
            DenseCityGeneratedRootAuthoring authoring =
                owner.AddComponent<DenseCityGeneratedRootAuthoring>();
            SerializedObject serialized = new(authoring);
            serialized.FindProperty("role").enumValueIndex =
                (int)DenseCityGeneratedRootRole.Unknown;
            serialized.FindProperty("generationId").stringValue = "generation";
            serialized.FindProperty("generatorSchema").stringValue = "dense-city-v1";
            serialized.FindProperty("generatorSchemaVersion").intValue = 1;
            serialized.FindProperty("deterministicGenerationHash").stringValue = "bad";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(authoring.TryValidate(out _), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AuthoredOverride_UsesFiniteColliderFreeSerializedBounds()
    {
        GameObject owner = new("AuthoredOverride");
        try
        {
            DenseCityAuthoredOverrideAuthoring authoring =
                owner.AddComponent<DenseCityAuthoredOverrideAuthoring>();
            SerializedObject serialized = new(authoring);
            serialized.FindProperty("stableId").stringValue = "military-base-protected-area";
            serialized.FindProperty("localCenter").vector3Value = new Vector3(1f, 2f, 3f);
            serialized.FindProperty("localSize").vector3Value = new Vector3(10f, 5f, 12f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(authoring.TryValidate(out string error), Is.True, error);
            owner.AddComponent<BoxCollider>();
            Assert.That(authoring.TryValidate(out error), Is.False);
            StringAssert.Contains("cannot own colliders", error);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}
