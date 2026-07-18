using System;
using System.Collections;
using System.Reflection;
using Game.Configs;
using Game.Rendering;
using NUnit.Framework;
using UnityEngine;

public sealed class SharedPrefabPreviewCacheTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            SharedPrefabPreviewCacheTests tests = new();
            tests.CacheState_IsInstanceOwnedAndDisposeClearsReferences();
            tests.MutableRuntimeFields_AreNotStatic();
            Debug.Log("[SharedPrefabPreviewCacheValidation] result=Passed tests=2");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[SharedPrefabPreviewCacheValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void CacheState_IsInstanceOwnedAndDisposeClearsReferences()
    {
        SharedPrefabPreviewCache first = new();
        SharedPrefabPreviewCache second = new();
        PrefabPreviewCameraConfig config = ScriptableObject.CreateInstance<PrefabPreviewCameraConfig>();
        try
        {
            first.Init(config);
            FieldInfo cacheField = typeof(SharedPrefabPreviewCache).GetField(
                "_cache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo configField = typeof(SharedPrefabPreviewCache).GetField(
                "_previewConfig",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(cacheField);
            Assert.NotNull(configField);
            Assert.AreNotSame(cacheField.GetValue(first), cacheField.GetValue(second));
            Assert.AreSame(config, configField.GetValue(first));

            first.Dispose();

            Assert.IsNull(configField.GetValue(first));
            Assert.AreEqual(0, ((IDictionary)cacheField.GetValue(first)).Count);
        }
        finally
        {
            first.Dispose();
            second.Dispose();
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void MutableRuntimeFields_AreNotStatic()
    {
        FieldInfo[] fields = typeof(SharedPrefabPreviewCache).GetFields(
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        for (int i = 0; i < fields.Length; i++)
        {
            Assert.IsTrue(
                fields[i].IsLiteral || fields[i].IsInitOnly,
                $"Preview cache field {fields[i].Name} must be immutable when static.");
        }
    }
}
