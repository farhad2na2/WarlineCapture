namespace Game.Editor
{
    #if UNITY_EDITOR

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    public sealed class SoldierAnimatorComboBakerWindow : EditorWindow
    {
        private const string DefaultMalePrefabPath =
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/Prefabs/SoldierRifleM.prefab";

        private const string DefaultMaleControllerPath =
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Soldier Animations/AnimatorControllers/HumanM@SoldierAnimations.controller";

        private const string DefaultOutputFolder = "Assets/Game/Animations/BakedSoldierCombos";
        private const string HumanSoldierControllerTypeName = "KevinIglesias.HumanSoldierController, KevinIglesias.HumanSoldierDemo";
        private const string SpineProxyTypeName = "KevinIglesias.SpineProxy, KevinIglesias.HumanAnimations";

        [SerializeField] private GameObject sourcePrefab;
        [SerializeField] private RuntimeAnimatorController sourceController;
        [SerializeField] private string outputFolder = DefaultOutputFolder;
        [SerializeField] private string clipPrefix = "HumanM_Combo";
        [SerializeField] private List<BakeComboDefinition> combos = new();
        [SerializeField] private Vector2 scrollPosition;

        [MenuItem("Game/Soldier Animator Combo Baker")]
        public static void Open()
        {
            var window = GetWindow<SoldierAnimatorComboBakerWindow>();
            window.titleContent = new GUIContent("Soldier Combo Baker");
            window.minSize = new Vector2(620f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            sourcePrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>(DefaultMalePrefabPath);
            sourceController ??= AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultMaleControllerPath);

            if (combos == null || combos.Count == 0)
                combos = CreateDefaultCombos();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
                sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", sourcePrefab, typeof(GameObject), false);
                sourceController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Source Controller", sourceController, typeof(RuntimeAnimatorController), false);
                outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
                clipPrefix = EditorGUILayout.TextField("Clip Prefix", clipPrefix);

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Combos", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                for (int i = 0; i < combos.Count; i++)
                {
                    BakeComboDefinition combo = combos[i];
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        combo.name = EditorGUILayout.TextField("Name", combo.name);
                        combo.weapon = EditorGUILayout.TextField("Weapon", combo.weapon);
                        combo.position = EditorGUILayout.TextField("Position", combo.position);
                        combo.action = EditorGUILayout.TextField("Action", combo.action);
                        combo.movement = EditorGUILayout.TextField("Movement", combo.movement);
                        combo.settleFrames = EditorGUILayout.IntField("Settle Frames", combo.settleFrames);
                        combo.recordFrames = EditorGUILayout.IntField("Record Frames", combo.recordFrames);
                        combos[i] = combo;
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset Defaults"))
                        combos = CreateDefaultCombos();

                    if (GUILayout.Button("Add Combo"))
                        combos.Add(new BakeComboDefinition { name = "NewCombo", weapon = "Rifle", position = "StandUp", action = "Nothing", movement = "NoMovement", settleFrames = 20, recordFrames = 30 });
                }

                EditorGUILayout.Space(12f);
                using (new EditorGUI.DisabledScope(sourcePrefab == null || sourceController == null || string.IsNullOrWhiteSpace(outputFolder)))
                {
                    if (GUILayout.Button("Bake All Combos", GUILayout.Height(36f)))
                        BakeAllCombos();
                }
            }
        }

        private void BakeAllCombos()
        {
            EnsureFolderExists(outputFolder);
            var instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("SoldierAnimatorComboBaker could not instantiate the source prefab.");
                return;
            }

            try
            {
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                Animator animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    Debug.LogError("SoldierAnimatorComboBaker could not find an Animator under the source prefab.");
                    return;
                }

                animator.runtimeAnimatorController = sourceController;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;

                Type controllerType = Type.GetType(HumanSoldierControllerTypeName);
                Component soldierController = controllerType != null ? instance.GetComponentInChildren(controllerType, true) : null;
                Component[] spineProxies = GetComponentsByTypeName(instance, SpineProxyTypeName);

                if (spineProxies.Length > 0)
                {
                    foreach (Component spineProxy in spineProxies)
                        InvokeMethod(spineProxy, "Awake");
                }

                int bakedCount = 0;
                foreach (BakeComboDefinition combo in combos)
                {
                    if (string.IsNullOrWhiteSpace(combo.name))
                        continue;

                    if (BakeSingleCombo(instance, animator, soldierController, spineProxies, combo))
                        bakedCount++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"SoldierAnimatorComboBaker baked {bakedCount} combo clips into '{outputFolder}'.");
            }
            finally
            {
                DestroyImmediate(instance);
            }
        }

        private bool BakeSingleCombo(GameObject instance, Animator animator, Component soldierController, Component[] spineProxies, BakeComboDefinition combo)
        {
            string clipPath = $"{outputFolder}/{clipPrefix}_{combo.name}.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(clipPath);

            animator.Rebind();
            animator.Update(0f);

            SetControllerState(soldierController, combo);

            var recorder = new GameObjectRecorder(instance);
            BindAnimatedChildren(recorder, instance.transform);

            const float sampleRate = 30f;
            float deltaTime = 1f / sampleRate;

            DrivePreviewFrame(soldierController, animator, spineProxies, 0f);

            for (int i = 0; i < Mathf.Max(1, combo.settleFrames); i++)
                DrivePreviewFrame(soldierController, animator, spineProxies, deltaTime);

            for (int i = 0; i < Mathf.Max(1, combo.recordFrames); i++)
            {
                DrivePreviewFrame(soldierController, animator, spineProxies, deltaTime);
                recorder.TakeSnapshot(deltaTime);
            }

            var recordedClip = new AnimationClip
            {
                name = $"{clipPrefix}_{combo.name}",
                frameRate = sampleRate
            };

            recorder.SaveToClip(recordedClip);
            AnimationClip clip = CreateRotationOnlyClip(recordedClip);
            clip.name = recordedClip.name;
            clip.frameRate = sampleRate;
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings
            {
                loopTime = IsLoopingCombo(combo.name)
            });

            AssetDatabase.CreateAsset(clip, clipPath);
            return true;
        }

        private static AnimationClip CreateRotationOnlyClip(AnimationClip sourceClip)
        {
            var sanitizedClip = new AnimationClip
            {
                frameRate = sourceClip.frameRate
            };

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
            {
                string propertyName = binding.propertyName;
                bool isLocalRotation =
                    propertyName == "m_LocalRotation.x" ||
                    propertyName == "m_LocalRotation.y" ||
                    propertyName == "m_LocalRotation.z" ||
                    propertyName == "m_LocalRotation.w";

                if (!isLocalRotation)
                    continue;

                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                if (curve != null)
                    AnimationUtility.SetEditorCurve(sanitizedClip, binding, curve);
            }

            sanitizedClip.EnsureQuaternionContinuity();
            return sanitizedClip;
        }

        private static void DrivePreviewFrame(Component soldierController, Animator animator, Component[] spineProxies, float deltaTime)
        {
            InvokeMethod(soldierController, "Update");
            animator.Update(deltaTime);
            if (spineProxies == null)
                return;

            foreach (Component spineProxy in spineProxies)
                InvokeMethod(spineProxy, "LateUpdate");
        }

        private static void BindAnimatedChildren(GameObjectRecorder recorder, Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root)
                    continue;
                recorder.BindComponentsOfType<Transform>(child.gameObject, false);
                recorder.BindComponentsOfType<SkinnedMeshRenderer>(child.gameObject, false);
            }
        }

        private static void SetControllerState(Component controller, BakeComboDefinition combo)
        {
            if (controller == null)
            {
                Debug.LogError("SoldierAnimatorComboBaker could not find HumanSoldierController on the source prefab.");
                return;
            }

            Type controllerType = controller.GetType();

            SetEnumField(controllerType, controller, "equippedWeapon", combo.weapon);
            SetEnumField(controllerType, controller, "position", combo.position);
            SetEnumField(controllerType, controller, "action", combo.action);
            SetEnumField(controllerType, controller, "movement", combo.movement);

            MethodInfo changeWeaponMethod = controllerType.GetMethod("ChangeWeapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo equippedWeaponField = controllerType.GetField("equippedWeapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (changeWeaponMethod != null && equippedWeaponField != null)
            {
                object equippedWeaponValue = equippedWeaponField.GetValue(controller);
                changeWeaponMethod.Invoke(controller, new[] { equippedWeaponValue });
            }
        }

        private static void SetEnumField(Type controllerType, Component controller, string fieldName, string enumName)
        {
            FieldInfo field = controllerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || string.IsNullOrWhiteSpace(enumName))
                return;

            try
            {
                object enumValue = Enum.Parse(field.FieldType, enumName);
                field.SetValue(controller, enumValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SoldierAnimatorComboBaker could not assign '{enumName}' to field '{fieldName}': {ex.Message}");
            }
        }

        private static void InvokeMethod(Component component, string methodName)
        {
            if (component == null)
                return;

            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.GetParameters().Length == 0)
                method.Invoke(component, null);
        }

        private static Component[] GetComponentsByTypeName(GameObject root, string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
                return Array.Empty<Component>();

            return root.GetComponentsInChildren(type, true);
        }

        private static bool IsLoopingCombo(string comboName)
        {
            string lower = comboName.ToLowerInvariant();
            return lower.Contains("idle") || lower.Contains("walk") || lower.Contains("run") || lower.Contains("aim");
        }

        private static void EnsureFolderExists(string assetFolder)
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

        private static List<BakeComboDefinition> CreateDefaultCombos()
        {
            return new List<BakeComboDefinition>
            {
                new() { name = "Idle", weapon = "Rifle", position = "StandUp", action = "Nothing", movement = "NoMovement", settleFrames = 20, recordFrames = 30 },
                new() { name = "IdleHoldWeapon", weapon = "Rifle", position = "StandUp", action = "HoldWeapon", movement = "NoMovement", settleFrames = 20, recordFrames = 30 },
                new() { name = "Aim", weapon = "Rifle", position = "StandUp", action = "Aim", movement = "NoMovement", settleFrames = 20, recordFrames = 30 },
                new() { name = "Shoot", weapon = "Rifle", position = "StandUp", action = "Shoot01", movement = "NoMovement", settleFrames = 20, recordFrames = 30 },
                new() { name = "Walk", weapon = "Rifle", position = "StandUp", action = "Nothing", movement = "Walk", settleFrames = 20, recordFrames = 30 },
                new() { name = "WalkHoldWeapon", weapon = "Rifle", position = "StandUp", action = "HoldWeapon", movement = "Walk", settleFrames = 20, recordFrames = 30 },
                new() { name = "WalkAim", weapon = "Rifle", position = "StandUp", action = "Aim", movement = "Walk", settleFrames = 20, recordFrames = 30 },
                new() { name = "WalkShoot", weapon = "Rifle", position = "StandUp", action = "Shoot01", movement = "Walk", settleFrames = 20, recordFrames = 30 },
                new() { name = "Run", weapon = "Rifle", position = "StandUp", action = "Nothing", movement = "Run", settleFrames = 20, recordFrames = 30 },
                new() { name = "RunHoldWeapon", weapon = "Rifle", position = "StandUp", action = "HoldWeapon", movement = "Run", settleFrames = 20, recordFrames = 30 },
                new() { name = "RunAim", weapon = "Rifle", position = "StandUp", action = "Aim", movement = "Run", settleFrames = 20, recordFrames = 30 },
                new() { name = "RunShoot", weapon = "Rifle", position = "StandUp", action = "Shoot01", movement = "Run", settleFrames = 20, recordFrames = 30 }
            };
        }

        [Serializable]
        private struct BakeComboDefinition
        {
            public string name;
            public string weapon;
            public string position;
            public string action;
            public string movement;
            public int settleFrames;
            public int recordFrames;
        }
    }

    #endif
}
