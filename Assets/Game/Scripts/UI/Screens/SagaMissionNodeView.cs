using UnityEngine;

public sealed class SagaMissionNodeView : MonoBehaviour
{
    [SerializeField] private string missionId;
    [SerializeField] private int chapterIndex;
    [SerializeField] private int missionIndex;
    [SerializeField] private bool locked;
    [SerializeField] private string lockedReason;

    public string MissionId => missionId;
    public int ChapterIndex => chapterIndex;
    public int MissionIndex => missionIndex;
    public bool Locked => locked;
    public string LockedReason => lockedReason;
}
