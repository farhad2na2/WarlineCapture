using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class UIShellContentView
    {
        [SerializeField] private GameObject threatAlertPopupPrefab;
        [SerializeField] private GameObject missionFieldGuidePrefab;
        private GameObject _threatAlertPopupInstance;
        private GameObject _missionFieldGuideInstance;
    }
}
