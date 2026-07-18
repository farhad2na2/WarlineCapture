using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class AISettingsOwnershipTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new AISettingsOwnershipTests();
            tests.ConfigStores_OwnIndependentClampedSnapshots();
            tests.MatchStartProjection_IsWorldOwnedAndConsumedOnce();
            Debug.Log("[AISettingsOwnershipValidation] result=Passed tests=2");
        }
        catch (Exception exception)
        {
            Debug.LogError("[AISettingsOwnershipValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void ConfigStores_OwnIndependentClampedSnapshots()
    {
        var first = new QuickCustomGameConfigStore();
        var second = new QuickCustomGameConfigStore();
        UiQuickCustomGameConfig config = first.Defaults;
        config.Difficulty = UiAiDifficultySetting.Brutal;
        config.IncomeMultiplier = 99f;
        config.EnemyCount = 99;

        first.Apply(config);

        Assert.AreEqual(UiAiDifficultySetting.Brutal, first.Current.Difficulty);
        Assert.AreEqual(3f, first.Current.IncomeMultiplier);
        Assert.AreEqual(3, first.Current.EnemyCount);
        Assert.AreEqual(UiAiDifficultySetting.Normal, second.Current.Difficulty);
        Assert.AreEqual(1f, second.Current.IncomeMultiplier);
        Assert.AreEqual(1, second.Current.EnemyCount);
    }

    [Test]
    public void MatchStartProjection_IsWorldOwnedAndConsumedOnce()
    {
        using var firstWorld = new World("AI settings first match");
        using var secondWorld = new World("AI settings second match");
        AISettingsSnapshot firstSnapshot = AISettingsSnapshot.Defaults;
        firstSnapshot.Difficulty = AIDifficultySetting.Hard;
        AISettingsSnapshot secondSnapshot = AISettingsSnapshot.Defaults;
        secondSnapshot.Difficulty = AIDifficultySetting.Brutal;

        var firstStart = new MatchStartRequestStartupSystemHelper();
        var secondStart = new MatchStartRequestStartupSystemHelper();
        Assert.IsTrue(firstStart.QueueStartAfterMatchLoaded(firstWorld.EntityManager));
        Assert.IsTrue(secondStart.QueueStartAfterMatchLoaded(secondWorld.EntityManager));
        Assert.IsTrue(MatchAISettingsStartupProjection.Project(firstWorld.EntityManager, firstSnapshot));
        Assert.IsTrue(MatchAISettingsStartupProjection.Project(secondWorld.EntityManager, secondSnapshot));

        Assert.IsTrue(MatchAISettingsStartupProjection.TryConsume(
            firstWorld.EntityManager,
            out AISettingsSnapshot consumedFirst));
        Assert.AreEqual(AIDifficultySetting.Hard, consumedFirst.Difficulty);
        Assert.IsFalse(MatchAISettingsStartupProjection.TryConsume(firstWorld.EntityManager, out _));

        Assert.IsTrue(MatchAISettingsStartupProjection.TryConsume(
            secondWorld.EntityManager,
            out AISettingsSnapshot consumedSecond));
        Assert.AreEqual(AIDifficultySetting.Brutal, consumedSecond.Difficulty);
    }
}
