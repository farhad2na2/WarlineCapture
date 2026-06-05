using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class ArmoryCategoryNavigationTabView
{
    [SerializeField] private ArmoryCatalogCategory category;
    [SerializeField] private Button button;
    [SerializeField] private Image frame;

    public ArmoryCatalogCategory Category => category;
    public Button Button => button;
    public Image Frame => frame;
}
