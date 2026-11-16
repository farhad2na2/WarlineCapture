namespace UnityEngine{
 public static class Time{
  public static int frameCount;public static float time,deltaTime,unscaledTime,unscaledDeltaTime,timeScale;
  static readonly System.Diagnostics.Stopwatch performance=System.Diagnostics.Stopwatch.StartNew();static double performanceEpoch;
  // Actual owned backend performance time. Independent of the virtual gameplay clock
  // and of the native recorded initial realtime epoch. Never a native future input.
  public static double realtimeSinceStartupAsDouble{get=>performanceEpoch+performance.Elapsed.TotalSeconds;set=>performanceEpoch=value-performance.Elapsed.TotalSeconds;}
 }
}
