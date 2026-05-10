using System;
using UnityEngine;

public sealed class M01InfantryOnlyHudScopeController : MonoBehaviour
{
    [SerializeField] private GameObject[] hiddenDuringM01 = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] shownDuringM01 = Array.Empty<GameObject>();

    public bool IsM01ScopeActive { get; private set; }
    public int HiddenRootCount => hiddenDuringM01 != null ? hiddenDuringM01.Length : 0;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        bool m01Active = WarlineCaptureMissionSession.HasActiveMission &&
            WarlineCaptureMissionSession.ActiveMissionId == ChapterOneMissionCatalog.FirstContactMissionId;
        IsM01ScopeActive = m01Active;

        SetActive(hiddenDuringM01, !m01Active);
        SetActive(shownDuringM01, true);
    }

    public bool AreM01SuppressedRootsHidden()
    {
        if (!IsM01ScopeActive || hiddenDuringM01 == null)
            return false;

        for (int i = 0; i < hiddenDuringM01.Length; i++)
        {
            GameObject root = hiddenDuringM01[i];
            if (root != null && root.activeSelf)
                return false;
        }

        return true;
    }

    private static void SetActive(GameObject[] roots, bool active)
    {
        if (roots == null)
            return;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
                roots[i].SetActive(active);
        }
    }
}
