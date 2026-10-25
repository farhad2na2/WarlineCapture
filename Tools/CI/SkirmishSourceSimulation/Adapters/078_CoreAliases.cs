extern alias ActualCore;
global using Unity.Jobs;
global using Vector3 = ActualCore::UnityEngine.Vector3;
global using RectInt = ActualCore::UnityEngine.RectInt;
global using Vector2 = ActualCore::UnityEngine.Vector2;
global using Vector2Int = ActualCore::UnityEngine.Vector2Int;
namespace Unity.Burst{[System.AttributeUsage(System.AttributeTargets.Struct|System.AttributeTargets.Method)]public sealed class BurstCompileAttribute:System.Attribute{}}
namespace Unity.Entities{
 public interface ISystem{} public partial struct SystemState{public bool Enabled;public EntityManager EntityManager;} public sealed class SimulationSystemGroup{}public sealed class PresentationSystemGroup{}
 [System.AttributeUsage(System.AttributeTargets.Struct)]public sealed class DisableAutoCreationAttribute:System.Attribute{}
 [System.AttributeUsage(System.AttributeTargets.Struct)]public sealed class UpdateInGroupAttribute:System.Attribute{public bool OrderFirst,OrderLast;public UpdateInGroupAttribute(System.Type type){}}
}
namespace UnityEngine{public static class Debug{public static void Log(object x)=>System.Console.Error.WriteLine(x);public static void LogWarning(object x)=>System.Console.Error.WriteLine(x);public static void LogError(object x)=>System.Console.Error.WriteLine(x);public static void LogError(object x,object context)=>System.Console.Error.WriteLine(x);}
public static class Mathf{
 public static float Max(float a,float b)=>ActualCore::UnityEngine.Mathf.Max(a,b);
 public static int Max(int a,int b)=>ActualCore::UnityEngine.Mathf.Max(a,b);
 public const float Deg2Rad=ActualCore::UnityEngine.Mathf.Deg2Rad;public static float Sin(float x)=>ActualCore::UnityEngine.Mathf.Sin(x);public static float Floor(float x)=>ActualCore::UnityEngine.Mathf.Floor(x);public static float Clamp(float x,float lo,float hi)=>ActualCore::UnityEngine.Mathf.Clamp(x,lo,hi);public static int Clamp(int x,int lo,int hi)=>ActualCore::UnityEngine.Mathf.Clamp(x,lo,hi);public static float Clamp01(float x)=>ActualCore::UnityEngine.Mathf.Clamp01(x);public static float Min(float a,float b)=>ActualCore::UnityEngine.Mathf.Min(a,b);public static int Min(int a,int b)=>ActualCore::UnityEngine.Mathf.Min(a,b);
 public static int CeilToInt(float f)=>ActualCore::UnityEngine.Mathf.CeilToInt(f);public static float Abs(float x)=>ActualCore::UnityEngine.Mathf.Abs(x);public static int Abs(int x)=>ActualCore::UnityEngine.Mathf.Abs(x);public static int FloorToInt(float f)=>ActualCore::UnityEngine.Mathf.FloorToInt(f);
 public static int RoundToInt(float f)=>ActualCore::UnityEngine.Mathf.RoundToInt(f);
}}
