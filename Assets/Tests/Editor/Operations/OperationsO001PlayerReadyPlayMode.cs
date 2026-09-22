#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsO001PlayerReadyPlayMode
    {
        public const string AriaFlagName = "aria-regular-en.txt";

        static string ProfileDirectory() =>
            Path.Combine(Application.persistentDataPath, "OperationsO001Player");

        [MenuItem("Operations/P4R/Arm Regular EN Aria")]
        public static void ArmRegularEnAria()
        {
            string directory = ProfileDirectory();
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, AriaFlagName), "1102\n");
            Debug.Log(
                "[OperationsO001PlayerShell] Regular EN Aria is armed. From the Ops dashboard press the D01 district, confirm Raid to deploy into the match, then use Select, Move, Attack, Scan, Hold, Board, and Continue.");
        }

        [MenuItem("Operations/P4R/Clear Aria Arm")]
        public static void ClearAriaArm()
        {
            string path = Path.Combine(ProfileDirectory(), AriaFlagName);
            if (File.Exists(path))
                File.Delete(path);
            Debug.Log("[OperationsO001PlayerShell] Regular EN Aria arm cleared. Manual play uses the same district, raid, and match controls.");
        }
    }
}
#endif
