using UnityEditor;
namespace Game.Editor
{
    public static class MainMenuPersistentResourcesPrefabBuilder
    {
        [MenuItem("Game/UI/Rebuild Main Menu Persistent Resources")]
        public static void Rebuild() => MainMenuV3PrefabBuilder.Build();
    }
}
