using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    public sealed class V3ToggleCheckView : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private GameObject checkRoot;

        public void Configure(Toggle source, GameObject visual)
        {
            toggle = source;
            checkRoot = visual;
            Refresh(toggle != null && toggle.isOn);
        }

        private void OnEnable()
        {
            if (toggle == null)
                toggle = GetComponent<Toggle>();
            if (toggle != null)
                toggle.onValueChanged.AddListener(Refresh);
            Refresh(toggle != null && toggle.isOn);
        }

        private void OnDisable()
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(Refresh);
        }

        private void Refresh(bool isOn)
        {
            if (checkRoot != null && checkRoot.activeSelf != isOn)
                checkRoot.SetActive(isOn);
        }
    }
}
