using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudObjectivesElapsedView : MonoBehaviour
    {
        [SerializeField] private TMP_Text elapsedText;

        private float elapsedSeconds;
        private int displayedTotalSeconds = -1;

        private void OnEnable()
        {
            elapsedSeconds = 0f;
            displayedTotalSeconds = -1;
            Refresh();
        }

        private void Update()
        {
            elapsedSeconds += Time.deltaTime;
            Refresh();
        }

        private void Refresh()
        {
            if (elapsedText == null)
                return;

            int totalSeconds = Mathf.FloorToInt(elapsedSeconds);
            if (totalSeconds == displayedTotalSeconds)
                return;

            displayedTotalSeconds = totalSeconds;
            elapsedText.text = $"Elapsed: {totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
