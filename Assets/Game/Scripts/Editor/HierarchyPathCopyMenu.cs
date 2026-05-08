using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HierarchyPathCopyMenu
{
    [MenuItem("GameObject/Copy Full Path", false, 0)]
    private static void CopyFullPath()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
            return;

        EditorGUIUtility.systemCopyBuffer = GetHierarchyPath(selected.transform);
        Debug.Log($"[HierarchyPath] Copied: {EditorGUIUtility.systemCopyBuffer}");
    }

    [MenuItem("GameObject/Copy Full Path", true)]
    private static bool ValidateCopyFullPath()
    {
        return Selection.activeGameObject != null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        List<string> names = new();
        Transform current = transform;
        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join(" / ", names);
    }
}
