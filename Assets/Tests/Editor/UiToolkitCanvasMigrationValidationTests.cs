#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class UiToolkitCanvasMigrationValidationTests
{
    private const string UiToolkitRoot = "Assets/Game/UI Toolkit";
    private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
    private const string ShellUxmlPath = "Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml";
    private const string ShellUssPath = "Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uss";

    private static readonly Regex UrlRegex = new(
        @"url\(\s*[""']?(?<path>[^""')]+)[""']?\s*\)",
        RegexOptions.CultureInvariant);

    private static readonly string[] OldArtMarkers =
    {
        "Generated/MainMenu/LayeredOneGo",
        "Generated/MainMenuAlt",
        "Art/UI/Final",
        "VisualLockLayered/SCN-02_MainMenu",
        "TargetLockV01",
        "LegacyVisualLock",
        "Generated/MatchHUD/TargetLockV01"
    };

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new UiToolkitCanvasMigrationValidationTests();
            tests.UiToolkitUxmlFilesImport();
            tests.UiToolkitUssFilesImport();
            tests.UiToolkitUssUrlReferencesResolve();
            tests.UiToolkitFilesDoNotReferenceOldArtDirection();
            tests.RuntimeUiConfigExistsAndDefaultsToCanvas();
            tests.MenuBootstrapViewKeepsCanvasFallbackEnabledInCanvasMode();
            tests.MenuBootstrapViewCanEnableIsolatedUiToolkitShellMode();
            tests.UiToolkitShellViewMountsShellUxmlThroughUidocument();
            tests.UiToolkitShellViewBindsRequiredShellRegionsByName();
            tests.UiToolkitShellViewBindsRequiredScreenSlotsByName();
            tests.UiToolkitShellMotionStyleClassesExist();
            tests.UiToolkitShellViewAppliesMotionStateClasses();
            tests.UiToolkitShellLayersRenderAboveNormalContent();
            UnityEngine.Debug.Log("[UiToolkitCanvasMigrationValidation] result=Passed tests=13");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[UiToolkitCanvasMigrationValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void UiToolkitUxmlFilesImport()
    {
        foreach (string path in EnumerateAssets("*.uxml"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(asset, $"UI Toolkit UXML must import as VisualTreeAsset: {path}");
        }
    }

    [Test]
    public void UiToolkitUssFilesImport()
    {
        foreach (string path in EnumerateAssets("*.uss"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            Assert.IsNotNull(asset, $"UI Toolkit USS must import as StyleSheet: {path}");
        }
    }

    [Test]
    public void UiToolkitUssUrlReferencesResolve()
    {
        var missing = new List<string>();

        foreach (string ussPath in EnumerateAssets("*.uss"))
        {
            string source = File.ReadAllText(ussPath);
            foreach (Match match in UrlRegex.Matches(source))
            {
                string value = match.Groups["path"].Value.Trim();
                if (ShouldSkipUrl(value))
                    continue;

                string assetPath = ResolveAssetPath(ussPath, value);
                if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
                    missing.Add($"{ussPath} -> {value}");
            }
        }

        AssertNoViolations(missing, "Every UI Toolkit USS url(...) reference must resolve to an existing asset.");
    }

    [Test]
    public void UiToolkitFilesDoNotReferenceOldArtDirection()
    {
        var violations = new List<string>();

        foreach (string path in EnumerateAssets("*.uxml", "*.uss"))
        {
            string source = File.ReadAllText(path);
            for (int i = 0; i < OldArtMarkers.Length; i++)
            {
                string marker = OldArtMarkers[i];
                if (source.Contains(marker, StringComparison.Ordinal))
                    violations.Add($"{path} -> {marker}");
            }
        }

        AssertNoViolations(violations, "UI Toolkit migration files must not reference old-art-direction assets.");
    }

    [Test]
    public void RuntimeUiConfigExistsAndDefaultsToCanvas()
    {
        RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
        Assert.IsNotNull(config, $"Missing runtime UI config: {RuntimeUiConfigPath}");
        Assert.AreEqual(RuntimeUiMode.Canvas, config.Mode, "Canvas must remain the default runtime UI mode until UI Toolkit parity gates pass.");
        Assert.IsFalse(config.UseUiToolkit, "The default runtime UI config must not enable UI Toolkit yet.");
    }

    [Test]
    public void MenuBootstrapViewKeepsCanvasFallbackEnabledInCanvasMode()
    {
        using var scope = new RuntimeUiModeSmokeScope(RuntimeUiMode.Canvas);

        scope.Canvas.enabled = false;
        scope.UiDocument.enabled = true;
        scope.UiToolkitRoot.SetActive(true);

        scope.BootstrapView.ApplyRuntimeUiMode();

        Assert.IsTrue(scope.Canvas.enabled, "Canvas mode must keep the Canvas fallback enabled.");
        Assert.IsFalse(scope.UiDocument.enabled, "Canvas mode must keep the isolated UI Toolkit document disabled.");
        Assert.IsFalse(scope.UiToolkitRoot.activeSelf, "Canvas mode must keep the isolated UI Toolkit shell root inactive.");
    }

    [Test]
    public void MenuBootstrapViewCanEnableIsolatedUiToolkitShellMode()
    {
        using var scope = new RuntimeUiModeSmokeScope(RuntimeUiMode.UiToolkit);

        scope.Canvas.enabled = true;
        scope.UiDocument.enabled = false;
        scope.UiToolkitRoot.SetActive(false);

        scope.BootstrapView.ApplyRuntimeUiMode();

        Assert.IsFalse(scope.Canvas.enabled, "UI Toolkit mode must disable the Canvas fallback without destroying it.");
        Assert.IsTrue(scope.UiDocument.enabled, "UI Toolkit mode must enable the isolated UI Toolkit document.");
        Assert.IsTrue(scope.UiToolkitRoot.activeSelf, "UI Toolkit mode must enable the isolated UI Toolkit shell root.");
    }

    [Test]
    public void UiToolkitShellViewMountsShellUxmlThroughUidocument()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "UiToolkitShellView must mount the shell VisualTreeAsset through UIDocument.");
            Assert.AreSame(shellAsset, document.visualTreeAsset, "UIDocument must use the UI Toolkit shell UXML.");
            Assert.IsNotNull(shellView.Root, "Mounted shell root must be cached.");
            Assert.IsNotNull(shellView.Root.Q<VisualElement>("SafeAreaRoot"), "Mounted shell must expose SafeAreaRoot.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewBindsRequiredShellRegionsByName()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellRegionSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed only when required regions are bound.");
            Assert.IsTrue(shellView.HasRequiredRegions, "Shell view must cache every required shell region.");
            Assert.IsNotNull(shellView.SafeAreaRoot, "Missing SafeAreaRoot region.");
            Assert.IsNotNull(shellView.HeaderBar, "Missing HeaderBar region.");
            Assert.IsNotNull(shellView.ContentRoot, "Missing ContentRoot region.");
            Assert.IsNotNull(shellView.FooterBar, "Missing FooterBar region.");
            Assert.IsNotNull(shellView.ModalOverlay, "Missing ModalOverlay region.");
            Assert.IsNotNull(shellView.TooltipLayer, "Missing TooltipLayer region.");
            Assert.IsNotNull(shellView.LoadingLayer, "Missing LoadingLayer region.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasRequiredRegions, "ClearCache must clear required-region state.");
            Assert.IsNull(shellView.Root, "ClearCache must clear Root.");
            Assert.IsNull(shellView.SafeAreaRoot, "ClearCache must clear SafeAreaRoot.");
            Assert.IsNull(shellView.ModalOverlay, "ClearCache must clear ModalOverlay.");
            Assert.IsNull(shellView.LoadingLayer, "ClearCache must clear LoadingLayer.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellLayersRenderAboveNormalContent()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellLayerOrderSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating overlay order.");
            Assert.AreSame(shellView.SafeAreaRoot, shellView.ContentRoot.parent, "ContentRoot must be a top-level safe-area child.");
            Assert.AreSame(shellView.SafeAreaRoot, shellView.ModalOverlay.parent, "ModalOverlay must be a top-level safe-area child.");
            Assert.AreSame(shellView.SafeAreaRoot, shellView.LoadingLayer.parent, "LoadingLayer must be a top-level safe-area child.");

            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.ModalOverlay, shellView.ContentRoot, "Popup overlay must draw above normal content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.ModalOverlay, shellView.FooterBar, "Popup overlay must draw above footer content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.ContentRoot, "Loading layer must draw above normal content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.FooterBar, "Loading layer must draw above footer content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.ModalOverlay, "Loading layer must draw above popups.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.TooltipLayer, "Loading layer must draw above tooltip overlays.");

            Assert.IsTrue(shellView.ModalOverlay.ClassListContains("shell-hidden"), "ModalOverlay must stay hidden by default.");
            Assert.IsTrue(shellView.LoadingLayer.ClassListContains("shell-hidden"), "LoadingLayer must stay hidden by default.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewBindsRequiredScreenSlotsByName()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellScreenSlotSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed only when required screen slots are bound.");
            Assert.IsTrue(shellView.HasRequiredScreenSlots, "Shell view must cache every required screen slot.");
            Assert.IsNotNull(shellView.LoadingScreenSlot, "Missing LoadingScreenSlot.");
            Assert.IsNotNull(shellView.MainMenuScreenSlot, "Missing MainMenuScreenSlot.");
            Assert.IsNotNull(shellView.MatchScreenSlot, "Missing MatchScreenSlot.");
            Assert.IsNotNull(shellView.ArmoryScreenSlot, "Missing ArmoryScreenSlot.");
            Assert.IsNotNull(shellView.CommanderProfileScreenSlot, "Missing CommanderProfileScreenSlot.");
            Assert.IsNotNull(shellView.ResultScreenSlot, "Missing ResultScreenSlot.");
            Assert.IsNotNull(shellView.PopupScreenSlot, "Missing PopupScreenSlot.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasRequiredScreenSlots, "ClearCache must clear required-screen-slot state.");
            Assert.IsNull(shellView.LoadingScreenSlot, "ClearCache must clear LoadingScreenSlot.");
            Assert.IsNull(shellView.PopupScreenSlot, "ClearCache must clear PopupScreenSlot.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellMotionStyleClassesExist()
    {
        string source = File.ReadAllText(ShellUssPath);

        Assert.That(source, Does.Contain(".shell-motion "), "Shell USS must define the shared motion base class.");
        Assert.That(source, Does.Contain(".shell-motion-visible"), "Shell USS must define visible motion state.");
        Assert.That(source, Does.Contain(".shell-motion-fade-out"), "Shell USS must define fade-out motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-left-out"), "Shell USS must define left slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-right-out"), "Shell USS must define right slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-top-out"), "Shell USS must define top slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-bottom-out"), "Shell USS must define bottom slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-scale-out"), "Shell USS must define scale-out motion state.");
        Assert.That(source, Does.Contain(".shell-motion-popup-visible"), "Shell USS must define popup visible motion state.");
        Assert.That(source, Does.Contain(".shell-motion-popup-hidden"), "Shell USS must define popup hidden motion state.");
    }

    [Test]
    public void UiToolkitShellViewAppliesMotionStateClasses()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellMotionSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before applying motion states.");

            shellView.ApplyShellMotion(shellView.HeaderBar, UiToolkitShellMotionState.SlideTopOut);

            Assert.IsTrue(shellView.HeaderBar.ClassListContains(UiToolkitShellView.MotionBaseClass), "Motion target must include the motion base class.");
            Assert.IsTrue(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.SlideTopOut)), "Motion target must include the requested state class.");
            Assert.IsFalse(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "Motion target must not retain stale state classes.");

            shellView.ApplyShellMotion(shellView.HeaderBar, UiToolkitShellMotionState.Visible);

            Assert.IsTrue(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "Motion target must switch to visible state.");
            Assert.IsFalse(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.SlideTopOut)), "Motion target must remove previous slide state.");

            shellView.ApplyShellMotion(shellView.PopupScreenSlot, UiToolkitShellMotionState.PopupHidden);

            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupHidden)), "Popup slot must support popup scale hidden state.");

            shellView.RemoveShellMotion(shellView.PopupScreenSlot);

            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.MotionBaseClass), "RemoveShellMotion must clear the base class.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupHidden)), "RemoveShellMotion must clear state classes.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static IEnumerable<string> EnumerateAssets(params string[] patterns)
    {
        for (int i = 0; i < patterns.Length; i++)
        {
            string[] paths = Directory.GetFiles(UiToolkitRoot, patterns[i], SearchOption.AllDirectories);
            Array.Sort(paths, StringComparer.Ordinal);
            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
                yield return NormalizeAssetPath(paths[pathIndex]);
        }
    }

    private static string ResolveAssetPath(string sourcePath, string url)
    {
        string normalized = url.Replace('\\', '/');
        if (normalized.StartsWith("project://database/", StringComparison.Ordinal))
            normalized = normalized.Substring("project://database/".Length);
        if (normalized.StartsWith("/", StringComparison.Ordinal))
            normalized = normalized.TrimStart('/');
        if (normalized.StartsWith("Assets/", StringComparison.Ordinal))
            return normalized;

        string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? UiToolkitRoot;
        string combined = Path.GetFullPath(Path.Combine(sourceDirectory, normalized));
        string projectRoot = Path.GetFullPath(".");
        if (!combined.StartsWith(projectRoot, StringComparison.Ordinal))
            return string.Empty;

        return NormalizeAssetPath(Path.GetRelativePath(projectRoot, combined));
    }

    private static bool ShouldSkipUrl(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.StartsWith("#", StringComparison.Ordinal)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static void AssertDrawsAfter(VisualElement parent, VisualElement upper, VisualElement lower, string message)
    {
        int upperIndex = GetChildIndex(parent, upper);
        int lowerIndex = GetChildIndex(parent, lower);

        Assert.GreaterOrEqual(upperIndex, 0, $"Upper element is not a direct child. {message}");
        Assert.GreaterOrEqual(lowerIndex, 0, $"Lower element is not a direct child. {message}");
        Assert.Greater(upperIndex, lowerIndex, message);
    }

    private static int GetChildIndex(VisualElement parent, VisualElement child)
    {
        int index = 0;
        foreach (VisualElement element in parent.Children())
        {
            if (ReferenceEquals(element, child))
                return index;
            index++;
        }

        return -1;
    }

    private static void AssertNoViolations(List<string> violations, string message)
    {
        violations.Sort(StringComparer.Ordinal);
        Assert.IsEmpty(violations, $"{message}\n{string.Join("\n", violations)}");
    }

    private static RuntimeUiConfig CreateRuntimeUiConfig(RuntimeUiMode mode)
    {
        RuntimeUiConfig config = ScriptableObject.CreateInstance<RuntimeUiConfig>();
        var serializedObject = new SerializedObject(config);
        SerializedProperty modeProperty = serializedObject.FindProperty("mode");
        Assert.IsNotNull(modeProperty, "RuntimeUiConfig must keep a serialized mode field for asset editing.");
        modeProperty.enumValueIndex = (int)mode;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return config;
    }

    private sealed class RuntimeUiModeSmokeScope : IDisposable
    {
        private readonly GameObject host;
        private readonly RuntimeUiConfig config;

        public RuntimeUiModeSmokeScope(RuntimeUiMode mode)
        {
            host = new GameObject("RuntimeUiModeSmoke");
            GameObject canvasObject = new("CanvasFallback");
            GameObject documentObject = new("UiToolkitDocument");
            UiToolkitRoot = new GameObject("UiToolkitShellRoot");

            canvasObject.transform.SetParent(host.transform);
            documentObject.transform.SetParent(host.transform);
            UiToolkitRoot.transform.SetParent(host.transform);

            BootstrapView = host.AddComponent<MenuBootstrapView>();
            Canvas = canvasObject.AddComponent<Canvas>();
            UiDocument = documentObject.AddComponent<UIDocument>();
            config = CreateRuntimeUiConfig(mode);

            BootstrapView.Configure(null, Canvas, null, null, null, null, config, UiDocument, UiToolkitRoot, null);
        }

        public MenuBootstrapView BootstrapView { get; }
        public Canvas Canvas { get; }
        public UIDocument UiDocument { get; }
        public GameObject UiToolkitRoot { get; }

        public void Dispose()
        {
            if (host != null)
                UnityEngine.Object.DestroyImmediate(host);
            if (config != null)
                UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
#endif
