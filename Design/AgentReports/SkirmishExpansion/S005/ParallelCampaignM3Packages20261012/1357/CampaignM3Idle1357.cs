using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CampaignM3Idle1357
{
    public static string Run()
    {
        int dirty=0;for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)dirty++;
        return EditorApplication.isPlaying+"|"+EditorApplication.isPlayingOrWillChangePlaymode+"|"+EditorApplication.isCompiling+"|"+EditorApplication.isUpdating+"|"+Game.Editor.ExistingEditorValidation.IsRunning+"|"+Application.dataPath+"|"+dirty;
    }
}
