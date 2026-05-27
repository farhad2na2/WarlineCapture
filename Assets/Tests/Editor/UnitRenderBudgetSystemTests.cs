using NUnit.Framework;
using UnityEngine;

public sealed class UnitRenderBudgetSystemTests
{
    [Test]
    public void MovingVisibleCharactersDoNotUseStaticFarImpostor()
    {
        UnitRenderVisualKind visual = UnitRenderBudgetSystem.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: true,
            forceDetailNearVisible: false,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            forceTacticalImpostor: false,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: true,
            hasSafeLow: true,
            lowRootAnimatable: true);

        Assert.AreEqual(UnitRenderVisualKind.Mid, visual);
    }

    [Test]
    public void MovingVisibleCharactersFallbackToDetailWhenMeshLodIsNotAnimatable()
    {
        UnitRenderVisualKind visual = UnitRenderBudgetSystem.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: true,
            forceDetailNearVisible: false,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            forceTacticalImpostor: false,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: false,
            hasSafeLow: true,
            lowRootAnimatable: false);

        Assert.AreEqual(UnitRenderVisualKind.Detail, visual);
    }

    [Test]
    public void IdleDistantVisibleCharactersCanUseFarImpostor()
    {
        UnitRenderVisualKind visual = UnitRenderBudgetSystem.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: false,
            forceDetailNearVisible: false,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            forceTacticalImpostor: false,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: true,
            hasSafeLow: true,
            lowRootAnimatable: true);

        Assert.AreEqual(UnitRenderVisualKind.Far, visual);
    }

    [Test]
    public void HighCameraVisibleCharactersUseTacticalFarImpostorEvenWhenMoving()
    {
        UnitRenderVisualKind visual = UnitRenderBudgetSystem.ResolveVisibleCharacterVisualKind(
            movingVisibleCharacter: true,
            forceDetailNearVisible: false,
            forceDetailByBudget: false,
            farEnoughForImpostor: true,
            forceTacticalImpostor: true,
            lowEnoughForSafeLow: true,
            hasSafeMid: true,
            midRootAnimatable: true,
            hasSafeLow: true,
            lowRootAnimatable: true);

        Assert.AreEqual(UnitRenderVisualKind.Far, visual);
    }

    [Test]
    public void CharacterImpostorsScaleUpAtHighTacticalCameraHeight()
    {
        Assert.AreEqual(1f, UnitImpostorRenderSystem.ResolveCharacterTacticalScale(80f), 0.001f);
        Assert.AreEqual(16f, UnitImpostorRenderSystem.ResolveCharacterTacticalScale(200f), 0.001f);
    }

    [Test]
    public void HighCameraCharacterImpostorsFaceCameraPlane()
    {
        Quaternion cameraRotation = Quaternion.Euler(70f, 35f, 0f);
        Quaternion characterRotation = UnitImpostorRenderSystem.ResolveBillboardRotation(
            true,
            Vector3.zero,
            new Vector3(0f, 200f, 0f),
            cameraRotation);
        Quaternion vehicleRotation = UnitImpostorRenderSystem.ResolveBillboardRotation(
            false,
            Vector3.zero,
            new Vector3(0f, 200f, 0f),
            cameraRotation);

        Vector3 expectedCharacterForward = -(cameraRotation * Vector3.forward);
        Assert.Less(Vector3.Angle(expectedCharacterForward, characterRotation * Vector3.forward), 0.1f);
        Assert.Less(Vector3.Angle(Vector3.forward, vehicleRotation * Vector3.forward), 0.1f);
    }
}
