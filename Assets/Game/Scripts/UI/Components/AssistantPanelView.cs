using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AssistantPanelView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text recommendationTitleText;
    [SerializeField] private TMP_Text recommendationBodyText;
    [SerializeField] private Transform assistantTabs;
    [SerializeField] private Transform recommendationChips;
    [SerializeField] private Button showMeButton;
    [SerializeField] private Button doItButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private TMP_Text[] tabLabels = System.Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] chipLabels = System.Array.Empty<TMP_Text>();

    public TMP_Text TitleText => titleText;
    public TMP_Text StatusText => statusText;
    public TMP_Text RecommendationTitleText => recommendationTitleText;
    public TMP_Text RecommendationBodyText => recommendationBodyText;
    public Transform AssistantTabs => assistantTabs;
    public Transform RecommendationChips => recommendationChips;
    public Button ShowMeButton => showMeButton;
    public Button DoItButton => doItButton;
    public Button StopButton => stopButton;
    public TMP_Text[] TabLabels => tabLabels;
    public TMP_Text[] ChipLabels => chipLabels;

    public void BindRecommendation(string title, string body, string[] chips)
    {
        BindRecommendation(title, body, chips, canShow: true, canExecute: true, canStop: false);
    }

    public void BindRecommendation(string title, string body, string[] chips, bool canShow, bool canExecute, bool canStop)
    {
        if (recommendationTitleText != null)
            recommendationTitleText.text = title;
        if (recommendationBodyText != null)
            recommendationBodyText.text = body;

        for (int i = 0; i < chipLabels.Length; i++)
        {
            bool hasChip = chips != null && i < chips.Length && !string.IsNullOrWhiteSpace(chips[i]);
            if (chipLabels[i] != null)
            {
                chipLabels[i].text = hasChip ? chips[i] : string.Empty;
                chipLabels[i].transform.parent.gameObject.SetActive(hasChip);
            }
        }

        SetActionAvailability(canShow, canExecute, canStop);
    }

    public void SetActionAvailability(bool canShow, bool canExecute, bool canStop)
    {
        if (showMeButton != null)
            showMeButton.interactable = canShow;
        if (doItButton != null)
            doItButton.interactable = canExecute;
        if (stopButton != null)
            stopButton.interactable = canStop;
    }
}
