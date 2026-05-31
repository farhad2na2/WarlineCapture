using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SoldierAnimationRootMotionContractTests
{
    [Test]
    public void FlatGeneratedSoldierClipsDoNotContainHumanoidRootMotionCurves()
    {
        string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Game/Animations/FlatGenerated" });
        Assert.Greater(clipGuids.Length, 0, "Expected generated flat soldier animation clips to exist.");

        string[] offenders = clipGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            .Where(ClipContainsHumanoidRootMotionCurve)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "ECS controls unit translation and rotation. Generated soldier clips must stay in-place and must not contain RootT/RootQ curves: " +
            string.Join(", ", offenders.Select(Path.GetFileName)));
    }

    [Test]
    public void GpuAnimationBakerWritesRendererLocalBoneMatrices()
    {
        string source = File.ReadAllText("Packages/com.sniveler-code.gpu-animation/Editor/Scripts/GenerateProcessor.cs");
        StringAssert.Contains(
            "renderer.transform.worldToLocalMatrix",
            source,
            "GPU animation baking must cancel sampled root-object movement so baked vertices remain local to the ECS entity.");
    }

    [Test]
    public void GpuAnimationShaderSamplesIntegerTexelCenters()
    {
        string source = File.ReadAllText("Packages/com.sniveler-code.gpu-animation/Runtime/Shaders/InstanceShader.hlsl");
        StringAssert.Contains("uint row = pixel_index / width;", source);
        StringAssert.Contains("(float2(col, row) + 0.5) * anim_texture.texelSize.xy", source);
    }

    [Test]
    public void CharacterGpuBakeUtilityUsesStableCombinedSkinnedOrder()
    {
        var instances = WarlineCaptureCharacterGpuBakeUtility.BuildCharacterPrefabInstances();

        Assert.GreaterOrEqual(instances.Count, 33);
        Assert.AreEqual("SM_Chr_Bombsuit_Male_01_CombinedSkinned", instances[0].Source.name);
        Assert.AreEqual("SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned", instances[31].Source.name);
        Assert.AreEqual("SM_Chr_Soldier_Male_02_CombinedSkinned", instances[32].Source.name);
    }

    [Test]
    public void CharacterGpuBakeUtilityEmitsUnitAnimationKindSlotOrder()
    {
        var instances = WarlineCaptureCharacterGpuBakeUtility.BuildCharacterPrefabInstances();

        string[] expectedStateNames =
        {
            "Idle",
            "Aim",
            "Shoot",
            "Grenade",
            "Walk",
            "WalkAim",
            "WalkShoot",
            "Run",
            "RunAim",
            "RunShoot",
            "Reload",
            "Death01",
            "Death02",
            "Death03"
        };

        var soldier = instances.First(instance => instance.Source.name == "SM_Chr_Soldier_Male_02_CombinedSkinned");
        CollectionAssert.AreEqual(expectedStateNames, soldier.Clips.Select(clip => clip.StateName).ToArray());
        Assert.IsTrue(instances.All(instance => instance.Clips.Count == expectedStateNames.Length));
    }

    [Test]
    public void UnitAnimationIndexSystemUsesStableEnumSlots()
    {
        string source = File.ReadAllText("Assets/Game/Scripts/Systems/UnitAnimationIndexSystem.cs");
        StringAssert.Contains("animationIndex = (byte)(preferred + 1);", source);
        StringAssert.DoesNotContain("animationIndex = (byte)(slotIndex + 1);", source);
    }

    [Test]
    public void GpuAnimationAnimatorDoesNotSkipTransitionCompletionRenderFrame()
    {
        string source = File.ReadAllText("Packages/com.sniveler-code.gpu-animation/Runtime/Scripts/Systems/MaterialAnimatorProcess.cs");
        StringAssert.Contains("data.RenderConfig = renderConfig;", source);
        StringAssert.Contains("data.AnimationIndex = targetIndex;", source);
        StringAssert.DoesNotContain("data.TransitionTime = 0f;\n                        return;", source);
        StringAssert.Contains("math.fmod(time * frameRate, frames)", source);
    }

    private static bool ClipContainsHumanoidRootMotionCurve(string assetPath)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (clip == null)
            return false;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            string property = binding.propertyName ?? string.Empty;
            if (property.IndexOf("RootT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                property.IndexOf("RootQ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
