using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class GameTextResolverInjectionTests
{
    private const string MenuBootstrapPath = "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs";
    private const string MatchBootstrapPath = "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs";
    private const string MainMenuPlayUiPath = "Assets/Game/Scripts/UI/MainMenuPlayUI.cs";
    private const string UiShellContentViewPath = "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs";

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new GameTextResolverInjectionTests();
            tests.MenuBootstrap_BindsResolverBeforeRouterInitialization();
            tests.MatchBootstrap_PassesResolverThroughBothMainMenuInitializationPaths();
            tests.UiRuntimeRoots_RetainResolverWithImmutableFallback();
            tests.Injection_DoesNotUseStaticRegistrationOrNewRuntimeConfigDependency();
            Debug.Log("[GameTextResolverInjectionValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[GameTextResolverInjectionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void MenuBootstrap_BindsResolverBeforeRouterInitialization()
    {
        string source = File.ReadAllText(MenuBootstrapPath);
        const string resolverField = "private readonly IGameTextResolver gameTextResolver = new GameTextResolverAdapter();";
        const string resolverBinding = "view.ContentSystem.BindGameTextResolver(gameTextResolver);";
        const string routerInitialization = "view.Router.Initialize();";

        StringAssert.Contains(resolverField, source);
        int bindingIndex = source.IndexOf(resolverBinding, StringComparison.Ordinal);
        int routerIndex = source.IndexOf(routerInitialization, StringComparison.Ordinal);
        Assert.GreaterOrEqual(bindingIndex, 0, "Menu composition must bind the resolver through the serialized content-system reference.");
        Assert.Greater(routerIndex, bindingIndex, "Resolver binding must happen before router content installation.");
    }

    [Test]
    public void MatchBootstrap_PassesResolverThroughBothMainMenuInitializationPaths()
    {
        string source = File.ReadAllText(MatchBootstrapPath);

        StringAssert.Contains("private readonly IGameTextResolver gameTextResolver = new GameTextResolverAdapter();", source);
        Assert.AreEqual(2, CountOccurrences(source, "MainMenu.Init("), "Expected both Match MainMenu initialization paths to remain explicit.");
        Assert.AreEqual(2, CountOccurrences(source, "gameTextResolver, resetRuntimeState"), "Both Match MainMenu initialization paths must receive the resolver.");
    }

    [Test]
    public void UiRuntimeRoots_RetainResolverWithImmutableFallback()
    {
        string mainMenuSource = File.ReadAllText(MainMenuPlayUiPath);
        string contentSource = File.ReadAllText(UiShellContentViewPath);

        StringAssert.Contains("private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;", mainMenuSource);
        StringAssert.Contains("_gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;", mainMenuSource);
        StringAssert.Contains("private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;", contentSource);
        StringAssert.Contains("public void BindGameTextResolver(IGameTextResolver gameTextResolver)", contentSource);
        StringAssert.Contains("_gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;", contentSource);
    }

    [Test]
    public void Injection_DoesNotUseStaticRegistrationOrNewRuntimeConfigDependency()
    {
        string menuSource = File.ReadAllText(MenuBootstrapPath);
        string matchSource = File.ReadAllText(MatchBootstrapPath);
        string mainMenuSource = File.ReadAllText(MainMenuPlayUiPath);
        string contentSource = File.ReadAllText(UiShellContentViewPath);
        string combined = menuSource + matchSource + mainMenuSource + contentSource;

        StringAssert.DoesNotContain("static IGameTextResolver", combined);
        StringAssert.DoesNotContain("static readonly IGameTextResolver", combined);
        StringAssert.DoesNotContain("GameTextResolverRegistry", combined);
        StringAssert.DoesNotContain("using Game.Configs;", mainMenuSource);
        StringAssert.Contains("view.ContentSystem.BindGameTextResolver(gameTextResolver);", menuSource);
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }
}
