using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandTabGroupView : MonoBehaviour
{
    private static readonly List<MatchOverlayCommandTabGroupView> RegisteredInstances = new();

    [SerializeField] private MatchOverlayCommandTabView[] tabs;
    [SerializeField] private int defaultSelectedIndex;

    public MatchOverlayCommandTabView[] Tabs => tabs;
    public int DefaultSelectedIndex => defaultSelectedIndex;
    public static IReadOnlyList<MatchOverlayCommandTabGroupView> Instances => RegisteredInstances;

    private void OnEnable()
    {
        if (!RegisteredInstances.Contains(this))
            RegisteredInstances.Add(this);
    }

    private void OnDisable()
    {
        RegisteredInstances.Remove(this);
    }
}

[Serializable]
public sealed class MatchOverlayCommandTabView
{
    [SerializeField] private Button button;
    [SerializeField] private Image frameImage;
    [SerializeField] private Sprite normalFrameSprite;
    [SerializeField] private Sprite selectedFrameSprite;

    public Button Button => button;
    public Image FrameImage => frameImage;
    public Sprite NormalFrameSprite => normalFrameSprite;
    public Sprite SelectedFrameSprite => selectedFrameSprite;
}
