#if UNITY_EDITOR
using System.Runtime.InteropServices;using Game.Components;using Unity.Burst;using Unity.Mathematics;using Unity.Transforms;
namespace Game.Runtime {public partial struct GroundMissileFlyingRocketVisualSystem {
 public struct ReferenceNumericObservation {public int BurstExecutionMarker;public float ElapsedSeconds,Duration,T,NextT,Scale;public float3 Position,NextPosition,Direction;public quaternion Rotation;}
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)]public delegate void ReferenceNumericDelegate(ref GroundMissileFlyingRocketVisualComponent input,float deltaTime,out ReferenceNumericObservation result);
 // Diagnostic-only prefix copied literally from original FlyingRocketVisualJob.Execute.
 // Actual owner bodies are unchanged; ECB/lookup restoration behavior remains exclusively in the real owner.
 public static ReferenceNumericObservation ReferenceEvaluateNumericPrefix(GroundMissileFlyingRocketVisualComponent flying,float DeltaTime){LocalTransform transform=default;
                flying.ElapsedSeconds += DeltaTime;
                float duration = math.max(0.01f, flying.DurationSeconds);
                float t = math.saturate(flying.ElapsedSeconds / duration);
                float3 position = EvaluateRocketArc(flying, t);

                float nextT = math.saturate((flying.ElapsedSeconds + math.min(0.05f, DeltaTime)) / duration);
                float3 nextPosition = EvaluateRocketArc(flying, nextT);
                float3 direction = math.normalizesafe(nextPosition - position, new float3(0f, 0f, 1f));

                transform.Position = position;
                transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
                transform.Scale = math.max(0.0001f, flying.InitialLocalScale);

 return new ReferenceNumericObservation{ElapsedSeconds=flying.ElapsedSeconds,Duration=duration,T=t,NextT=nextT,Position=position,NextPosition=nextPosition,Direction=direction,Rotation=transform.Rotation,Scale=transform.Scale};}
 static FunctionPointer<ReferenceNumericDelegate> ReferenceCompiledFunction;
 public static bool ReferenceExplicitBurstEnabled=>BurstCompiler.Options.EnableBurstCompilation;
 public static void ReferenceRunExplicitBurst(ref GroundMissileFlyingRocketVisualComponent input,float deltaTime,out ReferenceNumericObservation result){if(!ReferenceExplicitBurstEnabled)throw new System.InvalidOperationException("Native Burst disabled");if(!ReferenceCompiledFunction.IsCreated)ReferenceCompiledFunction=BurstCompiler.CompileFunctionPointer<ReferenceNumericDelegate>(ReferenceEvaluateNumericBurst);ReferenceCompiledFunction.Invoke(ref input,deltaTime,out result);if(result.BurstExecutionMarker!=1)throw new System.InvalidOperationException("Explicit Burst primitive executed managed fallback; numeric witness is unqualified");}
 [BurstDiscard]static void ReferenceManagedOnlyMarker(ref int marker){marker=0;}
 [BurstCompile(CompileSynchronously=true,DisableDirectCall=true)]public static void ReferenceEvaluateNumericBurst(ref GroundMissileFlyingRocketVisualComponent input,float deltaTime,out ReferenceNumericObservation result){result=ReferenceEvaluateNumericPrefix(input,deltaTime);int marker=1;ReferenceManagedOnlyMarker(ref marker);result.BurstExecutionMarker=marker;}
}}
#endif
