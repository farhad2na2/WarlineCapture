using System;
using UnityEngine;

[CreateAssetMenu(menuName = "WarlineCapture/UI/Loading Tips", fileName = "LoadingTips")]
public sealed class UILoadingTips : ScriptableObject
{
    [SerializeField] private string[] tips = Array.Empty<string>();

    public int Count => tips?.Length ?? 0;

    public string GetTip(int index)
    {
        if (tips == null || tips.Length == 0)
            return string.Empty;

        int safeIndex = Mathf.Abs(index) % tips.Length;
        return tips[safeIndex];
    }

    public string GetRandomTip()
    {
        if (tips == null || tips.Length == 0)
            return string.Empty;

        return tips[UnityEngine.Random.Range(0, tips.Length)];
    }
}
