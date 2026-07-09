using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class ResourceExchangeAudioWiringContractTests
{
    private static readonly string[] ResourceExchangeScriptRoots =
    {
        "Assets/Game/Scripts/Components",
        "Assets/Game/Scripts/Configs",
        "Assets/Game/Scripts/Editor",
        "Assets/Game/Scripts/Systems",
        "Assets/Game/Scripts/UI/Screens",
        "Assets/Game/Scripts/UI/Shell/Ecs"
    };

    private static readonly string[] ResourceExchangePrefabPaths =
    {
        "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab"
    };

    private static readonly string[] ForbiddenSourceTokens =
    {
        "AudioSource",
        "AudioClip",
        "AudioListener",
        "PlayOneShot",
        "PlayClipAtPoint",
        "PlayScheduled",
        "PlayDelayed",
        "AudioSettings.",
        "Resources.Load"
    };

    private static readonly string[] ForbiddenPrefabTokens =
    {
        "AudioSource",
        "AudioClip",
        "AudioListener",
        "m_AudioClip",
        "m_audioClip",
        "m_OutputAudioMixerGroup",
        "m_PlayOnAwake"
    };

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ResourceExchangeScriptsDoNotPlayAudioDirectly),
                test => test.ResourceExchangeScriptsDoNotPlayAudioDirectly(),
                ref passed);
            RunValidationStep(
                nameof(ResourceExchangePrefabsDoNotEmbedDirectAudioWiring),
                test => test.ResourceExchangePrefabsDoNotEmbedDirectAudioWiring(),
                ref passed);

            Debug.Log($"[ResourceExchangeAudioWiringValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeAudioWiringValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ResourceExchangeScriptsDoNotPlayAudioDirectly()
    {
        List<string> scriptPaths = FindResourceExchangeScripts();
        Assert.Greater(scriptPaths.Count, 0, "No Resource Exchange scripts were found for audio wiring validation.");

        for (int pathIndex = 0; pathIndex < scriptPaths.Count; pathIndex++)
            AssertNoForbiddenTokens(scriptPaths[pathIndex], ForbiddenSourceTokens);
    }

    [Test]
    public void ResourceExchangePrefabsDoNotEmbedDirectAudioWiring()
    {
        for (int pathIndex = 0; pathIndex < ResourceExchangePrefabPaths.Length; pathIndex++)
        {
            string path = ResourceExchangePrefabPaths[pathIndex];
            Assert.IsTrue(File.Exists(path), $"Missing Resource Exchange prefab for audio wiring validation: {path}");
            AssertNoForbiddenTokens(path, ForbiddenPrefabTokens);
        }
    }

    private static List<string> FindResourceExchangeScripts()
    {
        var result = new List<string>(32);
        for (int rootIndex = 0; rootIndex < ResourceExchangeScriptRoots.Length; rootIndex++)
        {
            string root = ResourceExchangeScriptRoots[rootIndex];
            if (!Directory.Exists(root))
                continue;

            string[] files = Directory.GetFiles(root, "*ResourceExchange*.cs", SearchOption.AllDirectories);
            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                string normalized = files[fileIndex].Replace('\\', '/');
                if (!normalized.StartsWith("Assets/Game/Scripts/", StringComparison.Ordinal))
                    continue;

                result.Add(normalized);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static void AssertNoForbiddenTokens(string path, IReadOnlyList<string> forbiddenTokens)
    {
        string contents = File.ReadAllText(path);
        for (int tokenIndex = 0; tokenIndex < forbiddenTokens.Count; tokenIndex++)
        {
            string token = forbiddenTokens[tokenIndex];
            StringAssert.DoesNotContain(
                token,
                contents,
                $"{path} must not use direct audio token `{token}`. Resource Exchange audio must flow through the central audio event catalog/request path.");
        }
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeAudioWiringContractTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeAudioWiringContractTests();
        action(test);
        passed++;
    }
}
