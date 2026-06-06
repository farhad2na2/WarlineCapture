using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchOverlayCommandTabGroupView : MonoBehaviour
{
    [SerializeField] private MatchOverlayCommandTabView[] tabs;
    [SerializeField] private int defaultSelectedIndex;

    public MatchOverlayCommandTabView[] Tabs => tabs;
    public int DefaultSelectedIndex => defaultSelectedIndex;
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
