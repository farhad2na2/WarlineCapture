#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnivelerCode.GpuAnimation.Editor.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class WarlineCaptureCharacterGpuBakeUtility
{
    private const string CombinedSkinnedFolder = "Assets/Game/Prefabs/Generated/CombinedSkinned";
    private const string CharacterBakeOutputFolder = "Assets/Game/Prefabs/Generated";
    private const string CharacterBakeBatchName = "CharactersBaked";
    private const string InstanceShaderPath = "Packages/com.sniveler-code.gpu-animation/Runtime/Shaders/InstanceShader.shadergraph";
    private const int CharacterBatchTextureSize = 4096;
    private const int CharacterClipFps = 60;
    private const int CharacterLodPercent = 10;
    private static readonly UnitAnimationKind[] CharacterAnimationOrder =
    {
        UnitAnimationKind.Idle,
        UnitAnimationKind.Aim,
        UnitAnimationKind.Shoot,
        UnitAnimationKind.Grenade,
        UnitAnimationKind.Walk,
        UnitAnimationKind.WalkAim,
        UnitAnimationKind.WalkShoot,
        UnitAnimationKind.Run,
        UnitAnimationKind.RunAim,
        UnitAnimationKind.RunShoot,
        UnitAnimationKind.Reload,
        UnitAnimationKind.Death01,
        UnitAnimationKind.Death02,
        UnitAnimationKind.Death03
    };

    [MenuItem("Tools/WarlineCapture/Rendering/Rebuild Flat Soldier Animations And GPU Characters")]
    public static void RebuildFlatSoldierAnimationsAndGpuCharacters()
    {
        SoldierAnimatorFlattener.GenerateAllCharactersAllWeaponsFlatControllersMenu();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RebuildGpuCharactersOnly();
    }

    [MenuItem("Tools/WarlineCapture/Rendering/Rebuild GPU Characters Only")]
    public static void RebuildGpuCharactersOnly()
    {
        Shader instanceShader = AssetDatabase.LoadAssetAtPath<Shader>(InstanceShaderPath);
        if (instanceShader == null)
            throw new InvalidOperationException($"GPU character bake shader was not found at '{InstanceShaderPath}'.");

        List<PrefabInstance> prefabs = BuildCharacterPrefabInstances();
        GenerateProcessor.Generate(
            CharacterBakeOutputFolder,
            CharacterBakeBatchName,
            instanceShader,
            prefabs,
            CharacterBatchTextureSize,
            batchTextureReadable: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WarlineCaptureCharacterGpuBakeUtility] Rebuilt {prefabs.Count} GPU character prefabs.");
    }

    public static List<PrefabInstance> BuildCharacterPrefabInstances()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { CombinedSkinnedFolder });
        var prefabPaths = prefabGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.Ordinal)
            .ToList();

        if (prefabPaths.Count == 0)
            throw new InvalidOperationException($"No combined skinned character prefabs were found under '{CombinedSkinnedFolder}'.");

        var result = new List<PrefabInstance>(prefabPaths.Count);
        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Could not load combined skinned character prefab '{prefabPath}'.");

            SkinnedMeshRenderer skin = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin == null)
                throw new InvalidOperationException($"Combined skinned character prefab '{prefabPath}' has no SkinnedMeshRenderer.");

            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController is not AnimatorController controller)
                throw new InvalidOperationException($"Combined skinned character prefab '{prefabPath}' has no AnimatorController.");

            var instance = new PrefabInstance
            {
                Source = prefab,
                Animator = controller
            };
            instance.SetSkin(skin);
            if (instance.Lods.Count > 0)
                instance.Lods[0].Percent = CharacterLodPercent;

            instance.SubAlpha = new List<bool>();
            foreach (Material material in skin.sharedMaterials)
            {
                float alphaClip = material != null && material.HasProperty(GenerateProcessor.AlphaClip)
                    ? material.GetFloat(GenerateProcessor.AlphaClip)
                    : 0f;
                instance.SubAlpha.Add(math.abs(alphaClip - 1f) < 0.1f);
            }

            Dictionary<string, AnimatorState> stateByName = EnumerateStates(controller)
                .Where(child => child.state != null)
                .GroupBy(child => child.state.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().state, StringComparer.Ordinal);

            foreach (UnitAnimationKind animationKind in CharacterAnimationOrder)
            {
                AnimatorState state = ResolveAnimationState(stateByName, animationKind);
                AnimationClip sourceClip = ResolveStateClip(state);
                if (sourceClip == null)
                {
                    state = ResolveFallbackAnimationState(stateByName, animationKind);
                    sourceClip = ResolveStateClip(state);
                }

                if (sourceClip == null)
                    throw new InvalidOperationException(
                        $"Combined skinned character prefab '{prefabPath}' could not resolve an animation clip for '{animationKind}'.");

                instance.Clips.Add(new ClipInstance
                {
                    Enable = true,
                    Fps = CharacterClipFps,
                    Speed = 1f,
                    StateName = state.name,
                    SourceClip = TryLoadGeneratedComboClip(controller, state.name) ?? sourceClip
                });
            }

            if (instance.Clips.Count == 0)
                throw new InvalidOperationException($"Combined skinned character prefab '{prefabPath}' resolved no animation clips.");

            result.Add(instance);
        }

        return result;
    }

    private static AnimationClip ResolveStateClip(AnimatorState state)
    {
        if (state == null || state.motion == null)
            return null;

        if (state.motion is AnimationClip animationClip)
            return animationClip;

        return state.motion is BlendTree blendTree
            ? ResolveBlendTreeClip(blendTree)
            : null;
    }

    private static AnimatorState ResolveAnimationState(
        IReadOnlyDictionary<string, AnimatorState> stateByName,
        UnitAnimationKind animationKind)
    {
        foreach (string candidate in GetStateNameCandidates(animationKind))
        {
            if (stateByName.TryGetValue(candidate, out AnimatorState state))
                return state;
        }

        return null;
    }

    private static AnimatorState ResolveFallbackAnimationState(
        IReadOnlyDictionary<string, AnimatorState> stateByName,
        UnitAnimationKind animationKind)
    {
        UnitAnimationKind[] fallbacks = animationKind switch
        {
            UnitAnimationKind.Aim => new[] { UnitAnimationKind.Idle },
            UnitAnimationKind.Shoot => new[] { UnitAnimationKind.Aim, UnitAnimationKind.Idle },
            UnitAnimationKind.Grenade => new[] { UnitAnimationKind.Shoot, UnitAnimationKind.Idle },
            UnitAnimationKind.WalkAim => new[] { UnitAnimationKind.Walk, UnitAnimationKind.Aim, UnitAnimationKind.Idle },
            UnitAnimationKind.WalkShoot => new[] { UnitAnimationKind.WalkAim, UnitAnimationKind.Walk, UnitAnimationKind.Shoot, UnitAnimationKind.Idle },
            UnitAnimationKind.RunAim => new[] { UnitAnimationKind.Run, UnitAnimationKind.Aim, UnitAnimationKind.Idle },
            UnitAnimationKind.RunShoot => new[] { UnitAnimationKind.RunAim, UnitAnimationKind.Run, UnitAnimationKind.Shoot, UnitAnimationKind.Idle },
            UnitAnimationKind.Reload => new[] { UnitAnimationKind.Idle },
            UnitAnimationKind.Death01 => new[] { UnitAnimationKind.Death02, UnitAnimationKind.Death03, UnitAnimationKind.Idle },
            UnitAnimationKind.Death02 => new[] { UnitAnimationKind.Death01, UnitAnimationKind.Death03, UnitAnimationKind.Idle },
            UnitAnimationKind.Death03 => new[] { UnitAnimationKind.Death01, UnitAnimationKind.Death02, UnitAnimationKind.Idle },
            _ => new[] { UnitAnimationKind.Idle }
        };

        for (int i = 0; i < fallbacks.Length; i++)
        {
            AnimatorState state = ResolveAnimationState(stateByName, fallbacks[i]);
            if (state != null)
                return state;
        }

        return stateByName.Values.FirstOrDefault();
    }

    private static string[] GetStateNameCandidates(UnitAnimationKind animationKind)
    {
        return animationKind switch
        {
            UnitAnimationKind.Idle => new[] { "Idle" },
            UnitAnimationKind.Aim => new[] { "Aim" },
            UnitAnimationKind.Shoot => new[] { "Shoot" },
            UnitAnimationKind.Grenade => new[] { "Grenade" },
            UnitAnimationKind.Walk => new[] { "Walk" },
            UnitAnimationKind.WalkAim => new[] { "WalkAim" },
            UnitAnimationKind.WalkShoot => new[] { "WalkShoot" },
            UnitAnimationKind.Run => new[] { "Run" },
            UnitAnimationKind.RunAim => new[] { "RunAim" },
            UnitAnimationKind.RunShoot => new[] { "RunShoot" },
            UnitAnimationKind.Reload => new[] { "Reload" },
            UnitAnimationKind.Death01 => new[] { "Death01" },
            UnitAnimationKind.Death02 => new[] { "Death02" },
            UnitAnimationKind.Death03 => new[] { "Death03" },
            _ => new[] { animationKind.ToString() }
        };
    }

    private static AnimationClip ResolveBlendTreeClip(BlendTree blendTree)
    {
        foreach (ChildMotion child in blendTree.children)
        {
            if (child.motion is AnimationClip clip)
                return clip;

            if (child.motion is BlendTree childTree)
            {
                AnimationClip nestedClip = ResolveBlendTreeClip(childTree);
                if (nestedClip != null)
                    return nestedClip;
            }
        }

        return null;
    }

    private static IEnumerable<ChildAnimatorState> EnumerateStates(AnimatorController animator)
    {
        if (animator == null || animator.layers == null)
            yield break;

        foreach (AnimatorControllerLayer layer in animator.layers)
        {
            if (layer.stateMachine == null)
                continue;

            foreach (ChildAnimatorState state in EnumerateStates(layer.stateMachine))
                yield return state;
        }
    }

    private static IEnumerable<ChildAnimatorState> EnumerateStates(AnimatorStateMachine stateMachine)
    {
        foreach (ChildAnimatorState state in stateMachine.states)
            yield return state;

        foreach (ChildAnimatorStateMachine childMachine in stateMachine.stateMachines)
        {
            if (childMachine.stateMachine == null)
                continue;

            foreach (ChildAnimatorState state in EnumerateStates(childMachine.stateMachine))
                yield return state;
        }
    }

    private static AnimationClip TryLoadGeneratedComboClip(AnimatorController animator, string clipName)
    {
        string controllerPath = AssetDatabase.GetAssetPath(animator);
        string controllerName = Path.GetFileNameWithoutExtension(controllerPath);
        string genderPrefix = controllerName.IndexOf("HumanF", StringComparison.OrdinalIgnoreCase) >= 0 ? "HumanF" : "HumanM";
        string weaponSuffix = "Rifle";
        const string flatToken = "_Flat_";
        int flatIndex = controllerName.IndexOf(flatToken, StringComparison.OrdinalIgnoreCase);
        if (flatIndex >= 0)
            weaponSuffix = controllerName[(flatIndex + flatToken.Length)..];

        string clipPath = $"Assets/Game/Animations/FlatGenerated/{genderPrefix}_{weaponSuffix}/{clipName}.anim";
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
    }
}

#endif
