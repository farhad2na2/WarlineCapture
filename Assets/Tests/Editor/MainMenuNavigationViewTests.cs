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
        MethodInfo selectNav = typeof(MainMenuNavigationView).GetMethod("SelectNav", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(selectNav);

        MainMenuNavigationSessionState.Reset();
        GameObject firstObject = new("MainMenuNavigationTestFirst");
        GameObject secondObject = new("MainMenuNavigationTestSecond");
        try
        {
            MainMenuNavigationView first = firstObject.AddComponent<MainMenuNavigationView>();
            secondObject.AddComponent<MainMenuNavigationView>();
            selectNav.Invoke(first, new object[] { MainMenuNavigationTabId.Armory });

            Assert.AreEqual(MainMenuNavigationTabId.Armory, MainMenuNavigationSessionState.ActiveTab);
            MainMenuNavigationSessionState.Reset();
            Assert.AreEqual(MainMenuNavigationTabId.Leaderboards, MainMenuNavigationSessionState.ActiveTab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstObject);
            UnityEngine.Object.DestroyImmediate(secondObject);
            MainMenuNavigationSessionState.Reset();
        }
    }
}
