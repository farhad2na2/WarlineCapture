using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class PoseMeshBakerWindow : EditorWindow
{
    [SerializeField] private GameObject _root;
    [SerializeField] private SkinnedMeshRenderer _skinned;
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private bool _useNormalizedTime = true;
    [SerializeField] private float _time = 0.5f;
    [SerializeField] private string _outputFolder = "Assets/BakedPoses";
    [SerializeField] private string _meshName = "";
    [SerializeField] private bool _createPrefab = false;

    [MenuItem("Tools/DOTS/Pose Mesh Baker")]
    public static void Open()
    {
        GetWindow<PoseMeshBakerWindow>("Pose Mesh Baker");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        _root = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Root", "Prefab or scene GameObject that has an Animator and the skinned mesh hierarchy."), _root, typeof(GameObject), true);
        _skinned = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(new GUIContent("Skinned Mesh", "The SkinnedMeshRenderer to bake. If null, the first one found under Root is used."), _skinned, typeof(SkinnedMeshRenderer), true);
        _clip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Animation Clip"), _clip, typeof(AnimationClip), false);

        EditorGUILayout.Space();
        _useNormalizedTime = EditorGUILayout.ToggleLeft("Use Normalized Time (0..1)", _useNormalizedTime);
        using (new EditorGUI.IndentLevelScope())
        {
            if (_useNormalizedTime)
            {
                _time = EditorGUILayout.Slider(new GUIContent("Normalized"), _time, 0f, 1f);
            }
            else
            {
                _time = EditorGUILayout.FloatField(new GUIContent("Seconds"), _time);
                if (_clip != null)
                    EditorGUILayout.LabelField("Clip Length", _clip.length.ToString("0.###") + "s");
            }
        }

        _outputFolder = EditorGUILayout.TextField(new GUIContent("Output Folder"), _outputFolder);
        _meshName = EditorGUILayout.TextField(new GUIContent("Mesh Name (optional)"), _meshName);
        _createPrefab = EditorGUILayout.ToggleLeft("Also Create Static Prefab (MeshFilter+MeshRenderer)", _createPrefab);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanBake(out _)))
        {
            if (GUILayout.Button("Bake Pose Mesh", GUILayout.Height(32)))
                Bake();
        }

        if (!CanBake(out var reason))
            EditorGUILayout.HelpBox(reason, MessageType.Warning);
    }

    private bool CanBake(out string reason)
    {
        if (_root == null)
        {
            reason = "Assign a Root GameObject (prefab or scene object).";
            return false;
        }

        if (_clip == null)
        {
            reason = "Assign an AnimationClip.";
            return false;
        }

        var animator = _root.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            reason = "Root must have an Animator in its hierarchy (humanoid clips need it to evaluate).";
            return false;
        }

        var smr = _skinned != null ? _skinned : _root.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            reason = "Root must have a SkinnedMeshRenderer (or assign one explicitly).";
            return false;
        }

        reason = "";
        return true;
    }

    private void Bake()
    {
        if (!CanBake(out var reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        string folder = string.IsNullOrWhiteSpace(_outputFolder) ? "Assets" : _outputFolder.Trim();
        if (!folder.StartsWith("Assets", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("Pose Mesh Baker", "Output Folder must be under Assets/ (e.g. Assets/BakedPoses).", "OK");
            return;
        }

        Directory.CreateDirectory(folder);

        GameObject instance = null;
        try
        {
            // Instantiate without affecting the scene/prefab.
            instance = PrefabUtility.IsPartOfPrefabAsset(_root)
                ? (GameObject)PrefabUtility.InstantiatePrefab(_root)
                : Instantiate(_root);

            instance.hideFlags = HideFlags.HideAndDontSave;

            var smr = _skinned != null ? FindMatchingRenderer(instance, _skinned) : instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null)
                throw new InvalidOperationException("Failed to find SkinnedMeshRenderer on instantiated root.");

            float sampleTimeSeconds = ComputeSampleTimeSeconds(_clip, _useNormalizedTime, _time);

            var baked = new Mesh();
            baked.name = string.IsNullOrWhiteSpace(_meshName)
                ? $"{_clip.name}_Pose_{(_useNormalizedTime ? $"{_time:0.00}" : $"{sampleTimeSeconds:0.00}s")}"
                : _meshName.Trim();

            SampleHumanoidClipToTransforms(instance, _clip, sampleTimeSeconds, () =>
            {
                var prevUpdateOffscreen = smr.updateWhenOffscreen;
                smr.updateWhenOffscreen = true;
                try
                {
                    smr.BakeMesh(baked);
                }
                finally
                {
                    smr.updateWhenOffscreen = prevUpdateOffscreen;
                }
            });

            string meshPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, baked.name + ".asset").Replace('\\', '/'));
            AssetDatabase.CreateAsset(baked, meshPath);

            if (_createPrefab)
            {
                var go = new GameObject(baked.name);
                go.AddComponent<MeshFilter>().sharedMesh = baked;
                var mr = go.AddComponent<MeshRenderer>();
                if (smr.sharedMaterial != null)
                    mr.sharedMaterial = smr.sharedMaterial;

                string prefabPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, baked.name + ".prefab").Replace('\\', '/'));
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                DestroyImmediate(go);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Baked pose mesh to {meshPath}", baked);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            if (instance != null)
                DestroyImmediate(instance);
        }
    }

    private static float ComputeSampleTimeSeconds(AnimationClip clip, bool normalized, float t)
    {
        if (clip == null)
            return 0f;
        if (!normalized)
            return Mathf.Clamp(t, 0f, clip.length);
        t = Mathf.Clamp01(t);
        return Mathf.Clamp(t * clip.length, 0f, clip.length);
    }

    private static SkinnedMeshRenderer FindMatchingRenderer(GameObject instantiatedRoot, SkinnedMeshRenderer original)
    {
        if (original == null || instantiatedRoot == null)
            return null;

        // Prefer matching by sharedMesh (most reliable across prefab instantiation).
        var all = instantiatedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].sharedMesh == original.sharedMesh)
                return all[i];
        }

        // Fallback: first renderer.
        return instantiatedRoot.GetComponentInChildren<SkinnedMeshRenderer>();
    }

    private static void SampleHumanoidClipToTransforms(GameObject root, AnimationClip clip, float timeSeconds, Action whilePoseApplied)
    {
        if (root == null || clip == null)
            return;

        // Ensure the hierarchy is enabled so animation/skin evaluation runs.
        if (!root.activeSelf)
            root.SetActive(true);

        var animator = root.GetComponentInChildren<Animator>();
        if (animator == null)
            throw new InvalidOperationException("Animator missing on root hierarchy.");

        if (animator.avatar == null || !animator.avatar.isValid)
            throw new InvalidOperationException("Animator Avatar is missing or invalid. For Humanoid clips you must assign a valid Avatar on the prefab's Animator.");

        animator.enabled = true;

        // Humanoid clips often contain muscle curves (not transform curves), so AnimationMode.SampleAnimationClip
        // won't always drive transforms. Using a PlayableGraph routed through Animator reliably evaluates humanoid clips.
        var prevController = animator.runtimeAnimatorController;
        var prevCulling = animator.cullingMode;
        var prevUpdateMode = animator.updateMode;
        animator.runtimeAnimatorController = null;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        var graph = PlayableGraph.Create("PoseMeshBaker");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);

            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            output.SetSourcePlayable(playable);

            graph.Play();

            // Deterministic sampling: in edit mode, wrap evaluation in AnimationMode sampling so transforms are applied.
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();

                animator.Rebind();
                animator.Update(0f);

                var t = Mathf.Clamp(timeSeconds, 0f, clip.length);
                playable.SetSpeed(0);
                playable.SetTime(t);

                // Apply pose.
                graph.Evaluate(0f);
                animator.Update(0f);

                // Some rigs need a tiny tick to flush.
                graph.Evaluate(1f / 120f);
                animator.Update(0f);

                whilePoseApplied?.Invoke();

                AnimationMode.EndSampling();
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            // Quick sanity check: if the hips are still effectively identity, the clip likely didn't apply.
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                var q = hips.localRotation;
                if (Mathf.Abs(q.x) < 1e-4f && Mathf.Abs(q.y) < 1e-4f && Mathf.Abs(q.z) < 1e-4f && Mathf.Abs(q.w - 1f) < 1e-4f)
                {
                    Debug.LogWarning("PoseMeshBaker: Hips rotation appears unchanged after sampling. " +
                                     "If baked meshes are still T-pose, verify the selected clip is Humanoid and contains body motion, " +
                                     "and that the selected SkinnedMeshRenderer is driven by this Animator/Avatar.");
                }
            }
        }
        finally
        {
            graph.Destroy();
            animator.runtimeAnimatorController = prevController;
            animator.cullingMode = prevCulling;
            animator.updateMode = prevUpdateMode;
        }
    }
}
