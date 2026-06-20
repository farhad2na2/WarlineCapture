using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class UiToolkitMenuStartupPlayModeTests
{
    [UnityTest]
    public IEnumerator MenuSceneRendersUiToolkitMainMenu()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
        while (load is { isDone: false })
            yield return null;

        for (int frame = 0; frame < 12; frame++)
            yield return null;

        MenuBootstrapView bootstrap =
            Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
        Assert.NotNull(bootstrap, "Menu scene must contain MenuBootstrapView.");
        Assert.NotNull(bootstrap.RuntimeUiConfig, "Menu scene must serialize RuntimeUiConfig.");
        Assert.AreEqual(RuntimeUiMode.UiToolkit, bootstrap.RuntimeUiConfig.Mode, "Menu must boot in UI Toolkit mode.");
        Assert.NotNull(bootstrap.UiToolkitDocument, "Menu scene must serialize a UIDocument.");
        Assert.NotNull(bootstrap.UiToolkitDocument.panelSettings, "UIDocument must have PanelSettings or it can render black.");
        Assert.NotNull(bootstrap.UiToolkitShellView, "Menu scene must serialize UiToolkitShellView.");

        UiToolkitShellView shellView = bootstrap.UiToolkitShellView;
        Assert.IsTrue(shellView.IsMounted || shellView.Mount(), "UI Toolkit shell must mount in PlayMode.");
        Assert.IsTrue(shellView.EnsureMainMenuVisible(UIRoute.MainMenu), "Main Menu must be made visible in PlayMode.");
        Assert.IsFalse(shellView.MainMenuScreenSlot.ClassListContains("shell-hidden"), "Main Menu slot must not be hidden.");
        Assert.IsTrue(
            shellView.MainMenuScreenSlot.ClassListContains(
                UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)),
            "Main Menu slot must have visible motion state.");

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Assert.NotNull(screenshot, "PlayMode screenshot capture returned null.");
        try
        {
            Assert.Greater(EstimateLuma(screenshot), 0.02f, "Rendered Menu frame is effectively black.");
        }
        finally
        {
            Object.Destroy(screenshot);
        }
    }

    private static float EstimateLuma(Texture2D screenshot)
    {
        int stepX = Mathf.Max(1, screenshot.width / 64);
        int stepY = Mathf.Max(1, screenshot.height / 36);
        double total = 0d;
        int samples = 0;
        for (int y = 0; y < screenshot.height; y += stepY)
        {
            for (int x = 0; x < screenshot.width; x += stepX)
            {
                Color color = screenshot.GetPixel(x, y);
                total += (0.2126d * color.r) + (0.7152d * color.g) + (0.0722d * color.b);
                samples++;
            }
        }

        return samples > 0 ? (float)(total / samples) : 0f;
    }
}
