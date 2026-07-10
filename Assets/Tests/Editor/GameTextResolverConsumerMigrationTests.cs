using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class GameTextResolverConsumerMigrationTests
{
    private const string ExpectedLiteralCallHash = "6a7ca45cd08cd8de0470e61c99a0b938c511d3300f80056c87d61ddc8d1d5d92";
    private const string MainMenuPlayUiPath = "Assets/Game/Scripts/UI/MainMenuPlayUI.cs";
    private const string BattleFeedbackSinkPath = "Assets/Game/Scripts/UI/Screens/BattleHudRuntimeFeedbackSink.cs";
    private const string UiShellContentViewPath = "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs";
    private const string BattleFeedbackPath = "Assets/Game/Scripts/UI/Screens/BattleHudRuntimeFeedbackUiSystemHelper.cs";
    private const string BuildDrawerPath = "Assets/Game/Scripts/UI/Screens/BuildDrawerCatalogRuntimeView.cs";
    private const string BuildPlacementPath = "Assets/Game/Scripts/UI/Screens/BuildPlacementConfirmationBarView.cs";
    private const string CurrentOrderPath = "Assets/Game/Scripts/UI/Screens/MatchHudCurrentOrderBannerUiSystemHelper.cs";
    private const string RightQuickRailPath = "Assets/Game/Scripts/UI/Screens/MatchHudRightQuickRailView.cs";
    private const string CommandInputPath = "Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs";
    private const string CommandWheelPath = "Assets/Game/Scripts/UI/Screens/CommandWheelPanelView.cs";

    private static readonly string[] ConsumerPaths =
    {
        BattleFeedbackPath,
        BuildDrawerPath,
        BuildPlacementPath,
        CurrentOrderPath,
        RightQuickRailPath,
        CommandInputPath,
        UiShellContentViewPath
    };

    private static readonly Regex ResolverCallRegex = new(
        @"(?:_gameTextResolver|textResolver)\.(Get|Format)\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex LiteralCallRegex = new(
        @"(?:_gameTextResolver|textResolver)\.(Get|Format)\(\s*""((?:\\.|[^""])*)""\s*,\s*""((?:\\.|[^""])*)""",
        RegexOptions.CultureInvariant | RegexOptions.Singleline);

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new GameTextResolverConsumerMigrationTests();
            tests.ConsumerCalls_MigrateExactlySixtyTwoGetsAndTwentyThreeFormats();
            tests.ConsumerCalls_PreserveOrderedLiteralKeysAndFallbacks();
            tests.ConsumerCalls_PreserveDynamicKeyAndFallbackShapes();
            tests.ResolverPropagation_UsesExistingRootsAndFallbackOnlyNullHandling();
            Debug.Log("[GameTextResolverConsumerMigrationValidation] result=Passed tests=4 gets=62 formats=23 expressions=85");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[GameTextResolverConsumerMigrationValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void ConsumerCalls_MigrateExactlySixtyTwoGetsAndTwentyThreeFormats()
    {
        int getCount = 0;
        int formatCount = 0;

        for (int i = 0; i < ConsumerPaths.Length; i++)
        {
            string source = File.ReadAllText(ConsumerPaths[i]);
            StringAssert.DoesNotContain("GameText.Get(", source, $"Direct Get remains in {ConsumerPaths[i]}.");
            StringAssert.DoesNotContain("GameText.Format(", source, $"Direct Format remains in {ConsumerPaths[i]}.");
            StringAssert.DoesNotContain("using Game.Configs;", source, $"Stale Game.Configs import remains in {ConsumerPaths[i]}.");

            MatchCollection calls = ResolverCallRegex.Matches(source);
            for (int callIndex = 0; callIndex < calls.Count; callIndex++)
            {
                if (calls[callIndex].Groups[1].Value == "Get")
                    getCount++;
                else
                    formatCount++;
            }
        }

        Assert.AreEqual(62, getCount);
        Assert.AreEqual(23, formatCount);
    }

    [Test]
    public void ConsumerCalls_PreserveOrderedLiteralKeysAndFallbacks()
    {
        var canonicalCalls = new StringBuilder();
        int literalCallCount = 0;

        for (int i = 0; i < ConsumerPaths.Length; i++)
        {
            string path = ConsumerPaths[i];
            MatchCollection calls = LiteralCallRegex.Matches(File.ReadAllText(path));
            for (int callIndex = 0; callIndex < calls.Count; callIndex++)
            {
                Match call = calls[callIndex];
                canonicalCalls
                    .Append(path).Append('\t')
                    .Append(call.Groups[1].Value).Append('\t')
                    .Append(call.Groups[2].Value).Append('\t')
                    .Append(call.Groups[3].Value).Append('\n');
                literalCallCount++;
            }
        }

        Assert.AreEqual(80, literalCallCount);
        Assert.AreEqual(ExpectedLiteralCallHash, ComputeSha256(canonicalCalls.ToString()));
    }

    [Test]
    public void ConsumerCalls_PreserveDynamicKeyAndFallbackShapes()
    {
        string battleSource = File.ReadAllText(BattleFeedbackPath);
        string currentOrderSource = File.ReadAllText(CurrentOrderPath);
        string commandInputSource = File.ReadAllText(CommandInputPath);

        StringAssert.Contains("textResolver.Get(\n                TacticalCommandFeedbackText.ToDisplayTextKey(mode),\n                TacticalCommandFeedbackText.ToDisplayText(mode))", battleSource);
        StringAssert.Contains("textResolver.Get(\n                TacticalCommandFeedbackText.ToDisplayTextKey(reasonCode),\n                TacticalCommandFeedbackText.ToDisplayText(reasonCode))", battleSource);
        StringAssert.Contains("textResolver.Get(\n                TacticalCommandFeedbackText.ToInstructionTextKey(mode),\n                TacticalCommandFeedbackText.ToInstructionText(mode))", battleSource);
        StringAssert.Contains("textResolver.Get(key, fallback)", currentOrderSource);
        StringAssert.Contains("_gameTextResolver.Get(\n                    TacticalCommandFeedbackText.ToDisplayTextKey(reason),\n                    TacticalCommandFeedbackText.ToDisplayText(reason))", commandInputSource);
    }

    [Test]
    public void ResolverPropagation_UsesExistingRootsAndFallbackOnlyNullHandling()
    {
        string mainMenuSource = File.ReadAllText(MainMenuPlayUiPath);
        string feedbackSinkSource = File.ReadAllText(BattleFeedbackSinkPath);
        string commandWheelSource = File.ReadAllText(CommandWheelPath);
        string contentSource = File.ReadAllText(UiShellContentViewPath);
        var combined = new StringBuilder();
        combined.Append(mainMenuSource).Append(feedbackSinkSource).Append(commandWheelSource).Append(contentSource);
        for (int i = 0; i < ConsumerPaths.Length; i++)
            combined.Append(File.ReadAllText(ConsumerPaths[i]));

        string source = combined.ToString();
        StringAssert.Contains("new BattleHudRuntimeFeedbackSink(_matchHudRuntimeFeedbackView, _gameTextResolver)", mainMenuSource);
        StringAssert.Contains("gameTextResolver ?? FallbackGameTextResolver.Instance", feedbackSinkSource);
        StringAssert.Contains("ApplyCommandResult(_view, result, _gameTextResolver)", feedbackSinkSource);
        StringAssert.Contains("_view.CommandWheelPanel?.BindGameTextResolver(_gameTextResolver);", File.ReadAllText(CommandInputPath));
        StringAssert.Contains("gameTextResolver ?? FallbackGameTextResolver.Instance", commandWheelSource);
        StringAssert.Contains("_rightQuickRailView.BindBuildCommand(\n                OpenBuildDrawerFromRightQuickRail,\n                _selectionUiCommandSystem,\n                ResolveMatchHudRuntimeFeedback(),\n                _gameTextResolver);", contentSource);
        StringAssert.Contains("_matchOverlayCommandInputSystem.Bind(", contentSource);
        StringAssert.Contains("_gameTextResolver);", contentSource);
        StringAssert.DoesNotContain("static IGameTextResolver", source);
        StringAssert.DoesNotContain("GameTextResolverRegistry", source);

        int nullCoalesceCount = CountOccurrences(source, "gameTextResolver ?? FallbackGameTextResolver.Instance");
        Assert.GreaterOrEqual(nullCoalesceCount, 6, "Every nullable consumer seam must use the immutable fallback resolver.");
    }

    private static string ComputeSha256(string value)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        var result = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
            result.Append(hash[i].ToString("x2"));
        return result.ToString();
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
