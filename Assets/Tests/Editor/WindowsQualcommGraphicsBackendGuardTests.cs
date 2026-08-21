using Game.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class WindowsQualcommGraphicsBackendGuardTests
{
    [TestCase("Qualcomm(R) Adreno(TM) X1-85 GPU")]
    [TestCase("Adreno X1 GPU")]
    public void RequiresD3D11_QualcommAdrenoOnWindowsD3D12_ReturnsTrue(string deviceName)
    {
        Assert.IsTrue(WindowsQualcommGraphicsBackendGuard.RequiresD3D11(
            OperatingSystemFamily.Windows,
            GraphicsDeviceType.Direct3D12,
            deviceName,
            isBatchMode: false));
    }

    [Test]
    public void RequiresD3D11_D3D11Backend_ReturnsFalse()
    {
        Assert.IsFalse(WindowsQualcommGraphicsBackendGuard.RequiresD3D11(
            OperatingSystemFamily.Windows,
            GraphicsDeviceType.Direct3D11,
            "Qualcomm(R) Adreno(TM) X1-85 GPU",
            isBatchMode: false));
    }

    [Test]
    public void RequiresD3D11_NonQualcommD3D12Device_ReturnsFalse()
    {
        Assert.IsFalse(WindowsQualcommGraphicsBackendGuard.RequiresD3D11(
            OperatingSystemFamily.Windows,
            GraphicsDeviceType.Direct3D12,
            "NVIDIA GeForce RTX 4080",
            isBatchMode: false));
    }

    [Test]
    public void RequiresD3D11_BatchMode_ReturnsFalse()
    {
        Assert.IsFalse(WindowsQualcommGraphicsBackendGuard.RequiresD3D11(
            OperatingSystemFamily.Windows,
            GraphicsDeviceType.Direct3D12,
            "Qualcomm(R) Adreno(TM) X1-85 GPU",
            isBatchMode: true));
    }

    [Test]
    public void RequiresPersistentD3D11Preference_DefaultGraphicsApis_ReturnsTrue()
    {
        Assert.IsTrue(WindowsQualcommGraphicsBackendGuard.RequiresPersistentD3D11Preference(
            useDefaultGraphicsApis: true,
            new[] { GraphicsDeviceType.Direct3D11 }));
    }

    [Test]
    public void RequiresPersistentD3D11Preference_D3D12First_ReturnsTrue()
    {
        Assert.IsTrue(WindowsQualcommGraphicsBackendGuard.RequiresPersistentD3D11Preference(
            useDefaultGraphicsApis: false,
            new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 }));
    }

    [Test]
    public void RequiresPersistentD3D11Preference_D3D11First_ReturnsFalse()
    {
        Assert.IsFalse(WindowsQualcommGraphicsBackendGuard.RequiresPersistentD3D11Preference(
            useDefaultGraphicsApis: false,
            new[] { GraphicsDeviceType.Direct3D11 }));
    }
}
