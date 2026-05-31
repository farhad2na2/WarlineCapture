#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace SnivelerCode.GpuAnimation.Editor.Scripts
{
    public static class GenerateProcessor
    {
        static readonly int[] s_AnimTextures =
        {
            Shader.PropertyToID("_SnivelerMainTextureFirst"),
            Shader.PropertyToID("_SnivelerMainTextureSecond"),
            Shader.PropertyToID("_SnivelerMainTextureThird")
        };

        static Dictionary<Texture2D, int> s_PartialTextureIndex;
        static Rect[] s_TexturePackRects;

        static readonly int s_MainTexture = Shader.PropertyToID("_SnivelerMainTexture");
        static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int s_BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int s_Color = Shader.PropertyToID("_Color");
        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int AlphaClip = Shader.PropertyToID("_AlphaClip");
        
        static List<TextureInstance> s_TextureInstances;
        
        static string s_FolderBatch;
        static string s_FolderResources;
        static string s_FolderAnimators;
        
        static Texture2D s_BaseTexture;
        static Material s_BaseMaterial;
        static int s_WritePixelIndex;
        static int s_BatchTextureSize;
        static bool s_BatchTextureReadable;
        static readonly Dictionary<Material, Texture2D> s_FallbackTextures = new();
        static string s_BaseMaterialPath;

        public static void Generate(string outputRootFolder, string batchName, Shader shader, List<PrefabInstance> prefabs, int batchTextureSize, bool batchTextureReadable)
        {
            if (shader == null)
            {
                throw new InvalidOperationException("Animator Baker requires an instance shader before generating.");
            }

            if (string.IsNullOrWhiteSpace(outputRootFolder) || !outputRootFolder.StartsWith("Assets"))
            {
                throw new InvalidOperationException("Animator Baker output folder must be under Assets/.");
            }

            if (string.IsNullOrWhiteSpace(batchName))
            {
                throw new InvalidOperationException("Animator Baker requires a batch name.");
            }

            if (prefabs == null || prefabs.Count == 0)
            {
                throw new InvalidOperationException("Animator Baker requires at least one prefab.");
            }

            s_BatchTextureSize = math.clamp(batchTextureSize, 256, 8192);
            s_BatchTextureReadable = batchTextureReadable;
            InitAnimatorTextureInstances(2048);
            GenerateFoldersStructure(outputRootFolder, batchName);
            GenerateMaterials(shader);
            GeneratePrefabsAtlas(prefabs);
            GenerateAnimationTextures(prefabs);
        }
        
        static void InitAnimatorTextureInstances(ushort size)
        {
            s_TextureInstances = new List<TextureInstance>();
            for (var i = 0; i < s_AnimTextures.Length; ++i)
            {
                s_TextureInstances.Add(new TextureInstance(size));
            }
        }
        
        static void GenerateFoldersStructure(string outputRootFolder, string name)
        {
            s_FolderBatch = Path.Combine(outputRootFolder, name);
            s_FolderResources = Path.Combine(s_FolderBatch, "ModelResources");
            s_FolderAnimators = Path.Combine(s_FolderBatch, "Animators");
            
            ForceDirectory(s_FolderResources);
            ForceDirectory(s_FolderAnimators);
        }
        
        static void GenerateMaterials(Shader shader)
        {
            s_BaseTexture = new Texture2D(s_BatchTextureSize, s_BatchTextureSize, TextureFormat.RGBA32, true);
            s_BaseTexture = CreateOrReplaceAsset(s_BaseTexture, Path.Combine(s_FolderResources, "BatchTexture.asset"));

            s_BaseMaterial = new Material(shader) { name = "BatchMaterial", enableInstancing = true };
            s_BaseMaterialPath = Path.Combine(s_FolderResources, "BatchMaterial.mat");
            s_BaseMaterial = CreateOrReplaceAsset(s_BaseMaterial, s_BaseMaterialPath);
            
            s_BaseMaterial.SetTexture(s_MainTexture, s_BaseTexture);
            s_PartialTextureIndex = new Dictionary<Texture2D, int>();
        }

        static void GeneratePrefabsAtlas(List<PrefabInstance> prefabs)
        {
            var partialTextures = new Dictionary<Texture2D, Texture2D>();
            foreach (var prefab in prefabs)
            {
                ValidatePrefab(prefab);

                var sharedMesh = prefab.Skin.sharedMesh;
                for (var i = 0; i < sharedMesh.subMeshCount; ++i)
                {
                    var material = prefab.Skin.sharedMaterials[i];
                    if (material == null)
                    {
                        throw new InvalidOperationException(
                            $"Prefab '{prefab.Source.name}' has a missing material in slot {i}.");
                    }

                    var mainTexture = GetTexture2D(material);
                    if (mainTexture == null)
                    {
                        throw new InvalidOperationException(
                            $"Prefab '{prefab.Source.name}' material '{material.name}' does not provide a readable Texture2D.");
                    }

                    if (!partialTextures.ContainsKey(mainTexture))
                    {
                        PrepareTexture(mainTexture);
                        s_PartialTextureIndex.Add(mainTexture, partialTextures.Count);
                        partialTextures.Add(mainTexture, mainTexture);
                    }
                }
            }

            if (partialTextures.Count == 0)
            {
                throw new InvalidOperationException("Animator Baker could not find any readable Texture2D main textures in the selected prefabs.");
            }

            s_TexturePackRects = s_BaseTexture.PackTextures(
                partialTextures.Values.ToArray(), 0, s_BatchTextureSize);
            s_BaseTexture.Apply(true, !s_BatchTextureReadable);
            EditorUtility.SetDirty(s_BaseTexture);
        }
        
        static void GenerateAnimationTextures(List<PrefabInstance> prefabs)
        {
            s_WritePixelIndex = 0;
            for (var i = 0; i < prefabs.Count; ++i)
            {
                var prefabInstance = prefabs[i];
                prefabInstance.Name = $"{prefabInstance.Source.name}_{i}";
                
                var rootObject = BuildLodMeshProcess(prefabInstance);

                // first animation -> t pose
                var animations = new List<MaterialAnimatorBake> { new() { frames = 1, start = s_WritePixelIndex, speed = 1 } };
                BuildTPose(prefabInstance.Skin);
                
                var enabledAnimations = (from clip in prefabInstance.Clips where clip.Enable select clip).ToList();
                var animatorStates = EnumerateStates(prefabInstance.Animator).ToList();
                
                var clonedPrefab = UnityEngine.Object.Instantiate(prefabs[i].Source);
                var prefabTransform = clonedPrefab.transform;
                prefabTransform.position = float3.zero;
                prefabTransform.rotation = quaternion.identity;

                var animator = clonedPrefab.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = clonedPrefab.AddComponent<Animator>();
                }

                if (animator == null)
                {
                    throw new InvalidOperationException(
                        $"Prefab '{prefabInstance.Source.name}' clone is missing an Animator component and one could not be added.");
                }

                animator.runtimeAnimatorController = prefabInstance.Animator;
                var clonedRenderer = clonedPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
                if (clonedRenderer == null)
                {
                    throw new InvalidOperationException(
                        $"Prefab '{prefabInstance.Source.name}' clone is missing a SkinnedMeshRenderer.");
                }
                
                foreach (var clip in enabledAnimations)
                {
                    var animatorState = animatorStates.FirstOrDefault(s => s.state != null && s.state.name == clip.StateName);
                    if (animatorState.state == null && clip.SourceClip == null)
                    {
                        throw new InvalidOperationException(
                            $"Prefab '{prefabInstance.Source.name}' is missing animator state '{clip.StateName}'.");
                    }

                    var animationClip = clip.SourceClip;
                    if (animationClip == null)
                    {
                        if (animatorState.state.motion is not AnimationClip stateMotionClip)
                        {
                            throw new InvalidOperationException(
                                $"Prefab '{prefabInstance.Source.name}' state '{clip.StateName}' does not reference an AnimationClip.");
                        }

                        animationClip = stateMotionClip;
                    }
                   
                    var clipSettings = AnimationUtility.GetAnimationClipSettings(animationClip);
                    var sampleStart = Mathf.Max(0f, clipSettings.startTime);
                    var sampleStop = clipSettings.stopTime > sampleStart
                        ? clipSettings.stopTime
                        : animationClip.length;
                    var sampleDuration = Mathf.Max(1f / clip.Fps, sampleStop - sampleStart);
                    var frameCount = Mathf.Max(1, Mathf.CeilToInt(sampleDuration * clip.Fps));
                    var materialAnimation = new MaterialAnimatorBake
                    {
                        fps = (byte)clip.Fps,
                        frames = frameCount,
                        start = s_WritePixelIndex,
                        loop = animationClip.isLooping,
                        speed = (byte)clip.Speed,
                        transitions = new List<AnimationTransitionBake>()
                    };
                    
                    foreach (var frame in Enumerable.Range(0, frameCount))
                    {
                        var sampleTime = Mathf.Min(sampleStop, sampleStart + (float)frame / clip.Fps);
                        animationClip.SampleAnimation(clonedPrefab, sampleTime);
                        WriteBoneMatrix(clonedRenderer);
                    }
                    
                    // find transitions
                    if (animatorState.state != null)
                    {
                        foreach (var transition in animatorState.state.transitions)
                        {
                            if (transition.destinationState == null)
                            {
                                continue;
                            }

                            var targetInstance = enabledAnimations.FirstOrDefault(x => x.StateName == transition.destinationState.name);
                            if (targetInstance != null)
                            {
                                var targetIndex = enabledAnimations.IndexOf(targetInstance);
                                materialAnimation.transitions.Add(new AnimationTransitionBake
                                {
                                    duration = transition.duration,
                                    index = (byte)(targetIndex + 1),
                                    offset = transition.offset,
                                    start = transition.exitTime
                                });
                            }
                        }
                    }
                    
                    animations.Add(materialAnimation);
                }

                UnityEngine.Object.DestroyImmediate(clonedPrefab);
                
                // create animator prefab
                var animatorObject = new GameObject("Animator");
                var animatorComponent = animatorObject.AddComponent<MaterialAnimatorAuthoring>();
                animatorComponent.bonesCount = prefabInstance.Skin.bones.Length;
                animatorComponent.animations = animations;
                animatorComponent.alphas = prefabInstance.SubAlpha;

                var animatorPrefabPath = Path.Combine(s_FolderAnimators, $"Animator_{prefabInstance.Name}.prefab");
                PrefabUtility.SaveAsPrefabAsset(animatorObject, animatorPrefabPath);
                UnityEngine.Object.DestroyImmediate(animatorObject);
                
                // create model prefab
                var configComponent = rootObject.AddComponent<MaterialAnimatorIndexAuthoring>();
                configComponent.animator = AssetDatabase.LoadAssetAtPath<GameObject>(animatorPrefabPath);
                configComponent.firstAnimation = animations[0];
                PrefabUtility.SaveAsPrefabAsset(rootObject, Path.Combine(s_FolderBatch, $"Prefab_{prefabInstance.Name}.prefab"));
                
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
            
            if (s_WritePixelIndex == 0)
            {
                return;
            }

            var textureWidth = 1;
            var textureHeight = 1;

            while (textureWidth * textureHeight < s_WritePixelIndex)
            {
                if (textureWidth <= textureHeight)
                {
                    textureWidth *= 2;
                }
                else
                {
                    textureHeight *= 2;
                }
            }

            for (var i = 0; i < s_TextureInstances.Count; ++i)
            {
                var texturePixels = new Color[textureWidth * textureHeight];
                Array.Copy(s_TextureInstances[i].Pixels, texturePixels, s_WritePixelIndex);

                var animTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false, true);
                animTexture.SetPixels(texturePixels);
                animTexture.Apply();
                animTexture.filterMode = FilterMode.Point;
                animTexture = CreateOrReplaceAsset(animTexture, Path.Combine(s_FolderResources, $"AnimationTexture{i}.asset"));
                s_BaseMaterial.SetTexture(s_AnimTextures[i], animTexture);
            }
        }
        
        static GameObject BuildLodMeshProcess(PrefabInstance prefab)
        {
            var rootObject = new GameObject(prefab.Source.name);
            var lodGroupComponent = rootObject.AddComponent<LODGroup>();

            var lodsGroups = new List<LOD>();
            for (var i = 0; i < prefab.Lods.Count; ++i)
            {
                var lodInstance = prefab.Lods[i];
                if (lodInstance == null || lodInstance.Mesh == null)
                {
                    throw new InvalidOperationException(
                        $"Prefab '{prefab.Source.name}' has an invalid LOD entry at index {i} with no mesh assigned.");
                }

                if (lodInstance.Skin == null)
                {
                    throw new InvalidOperationException(
                        $"Prefab '{prefab.Source.name}' has an invalid LOD entry at index {i} with no skinned renderer assigned.");
                }

                var lodMesh = lodInstance.Mesh;
                var mesh = UnityEngine.Object.Instantiate(lodMesh);
                
                var uvUpdate = new Vector2[mesh.uv.Length];
                for (var k = 0; k < mesh.subMeshCount; ++k)
                {
                    var material = prefab.Skin.sharedMaterials[k];
                    if (material == null)
                    {
                        throw new InvalidOperationException(
                            $"Prefab '{prefab.Source.name}' has a missing material in slot {k}.");
                    }

                    var mainTexture = GetTexture2D(material);

                    var rectIndex = s_PartialTextureIndex[mainTexture];
                    var textureRect = s_TexturePackRects[rectIndex];

                    var subMeshInfo = mesh.GetSubMesh(k);
                    for (var v = 0; v < subMeshInfo.vertexCount; ++v)
                    {
                        var uvVector = mesh.uv[subMeshInfo.firstVertex + v];
                        uvUpdate[subMeshInfo.firstVertex + v] = new Vector2
                        {
                            x = uvVector.x * textureRect.width + textureRect.x,
                            y = uvVector.y * textureRect.height + textureRect.y
                        };
                    }
                }

                mesh.SetUVs(0, uvUpdate);
                mesh.RecalculateNormals();

                var boneIndexes = new List<Vector4>();
                var boneWeights = new List<Vector4>();
                var skinBones = lodInstance.Skin.bones;
                foreach (var boneWeight in mesh.boneWeights)
                {
                    var boneIndex0 = prefab.BonesIndex(skinBones[boneWeight.boneIndex0].name);
                    var boneIndex1 = prefab.BonesIndex(skinBones[boneWeight.boneIndex1].name);
                    var boneIndex2 = prefab.BonesIndex(skinBones[boneWeight.boneIndex2].name);
                    var boneIndex3 = prefab.BonesIndex(skinBones[boneWeight.boneIndex3].name);
                    boneIndexes.Add(new Vector4(boneIndex0, boneIndex1, boneIndex2, boneIndex3));
                    boneWeights.Add(new Vector4(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3));
                }

                mesh.SetUVs(2, boneIndexes);
                mesh.SetUVs(3, boneWeights);

                var meshAssetPath = Path.Combine(s_FolderResources, $"{prefab.Name}_lod{i}.asset");
                var meshAsset = CreateOrReplaceAsset(mesh, meshAssetPath);
                var materialAsset = AssetDatabase.LoadAssetAtPath<Material>(s_BaseMaterialPath);
                if (meshAsset == null)
                {
                    throw new InvalidOperationException(
                        $"Animator Baker failed to create mesh asset '{meshAssetPath}'.");
                }

                if (materialAsset == null)
                {
                    throw new InvalidOperationException(
                        $"Animator Baker failed to load generated material '{s_BaseMaterialPath}'.");
                }

                var lodObject = new GameObject($"_lod{i}");
                lodObject.transform.SetParent(rootObject.transform);
                lodObject.transform.localPosition = Vector3.zero;
                lodObject.transform.localRotation = Quaternion.identity;
                lodObject.transform.localScale = Vector3.one;

                var meshFilter = lodObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = meshAsset;

                var subMeshCount = meshAsset.subMeshCount;

                var meshRenderer = lodObject.AddComponent<MeshRenderer>();
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;

                var sharedMaterials = new Material[subMeshCount];
                for (var k = 0; k < subMeshCount; ++k)
                {
                    sharedMaterials[k] = materialAsset;
                }

                meshRenderer.sharedMaterials = sharedMaterials;
                if (meshFilter.sharedMesh == null || meshRenderer.sharedMaterials == null || meshRenderer.sharedMaterials.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Generated LOD '{lodObject.name}' for prefab '{prefab.Source.name}' is missing mesh or materials after asset assignment.");
                }

                lodsGroups.Add(new LOD
                {
                    screenRelativeTransitionHeight = lodInstance.Percent * 0.01f,
                    renderers = new Renderer[] { meshRenderer }
                });
            }

            lodGroupComponent.SetLODs(lodsGroups.ToArray());
            lodGroupComponent.RecalculateBounds();

            return rootObject;
        }

        static void BuildTPose(SkinnedMeshRenderer renderer)
        {
            if (renderer == null)
            {
                throw new InvalidOperationException("Animator Baker could not capture the bind pose because the source renderer is missing.");
            }

            WriteBoneMatrix(renderer);
        }

        static void WriteBoneMatrix(SkinnedMeshRenderer renderer)
        {
            var rendererWorldToLocal = renderer.transform.worldToLocalMatrix;
            foreach (var boneMatrix in renderer.bones.Select((b, idx) => rendererWorldToLocal * b.localToWorldMatrix * renderer.sharedMesh.bindposes[idx]))
            {
                s_TextureInstances[0].Write(s_WritePixelIndex, new Color(boneMatrix.m00, boneMatrix.m01, boneMatrix.m02, boneMatrix.m03));
                s_TextureInstances[1].Write(s_WritePixelIndex, new Color(boneMatrix.m10, boneMatrix.m11, boneMatrix.m12, boneMatrix.m13));
                s_TextureInstances[2].Write(s_WritePixelIndex, new Color(boneMatrix.m20, boneMatrix.m21, boneMatrix.m22, boneMatrix.m23));
                s_WritePixelIndex++;
            }
        }

        static T CreateOrReplaceAsset<T>(T asset, string assetPath) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                UnityEngine.Object.DestroyImmediate(asset);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        static void ForceDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        static void PrepareTexture(Texture texture)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                if (!tImporter.isReadable)
                {
                    tImporter.isReadable = true;
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
        }

        static void ValidatePrefab(PrefabInstance prefab)
        {
            if (prefab == null || prefab.Source == null)
            {
                throw new InvalidOperationException("Animator Baker encountered an empty prefab entry.");
            }

            if (prefab.Skin == null || prefab.Skin.sharedMesh == null)
            {
                throw new InvalidOperationException(
                    $"Prefab '{prefab.Source.name}' is missing a valid SkinnedMeshRenderer with a shared mesh.");
            }

            if (prefab.Skin.sharedMaterials == null || prefab.Skin.sharedMaterials.Length < prefab.Skin.sharedMesh.subMeshCount)
            {
                throw new InvalidOperationException(
                    $"Prefab '{prefab.Source.name}' does not have enough materials for its submeshes.");
            }

            if (prefab.Animator == null)
            {
                throw new InvalidOperationException(
                    $"Prefab '{prefab.Source.name}' is missing an AnimatorController in the baker window.");
            }

            if (prefab.Animator.layers == null || prefab.Animator.layers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Prefab '{prefab.Source.name}' has an AnimatorController with no layers.");
            }
        }

        static Texture2D GetTexture2D(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.mainTexture is Texture2D mainTexture)
            {
                return mainTexture;
            }

            if (material.HasProperty(s_BaseMap) && material.GetTexture(s_BaseMap) is Texture2D baseMap)
            {
                return baseMap;
            }

            if (material.HasProperty(s_MainTex) && material.GetTexture(s_MainTex) is Texture2D fallbackMain)
            {
                return fallbackMain;
            }

            if (s_FallbackTextures.TryGetValue(material, out Texture2D fallbackTexture))
            {
                return fallbackTexture;
            }

            fallbackTexture = BuildFallbackTexture(material);
            s_FallbackTextures[material] = fallbackTexture;
            return fallbackTexture;
        }

        static Texture2D BuildFallbackTexture(Material material)
        {
            var color = Color.white;
            if (material.HasProperty(s_BaseColor))
            {
                color = material.GetColor(s_BaseColor);
            }
            else if (material.HasProperty(s_Color))
            {
                color = material.GetColor(s_Color);
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = $"{material.name}_FallbackTexture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            var pixels = new[] { color, color, color, color };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
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

    }
}

#endif
