#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public sealed class SoldierAnimatorFlattenerWindow : EditorWindow
{
    private RuntimeAnimatorController _sourceController;
    private RuntimeAnimatorController _lastSourceController;
    private DefaultAsset _outputFolder;
    private string _outputNamePrefix = string.Empty;
    private readonly Dictionary<string, bool> _stateSelections = new Dictionary<string, bool>();
    private HashSet<string> _availableStates = new HashSet<string>();
    private Vector2 _stateScroll;

    public static void OpenWindow()
    {
        var window = GetWindow<SoldierAnimatorFlattenerWindow>("Soldier Flattener");
        window.minSize = new Vector2(420f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        if (_outputFolder == null)
            _outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Game/Animations");
        EnsureStateSelections();
        RefreshAvailableStates(forceResetSelections: true);
    }

    private void OnGUI()
    {
        EnsureStateSelections();

        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        _sourceController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Original Animator", _sourceController, typeof(RuntimeAnimatorController), false);
        _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", _outputFolder, typeof(DefaultAsset), false);

        RefreshAvailableStates(forceResetSelections: _sourceController != _lastSourceController);

        if (string.IsNullOrWhiteSpace(_outputNamePrefix) && _sourceController != null)
            _outputNamePrefix = _sourceController.name;

        _outputNamePrefix = EditorGUILayout.TextField("Output Prefix", _outputNamePrefix);

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Output Animations", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("All", GUILayout.Width(60f)))
                SetAllStates(true);

            if (GUILayout.Button("None", GUILayout.Width(60f)))
                SetAllStates(false);
        }

        _stateScroll = EditorGUILayout.BeginScrollView(_stateScroll, GUILayout.Height(180f));
        foreach (string stateName in SoldierAnimatorFlattener.GetDefaultOutputStateNames())
        {
            bool available = _availableStates.Contains(stateName);
            using (new EditorGUI.DisabledScope(!available))
            {
                _stateSelections[stateName] = EditorGUILayout.ToggleLeft(stateName, _stateSelections[stateName]);
            }
        }
        EditorGUILayout.EndScrollView();

        IEnumerable<string> unavailableStates = SoldierAnimatorFlattener.GetDefaultOutputStateNames().Where(state => !_availableStates.Contains(state));
        if (unavailableStates.Any())
        {
            EditorGUILayout.HelpBox($"Unavailable from source animator: {string.Join(", ", unavailableStates)}", MessageType.Info);
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Generate", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(_sourceController == null || !IsValidFolder(_outputFolder)))
        {
            DrawWeaponButtonRow("Rifle", "Gun");
            DrawWeaponButtonRow("DualGun", "Bazooka");
            DrawSingleButton("AssaultRifle");

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("All Weapons", GUILayout.Height(28f)))
                GenerateAllWeapons();
        }

        if (_outputFolder != null && !IsValidFolder(_outputFolder))
            EditorGUILayout.HelpBox("Output Folder must be a folder asset inside the project.", MessageType.Warning);
    }

    private void DrawWeaponButtonRow(string leftWeaponFamily, string rightWeaponFamily)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(leftWeaponFamily, GUILayout.Height(28f)))
                Generate(leftWeaponFamily);

            if (GUILayout.Button(rightWeaponFamily, GUILayout.Height(28f)))
                Generate(rightWeaponFamily);
        }
    }

    private void DrawSingleButton(string weaponFamily)
    {
        if (GUILayout.Button(weaponFamily, GUILayout.Height(28f)))
            Generate(weaponFamily);
    }

    private void GenerateAllWeapons()
    {
        Generate("Rifle");
        Generate("Gun");
        Generate("DualGun");
        Generate("Bazooka");
        Generate("AssaultRifle");
    }

    private void Generate(string weaponFamily)
    {
        string outputFolderPath = AssetDatabase.GetAssetPath(_outputFolder);
        List<string> selectedStates = GetSelectedStateNames().ToList();
        Debug.Log($"SoldierAnimatorFlattenerWindow generating '{weaponFamily}' with selected states: {string.Join(", ", selectedStates)}");
        SoldierAnimatorFlattener.GenerateCustomFlatController(_sourceController, outputFolderPath, _outputNamePrefix, weaponFamily, selectedStates);
    }

    private void EnsureStateSelections()
    {
        foreach (string stateName in SoldierAnimatorFlattener.GetDefaultOutputStateNames())
        {
            if (!_stateSelections.ContainsKey(stateName))
                _stateSelections[stateName] = true;
        }
    }

    private void RefreshAvailableStates(bool forceResetSelections)
    {
        if (_sourceController == _lastSourceController && !forceResetSelections)
            return;

        _lastSourceController = _sourceController;
        _availableStates = new HashSet<string>(SoldierAnimatorFlattener.GetAvailableOutputStateNames(_sourceController));

        foreach (string stateName in SoldierAnimatorFlattener.GetDefaultOutputStateNames())
        {
            if (!_availableStates.Contains(stateName))
            {
                _stateSelections[stateName] = false;
            }
            else if (forceResetSelections)
            {
                _stateSelections[stateName] = true;
            }
        }
    }

    private void SetAllStates(bool value)
    {
        foreach (string stateName in SoldierAnimatorFlattener.GetDefaultOutputStateNames())
            _stateSelections[stateName] = value;
    }

    private IEnumerable<string> GetSelectedStateNames()
    {
        foreach (string stateName in SoldierAnimatorFlattener.GetDefaultOutputStateNames())
        {
            if (_availableStates.Contains(stateName) && _stateSelections.TryGetValue(stateName, out bool selected) && selected)
                yield return stateName;
        }
    }

    private static bool IsValidFolder(DefaultAsset folderAsset)
    {
        if (folderAsset == null)
            return false;

        string path = AssetDatabase.GetAssetPath(folderAsset);
        return !string.IsNullOrWhiteSpace(path) && AssetDatabase.IsValidFolder(path);
    }
}

#endif
