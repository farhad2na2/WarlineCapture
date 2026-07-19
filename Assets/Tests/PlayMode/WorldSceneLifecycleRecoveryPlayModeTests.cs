using System.Collections;
using System.Reflection;
using Game.Runtime;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class WorldSceneLifecycleRecoveryPlayModeTests
{
    private const string FirstSceneName = "AM022.WorldSceneRecovery.First";
    private const string SecondSceneName = "AM022.WorldSceneRecovery.Second";

    [UnityTest]
    public IEnumerator ProductionMenuMatchMenu_ClearsMatchDependenciesAndKeepsOneLifecycleRoot()
    {
        var productionLifecycle = new Aph805MenuMatchMenuLifecyclePlayModeTests();
        IEnumerator test = productionLifecycle.MenuToMatchToMenu_PreservesWorldBindsUiAndCleansMatchRuntime();
        while (test.MoveNext())
            yield return test.Current;

        IEnumerator cleanup = productionLifecycle.TearDown();
        while (cleanup.MoveNext())
            yield return cleanup.Current;
    }

    [UnityTest]
    public IEnumerator SceneUnloadAndSubsystemReset_ClearRootsSubscriptionsAndAllowOneRebind()
    {
        MethodInfo resetLogBuffer = GetRuntimeLogBufferMethod("ResetBeforeSubsystemRegistration");
        MethodInfo initializeLogBuffer = GetRuntimeLogBufferMethod("InitializeBeforeSceneLoad");
        FieldInfo initialized = GetRuntimeLogBufferField("_initialized");
        FieldInfo entries = GetRuntimeLogBufferField("Entries");
        PropertyInfo entryCount = entries.FieldType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(entryCount, Is.Not.Null);

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene firstScene = SceneManager.CreateScene(FirstSceneName);
        resetLogBuffer.Invoke(null, null);
        initializeLogBuffer.Invoke(null, null);
        Assert.That((bool)initialized.GetValue(null), Is.True);

        SceneManager.SetActiveScene(firstScene);
        MissileTrailVfxView.Sync(Entity.Null, float3.zero, new float3(0f, 0f, 1f));
        GameObject firstRoot = GameObject.Find("MissileTrailVfxView");
        Assert.That(firstRoot, Is.Not.Null);
        SceneManager.MoveGameObjectToScene(firstRoot, firstScene);

        AsyncOperation unloadFirst = SceneManager.UnloadSceneAsync(firstScene);
        Assert.That(unloadFirst, Is.Not.Null);
        while (!unloadFirst.isDone)
            yield return null;
        yield return null;

        Assert.That(firstRoot == null, Is.True, "Scene unload retained its presentation root.");
        MissileTrailVfxView.ReleaseAll();
        MissileTrailVfxView.ReleaseAll();
        Assert.That(ReadStaticField(typeof(MissileTrailVfxView), "_instance"), Is.Null);

        resetLogBuffer.Invoke(null, null);
        resetLogBuffer.Invoke(null, null);
        Assert.That((bool)initialized.GetValue(null), Is.False);
        Assert.That((int)entryCount.GetValue(entries.GetValue(null)), Is.Zero);

        initializeLogBuffer.Invoke(null, null);
        initializeLogBuffer.Invoke(null, null);
        Assert.That((bool)initialized.GetValue(null), Is.True);

        Scene secondScene = SceneManager.CreateScene(SecondSceneName);
        SceneManager.SetActiveScene(secondScene);
        MissileTrailVfxView.Sync(Entity.Null, float3.zero, new float3(1f, 0f, 0f));
        GameObject secondRoot = GameObject.Find("MissileTrailVfxView");
        Assert.That(secondRoot, Is.Not.Null);
        SceneManager.MoveGameObjectToScene(secondRoot, secondScene);
        Assert.That(CountNamedRoots("MissileTrailVfxView"), Is.EqualTo(1));

        MissileTrailVfxView.ReleaseAll();
        resetLogBuffer.Invoke(null, null);
        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            SceneManager.SetActiveScene(previousActiveScene);
        AsyncOperation unloadSecond = SceneManager.UnloadSceneAsync(secondScene);
        Assert.That(unloadSecond, Is.Not.Null);
        while (!unloadSecond.isDone)
            yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        MissileTrailVfxView.ReleaseAll();
        GetRuntimeLogBufferMethod("ResetBeforeSubsystemRegistration").Invoke(null, null);
        foreach (string sceneName in new[] { "Match", FirstSceneName, SecondSceneName })
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;
            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload == null)
                continue;
            while (!unload.isDone)
                yield return null;
        }
    }

    private static MethodInfo GetRuntimeLogBufferMethod(string methodName)
    {
        MethodInfo method = GetRuntimeLogBufferType().GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        return method;
    }

    private static FieldInfo GetRuntimeLogBufferField(string fieldName)
    {
        FieldInfo field = GetRuntimeLogBufferType().GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field;
    }

    private static System.Type GetRuntimeLogBufferType()
    {
        return typeof(MainMenuPlayUI).Assembly.GetType(
            "Game.UI.Runtime.RuntimeLogBuffer",
            throwOnError: true);
    }

    private static object ReadStaticField(System.Type owner, string fieldName)
    {
        FieldInfo field = owner.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field.GetValue(null);
    }

    private static int CountNamedRoots(string objectName)
    {
        int count = 0;
        GameObject[] roots = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int index = 0; index < roots.Length; index++)
        {
            if (roots[index].name == objectName && roots[index].transform.parent == null)
                count++;
        }
        return count;
    }
}
