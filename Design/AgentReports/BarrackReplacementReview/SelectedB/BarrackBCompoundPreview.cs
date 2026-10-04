#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Game.Editor {
public static class BarrackBCompoundPreview {
 const string Output="/private/tmp/warline-barrack-b-preview";
 const string Folder="Assets/Game/Prefabs/Buildings/Candidates/BarrackB";
 static readonly List<Material> mats=new List<Material>();
 static Bounds Measure(GameObject model) { var rs=model.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)throw new InvalidOperationException("Missing visual");var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b; }
 static Material Mat(string name,Color color) {var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);mats.Add(m);return m;}
 static GameObject Solid(Scene scene,Transform parent,string name,Vector3 center,Vector3 size,Material mat) {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(parent,false);g.transform.localPosition=center;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static GameObject Source(Scene scene,Transform parent,string path,string name,Vector3 center,float yaw,Vector2 maxSize) {
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new InvalidOperationException(path);var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);g.name=name;g.transform.SetParent(parent,false);g.transform.localRotation=Quaternion.Euler(0,yaw,0);var b=Measure(g);float scale=Mathf.Min(1,Mathf.Min(maxSize.x/b.size.x,maxSize.y/b.size.z));g.transform.localScale*=scale;b=Measure(g);g.transform.position+=center-new Vector3(b.center.x,b.min.y,b.center.z);foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);Debug.Log("[BarrackBPart] name="+name+" source="+path+" scale="+scale+" bounds="+Measure(g));return g;
 }
 public static void Run() {
 if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Edit mode and completed compilation required");
 Directory.CreateDirectory(Output);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
 var before=SceneManager.GetActiveScene();bool dirty=before.isDirty;var sourcePath="Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";string sourceHash=AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
 var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D tex=null;GameObject cg=null;
 try {
 var root=new GameObject("Building_Barrack_B_Masonry_Visual_Preview");SceneManager.MoveGameObjectToScene(root,scene);
 var concrete=Mat("BarrackB_Concrete",new Color(.37f,.35f,.31f));var asphalt=Mat("BarrackB_Yard",new Color(.22f,.24f,.23f));var marking=Mat("BarrackB_Marking",new Color(.68f,.61f,.39f));
 Solid(scene,root.transform,"Concrete foundation",new Vector3(0,-.13f,0),new Vector3(27.6f,.26f,14.6f),concrete);
 Solid(scene,root.transform,"Service and muster yard",new Vector3(6.5f,.008f,0),new Vector3(12.6f,.016f,13.8f),asphalt);
 Source(scene,root.transform,"Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_House_02.prefab","B - masonry barrack",new Vector3(-6.7f,0,0),0,new Vector2(13.4f,12.5f));
 Source(scene,root.transform,"Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_SmallBuilding_01.prefab","Service office",new Vector3(9,0,4.2f),90,new Vector2(5.2f,3.5f));
 Source(scene,root.transform,"Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Generator_01.prefab","Generator",new Vector3(11,0,.1f),0,new Vector2(2.5f,3));
 Source(scene,root.transform,"Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Pallet_Loaded_01.prefab","Supplies",new Vector3(10.8f,0,-4.8f),0,new Vector2(2.8f,2.8f));
 Source(scene,root.transform,"Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Crate_Medical_01.prefab","Medical stores",new Vector3(11.2f,0,-2.8f),0,new Vector2(1.4f,1.4f));
 // Small inlaid muster markings; open space stays clear of the main entrance.
 for(int i=0;i<4;i++)Solid(scene,root.transform,"Muster line "+i,new Vector3(5.2f,.026f,-4.9f+i*1.2f),new Vector3(4.3f,.016f,.07f),marking);
 Solid(scene,root.transform,"Yard edge",new Vector3(12.6f,.026f,-.9f),new Vector3(.06f,.016f,10.2f),marking);
 var bounds=Measure(root);if(bounds.min.x< -14||bounds.max.x>14||bounds.min.z< -7.5f||bounds.max.z>7.5f)throw new InvalidOperationException("Compound exceeds current footprint "+bounds);
 var prefabPath=Folder+"/Building_Barrack_B_Masonry_Visual_Preview.prefab";PrefabUtility.SaveAsPrefabAsset(root,prefabPath);AssetDatabase.SaveAssets();
 foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
 foreach(var r in root.GetComponentsInChildren<Renderer>(true)){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetFloat("_SnivelerModelShown",1);r.SetPropertyBlock(block);}
 var groundMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mats.Add(groundMat);groundMat.color=new Color(.27f,.25f,.22f);Solid(scene,null,"Preview ground",new Vector3(0,-.3f,0),new Vector3(150,.1f,150),groundMat).layer=30;
 var lg=new GameObject("Preview sun");SceneManager.MoveGameObjectToScene(lg,scene);var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(48,-35,0);light.shadows=LightShadows.None;
 cg=new GameObject("Preview camera");SceneManager.MoveGameObjectToScene(cg,scene);var camera=cg.AddComponent<Camera>();camera.scene=scene;camera.cullingMask=1<<30;camera.orthographic=true;camera.aspect=1.6f;camera.nearClipPlane=.01f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.16f);
 rt=new RenderTexture(1600,1000,24){antiAliasing=4};rt.Create();camera.targetTexture=rt;
 camera.transform.position=bounds.center+new Vector3(1,.85f,-1).normalized*100;camera.transform.LookAt(bounds.center);camera.orthographicSize=16;Render(camera,rt,ref tex,Output+"/Barrack-B-Compound-Angle.png");
 camera.transform.position=bounds.center+new Vector3(-1,.85f,-1).normalized*100;camera.transform.LookAt(bounds.center);camera.orthographicSize=16;Render(camera,rt,ref tex,Output+"/Barrack-B-Compound-Reverse.png");
 camera.transform.position=new Vector3(0,100,0);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.orthographicSize=10;
 var edgeMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mats.Add(edgeMat);edgeMat.color=new Color(1,.72f,.2f);var edge=new GameObject("28 x 15 footprint reference");SceneManager.MoveGameObjectToScene(edge,scene);edge.layer=30;var line=edge.AddComponent<LineRenderer>();line.sharedMaterial=edgeMat;line.widthMultiplier=.08f;line.useWorldSpace=true;line.loop=true;line.positionCount=4;line.SetPositions(new[]{new Vector3(-14,.05f,-7.5f),new Vector3(14,.05f,-7.5f),new Vector3(14,.05f,7.5f),new Vector3(-14,.05f,7.5f)});Render(camera,rt,ref tex,Output+"/Barrack-B-Compound-Top.png");
 File.WriteAllText(Output+"/manifest.json",JsonUtility.ToJson(new Manifest{source="Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_House_02.prefab",candidate=prefabPath,size=bounds.size,center=bounds.center,renderers=root.GetComponentsInChildren<Renderer>().Length},true));
 if(sourceHash!=AssetDatabase.GetAssetDependencyHash(sourcePath).ToString())throw new InvalidOperationException("Production Barrack changed");if(before!=SceneManager.GetActiveScene()||dirty!=before.isDirty)throw new InvalidOperationException("Active scene changed");
 Debug.Log("[BarrackBCompoundPreview] result=Passed footprint=28x15 sources=Linked productionBarrack=Unchanged activeScene=Preserved candidate="+prefabPath+" output="+Output);
 }finally {if(cg)cg.GetComponent<Camera>().targetTexture=null;if(tex)UnityEngine.Object.DestroyImmediate(tex);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);foreach(var m in mats)if(m&&!EditorUtility.IsPersistent(m))UnityEngine.Object.DestroyImmediate(m);mats.Clear();}
 }
 [Serializable]class Manifest {public string source,candidate;public Vector3 size,center;public int renderers;}
 static void Render(Camera c,RenderTexture rt,ref Texture2D tex,string output){c.Render();var old=RenderTexture.active;try{RenderTexture.active=rt;if(!tex)tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(output,tex.EncodeToPNG());}finally{RenderTexture.active=old;}}
}
}
#endif
