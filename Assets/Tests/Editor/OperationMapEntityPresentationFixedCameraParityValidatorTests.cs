using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapEntityPresentationFixedCameraParityValidatorTests
{
    public static void RunFocusedValidation()
    {
        var suite = new OperationMapEntityPresentationFixedCameraParityValidatorTests();
        Action[] tests =
        {
            suite.Compare_AcceptsIdenticalDetailedPixels,
            suite.Compare_ReportsChangedPixelsAndNormalizedDelta,
            suite.Compare_RejectsMismatchedBufferLengths,
            suite.Percentile_InterpolatesDeterministically
        };
        for (int i = 0; i < tests.Length; i++)
            tests[i]();
        Debug.Log($"[OperationMapFixedCameraParityValidation] result=Passed tests={tests.Length}");
    }

    [Test]
    public void Compare_AcceptsIdenticalDetailedPixels()
    {
        Color32[] pixels =
        {
            new(10, 20, 30, 255),
            new(100, 120, 140, 255),
            new(220, 200, 180, 255)
        };

        OperationMapEntityPresentationFixedCameraParityValidator.PixelComparison comparison =
            OperationMapEntityPresentationFixedCameraParityValidator.Compare(pixels, pixels, 3);

        Assert.That(comparison.meanChannelDelta, Is.Zero);
        Assert.That(comparison.maximumChannelDelta, Is.Zero);
        Assert.That(comparison.changedPixelRatio, Is.Zero);
        Assert.That(comparison.sourceLumaVariance, Is.GreaterThan(0f));
    }

    [Test]
    public void Compare_ReportsChangedPixelsAndNormalizedDelta()
    {
        Color32[] source = { new(0, 0, 0, 255), new(255, 255, 255, 255) };
        Color32[] candidate = { new(0, 0, 0, 255), new(245, 255, 255, 255) };

        OperationMapEntityPresentationFixedCameraParityValidator.PixelComparison comparison =
            OperationMapEntityPresentationFixedCameraParityValidator.Compare(source, candidate, 3);

        Assert.That(comparison.changedPixelRatio, Is.EqualTo(0.5f));
        Assert.That(comparison.maximumChannelDelta, Is.EqualTo(10f / 255f).Within(0.00001f));
        Assert.That(comparison.meanChannelDelta, Is.EqualTo(10f / (2f * 4f * 255f)).Within(0.00001f));
    }

    [Test]
    public void Compare_RejectsMismatchedBufferLengths()
    {
        Assert.That(
            () => OperationMapEntityPresentationFixedCameraParityValidator.Compare(
                new[] { new Color32(0, 0, 0, 255) },
                Array.Empty<Color32>(),
                3),
            Throws.InvalidOperationException);
    }

    [Test]
    public void Percentile_InterpolatesDeterministically()
    {
        float[] values = { 0f, 10f, 20f, 30f, 40f };
        Assert.That(
            OperationMapEntityPresentationFixedCameraParityValidator.Percentile(values, 0.25f),
            Is.EqualTo(10f));
        Assert.That(
            OperationMapEntityPresentationFixedCameraParityValidator.Percentile(values, 0.625f),
            Is.EqualTo(25f));
    }
}
