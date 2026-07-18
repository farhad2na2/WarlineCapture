using System;
using System.Reflection;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class MainMenuNavigationViewTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            new MainMenuNavigationViewTests().ActiveTab_PersistsAcrossViewsAndResetsForNewSession();
            Debug.Log("[MainMenuNavigationValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MainMenuNavigationValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ActiveTab_PersistsAcrossViewsAndResetsForNewSession()
    {
        Type viewType = typeof(MainMenuNavigationView);
        FieldInfo activeTab = viewType.GetField("activeTab", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo selectNav = viewType.GetMethod("SelectNav", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo reset = viewType.GetMethod("ResetActiveTab", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(activeTab);
        Assert.NotNull(selectNav);
        Assert.NotNull(reset);

        reset.Invoke(null, null);
        GameObject firstObject = new("MainMenuNavigationTestFirst");
        GameObject secondObject = new("MainMenuNavigationTestSecond");
        try
        {
            MainMenuNavigationView first = firstObject.AddComponent<MainMenuNavigationView>();
            secondObject.AddComponent<MainMenuNavigationView>();
            selectNav.Invoke(first, new object[] { MainMenuNavigationTabId.Armory });

            Assert.AreEqual(MainMenuNavigationTabId.Armory, activeTab.GetValue(null));
            reset.Invoke(null, null);
            Assert.AreEqual(MainMenuNavigationTabId.Leaderboards, activeTab.GetValue(null));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstObject);
            UnityEngine.Object.DestroyImmediate(secondObject);
            reset.Invoke(null, null);
        }
    }
}
