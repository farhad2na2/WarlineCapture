using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuPersistentResourcesPrefabTests
{
    [Test]
    public void HeaderShowsInformationalCreditsWithoutRetiredWalletOrHitTargets()
    {
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab");
        Assert.IsNotNull(prefab);
        var header=prefab.transform.Find("HeaderContent");
        Assert.IsNull(header.Find("HeaderResourceArea"));
        Assert.IsNull(header.Find("CommandVisualPanel"));
        Assert.IsNotNull(header.GetComponent<MainMenuAccountHeaderView>());
        var credits=header.Find("CreditsVisualPanel");
        Assert.AreEqual("CREDITS",credits.Find("Label").GetComponent<TMP_Text>().text);
        Assert.AreEqual("—",credits.Find("Value").GetComponent<TMP_Text>().text);
        Assert.IsEmpty(credits.GetComponentsInChildren<Selectable>(true));
    }
}
