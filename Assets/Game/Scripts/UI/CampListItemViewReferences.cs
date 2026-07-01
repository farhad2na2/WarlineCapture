using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class CampListItemViewReferences : MonoBehaviour
    {
        public Button button;
        public Image portraitImage;
        public GameObject selectedRoot;
        public TMP_Text selectedName;
        public Graphic clickTarget;

        private void OnValidate()
        {
            if (selectedName != null)
                selectedName.maskable = true;
        }

        private void Awake()
        {
            if (selectedName != null)
                selectedName.maskable = true;
        }
    }
}
