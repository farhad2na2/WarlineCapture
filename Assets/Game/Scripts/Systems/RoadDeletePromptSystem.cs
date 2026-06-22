using System;
using UnityEngine;

public sealed class RoadDeletePromptSystem
{
    public struct Context
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RoadBuildSessionSystem SessionSystem;
        public readonly RoadBuildSessionSystem.State SessionState;
        public readonly Action<int> DeleteStroke;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RoadBuildSessionSystem sessionSystem,
            RoadBuildSessionSystem.State sessionState,
            Action<int> deleteStroke)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            SessionSystem = sessionSystem;
            SessionState = sessionState;
            DeleteStroke = deleteStroke;
        }
    }

    public void OnGui(Context context)
    {
        if (!context.RuntimeGameplayStateSystem.PlayRequested ||
            !context.RuntimeGameplayStateSystem.BuildModeActive ||
            !context.SessionSystem.HasDeletePrompt(context.SessionState))
        {
            return;
        }

        const int deleteRoadWindowId = 12001;
        const float width = 320f;
        const float height = 150f;
        Rect windowRect = new(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);

        GUI.ModalWindow(deleteRoadWindowId, windowRect, windowId => DrawDeleteWindow(context, windowId), "Delete Road");
    }

    private void DrawDeleteWindow(Context context, int windowId)
    {
        GUILayout.Space(12f);
        GUILayout.Label(context.SessionSystem.GetDeletePromptMessage(context.SessionState, "Delete this road?"));
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Delete", GUILayout.Height(32f)))
        {
            if (context.SessionSystem.TryGetDeleteStrokeId(context.SessionState, out int strokeId))
                context.DeleteStroke?.Invoke(strokeId);

            ClearDeletePrompt(context);
        }

        if (GUILayout.Button("Cancel", GUILayout.Height(32f)))
            ClearDeletePrompt(context);

        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
    }

    private void ClearDeletePrompt(Context context)
    {
        context.SessionSystem.ClearDeletePrompt(context.SessionState);
    }
}
