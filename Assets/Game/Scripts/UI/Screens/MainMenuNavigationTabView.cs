using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class MainMenuNavigationTabView
{
    [SerializeField] private MainMenuNavigationTabId tabId;
    [SerializeField] private Button button;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text label;

    public MainMenuNavigationTabId TabId => tabId;
    public Button Button => button;
    public Image Frame => frame;
    public TMP_Text Label => label;
}
