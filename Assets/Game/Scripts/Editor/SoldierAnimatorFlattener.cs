#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SoldierAnimatorFlattener
{
    private static readonly string[] DefaultOutputStateNames =
    {
        "Idle",
        "IdleHoldWeapon",
        "Aim",
        "Shoot",
        "Grenade",
        "Walk",
        "WalkHoldWeapon",
        "WalkAim",
        "WalkShoot",
        "Run",
        "RunHoldWeapon",
        "RunAim",
        "RunShoot",
        "Reload",
        "Death01",
        "Death02",
        "Death03"
    };

    private const string MaleSourceControllerPath =
        "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/AnimatorControllers/HumanM@SoldierAnimations.controller";

    private const string FemaleSourceControllerPath =
        "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/AnimatorControllers/HumanF@SoldierAnimations.overrideController";

    private const string MaleTargetControllerPath =
        "Assets/Game/Animations/HumanM@SoldierAnimations_Flat.controller";

    private const string MaleRifleTargetControllerPath =
        "Assets/Game/Animations/HumanM@SoldierAnimations_Flat_Rifle.controller";

    private const string MaleGunTargetControllerPath =
        "Assets/Game/Animations/HumanM@SoldierAnimations_Flat_Gun.controller";

    private const string MaleDualGunTargetControllerPath =
        "Assets/Game/Animations/HumanM@SoldierAnimations_Flat_DualGun.controller";

    private const string MaleBazookaTargetControllerPath =
        "Assets/Game/Animations/HumanM@SoldierAnimations_Flat_Bazooka.controller";

    private const string MaleAssaultRifleTargetControllerPath =
        "Assets/Game/Animations/HumanM@SoldierAnimations_Flat_AssaultRifle.controller";

    private const string FemaleTargetControllerPath =
        "Assets/Game/Animations/HumanF@SoldierAnimations_Flat.controller";

    private const string FemaleRifleTargetControllerPath =
        "Assets/Game/Animations/HumanF@SoldierAnimations_Flat_Rifle.controller";

    private const string FemaleGunTargetControllerPath =
        "Assets/Game/Animations/HumanF@SoldierAnimations_Flat_Gun.controller";

    private const string FemaleDualGunTargetControllerPath =
        "Assets/Game/Animations/HumanF@SoldierAnimations_Flat_DualGun.controller";

    private const string FemaleBazookaTargetControllerPath =
        "Assets/Game/Animations/HumanF@SoldierAnimations_Flat_Bazooka.controller";

    private const string FemaleAssaultRifleTargetControllerPath =
        "Assets/Game/Animations/HumanF@SoldierAnimations_Flat_AssaultRifle.controller";

    private const string GeneratedClipFolder = "Assets/Game/Animations/FlatGenerated";
    private const string MaleGrenadeFallbackClipPath =
        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Grenade/HumanM@ThrowGrenade01_L.fbx";
    private const string FemaleGrenadeFallbackClipPath =
        "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Grenade/HumanF@ThrowGrenade01_L.fbx";

    [InitializeOnLoadMethod]
    private static void EnsureControllerExists()
    {
        EnsureControllerExists(MaleTargetControllerPath, () => GenerateFlatController(MaleSourceControllerPath, MaleTargetControllerPath, WeaponFamily.Rifle, logResult: false));
        EnsureControllerExists(MaleRifleTargetControllerPath, () => GenerateFlatController(MaleSourceControllerPath, MaleRifleTargetControllerPath, WeaponFamily.Rifle, logResult: false));
        EnsureControllerExists(MaleGunTargetControllerPath, () => GenerateFlatController(MaleSourceControllerPath, MaleGunTargetControllerPath, WeaponFamily.Gun, logResult: false));
        EnsureControllerExists(MaleDualGunTargetControllerPath, () => GenerateFlatController(MaleSourceControllerPath, MaleDualGunTargetControllerPath, WeaponFamily.DualGun, logResult: false));
        EnsureControllerExists(MaleBazookaTargetControllerPath, () => GenerateFlatController(MaleSourceControllerPath, MaleBazookaTargetControllerPath, WeaponFamily.Bazooka, logResult: false));
        EnsureControllerExists(MaleAssaultRifleTargetControllerPath, () => GenerateFlatController(MaleSourceControllerPath, MaleAssaultRifleTargetControllerPath, WeaponFamily.AssaultRifle, logResult: false));
        EnsureControllerExists(FemaleTargetControllerPath, () => GenerateFlatController(FemaleSourceControllerPath, FemaleTargetControllerPath, WeaponFamily.Rifle, logResult: false));
        EnsureControllerExists(FemaleRifleTargetControllerPath, () => GenerateFlatController(FemaleSourceControllerPath, FemaleRifleTargetControllerPath, WeaponFamily.Rifle, logResult: false));
        EnsureControllerExists(FemaleGunTargetControllerPath, () => GenerateFlatController(FemaleSourceControllerPath, FemaleGunTargetControllerPath, WeaponFamily.Gun, logResult: false));
        EnsureControllerExists(FemaleDualGunTargetControllerPath, () => GenerateFlatController(FemaleSourceControllerPath, FemaleDualGunTargetControllerPath, WeaponFamily.DualGun, logResult: false));
        EnsureControllerExists(FemaleBazookaTargetControllerPath, () => GenerateFlatController(FemaleSourceControllerPath, FemaleBazookaTargetControllerPath, WeaponFamily.Bazooka, logResult: false));
        EnsureControllerExists(FemaleAssaultRifleTargetControllerPath, () => GenerateFlatController(FemaleSourceControllerPath, FemaleAssaultRifleTargetControllerPath, WeaponFamily.AssaultRifle, logResult: false));
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Male")]
    public static void GenerateMaleFlatControllerMenu()
    {
        GenerateFlatController(MaleSourceControllerPath, MaleTargetControllerPath, WeaponFamily.Rifle, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Male All Weapons")]
    public static void GenerateMaleAllWeaponsFlatControllersMenu()
    {
        GenerateFlatController(MaleSourceControllerPath, MaleRifleTargetControllerPath, WeaponFamily.Rifle, logResult: true);
        GenerateFlatController(MaleSourceControllerPath, MaleGunTargetControllerPath, WeaponFamily.Gun, logResult: true);
        GenerateFlatController(MaleSourceControllerPath, MaleDualGunTargetControllerPath, WeaponFamily.DualGun, logResult: true);
        GenerateFlatController(MaleSourceControllerPath, MaleBazookaTargetControllerPath, WeaponFamily.Bazooka, logResult: true);
        GenerateFlatController(MaleSourceControllerPath, MaleAssaultRifleTargetControllerPath, WeaponFamily.AssaultRifle, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Male Rifle")]
    public static void GenerateMaleRifleFlatControllerMenu()
    {
        GenerateFlatController(MaleSourceControllerPath, MaleRifleTargetControllerPath, WeaponFamily.Rifle, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Male Gun")]
    public static void GenerateMaleGunFlatControllerMenu()
    {
        GenerateFlatController(MaleSourceControllerPath, MaleGunTargetControllerPath, WeaponFamily.Gun, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Male DualGun")]
    public static void GenerateMaleDualGunFlatControllerMenu()
    {
        GenerateFlatController(MaleSourceControllerPath, MaleDualGunTargetControllerPath, WeaponFamily.DualGun, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Male Bazooka")]
    public static void GenerateMaleBazookaFlatControllerMenu()
    {
        GenerateFlatController(MaleSourceControllerPath, MaleBazookaTargetControllerPath, WeaponFamily.Bazooka, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Male AssaultRifle")]
    public static void GenerateMaleAssaultRifleFlatControllerMenu()
    {
        GenerateFlatController(MaleSourceControllerPath, MaleAssaultRifleTargetControllerPath, WeaponFamily.AssaultRifle, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Female")]
    public static void GenerateFemaleFlatControllerMenu()
    {
        GenerateFlatController(FemaleSourceControllerPath, FemaleTargetControllerPath, WeaponFamily.Rifle, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Female All Weapons")]
    public static void GenerateFemaleAllWeaponsFlatControllersMenu()
    {
        GenerateFlatController(FemaleSourceControllerPath, FemaleRifleTargetControllerPath, WeaponFamily.Rifle, logResult: true);
        GenerateFlatController(FemaleSourceControllerPath, FemaleGunTargetControllerPath, WeaponFamily.Gun, logResult: true);
        GenerateFlatController(FemaleSourceControllerPath, FemaleDualGunTargetControllerPath, WeaponFamily.DualGun, logResult: true);
        GenerateFlatController(FemaleSourceControllerPath, FemaleBazookaTargetControllerPath, WeaponFamily.Bazooka, logResult: true);
        GenerateFlatController(FemaleSourceControllerPath, FemaleAssaultRifleTargetControllerPath, WeaponFamily.AssaultRifle, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/All Characters All Weapons")]
    public static void GenerateAllCharactersAllWeaponsFlatControllersMenu()
    {
        GenerateMaleAllWeaponsFlatControllersMenu();
        GenerateFemaleAllWeaponsFlatControllersMenu();
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Window")]
    public static void OpenFlattenerWindow()
    {
        SoldierAnimatorFlattenerWindow.OpenWindow();
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Female Rifle")]
    public static void GenerateFemaleRifleFlatControllerMenu()
    {
        GenerateFlatController(FemaleSourceControllerPath, FemaleRifleTargetControllerPath, WeaponFamily.Rifle, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Female Gun")]
    public static void GenerateFemaleGunFlatControllerMenu()
    {
        GenerateFlatController(FemaleSourceControllerPath, FemaleGunTargetControllerPath, WeaponFamily.Gun, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Female DualGun")]
    public static void GenerateFemaleDualGunFlatControllerMenu()
    {
        GenerateFlatController(FemaleSourceControllerPath, FemaleDualGunTargetControllerPath, WeaponFamily.DualGun, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Female Bazooka")]
    public static void GenerateFemaleBazookaFlatControllerMenu()
    {
        GenerateFlatController(FemaleSourceControllerPath, FemaleBazookaTargetControllerPath, WeaponFamily.Bazooka, logResult: true);
    }

    [MenuItem("Tools/Game/Generate Flat Soldier Animator/Female AssaultRifle")]
    public static void GenerateFemaleAssaultRifleFlatControllerMenu()
    {
        GenerateFlatController(FemaleSourceControllerPath, FemaleAssaultRifleTargetControllerPath, WeaponFamily.AssaultRifle, logResult: true);
    }

    private static void EnsureControllerExists(string targetControllerPath, System.Action generate)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(targetControllerPath);
        if (controller == null || !IsControllerValid(controller))
            generate();
    }

    public static void GenerateCustomFlatController(RuntimeAnimatorController source, string outputFolderAssetPath, string outputNamePrefix, string weaponFamilyName)
    {
        GenerateCustomFlatController(source, outputFolderAssetPath, outputNamePrefix, weaponFamilyName, DefaultOutputStateNames);
    }

    public static void GenerateCustomFlatController(RuntimeAnimatorController source, string outputFolderAssetPath, string outputNamePrefix, string weaponFamilyName, IEnumerable<string> selectedStateNames)
    {
        if (source == null)
        {
            Debug.LogError("SoldierAnimatorFlattener requires a source animator controller.");
            return;
        }

        if (!TryParseWeaponFamily(weaponFamilyName, out WeaponFamily weaponFamily))
        {
            Debug.LogError($"SoldierAnimatorFlattener does not recognize weapon family '{weaponFamilyName}'.");
            return;
        }

        string outputFolder = string.IsNullOrWhiteSpace(outputFolderAssetPath) ? "Assets/Game/Animations" : outputFolderAssetPath.Replace('\\', '/');
        EnsureFolder(outputFolder);

        string prefix = string.IsNullOrWhiteSpace(outputNamePrefix)
            ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source))
            : outputNamePrefix.Trim();

        string suffix = $"_Flat_{weaponFamily}";
        string targetControllerPath = $"{outputFolder}/{prefix}{suffix}.controller";
        GenerateFlatController(source, targetControllerPath, weaponFamily, selectedStateNames, logResult: true);
    }

    public static IReadOnlyList<string> GetDefaultOutputStateNames()
    {
        return DefaultOutputStateNames;
    }

    public static IReadOnlyList<string> GetAvailableOutputStateNames(RuntimeAnimatorController source)
    {
        var available = new List<string>();
        if (source == null || !TryGetSourceData(source, out AnimatorController sourceController, out Dictionary<AnimationClip, AnimationClip> overrides))
            return available;

        List<SourceStateClipEntry> sourceStateClips = BuildSourceStateClips(sourceController, overrides);

        foreach (string stateName in DefaultOutputStateNames)
        {
            if (StateHasSourceClip(sourceStateClips, stateName))
                available.Add(stateName);
        }

        return available;
    }

    private static void GenerateFlatController(string sourcePath, string targetControllerPath, WeaponFamily weaponFamily, bool logResult)
    {
        CloseAnimatorControllerWindows();

        var source = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(sourcePath);
        GenerateFlatController(source, targetControllerPath, weaponFamily, DefaultOutputStateNames, logResult);
    }

    private static void GenerateFlatController(RuntimeAnimatorController source, string targetControllerPath, WeaponFamily weaponFamily, IEnumerable<string> selectedStateNames, bool logResult)
    {
        CloseAnimatorControllerWindows();

        if (source == null)
        {
            Debug.LogError("SoldierAnimatorFlattener could not find the source controller.");
            return;
        }

        if (!TryGetSourceData(source, out AnimatorController sourceController, out Dictionary<AnimationClip, AnimationClip> overrides))
        {
            Debug.LogError($"SoldierAnimatorFlattener could not read source controller data from '{source.name}'.");
            return;
        }

        string targetDirectory = Path.GetDirectoryName(targetControllerPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory) && !AssetDatabase.IsValidFolder(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
            AssetDatabase.Refresh();
        }

        EnsureFolder(GeneratedClipFolder);
        var allowedStates = new HashSet<string>(selectedStateNames ?? DefaultOutputStateNames, System.StringComparer.OrdinalIgnoreCase);

        List<SourceStateClipEntry> sourceStateClips = BuildSourceStateClips(sourceController, overrides);
        List<StateClipEntry> generatedStates = BuildGeneratedFlatStates(sourceStateClips, targetControllerPath, weaponFamily, allowedStates);

        var target = AssetDatabase.LoadAssetAtPath<AnimatorController>(targetControllerPath);
        if (target == null)
            target = AnimatorController.CreateAnimatorControllerAtPath(targetControllerPath);

        if (target == null)
        {
            Debug.LogError($"SoldierAnimatorFlattener could not create target controller at '{targetControllerPath}'.");
            return;
        }

        ClearControllerSubAssets(targetControllerPath, target);

        var stateMachine = new AnimatorStateMachine
        {
            name = "Base Layer"
        };
        AssetDatabase.AddObjectToAsset(stateMachine, target);
        target.layers = new[]
        {
            new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = stateMachine,
                defaultWeight = 1f
            }
        };

        var orderedStates = generatedStates.Count > 0 ? generatedStates : BuildFallbackStates(sourceController, overrides, weaponFamily);
        orderedStates = EnsureFinalFamilyStates(orderedStates, sourceStateClips, weaponFamily, allowedStates);
        orderedStates = EnsureExplicitGrenadeFallback(orderedStates, sourceStateClips, source, weaponFamily, allowedStates);
        orderedStates = orderedStates
            .Where(entry => entry.Clip != null && allowedStates.Contains(entry.StateName))
            .ToList();
        orderedStates = OrderStatesByDefaultList(orderedStates);
        if (orderedStates.Count == 0)
        {
            Debug.LogWarning("SoldierAnimatorFlattener found no animation clips in the source controller.");
            return;
        }

        stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        stateMachine.entryTransitions = new AnimatorTransition[0];
        stateMachine.stateMachines = new ChildAnimatorStateMachine[0];
        stateMachine.states = new ChildAnimatorState[0];

        var childStates = new List<ChildAnimatorState>(orderedStates.Count);
        AnimatorState defaultState = null;

        const float startX = 260f;
        const float startY = 20f;
        const float stepX = 240f;
        const float stepY = 80f;
        const int maxRows = 18;

        for (int i = 0; i < orderedStates.Count; i++)
        {
            StateClipEntry entry = orderedStates[i];
            var state = stateMachine.AddState(entry.StateName, new Vector3(
                startX + ((i / maxRows) * stepX),
                startY + ((i % maxRows) * stepY),
                0f));

            state.motion = entry.Clip;
            state.writeDefaultValues = true;

            childStates.Add(new ChildAnimatorState
            {
                state = state,
                position = new Vector3(
                    startX + ((i / maxRows) * stepX),
                    startY + ((i % maxRows) * stepY),
                    0f)
            });

            if (defaultState == null && IsPreferredDefault(entry))
                defaultState = state;
        }

        stateMachine.states = childStates.ToArray();
        stateMachine.defaultState = defaultState ?? childStates[0].state;
        EnsureRequiredControllerStates(stateMachine, sourceStateClips, weaponFamily, allowedStates, childStates.Count);

        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (logResult)
        {
            string[] finalStateNames = stateMachine.states
                .Where(child => child.state != null)
                .Select(child => child.state.name)
                .ToArray();
            Debug.Log($"Generated flat soldier animator at '{targetControllerPath}' with {finalStateNames.Length} states: {string.Join(", ", finalStateNames)}");
        }
    }

    private static bool TryParseWeaponFamily(string weaponFamilyName, out WeaponFamily weaponFamily)
    {
        weaponFamily = WeaponFamily.Rifle;
        return System.Enum.TryParse(weaponFamilyName, ignoreCase: true, out weaponFamily);
    }

    private static List<StateClipEntry> EnsureExplicitGrenadeFallback(
        List<StateClipEntry> orderedStates,
        List<SourceStateClipEntry> sourceStateClips,
        RuntimeAnimatorController source,
        WeaponFamily weaponFamily,
        HashSet<string> allowedStates)
    {
        if (!allowedStates.Contains("Grenade"))
            return orderedStates;

        if (orderedStates.Any(entry => entry.StateName == "Grenade" && entry.Clip != null))
            return orderedStates;

        WeaponFamilyDefinition familyDefinition = GetWeaponFamilyDefinition(weaponFamily);
        AnimationClip fallbackClip = FindExactStateClip(sourceStateClips, familyDefinition.DirectGrenadeStateCandidates)
            ?? FindBestStateClip(sourceStateClips, familyDefinition.DirectGrenadeStateCandidates, System.Array.Empty<string>());

        if (fallbackClip == null)
        {
            string sourceName = source != null ? source.name : string.Empty;
            string fallbackPath = sourceName.StartsWith("HumanF", System.StringComparison.OrdinalIgnoreCase)
                ? FemaleGrenadeFallbackClipPath
                : MaleGrenadeFallbackClipPath;

            fallbackClip = LoadNamedClipAtPath(
                fallbackPath,
                sourceName.StartsWith("HumanF", System.StringComparison.OrdinalIgnoreCase) ? "HumanF@ThrowGrenade01_L" : "HumanM@ThrowGrenade01_L");
        }

        if (fallbackClip == null)
            return orderedStates;

        var result = new List<StateClipEntry>(orderedStates) { new StateClipEntry("Grenade", fallbackClip) };
        return result;
    }

    private static List<StateClipEntry> OrderStatesByDefaultList(List<StateClipEntry> orderedStates)
    {
        var byName = new Dictionary<string, StateClipEntry>(System.StringComparer.OrdinalIgnoreCase);
        foreach (StateClipEntry entry in orderedStates)
        {
            if (entry.Clip != null && !byName.ContainsKey(entry.StateName))
                byName.Add(entry.StateName, entry);
        }

        var result = new List<StateClipEntry>(orderedStates.Count);
        foreach (string stateName in DefaultOutputStateNames)
        {
            if (byName.TryGetValue(stateName, out StateClipEntry entry))
                result.Add(entry);
        }

        foreach (StateClipEntry entry in orderedStates)
        {
            if (entry.Clip != null && !result.Any(existing => existing.StateName.Equals(entry.StateName, System.StringComparison.OrdinalIgnoreCase)))
                result.Add(entry);
        }

        return result;
    }

    private static AnimationClip LoadNamedClipAtPath(string assetPath, string clipName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is AnimationClip clip && clip != null && clip.name == clipName)
                return clip;
        }

        return null;
    }

    private static bool StateHasSourceClip(List<SourceStateClipEntry> sourceStateClips, string stateName)
    {
        if (sourceStateClips == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        foreach (WeaponFamily weaponFamily in System.Enum.GetValues(typeof(WeaponFamily)))
        {
            WeaponFamilyDefinition familyDefinition = GetWeaponFamilyDefinition(weaponFamily);
            if (HasGeneratedStateSources(sourceStateClips, familyDefinition, stateName))
                return true;
        }

        return false;
    }

    private static bool HasGeneratedStateSources(
        List<SourceStateClipEntry> sourceStateClips,
        WeaponFamilyDefinition familyDefinition,
        string stateName)
    {
        string[] directCandidates = stateName switch
        {
            "IdleHoldWeapon" => familyDefinition.DirectHoldStateCandidates,
            "Aim" => familyDefinition.DirectAimStateCandidates,
            "Shoot" => familyDefinition.DirectShootStateCandidates,
            "Grenade" => familyDefinition.DirectGrenadeStateCandidates,
            "Reload" => familyDefinition.ReloadCandidates,
            _ => null
        };

        if (directCandidates != null)
        {
            AnimationClip directClip = FindExactStateClip(sourceStateClips, directCandidates)
                ?? FindBestStateClip(sourceStateClips, directCandidates, System.Array.Empty<string>());
            if (directClip != null)
                return true;
        }

        string[] lowerCandidates = stateName switch
        {
            "Idle" => new[] { "Military Idle - Idle", "Idle - Military Idle", "Idle" },
            "Walk" => new[] { "Walk", "Crouch Walk" },
            "Run" => new[] { "Run", "Sprint" },
            "WalkHoldWeapon" => new[] { "Walk", "Crouch Walk" },
            "WalkAim" => new[] { "Walk", "Crouch Walk" },
            "WalkShoot" => new[] { "Walk", "Crouch Walk" },
            "RunHoldWeapon" => new[] { "Run", "Sprint" },
            "RunAim" => new[] { "Run", "Sprint" },
            "RunShoot" => new[] { "Run", "Sprint" },
            "Death01" => new[] { "Death01", "Death" },
            "Death02" => new[] { "Death02", "Death" },
            "Death03" => new[] { "Death03", "Death" },
            _ => null
        };

        if (lowerCandidates == null)
            return false;

        AnimationClip lowerClip = FindBestStateClip(sourceStateClips, lowerCandidates, new[] { "Movement", "Idles", "Prone" });
        if (lowerClip == null)
            return false;

        string[] upperCandidates = stateName switch
        {
            "WalkHoldWeapon" => familyDefinition.HoldCandidates,
            "RunHoldWeapon" => familyDefinition.HoldCandidates,
            "WalkAim" => familyDefinition.AimCandidates,
            "RunAim" => familyDefinition.AimCandidates,
            "WalkShoot" => familyDefinition.ShootCandidates,
            "RunShoot" => familyDefinition.ShootCandidates,
            _ => null
        };

        if (upperCandidates == null)
            return true;

        AnimationClip upperClip = FindExactStateClip(sourceStateClips, upperCandidates)
            ?? FindBestStateClip(sourceStateClips, upperCandidates, new[] { "Weapon Hold Arms", "Weapon Hold Hand L", "Weapon Hold Hand R", "Shoots Upper Body", "Shoots Full Body" });
        if (upperClip != null)
            return true;

        if (stateName == "WalkHoldWeapon" || stateName == "RunHoldWeapon")
            return FindExactHoldStateClips(sourceStateClips, familyDefinition.HoldStateName, stateName, familyDefinition.IncludeHandHoldClips).Count > 0;

        return false;
    }

    private static void CloseAnimatorControllerWindows()
    {
        foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (window == null)
                continue;

            System.Type windowType = window.GetType();
            if (windowType.Name != "AnimatorControllerTool")
                continue;

            MethodInfo closeMethod = typeof(EditorWindow).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
            closeMethod?.Invoke(window, null);
        }
    }

    private static void ClearControllerSubAssets(string targetControllerPath, AnimatorController target)
    {
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(targetControllerPath);
        foreach (Object asset in subAssets)
        {
            if (asset == null || asset == target)
                continue;

            if (asset is AnimatorStateMachine
                || asset is AnimatorState
                || asset is AnimatorStateTransition
                || asset is AnimatorTransition
                || asset is BlendTree)
            {
                Object.DestroyImmediate(asset, true);
            }
        }
    }

    private static bool IsControllerValid(AnimatorController controller)
    {
        if (controller == null || controller.layers == null || controller.layers.Length == 0)
            return false;

        return controller.layers[0].stateMachine != null;
    }

    private static List<StateClipEntry> BuildGeneratedFlatStates(
        List<SourceStateClipEntry> sourceStateClips,
        string targetControllerPath,
        WeaponFamily weaponFamily,
        HashSet<string> allowedStates)
    {
        WeaponFamilyDefinition familyDefinition = GetWeaponFamilyDefinition(weaponFamily);
        string familyPrefix = GetFamilyPrefixFromTargetPath(targetControllerPath, familyDefinition.GeneratedSuffix);
        string generatedFolder = $"{GeneratedClipFolder}/{familyPrefix}";
        EnsureFolder(generatedFolder);

        var definitions = new[]
        {
            new GeneratedStateDefinition("Idle", true, lowerStateCandidates: new[] { "Military Idle - Idle", "Idle - Military Idle", "Idle" }),
            new GeneratedStateDefinition("IdleHoldWeapon", true, new[] { "Military Idle - Idle", "Idle - Military Idle", "Idle" }, familyDefinition.HoldCandidates, directStateName: familyDefinition.HoldStateName),
            new GeneratedStateDefinition("Aim", true, new[] { "Military Idle - Idle", "Idle - Military Idle", "Idle" }, familyDefinition.AimCandidates, directStateName: familyDefinition.DirectAimStateName),
            new GeneratedStateDefinition("Shoot", false, new[] { "Military Idle - Idle", "Idle - Military Idle", "Idle" }, familyDefinition.ShootCandidates, directStateName: familyDefinition.DirectShootStateName),
            new GeneratedStateDefinition("Grenade", false, new[] { "Military Idle - Idle", "Idle - Military Idle", "Idle" }, familyDefinition.GrenadeCandidates, directStateName: familyDefinition.DirectGrenadeStateName),
            new GeneratedStateDefinition("Walk", true, lowerStateCandidates: new[] { "Walk", "Crouch Walk" }),
            new GeneratedStateDefinition("WalkHoldWeapon", true, new[] { "Walk", "Crouch Walk" }, familyDefinition.HoldCandidates, holdStateName: familyDefinition.HoldStateName),
            new GeneratedStateDefinition("WalkAim", true, new[] { "Walk", "Crouch Walk" }, familyDefinition.AimCandidates),
            new GeneratedStateDefinition("WalkShoot", false, new[] { "Walk", "Crouch Walk" }, familyDefinition.ShootCandidates),
            new GeneratedStateDefinition("Run", true, lowerStateCandidates: new[] { "Run", "Sprint" }),
            new GeneratedStateDefinition("RunHoldWeapon", true, new[] { "Run", "Sprint" }, familyDefinition.HoldCandidates, holdStateName: familyDefinition.HoldStateName),
            new GeneratedStateDefinition("RunAim", true, new[] { "Run", "Sprint" }, familyDefinition.AimCandidates),
            new GeneratedStateDefinition("RunShoot", false, new[] { "Run", "Sprint" }, familyDefinition.ShootCandidates),
            new GeneratedStateDefinition("Reload", false, lowerStateCandidates: familyDefinition.ReloadCandidates),
            new GeneratedStateDefinition("Death01", false, lowerStateCandidates: new[] { "Death01", "Death" }),
            new GeneratedStateDefinition("Death02", false, lowerStateCandidates: new[] { "Death02", "Death" }),
            new GeneratedStateDefinition("Death03", false, lowerStateCandidates: new[] { "Death03", "Death" })
        };

        var generatedStates = new List<StateClipEntry>();
        foreach (GeneratedStateDefinition definition in definitions)
        {
            if (!allowedStates.Contains(definition.Name))
                continue;

            AnimationClip finalClip;
            if (!string.IsNullOrWhiteSpace(definition.DirectStateName))
            {
                finalClip = FindExactStateClip(sourceStateClips, new[] { definition.DirectStateName })
                    ?? FindBestStateClip(sourceStateClips, new[] { definition.DirectStateName }, definition.PreferredUpperLayers);
                if (finalClip == null && definition.UpperStateCandidates != null && definition.UpperStateCandidates.Length > 0)
                {
                    finalClip = FindExactStateClip(sourceStateClips, definition.UpperStateCandidates)
                        ?? FindBestStateClip(sourceStateClips, definition.UpperStateCandidates, definition.PreferredUpperLayers);
                }
                if (finalClip == null)
                    continue;

                finalClip = CreateSanitizedClip(
                    finalClip,
                    $"{generatedFolder}/{definition.Name}.anim",
                    definition.Name,
                    definition.Loop);
            }
            else if (definition.UpperStateCandidates == null || definition.UpperStateCandidates.Length == 0)
            {
                AnimationClip lowerClip = FindBestStateClip(sourceStateClips, definition.LowerStateCandidates, definition.PreferredLowerLayers);
                if (lowerClip == null)
                    continue;

                finalClip = CreateSanitizedClip(
                    lowerClip,
                    $"{generatedFolder}/{definition.Name}.anim",
                    definition.Name,
                    definition.Loop);
            }
            else
            {
                AnimationClip lowerClip = FindBestStateClip(sourceStateClips, definition.LowerStateCandidates, definition.PreferredLowerLayers);
                if (lowerClip == null)
                    continue;

                List<AnimationClip> upperClips;
                if (!string.IsNullOrWhiteSpace(definition.HoldStateName))
                {
                    upperClips = FindExactHoldStateClips(sourceStateClips, definition.HoldStateName, definition.Name, familyDefinition.IncludeHandHoldClips);
                }
                else if (definition.Name == "Shoot" || definition.Name == "WalkShoot" || definition.Name == "RunShoot")
                {
                    AnimationClip exactShootClip = FindExactStateClip(sourceStateClips, definition.UpperStateCandidates);
                    upperClips = exactShootClip != null
                        ? new List<AnimationClip> { exactShootClip }
                        : FindUpperStateClips(sourceStateClips, definition.UpperStateCandidates, definition.PreferredUpperLayers);
                }
                else
                {
                    upperClips = FindUpperStateClips(sourceStateClips, definition.UpperStateCandidates, definition.PreferredUpperLayers);
                }

                if (upperClips.Count == 0)
                    continue;

                string clipPath = $"{generatedFolder}/{definition.Name}.anim";
                finalClip = CreateMergedClip(lowerClip, upperClips, clipPath, definition.Name, definition.Loop);
            }

            if (finalClip != null)
                generatedStates.Add(new StateClipEntry(definition.Name, finalClip));
        }

        EnsureDirectStateGenerated(generatedStates, sourceStateClips, generatedFolder, "IdleHoldWeapon", familyDefinition.DirectHoldStateCandidates, allowedStates, loop: true);
        EnsureDirectStateGenerated(generatedStates, sourceStateClips, generatedFolder, "Aim", familyDefinition.DirectAimStateCandidates, allowedStates, loop: true);
        EnsureDirectStateGenerated(generatedStates, sourceStateClips, generatedFolder, "Shoot", familyDefinition.DirectShootStateCandidates, allowedStates, loop: false);
        EnsureDirectStateGenerated(generatedStates, sourceStateClips, generatedFolder, "Grenade", familyDefinition.DirectGrenadeStateCandidates, allowedStates, loop: false);

        return generatedStates;
    }

    private static void EnsureDirectStateGenerated(
        List<StateClipEntry> generatedStates,
        List<SourceStateClipEntry> sourceStateClips,
        string generatedFolder,
        string stateName,
        string[] candidates,
        HashSet<string> allowedStates,
        bool loop)
    {
        if (!allowedStates.Contains(stateName))
            return;

        if (generatedStates.Any(entry => entry.StateName == stateName))
            return;

        AnimationClip clip = FindExactStateClip(sourceStateClips, candidates)
            ?? FindBestStateClip(sourceStateClips, candidates, System.Array.Empty<string>());

        if (clip != null)
        {
            AnimationClip sanitizedClip = CreateSanitizedClip(
                clip,
                $"{generatedFolder}/{stateName}.anim",
                stateName,
                loop);
            generatedStates.Add(new StateClipEntry(stateName, sanitizedClip));
        }
    }

    private static List<StateClipEntry> EnsureFinalFamilyStates(
        List<StateClipEntry> orderedStates,
        List<SourceStateClipEntry> sourceStateClips,
        WeaponFamily weaponFamily,
        HashSet<string> allowedStates)
    {
        var result = new List<StateClipEntry>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (StateClipEntry entry in orderedStates)
        {
            if (entry.Clip == null || !seen.Add(entry.StateName))
                continue;

            result.Add(entry);
        }

        WeaponFamilyDefinition familyDefinition = GetWeaponFamilyDefinition(weaponFamily);
        EnsureStateInFinalList(result, seen, sourceStateClips, "IdleHoldWeapon", familyDefinition.DirectHoldStateCandidates, allowedStates);
        EnsureStateInFinalList(result, seen, sourceStateClips, "Aim", familyDefinition.DirectAimStateCandidates, allowedStates);
        EnsureStateInFinalList(result, seen, sourceStateClips, "Shoot", familyDefinition.DirectShootStateCandidates, allowedStates);
        EnsureStateInFinalList(result, seen, sourceStateClips, "Grenade", familyDefinition.DirectGrenadeStateCandidates, allowedStates);
        EnsureStateInFinalList(result, seen, sourceStateClips, "Reload", familyDefinition.ReloadCandidates, allowedStates);

        return result;
    }

    private static void EnsureStateInFinalList(
        List<StateClipEntry> result,
        HashSet<string> seen,
        List<SourceStateClipEntry> sourceStateClips,
        string stateName,
        string[] candidates,
        HashSet<string> allowedStates)
    {
        if (!allowedStates.Contains(stateName))
            return;

        if (seen.Contains(stateName))
            return;

        AnimationClip clip = FindExactStateClip(sourceStateClips, candidates)
            ?? FindBestStateClip(sourceStateClips, candidates, System.Array.Empty<string>());

        if (clip == null)
            return;

        result.Add(new StateClipEntry(stateName, clip));
        seen.Add(stateName);
    }

    private static void EnsureRequiredControllerStates(
        AnimatorStateMachine stateMachine,
        List<SourceStateClipEntry> sourceStateClips,
        WeaponFamily weaponFamily,
        HashSet<string> allowedStates,
        int existingStateCount)
    {
        WeaponFamilyDefinition familyDefinition = GetWeaponFamilyDefinition(weaponFamily);
        EnsureControllerState(stateMachine, sourceStateClips, "IdleHoldWeapon", familyDefinition.DirectHoldStateCandidates, allowedStates, existingStateCount + 1);
        EnsureControllerState(stateMachine, sourceStateClips, "Aim", familyDefinition.DirectAimStateCandidates, allowedStates, existingStateCount + 2);
        EnsureControllerState(stateMachine, sourceStateClips, "Shoot", familyDefinition.DirectShootStateCandidates, allowedStates, existingStateCount + 3);
        EnsureControllerState(stateMachine, sourceStateClips, "Grenade", familyDefinition.DirectGrenadeStateCandidates, allowedStates, existingStateCount + 4);
        EnsureControllerState(stateMachine, sourceStateClips, "Reload", familyDefinition.ReloadCandidates, allowedStates, existingStateCount + 5);
    }

    private static void EnsureControllerState(
        AnimatorStateMachine stateMachine,
        List<SourceStateClipEntry> sourceStateClips,
        string stateName,
        string[] candidates,
        HashSet<string> allowedStates,
        int orderIndex)
    {
        if (!allowedStates.Contains(stateName))
            return;

        ChildAnimatorState existing = stateMachine.states.FirstOrDefault(child =>
            child.state != null && child.state.name == stateName);
        if (existing.state != null)
            return;

        AnimationClip clip = FindExactStateClip(sourceStateClips, candidates)
            ?? FindBestStateClip(sourceStateClips, candidates, System.Array.Empty<string>());
        if (clip == null)
            return;

        stateMachine.AddState(stateName, new Vector3(260f + ((orderIndex / 18) * 240f), 20f + ((orderIndex % 18) * 80f), 0f)).motion = clip;
    }

    private static AnimationClip CreateMergedClip(AnimationClip lowerClip, List<AnimationClip> upperClips, string clipPath, string clipName, bool loop)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(clipPath);

        var mergedClip = new AnimationClip
        {
            name = clipName,
            frameRate = Mathf.Max(lowerClip.frameRate, GetMaxFrameRate(upperClips))
        };

        CopyBindings(lowerClip, mergedClip, includeUpperBody: false);
        foreach (AnimationClip upperClip in upperClips)
            CopyBindings(upperClip, mergedClip, includeUpperBody: true);

        var settings = AnimationUtility.GetAnimationClipSettings(lowerClip);
        settings.loopTime = loop;
        settings.loopBlend = loop;
        settings.keepOriginalOrientation = true;
        settings.keepOriginalPositionY = true;
        settings.keepOriginalPositionXZ = true;
        settings.heightFromFeet = false;
        AnimationUtility.SetAnimationClipSettings(mergedClip, settings);
        mergedClip.EnsureQuaternionContinuity();

        AssetDatabase.CreateAsset(mergedClip, clipPath);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
    }

    private static AnimationClip CreateSanitizedClip(AnimationClip sourceClip, string clipPath, string clipName, bool loop)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(clipPath);

        var sanitizedClip = new AnimationClip
        {
            name = clipName,
            frameRate = sourceClip.frameRate
        };

        CopySanitizedBindings(sourceClip, sanitizedClip);

        var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        settings.loopTime = loop;
        settings.loopBlend = loop;
        settings.keepOriginalOrientation = true;
        settings.keepOriginalPositionY = true;
        settings.keepOriginalPositionXZ = true;
        settings.heightFromFeet = false;
        AnimationUtility.SetAnimationClipSettings(sanitizedClip, settings);
        sanitizedClip.EnsureQuaternionContinuity();

        AssetDatabase.CreateAsset(sanitizedClip, clipPath);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
    }

    private static void CopySanitizedBindings(AnimationClip sourceClip, AnimationClip targetClip)
    {
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
        {
            string property = binding.propertyName ?? string.Empty;
            if (IsHumanoidRootMotionProperty(property))
                continue;

            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (curve != null)
                AnimationUtility.SetEditorCurve(targetClip, binding, curve);
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
        {
            string property = binding.propertyName ?? string.Empty;
            if (IsHumanoidRootMotionProperty(property))
                continue;

            ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
            if (curve != null && curve.Length > 0)
                AnimationUtility.SetObjectReferenceCurve(targetClip, binding, curve);
        }
    }

    private static float GetMaxFrameRate(IEnumerable<AnimationClip> clips)
    {
        float maxFrameRate = 0f;
        foreach (AnimationClip clip in clips)
        {
            if (clip != null && clip.frameRate > maxFrameRate)
                maxFrameRate = clip.frameRate;
        }

        return maxFrameRate > 0f ? maxFrameRate : 30f;
    }

    private static void CopyBindings(AnimationClip sourceClip, AnimationClip targetClip, bool includeUpperBody)
    {
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
        {
            if (!ShouldCopyBinding(binding, includeUpperBody))
                continue;

            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (curve != null)
                AnimationUtility.SetEditorCurve(targetClip, binding, curve);
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
        {
            if (!ShouldCopyBinding(binding, includeUpperBody))
                continue;

            ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
            if (curve != null && curve.Length > 0)
                AnimationUtility.SetObjectReferenceCurve(targetClip, binding, curve);
        }
    }

    private static bool ShouldCopyBinding(EditorCurveBinding binding, bool includeUpperBody)
    {
        string path = binding.path ?? string.Empty;
        string property = binding.propertyName ?? string.Empty;

        if (IsHumanoidRootMotionProperty(property))
            return false;

        if (string.IsNullOrEmpty(path))
        {
            if (IsRootBinding(path, property))
                return !includeUpperBody;

            bool isUpperBodyHumanoid = IsUpperBodyHumanoidProperty(property);
            return includeUpperBody ? isUpperBodyHumanoid : !isUpperBodyHumanoid;
        }

        if (IsRootBinding(path, property))
            return !includeUpperBody;

        bool isUpperBody = IsUpperBodyPath(path);
        return includeUpperBody ? isUpperBody : !isUpperBody;
    }

    private static bool IsRootBinding(string path, string property)
    {
        if (path == "Rig" || path == "Rig/B-root")
            return true;

        return IsHumanoidRootMotionProperty(property);
    }

    private static bool IsHumanoidRootMotionProperty(string property)
    {
        return property.IndexOf("RootT", System.StringComparison.OrdinalIgnoreCase) >= 0
            || property.IndexOf("RootQ", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsUpperBodyPath(string path)
    {
        string lower = path.ToLowerInvariant();

        if (lower.Contains("spineproxy") || lower.Contains("weapon") || lower.Contains("rifle") || lower.Contains("gun") || lower.Contains("bazooka"))
            return true;

        return lower.Contains("spine")
            || lower.Contains("chest")
            || lower.Contains("neck")
            || lower.Contains("head")
            || lower.Contains("clavicle")
            || lower.Contains("shoulder")
            || lower.Contains("upperarm")
            || lower.Contains("lowerarm")
            || lower.Contains("hand")
            || lower.Contains("thumb")
            || lower.Contains("index")
            || lower.Contains("middle")
            || lower.Contains("ring")
            || lower.Contains("pinky");
    }

    private static bool IsUpperBodyHumanoidProperty(string property)
    {
        if (string.IsNullOrWhiteSpace(property))
            return false;

        string lower = property.ToLowerInvariant();

        if (lower.Contains("roott") || lower.Contains("rootq") || lower.Contains("foot") || lower.Contains("toes"))
            return false;

        if (lower.Contains("spine")
            || lower.Contains("chest")
            || lower.Contains("upperchest")
            || lower.Contains("neck")
            || lower.Contains("head")
            || lower.Contains("jaw")
            || lower.Contains("eye")
            || lower.Contains("shoulder")
            || lower.Contains("arm")
            || lower.Contains("forearm")
            || lower.Contains("hand")
            || lower.Contains("thumb")
            || lower.Contains("index")
            || lower.Contains("middle")
            || lower.Contains("ring")
            || lower.Contains("little"))
        {
            return true;
        }

        return false;
    }

    private static List<SourceStateClipEntry> BuildSourceStateClips(
        AnimatorController sourceController,
        Dictionary<AnimationClip, AnimationClip> overrides)
    {
        var entries = new List<SourceStateClipEntry>();
        foreach (AnimatorControllerLayer layer in sourceController.layers)
            CollectStateClips(layer.name, layer.stateMachine, entries, overrides);
        return entries;
    }

    private static void CollectStateClips(
        string layerName,
        AnimatorStateMachine stateMachine,
        List<SourceStateClipEntry> entries,
        Dictionary<AnimationClip, AnimationClip> overrides)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            if (childState.state == null || childState.state.motion == null)
                continue;

            if (childState.state.motion is AnimationClip clip)
            {
                AnimationClip resolved = ResolveClip(clip, overrides);
                if (resolved != null)
                    entries.Add(new SourceStateClipEntry(layerName, childState.state.name, resolved));
            }
            else if (childState.state.motion is BlendTree blendTree)
            {
                foreach (ChildMotion childMotion in blendTree.children)
                {
                    if (childMotion.motion is not AnimationClip clipMotion)
                        continue;

                    AnimationClip resolved = ResolveClip(clipMotion, overrides);
                    string stateName = string.IsNullOrWhiteSpace(childMotion.motion.name) ? childState.state.name : childMotion.motion.name;
                    if (resolved != null)
                        entries.Add(new SourceStateClipEntry(layerName, stateName, resolved));
                }
            }
        }

        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
        {
            if (childStateMachine.stateMachine != null)
                CollectStateClips(layerName, childStateMachine.stateMachine, entries, overrides);
        }
    }

    private static AnimationClip FindBestStateClip(
        List<SourceStateClipEntry> sourceStateClips,
        IEnumerable<string> candidates,
        IEnumerable<string> preferredLayers)
    {
        List<string> candidateList = candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)).ToList();
        List<string> preferredLayerList = preferredLayers?.Where(layer => !string.IsNullOrWhiteSpace(layer)).ToList() ?? new List<string>();

        foreach (string preferredLayer in preferredLayerList)
        {
            foreach (string candidate in candidateList)
            {
                SourceStateClipEntry directMatch = sourceStateClips.FirstOrDefault(entry =>
                    entry.LayerName.Equals(preferredLayer, System.StringComparison.OrdinalIgnoreCase) &&
                    entry.StateName.Equals(candidate, System.StringComparison.OrdinalIgnoreCase));

                if (directMatch.Clip != null)
                    return directMatch.Clip;
            }

            foreach (string candidate in candidateList)
            {
                SourceStateClipEntry fuzzyMatch = sourceStateClips.FirstOrDefault(entry =>
                    entry.LayerName.Equals(preferredLayer, System.StringComparison.OrdinalIgnoreCase) &&
                    entry.StateName.IndexOf(candidate, System.StringComparison.OrdinalIgnoreCase) >= 0);

                if (fuzzyMatch.Clip != null)
                    return fuzzyMatch.Clip;
            }
        }

        foreach (string candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            SourceStateClipEntry directMatch = sourceStateClips.FirstOrDefault(entry =>
                entry.StateName.Equals(candidate, System.StringComparison.OrdinalIgnoreCase));

            if (directMatch.Clip != null)
                return directMatch.Clip;

            foreach (SourceStateClipEntry entry in sourceStateClips)
            {
                if (entry.StateName.IndexOf(candidate, System.StringComparison.OrdinalIgnoreCase) >= 0 && entry.Clip != null)
                    return entry.Clip;
            }
        }

        return null;
    }

    private static AnimationClip FindExactStateClip(
        List<SourceStateClipEntry> sourceStateClips,
        IEnumerable<string> candidates)
    {
        foreach (string candidate in candidates ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            SourceStateClipEntry directMatch = sourceStateClips.FirstOrDefault(entry =>
                entry.StateName.Equals(candidate, System.StringComparison.OrdinalIgnoreCase));

            if (directMatch.Clip != null)
                return directMatch.Clip;
        }

        return null;
    }

    private static List<AnimationClip> FindUpperStateClips(
        List<SourceStateClipEntry> sourceStateClips,
        IEnumerable<string> candidates,
        IEnumerable<string> preferredLayers)
    {
        var clips = new List<AnimationClip>();
        foreach (string preferredLayer in preferredLayers ?? Enumerable.Empty<string>())
        {
            AnimationClip clip = FindBestStateClip(
                sourceStateClips.Where(entry => entry.LayerName.Equals(preferredLayer, System.StringComparison.OrdinalIgnoreCase)).ToList(),
                candidates,
                System.Array.Empty<string>());

            if (clip != null && !clips.Contains(clip))
                clips.Add(clip);
        }

        if (clips.Count == 0)
        {
            AnimationClip fallback = FindBestStateClip(sourceStateClips, candidates, System.Array.Empty<string>());
            if (fallback != null)
                clips.Add(fallback);
        }

        return clips;
    }

    private static List<AnimationClip> FindExactHoldStateClips(
        List<SourceStateClipEntry> sourceStateClips,
        string holdStateName,
        string generatedStateName,
        bool includeHandHoldClips)
    {
        var clips = new List<AnimationClip>();

        AnimationClip armsClip = FindBestStateClip(
            sourceStateClips,
            new[] { holdStateName },
            new[] { "Weapon Hold Arms" });
        if (armsClip != null)
            clips.Add(armsClip);

        bool useExactRifleArmsOnly =
            generatedStateName == "WalkHoldWeapon" ||
            generatedStateName == "RunHoldWeapon";

        if (useExactRifleArmsOnly && !includeHandHoldClips)
            return clips;

        AnimationClip leftHandClip = FindBestStateClip(
            sourceStateClips,
            new[] { "Hold Gun L" },
            new[] { "Weapon Hold Hand L" });
        if (leftHandClip != null && !clips.Contains(leftHandClip))
            clips.Add(leftHandClip);

        AnimationClip rightHandClip = FindBestStateClip(
            sourceStateClips,
            new[] { "Hold Gun R" },
            new[] { "Weapon Hold Hand R" });
        if (rightHandClip != null && !clips.Contains(rightHandClip))
            clips.Add(rightHandClip);

        return clips;
    }

    private static List<StateClipEntry> BuildFallbackStates(
        AnimatorController sourceController,
        Dictionary<AnimationClip, AnimationClip> overrides,
        WeaponFamily weaponFamily)
    {
        var uniqueClips = new Dictionary<string, AnimationClip>();
        var orderedStates = new List<StateClipEntry>();
        string preferredWeaponFamily = GetWeaponFamilyDefinition(weaponFamily).PreferredFilter;

        foreach (AnimatorControllerLayer layer in sourceController.layers)
            CollectFromStateMachine(layer.stateMachine, uniqueClips, orderedStates, overrides, preferredWeaponFamily);

        if (orderedStates.Count == 0)
        {
            foreach (AnimatorControllerLayer layer in sourceController.layers)
                CollectFromStateMachine(layer.stateMachine, uniqueClips, orderedStates, overrides, preferredFamilyFilter: null);
        }

        return orderedStates;
    }

    private static string GetFamilyPrefixFromTargetPath(string targetControllerPath, string generatedSuffix)
    {
        string filename = Path.GetFileNameWithoutExtension(targetControllerPath);
        string characterPrefix = filename.StartsWith("HumanF", System.StringComparison.OrdinalIgnoreCase) ? "HumanF" : "HumanM";
        return string.IsNullOrWhiteSpace(generatedSuffix) ? characterPrefix : $"{characterPrefix}_{generatedSuffix}";
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (string.IsNullOrWhiteSpace(assetFolder) || AssetDatabase.IsValidFolder(assetFolder))
            return;

        string normalized = assetFolder.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }


    private static void CollectFromStateMachine(
        AnimatorStateMachine stateMachine,
        Dictionary<string, AnimationClip> uniqueClips,
        List<StateClipEntry> orderedStates,
        Dictionary<AnimationClip, AnimationClip> overrides,
        string preferredFamilyFilter)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            if (childState.state == null || childState.state.motion == null)
                continue;

            CollectFromMotion(childState.state.name, childState.state.motion, uniqueClips, orderedStates, overrides, preferredFamilyFilter);
        }

        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
        {
            if (childStateMachine.stateMachine == null)
                continue;

            CollectFromStateMachine(childStateMachine.stateMachine, uniqueClips, orderedStates, overrides, preferredFamilyFilter);
        }
    }

    private static void CollectFromMotion(
        string stateName,
        Motion motion,
        Dictionary<string, AnimationClip> uniqueClips,
        List<StateClipEntry> orderedStates,
        Dictionary<AnimationClip, AnimationClip> overrides,
        string preferredFamilyFilter)
    {
        if (!ShouldIncludeState(stateName, preferredFamilyFilter))
            return;

        if (motion is AnimationClip clip)
        {
            AnimationClip resolvedClip = ResolveClip(clip, overrides);
            if (resolvedClip == null)
                return;

            string key = $"{AssetDatabase.GetAssetPath(resolvedClip)}::{resolvedClip.name}";
            if (uniqueClips.ContainsKey(key))
                return;

            uniqueClips.Add(key, resolvedClip);
            orderedStates.Add(new StateClipEntry(GetStateName(stateName, resolvedClip.name), resolvedClip));
            return;
        }

        if (motion is BlendTree blendTree)
        {
            foreach (ChildMotion childMotion in blendTree.children)
            {
                if (childMotion.motion == null)
                    continue;

                CollectFromMotion(GetStateName(stateName, childMotion.motion.name), childMotion.motion, uniqueClips, orderedStates, overrides, preferredFamilyFilter);
            }
        }
    }

    private static bool ShouldIncludeState(string stateName, string preferredFamilyFilter)
    {
        if (string.IsNullOrWhiteSpace(preferredFamilyFilter))
            return true;

        string name = stateName.ToLowerInvariant();
        string family = preferredFamilyFilter.ToLowerInvariant();

        if (name.Contains(family))
            return true;

        // Keep shared locomotion/idle/support states that the weapon family still needs.
        return name.Contains("military idle")
            || name.Contains("idle")
            || name.Contains("aim")
            || name.Contains("reload")
            || name.Contains("damage")
            || name.Contains("death");
    }

    private static bool TryGetSourceData(
        RuntimeAnimatorController source,
        out AnimatorController sourceController,
        out Dictionary<AnimationClip, AnimationClip> overrides)
    {
        sourceController = null;
        overrides = new Dictionary<AnimationClip, AnimationClip>();

        if (source is AnimatorController controller)
        {
            sourceController = controller;
            return true;
        }

        if (source is AnimatorOverrideController overrideController)
        {
            sourceController = overrideController.runtimeAnimatorController as AnimatorController;
            if (sourceController == null)
                return false;

            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(pairs);
            foreach (var pair in pairs)
            {
                if (pair.Key == null)
                    continue;

                overrides[pair.Key] = pair.Value != null ? pair.Value : pair.Key;
            }

            return true;
        }

        return false;
    }

    private static AnimationClip ResolveClip(AnimationClip clip, Dictionary<AnimationClip, AnimationClip> overrides)
    {
        if (clip == null)
            return null;

        if (overrides != null && overrides.TryGetValue(clip, out AnimationClip overriddenClip) && overriddenClip != null)
            return overriddenClip;

        return clip;
    }

    private static bool IsPreferredDefault(StateClipEntry entry)
    {
        string name = entry.StateName.ToLowerInvariant();
        if (name == "rifle")
            return true;

        if (name == "assault rifle")
            return true;

        if (name == "gun")
            return true;

        if (name == "dualgun")
            return true;

        if (name == "bazooka")
            return true;

        if (name.Contains("military idle"))
            return true;

        if (name.Contains("hold weapon"))
            return true;

        if (name.Contains("rifle") && name.Contains("idle"))
            return true;

        return name.Contains("idle") || name.Contains("locomotion idle");
    }

    private static string GetStateName(string preferredName, string fallbackName)
    {
        return string.IsNullOrWhiteSpace(preferredName) ? fallbackName : preferredName;
    }

    private readonly struct StateClipEntry
    {
        public StateClipEntry(string stateName, AnimationClip clip)
        {
            StateName = stateName;
            Clip = clip;
        }

        public string StateName { get; }
        public AnimationClip Clip { get; }
    }

    private readonly struct GeneratedStateDefinition
    {
        public GeneratedStateDefinition(
            string name,
            bool loop,
            string[] lowerStateCandidates,
            string[] upperStateCandidates = null,
            string[] preferredLowerLayers = null,
            string[] preferredUpperLayers = null,
            string directStateName = null,
            string holdStateName = null)
        {
            Name = name;
            Loop = loop;
            LowerStateCandidates = lowerStateCandidates;
            UpperStateCandidates = upperStateCandidates;
            PreferredLowerLayers = preferredLowerLayers ?? new[] { "Movement", "Idles", "Prone" };
            PreferredUpperLayers = preferredUpperLayers ?? new[] { "Weapon Hold Arms", "Weapon Hold Hand L", "Weapon Hold Hand R", "Shoots Upper Body", "Shoots Full Body" };
            DirectStateName = directStateName;
            HoldStateName = holdStateName;
        }

        public string Name { get; }
        public bool Loop { get; }
        public string[] LowerStateCandidates { get; }
        public string[] UpperStateCandidates { get; }
        public string[] PreferredLowerLayers { get; }
        public string[] PreferredUpperLayers { get; }
        public string DirectStateName { get; }
        public string HoldStateName { get; }
    }

    private readonly struct SourceStateClipEntry
    {
        public SourceStateClipEntry(string layerName, string stateName, AnimationClip clip)
        {
            LayerName = layerName;
            StateName = stateName;
            Clip = clip;
        }

        public string LayerName { get; }
        public string StateName { get; }
        public AnimationClip Clip { get; }
    }

    private static WeaponFamilyDefinition GetWeaponFamilyDefinition(WeaponFamily weaponFamily)
    {
        switch (weaponFamily)
        {
            case WeaponFamily.Gun:
                return new WeaponFamilyDefinition(
                    "Gun",
                    "gun",
                    new[] { "Gun", "Hold Gun L", "Hold Gun R" },
                    new[] { "Gun Aim 01", "Aim Gun" },
                    new[] { "Gun Shoot01", "Gun Shoot 01", "Gun Shoot02", "Gun Shoot 02", "Gun Shoot03", "Gun Shoot 03" },
                    new[] { "Reload Gun", "Reload" },
                    directAimStateName: "Aim Gun",
                    directShootStateName: "Gun Shoot01",
                    grenadeCandidates: new[] { "Grenade R", "Grenade L", "Grenade01 R", "Grenade01 L", "Grenade02 R", "Grenade02 L" },
                    directGrenadeStateName: "Grenade R");
            case WeaponFamily.DualGun:
                return new WeaponFamilyDefinition(
                    "DualGun",
                    "dualgun",
                    new[] { "DualGun", "Dual Gun Aim 01" },
                    new[] { "Aim Dual Gun", "DualGun Aiming", "Dual Gun Aim 01" },
                    new[] { "DualGun Shoot01", "DualGun Shoot 01", "Dual Gun Shoot 01", "DualGun Shoot02", "DualGun Shoot 02", "Dual Gun Shoot 02", "DualGun Shoot03", "DualGun Shoot 03", "Dual Gun Shoot 03" },
                    new[] { "Reload DualGun", "Reload" },
                    includeHandHoldClips: true,
                    directAimStateName: "Dual Gun Aim 01",
                    directShootStateName: "Dual Gun Shoot 01",
                    grenadeCandidates: new[] { "Grenade R", "Grenade L", "Grenade01 R", "Grenade01 L", "Grenade02 R", "Grenade02 L" },
                    directGrenadeStateName: "Grenade R");
            case WeaponFamily.Bazooka:
                return new WeaponFamilyDefinition(
                    "Bazooka",
                    "bazooka",
                    new[] { "Bazooka" },
                    new[] { "Bazooka Aim", "Aim Bazooka" },
                    new[] { "Bazooka Shoot 01", "Bazooka Shoot01" },
                    new[] { "Reload Bazooka", "Reload" },
                    directAimStateName: "Aim Bazooka",
                    directShootStateName: "Bazooka Shoot 01",
                    grenadeCandidates: new[] { "Grenade Bazooka" },
                    directGrenadeStateName: "Grenade Bazooka");
            case WeaponFamily.AssaultRifle:
                return new WeaponFamilyDefinition(
                    "AssaultRifle",
                    "assault rifle",
                    new[] { "Assault Rifle", "AssaultRifle" },
                    new[] { "Assault Rifle Aim", "Aim Assault Rifle", "Assault Rifle Aim 01", "Assault Rifle Only Aim" },
                    new[] { "Assault Rifle Shoot 01", "AssaultRifle Shoot 01", "Assault Rifle Shoot 02", "AssaultRifle Shoot 02", "Assault Rifle Shoot 03", "AssaultRifle Shoot 03" },
                    new[] { "Reload Assault Rifle", "Assault Rifle Reload", "Reload" },
                    directAimStateName: "Assault Rifle Only Aim",
                    directShootStateName: "Assault Rifle Shoot 01",
                    grenadeCandidates: new[] { "Grenade Assault Rifle" },
                    directGrenadeStateName: "Grenade Assault Rifle");
            default:
                return new WeaponFamilyDefinition(
                    "Rifle",
                    "rifle",
                    new[] { "Rifle" },
                    new[] { "Rifle Aim", "Aim Rifle", "Rifle Aim 01", "Rifle Aiming", "Rifle Only Aim" },
                    new[] { "Rifle Shoot 01", "Rifle Shoot 02", "Rifle Shoot 03" },
                    new[] { "Reload Rifle", "Rifle Reload", "Reload" },
                    directAimStateName: "Rifle Only Aim",
                    directShootStateName: "Rifle Shoot 01",
                    grenadeCandidates: new[] { "Grenade Rifle" },
                    directGrenadeStateName: "Grenade Rifle");
        }
    }

    private readonly struct WeaponFamilyDefinition
    {
        public WeaponFamilyDefinition(
            string generatedSuffix,
            string preferredFilter,
            string[] holdCandidates,
            string[] aimCandidates,
            string[] shootCandidates,
            string[] reloadCandidates,
            bool includeHandHoldClips = false,
            string directAimStateName = null,
            string directShootStateName = null,
            string[] grenadeCandidates = null,
            string directGrenadeStateName = null)
        {
            GeneratedSuffix = generatedSuffix;
            PreferredFilter = preferredFilter;
            HoldCandidates = holdCandidates;
            AimCandidates = aimCandidates;
            ShootCandidates = shootCandidates;
            ReloadCandidates = reloadCandidates;
            IncludeHandHoldClips = includeHandHoldClips;
            DirectAimStateName = directAimStateName;
            DirectShootStateName = directShootStateName;
            GrenadeCandidates = grenadeCandidates ?? System.Array.Empty<string>();
            DirectGrenadeStateName = directGrenadeStateName;
        }

        public string GeneratedSuffix { get; }
        public string PreferredFilter { get; }
        public string HoldStateName => HoldCandidates != null && HoldCandidates.Length > 0 ? HoldCandidates[0] : "Rifle";
        public string[] DirectHoldStateCandidates => HoldCandidates;
        public string[] DirectAimStateCandidates => !string.IsNullOrWhiteSpace(DirectAimStateName) ? new[] { DirectAimStateName }.Concat(AimCandidates ?? System.Array.Empty<string>()).ToArray() : AimCandidates;
        public string[] DirectShootStateCandidates => !string.IsNullOrWhiteSpace(DirectShootStateName) ? new[] { DirectShootStateName }.Concat(ShootCandidates ?? System.Array.Empty<string>()).ToArray() : ShootCandidates;
        public string[] HoldCandidates { get; }
        public string[] AimCandidates { get; }
        public string[] ShootCandidates { get; }
        public string[] ReloadCandidates { get; }
        public bool IncludeHandHoldClips { get; }
        public string DirectAimStateName { get; }
        public string DirectShootStateName { get; }
        public string[] GrenadeCandidates { get; }
        public string DirectGrenadeStateName { get; }
        public string[] DirectGrenadeStateCandidates => !string.IsNullOrWhiteSpace(DirectGrenadeStateName) ? new[] { DirectGrenadeStateName }.Concat(GrenadeCandidates ?? System.Array.Empty<string>()).ToArray() : GrenadeCandidates;
    }

    private enum WeaponFamily
    {
        Rifle,
        Gun,
        DualGun,
        Bazooka,
        AssaultRifle
    }

}

#endif
