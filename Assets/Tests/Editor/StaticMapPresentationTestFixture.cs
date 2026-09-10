using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Small owned legacy package: production now uses EntityScene, so these contracts
// must not depend on a retired 514-scene package remaining in the shipping map.
internal sealed class StaticMapPresentationTestFixture : IDisposable
{
    private readonly SceneSetup[] previousSetup;
    private readonly string root;
    public StaticMapPresentationManifest Manifest { get; private set; }

    public StaticMapPresentationTestFixture()
    {
        previousSetup = EditorSceneManager.GetSceneManagerSetup();
        root = "Assets/Tests/Temp/StaticMap_" + Guid.NewGuid().ToString("N");
        string mapRoot = root + "/opmap/skirmish/desert_base_01";
        Directory.CreateDirectory(mapRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene canonical = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string canonicalPath = root + "/Canonical.unity";
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
        Object.DestroyImmediate(primitive);
        AssetDatabase.CreateAsset(mesh, root + "/Mesh.asset");
        Material material = new(Shader.Find("Universal Render Pipeline/Unlit"));
        AssetDatabase.CreateAsset(material, root + "/Material.mat");
        var renderers = new List<MeshRenderer>();
        for (int i = 0; i < 3; i++)
        {
            GameObject source = new("Source_" + i, typeof(MeshFilter), typeof(MeshRenderer));
            source.transform.position = new Vector3(i * 32 + 2, 1 + i, 4);
            source.transform.rotation = Quaternion.Euler(0, i * 15, 0);
            source.transform.localScale = new Vector3(2 + i, 2, 3);
            source.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = source.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderers.Add(renderer);
        }
        EditorSceneManager.SaveScene(canonical, canonicalPath);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string meshGuid, out long meshId);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string materialGuid, out long materialId);
        var chunks = new List<StaticMapPresentationChunkEntry>();
        var sources = new List<StaticMapPresentationSourceEntry>();
        for (int i = 0; i < renderers.Count; i++)
        {
            MeshRenderer source = renderers[i];
            string chunkId = "chunk_p" + i + "_p0";
            string generatedName = "Renderer_" + i;
            string path = mapRoot + "/StaticMapPresentation_opmap_skirmish_desert_base_01_" + chunkId + ".unity";
            Scene chunkScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject chunkRoot = new("StaticMapPresentation_" + chunkId);
            SceneManager.MoveGameObjectToScene(chunkRoot, chunkScene);
            GameObject copy = Object.Instantiate(source.gameObject, chunkRoot.transform, true);
            copy.name = generatedName;
            EditorSceneManager.SaveScene(chunkScene, path);
            EditorSceneManager.CloseScene(chunkScene, true);
            sources.Add(new StaticMapPresentationSourceEntry(
                GlobalObjectId.GetGlobalObjectIdSlow(source).ToString(), source.name, new string('b', 64),
                chunkId, generatedName, source.bounds, mesh, meshGuid, meshId,
                new List<StaticMapPresentationMaterialEntry> { new(material, materialGuid, materialId) }, false));
            chunks.Add(new StaticMapPresentationChunkEntry(chunkId, path, source.bounds, i, 1));
        }
        Manifest = ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
        Manifest.EditorSetData("opmap.skirmish.desert_base_01", AssetDatabase.AssetPathToGUID(canonicalPath),
            canonicalPath, new string('b', 64), 32f, new string('c', 64), chunks, sources);
        AssetDatabase.CreateAsset(Manifest, root + "/Manifest.asset");
        AssetDatabase.SaveAssets();
    }

    public void Dispose()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(root);
        Manifest = null;
        if (previousSetup.Any(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)) &&
            previousSetup.All(s => !s.isLoaded || !string.IsNullOrEmpty(s.path) && File.Exists(s.path)))
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
    }
}
