#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SnivelerCode.GpuAnimation.Editor.Scripts
{
    public class GeneratorWindow : EditorWindow
    {
        const string MaleGeneratedClipFolder = "Assets/Game/Animations/FlatGenerated/HumanM";
        const string FemaleGeneratedClipFolder = "Assets/Game/Animations/FlatGenerated/HumanF";

        [SerializeField] Shader instanceShader;
        [SerializeField] int batchTextureSize = 4096;
        [SerializeField] bool batchTextureReadable = true;
        [SerializeField] List<GameObject> sourcePrefabs = new();
        [SerializeField] int defaultLodPercent = 10;
        [SerializeField] int defaultClipFps = 60;
        [SerializeField] string outputFolder = "Assets/Game/Prefabs/Generated";
        List<PrefabInstance> m_PrefabInstances;
        SerializedObject m_SerializedObject;
        string m_LastPrefabListSignature = string.Empty;
        
        [MenuItem("Window/Sniveler Code/Animator Baker", false)]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(GeneratorWindow));
            window.titleContent = new GUIContent("Animator Baker");
        }

        T RootQuery<T>(string elementName) where T : VisualElement => rootVisualElement.Q<T>(elementName);
        StyleEnum<DisplayStyle> GetDisplay(bool value) => new(value ? DisplayStyle.Flex : DisplayStyle.None);

        public void CreateGUI()
        {
            m_PrefabInstances = new List<PrefabInstance>();
            m_SerializedObject = new SerializedObject(this);

            var uiAsset = PackageResourceLoader.LoadGuiTemplate("GenerateWindow");
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            scrollView.style.flexGrow = 1f;
            scrollView.style.minHeight = 0f;
            scrollView.contentContainer.style.flexGrow = 0f;
            scrollView.contentContainer.style.justifyContent = Justify.FlexStart;
            scrollView.contentContainer.style.alignItems = Align.Stretch;

            var root = uiAsset.Instantiate();
            root.style.flexGrow = 1f;
            scrollView.Add(root);
            rootVisualElement.Add(scrollView);
            AddBatchTextureSettings(root);
            AddBakeDefaults(root);
            
            var prefabContent = RootQuery<VisualElement>("BatchActions");
            prefabContent.SetEnabled(false);

            var batchFieldName = RootQuery<TextField>("BatchName");
            batchFieldName.RegisterValueChangedCallback(evt =>
                prefabContent.SetEnabled(evt.newValue.Length > 5));

            AddPrefabListField(root, batchFieldName, prefabContent);
            AddOutputFolderField(root);

            var addPrefabButton = prefabContent.Q<Button>("AddPrefab");
            addPrefabButton.style.display = DisplayStyle.None;
            var resetButton = new Button(() => ResetWorkflow(batchFieldName, prefabContent)) { text = "Reset" };
            resetButton.style.marginTop = 3;
            resetButton.style.marginBottom = 3;
            prefabContent.Insert(0, resetButton);
            prefabContent.Q<Button>("AddPrefab").RegisterCallback<ClickEvent>(OnPrefabButtonClick);
            prefabContent.Q<Button>("Generate").RegisterCallback<ClickEvent>(_ =>
            {
                try
                {
                    GenerateProcessor.Generate(outputFolder, batchFieldName.text, instanceShader, m_PrefabInstances, batchTextureSize, batchTextureReadable);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }
            });

            SyncPrefabListFromSerialized(batchFieldName, prefabContent, forceRefresh: true);
        }

        void AddBatchTextureSettings(VisualElement root)
        {
            var sizeChoices = new List<int> { 256, 512, 1024, 2048, 4096, 8192 };
            if (!sizeChoices.Contains(batchTextureSize))
            {
                sizeChoices.Add(batchTextureSize);
                sizeChoices.Sort();
            }

            var settingsRow = new VisualElement();
            settingsRow.style.flexDirection = FlexDirection.Row;
            settingsRow.style.alignItems = Align.Center;
            settingsRow.style.marginTop = 3;
            settingsRow.style.marginBottom = 3;

            var sizeField = new PopupField<int>("BatchTexture Size", sizeChoices, batchTextureSize);
            sizeField.style.minWidth = 180;
            sizeField.style.marginRight = 8;
            sizeField.RegisterValueChangedCallback(evt => batchTextureSize = evt.newValue);

            var readableField = new Toggle("BatchTexture Read/Write Enabled");
            readableField.value = batchTextureReadable;
            readableField.RegisterValueChangedCallback(evt => batchTextureReadable = evt.newValue);

            settingsRow.Add(sizeField);
            settingsRow.Add(readableField);
            root.Insert(1, settingsRow);
        }

        void AddBakeDefaults(VisualElement root)
        {
            var settingsRow = new VisualElement();
            settingsRow.style.flexDirection = FlexDirection.Row;
            settingsRow.style.alignItems = Align.Center;
            settingsRow.style.marginTop = 3;
            settingsRow.style.marginBottom = 3;

            var defaultLodField = new IntegerField("Default LOD %")
            {
                value = defaultLodPercent
            };
            defaultLodField.style.minWidth = 180;
            defaultLodField.style.marginRight = 8;
            defaultLodField.RegisterValueChangedCallback(evt => defaultLodPercent = Mathf.Clamp(evt.newValue, 1, 100));

            var defaultFpsField = new IntegerField("Default FPS")
            {
                value = defaultClipFps
            };
            defaultFpsField.style.minWidth = 180;
            defaultFpsField.RegisterValueChangedCallback(evt => defaultClipFps = Mathf.Clamp(evt.newValue, 1, 240));

            settingsRow.Add(defaultLodField);
            settingsRow.Add(defaultFpsField);
            root.Insert(2, settingsRow);
        }

        void AddPrefabListField(VisualElement root, TextField batchFieldName, VisualElement prefabContent)
        {
            var prefabField = new PropertyField(m_SerializedObject.FindProperty(nameof(sourcePrefabs)), "Prefabs")
            {
                name = "QuickPrefabListField"
            };
            prefabField.Bind(m_SerializedObject);
            prefabField.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                SyncPrefabListFromSerialized(batchFieldName, prefabContent);
            });

            root.Insert(2, prefabField);
        }

        void AddOutputFolderField(VisualElement root)
        {
            var outputField = new TextField("Output Folder")
            {
                value = outputFolder
            };

            outputField.RegisterValueChangedCallback(evt => outputFolder = evt.newValue);
            root.Insert(3, outputField);
        }

        void SyncPrefabListFromSerialized(TextField batchFieldName, VisualElement prefabContent, bool forceRefresh = false)
        {
            m_SerializedObject.Update();
            string signature = BuildPrefabListSignature();
            if (!forceRefresh && signature == m_LastPrefabListSignature)
                return;

            m_LastPrefabListSignature = signature;
            var container = RootQuery<VisualElement>("PrefabList");
            container.Clear();
            m_PrefabInstances.Clear();

            List<GameObject> validPrefabs = sourcePrefabs?.Where(prefab => prefab != null).ToList() ?? new List<GameObject>();
            if (validPrefabs.Count == 0)
            {
                prefabContent.SetEnabled(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(batchFieldName.value))
                batchFieldName.SetValueWithoutNotify(validPrefabs[0].name);

            for (int prefabIndex = 0; prefabIndex < validPrefabs.Count; prefabIndex++)
            {
                GameObject prefab = validPrefabs[prefabIndex];
                var prefabInstance = new PrefabInstance();
                var template = PackageResourceLoader.LoadGuiTemplate("PrefabTemplate")
                    .Instantiate().Q<VisualElement>("RootElement");

                var configToggle = template.Q<Toggle>("Config");
                var removeButton = template.Q<Button>("Remove");
                var prefabField = template.Q<ObjectField>("PrefabField");
                var prefabParams = template.Q<VisualElement>("PrefabParams");

                configToggle.value = true;
                configToggle.style.display = DisplayStyle.None;
                prefabParams.style.display = DisplayStyle.Flex;
                removeButton.style.display = DisplayStyle.None;
                prefabField.SetValueWithoutNotify(prefab);
                prefabField.SetEnabled(false);

                container.Add(template);
                m_PrefabInstances.Add(prefabInstance);

                ApplyPrefab(prefab, prefabInstance, template, prefabField);
            }

            prefabContent.SetEnabled(batchFieldName.value.Length > 0);
        }

        void ResetWorkflow(TextField batchFieldName, VisualElement prefabContent)
        {
            sourcePrefabs ??= new List<GameObject>();
            sourcePrefabs.Clear();
            m_SerializedObject.Update();
            m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
            m_LastPrefabListSignature = string.Empty;
            batchFieldName.SetValueWithoutNotify(string.Empty);
            var container = RootQuery<VisualElement>("PrefabList");
            container.Clear();
            m_PrefabInstances.Clear();

            var quickPrefabField = rootVisualElement.Q<PropertyField>("QuickPrefabListField");
            if (quickPrefabField != null)
            {
                quickPrefabField.Bind(m_SerializedObject);
            }

            prefabContent.SetEnabled(false);
        }

        string BuildPrefabListSignature()
        {
            if (sourcePrefabs == null || sourcePrefabs.Count == 0)
                return string.Empty;

            return string.Join("|", sourcePrefabs.Select(prefab =>
            {
                if (prefab == null)
                    return "<null>";

                string path = AssetDatabase.GetAssetPath(prefab);
                return string.IsNullOrEmpty(path) ? prefab.name : path;
            }));
        }

        void OnPrefabButtonClick(ClickEvent evt)
        {
            var prefabInstance = new PrefabInstance();

            var container = RootQuery<VisualElement>("PrefabList");
            var template = PackageResourceLoader.LoadGuiTemplate("PrefabTemplate")
                .Instantiate().Q<VisualElement>("RootElement");

            var configToggle = template.Q<Toggle>("Config");
            configToggle.SetEnabled(false);
            configToggle.RegisterValueChangedCallback(configEvent =>
                template.Q<VisualElement>("PrefabParams").style.display = GetDisplay(configEvent.newValue));
            
            template.Q<ObjectField>("PrefabField").RegisterValueChangedCallback(changeEvent =>
                PrefabChange(changeEvent, prefabInstance, template));

            template.Q<Button>("Remove").RegisterCallback<ClickEvent>(_ =>
            {
                container.Remove(template);
                m_PrefabInstances.Remove(prefabInstance);
            });

            container.Add(template);
            m_PrefabInstances.Add(prefabInstance);
        }

        void PrefabChange(ChangeEvent<Object> evt, PrefabInstance instance, VisualElement template)
        {
            ApplyPrefab((GameObject)evt.newValue, instance, template, (ObjectField)evt.target);
        }

        void ApplyPrefab(GameObject prefab, PrefabInstance instance, VisualElement template, ObjectField sourceField)
        {
            instance.Clear();

            var configToggle = template.Q<Toggle>("Config");
            var message = template.Q<Label>("ErrorMessage");
            var animatorField = template.Q<ObjectField>("AnimatorField");
            var lodsContent = template.Q<VisualElement>("LodsContent");

            lodsContent.Clear();
            animatorField.SetValueWithoutNotify(null);
            message.text = string.Empty;

            instance.Source = prefab;
            if (instance.Source == null)
            {
                configToggle.value = false;
                if (sourceField != null)
                    sourceField.SetValueWithoutNotify(null);
                return;
            }

            var skinRenderer = instance.Source.GetComponentInChildren<SkinnedMeshRenderer>();
            configToggle.SetEnabled(skinRenderer);
            if (!skinRenderer)
            {
                configToggle.value = false;
                if (sourceField != null)
                    sourceField.SetValueWithoutNotify(null);
                message.text = "no skinned renderer";
                return;
            }

            configToggle.value = true;
            instance.SetSkin(skinRenderer);
            if (instance.Lods.Count > 0)
                instance.Lods[0].Percent = Mathf.Clamp(defaultLodPercent, 1, 100);

            animatorField.UnregisterValueChangedCallback(animatorEvent => AnimatorChange(animatorEvent, instance, template));
            animatorField.RegisterValueChangedCallback(animatorEvent => AnimatorChange(animatorEvent, instance, template));

            var animator = instance.Source.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
            {
                if (animator.runtimeAnimatorController is AnimatorController animatorController)
                {
                    animatorField.value = animatorController;
                }
                else
                {
                    message.text = $"unsupported controller type: {animator.runtimeAnimatorController.GetType().Name}";
                    animatorField.SetValueWithoutNotify(null);
                }
            }

            instance.SubAlpha = new List<bool>();
            foreach (var material in instance.Skin.sharedMaterials)
            {
                var propertyAlpha = material.GetFloat(GenerateProcessor.AlphaClip);
                instance.SubAlpha.Add(math.abs(propertyAlpha - 1f) < 0.1f);
            }

            template.Q<Button>("AddLod").RegisterCallback<ClickEvent>(_ =>
                RenderLodElement(instance, instance.AddLod(), lodsContent));

            instance.Lods.ForEach(lod => RenderLodElement(instance, lod, lodsContent));
        }

        void RenderLodElement(PrefabInstance instance, LodInstance lodInstance, VisualElement content)
        {
            var lodTemplate = PackageResourceLoader.LoadGuiTemplate("LodTemplate")
                .Instantiate().Q<VisualElement>("RootElement");

            var percentField = lodTemplate.Q<SliderInt>("Percent");
            var meshField = lodTemplate.Q<ObjectField>("Mesh");
            var buttonField = lodTemplate.Q<Button>("Remove");
            var messageField = lodTemplate.Q<Label>("Message");

            percentField.value = lodInstance.Percent;
            meshField.value = lodInstance.Mesh;
                
            meshField.SetEnabled(!lodInstance.Locked);
            buttonField.SetEnabled(!lodInstance.Locked);
            
            buttonField.RegisterCallback<ClickEvent>(_ =>
            {
                content.Remove(lodTemplate);
                instance.Lods.Remove(lodInstance);
            });
            percentField.RegisterValueChangedCallback(percentEvent => lodInstance.Percent = percentEvent.newValue);
            meshField.RegisterValueChangedCallback(meshEvent =>
            {
                messageField.text = string.Empty;
                var meshValue = (Mesh)meshEvent.newValue;
                if (meshValue)
                {
                    var assetMeshPath = AssetDatabase.GetAssetPath(meshValue);
                    var assetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetMeshPath);
                    SkinnedMeshRenderer skinRenderer;
                    if (assetPrefab && (skinRenderer = assetPrefab.GetComponentInChildren<SkinnedMeshRenderer>()))
                    {
                        lodInstance.Mesh = meshValue;
                        lodInstance.Skin = skinRenderer;
                    }
                    else
                    {
                        messageField.text = "no skinned renderer";
                        meshField.SetValueWithoutNotify(null);
                    } 
                }
            });
                
            content.Add(lodTemplate);
        }

        void AnimatorChange(ChangeEvent<Object> evt, PrefabInstance instance, VisualElement template)
        {
            var animator = (AnimatorController)evt.newValue;
            var animationsContainer = template.Q<VisualElement>("AnimationsContainer");
            animationsContainer.style.display = GetDisplay(animator);
            animationsContainer.Clear();
            instance.Clips.Clear();

            if (!animator) return;
            
            instance.Animator = animator;

            var renderedNames = new HashSet<string>();
            foreach (var animatorState in EnumerateStates(animator))
            {
                var state = animatorState.state;
                var sourceClip = ResolveStateClip(state);
                if (sourceClip == null)
                    continue;

                if (!renderedNames.Add(state.name))
                    continue;

                var clipInstance = new ClipInstance
                {
                    Enable = true,
                    Fps = Mathf.Clamp(defaultClipFps, 1, 240),
                    Speed = 1f,
                    StateName = state.name,
                    SourceClip = TryLoadGeneratedComboClip(animator, state.name) ?? sourceClip
                };

                var animationTemplate = PackageResourceLoader.LoadGuiTemplate("AnimationTemplate")
                    .Instantiate().Q<VisualElement>("RootElement");

                animationTemplate.Q<Label>("ClipName").text = state.name;
                animationTemplate.Q<Toggle>("ClipEnabled").RegisterValueChangedCallback(toggleEvent =>
                    clipInstance.Enable = toggleEvent.newValue);

                var sliderFps = animationTemplate.Q<SliderInt>("ClipFps");
                sliderFps.value = clipInstance.Fps;
                sliderFps.RegisterValueChangedCallback(fpsEvent => clipInstance.Fps = fpsEvent.newValue);

                var clipField = new ObjectField("Clip")
                {
                    objectType = typeof(AnimationClip),
                    allowSceneObjects = false,
                    value = clipInstance.SourceClip
                };
                clipField.style.minWidth = 280f;
                clipField.style.marginLeft = 8f;
                clipField.RegisterValueChangedCallback(clipEvent =>
                    clipInstance.SourceClip = clipEvent.newValue as AnimationClip);
                animationTemplate.Add(clipField);
                
                animationsContainer.Add(animationTemplate);
                instance.Clips.Add(clipInstance);
            }
        }

        static AnimationClip ResolveStateClip(AnimatorState state)
        {
            if (state == null || state.motion == null)
                return null;

            if (state.motion is AnimationClip animationClip)
                return animationClip;

            if (state.motion is BlendTree blendTree)
                return ResolveBlendTreeClip(blendTree);

            return null;
        }

        static AnimationClip ResolveBlendTreeClip(BlendTree blendTree)
        {
            foreach (var child in blendTree.children)
            {
                if (child.motion is AnimationClip clip)
                    return clip;

                if (child.motion is BlendTree childTree)
                {
                    var nestedClip = ResolveBlendTreeClip(childTree);
                    if (nestedClip != null)
                        return nestedClip;
                }
            }

            return null;
        }

        static IEnumerable<ChildAnimatorState> EnumerateStates(AnimatorController animator)
        {
            if (animator == null || animator.layers == null)
                yield break;

            foreach (var layer in animator.layers)
            {
                if (layer.stateMachine == null)
                    continue;

                foreach (var state in EnumerateStates(layer.stateMachine))
                    yield return state;
            }
        }

        static IEnumerable<ChildAnimatorState> EnumerateStates(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
                yield return state;

            foreach (var childMachine in stateMachine.stateMachines)
            {
                if (childMachine.stateMachine == null)
                    continue;

                foreach (var state in EnumerateStates(childMachine.stateMachine))
                    yield return state;
            }
        }

        static AnimationClip TryLoadGeneratedComboClip(AnimatorController animator, string clipName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(clipName))
                return null;

            string folder = IsFemaleController(animator) ? FemaleGeneratedClipFolder : MaleGeneratedClipFolder;
            return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{folder}/{clipName}.anim");
        }

        static bool IsFemaleController(AnimatorController animator)
        {
            string path = AssetDatabase.GetAssetPath(animator);
            string name = animator.name;
            return (!string.IsNullOrWhiteSpace(path) && path.IndexOf("HumanF", System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrWhiteSpace(name) && name.IndexOf("HumanF", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}

#endif
