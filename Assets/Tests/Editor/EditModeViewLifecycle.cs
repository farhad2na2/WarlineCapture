using System;
using System.Reflection;
using UnityEngine;

internal static class EditModeViewLifecycle
{
    public static void Invoke(Component target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null) throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, null);
    }
}
