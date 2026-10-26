using Unity.Entities;
namespace Game.Runtime{
 internal readonly struct UnitDeathMechanicalClipInput{internal readonly int Frames;internal readonly byte Fps,Speed;internal UnitDeathMechanicalClipInput(int frames,byte fps,byte speed){Frames=frames;Fps=fps;Speed=speed;}}
 internal static partial class UnitDeathAnimationPlaybackUtility{
 internal delegate bool QualifiedHeadlessReader(EntityManager manager,Entity unit,float configured,byte deathIndex,out UnitDeathMechanicalClipInput[][] clipArrays);
 internal static QualifiedHeadlessReader QualifiedHeadlessClipReader;
 internal static bool ResolveActualDeathIndex(EntityManager manager,Entity unit,out byte index)=>TryResolveDeathAnimationIndex(manager,unit,out index);
 internal static float AccumulateMechanicalClip(float duration,UnitDeathMechanicalClipInput clip)=>UnitDeathLifecycleKernel.AccumulatePlaybackDuration(duration,UnitDeathLifecycleKernel.ResolveSinglePlaybackDuration(clip.Frames,clip.Fps,clip.Speed,duration));
 internal static float ResolveQualifiedMechanicalPlayback(float configured,byte deathIndex,UnitDeathMechanicalClipInput[][] clipArrays){float duration=UnitDeathLifecycleKernel.ClampPlaybackDuration(configured);foreach(var clips in clipArrays){if(clips==null||clips.Length==0)continue;duration=AccumulateMechanicalClip(duration,clips[deathIndex%clips.Length]);}return UnitDeathLifecycleKernel.ClampPlaybackDuration(duration);}
 }
}