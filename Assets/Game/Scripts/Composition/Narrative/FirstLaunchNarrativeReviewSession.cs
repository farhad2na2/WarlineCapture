using UnityEngine;

namespace Game.Composition
{
    public static class FirstLaunchNarrativeReviewSession
    {
        private const string RequestKey = "WarlineCapture.FirstLaunch.ReviewerRequested";

        public static void Request()
        {
#if UNITY_EDITOR
            PlayerPrefs.SetInt(RequestKey, 1);
            PlayerPrefs.Save();
#endif
        }

        internal static bool ConsumeRequest()
        {
#if UNITY_EDITOR
            bool requested = PlayerPrefs.GetInt(RequestKey, 0) == 1;
            PlayerPrefs.DeleteKey(RequestKey);
            return requested;
#else
            return false;
#endif
        }
    }
}
