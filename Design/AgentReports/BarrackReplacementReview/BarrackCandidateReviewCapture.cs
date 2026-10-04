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
public static class BarrackCandidateReviewCapture {
 const string Out="/private/tmp/warline-barrack-review";
 [Serializable] class Entry { public string id,path,guid; public Vector3 size; public int renderers; public bool rotatedForFit; public float uniformFit; }
 [Serializable] class Report { public Vector2Int footprint=new Vector2Int(28,15); public Entry[] entries; }
 public static void Run() {
 if(EditorApplication.isPlaying) throw new InvalidOperationException("Preview requires Edit mode.");
 Directory.CreateDirectory(Out);
 var before=SceneManager.GetActiveScene(); var dirty=before.isDirty;
 var paths=new[]{"Assets/Game/Prefabs/Buildings/Building_Barrack.prefab","Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_SmallBuilding_01.prefab","Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_SmallBuilding_02.prefab","Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_SmallBuilding_03.prefab","Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_House_01.prefab","Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_House_02.prefab","Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_House_03.prefab","Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_Warehouse_01.prefab"};
 var entries=new List<Entry>();
 for(int i=0;i<paths.Length;i++) entries.Add(Capture(paths[i],i.ToString("00")));
 File.WriteAllText(Out+"/measurements.json",JsonUtility.ToJson(new Report{entries=entries.ToArray()},true));
 if(SceneManager.GetActiveScene()!=before || before.isDirty!=dirty) throw new InvalidOperationException("Active scene changed.");
 Debug.Log("[BarrackCandidateReview] result=Passed prefabs=8 views=16 sourceAssets=Unchanged activeScene=Preserved output="+Out);
 }
 static Bounds Measure(GameObject model) {
 var rs=model.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
 if(rs.Length==0)throw new InvalidOperationException("No renderers");
 var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
 }
 static Entry Capture(string path,string id) {
 var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new InvalidOperationException(path);
 var scene=EditorSceneManager.NewPreviewScene(); RenderTexture rt=null; Texture2D image=null; Material groundMat=null,lineMat=null;
 try {
 var model=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(model,scene);model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
 foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
 foreach(var lod in model.GetComponentsInChildren<LODGroup>(true))lod.ForceLOD(0);
 foreach(var r in model.GetComponentsInChildren<Renderer>(true)) { var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetFloat("_SnivelerModelShown",1);r.SetPropertyBlock(block); }
 var b=Measure(model);var e=new Entry{id=id,path=path,guid=AssetDatabase.AssetPathToGUID(path),size=b.size,renderers=model.GetComponentsInChildren<Renderer>().Length,rotatedForFit=b.size.z>b.size.x};
 float sx=e.rotatedForFit?b.size.z:b.size.x,sz=e.rotatedForFit?b.size.x:b.size.z;e.uniformFit=Mathf.Min(28/sx,15/sz);
 model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z); b=Measure(model);
 var cg=new GameObject("Review camera");SceneManager.MoveGameObjectToScene(cg,scene);var c=cg.AddComponent<Camera>();c.scene=scene;c.cullingMask=1<<30;c.orthographic=true;c.aspect=4f/3;c.nearClipPlane=.01f;c.farClipPlane=500;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.115f,.14f,.17f);
 var lg=new GameObject("Review light");SceneManager.MoveGameObjectToScene(lg,scene);var l=lg.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.7f;l.cullingMask=1<<30;l.transform.rotation=Quaternion.Euler(48,-35,0);l.shadows=LightShadows.None;
 groundMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));groundMat.color=new Color(.26f,.29f,.30f);
 var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(ground,scene);ground.layer=30;ground.transform.position=new Vector3(0,-.15f,0);ground.transform.localScale=new Vector3(80,.2f,80);ground.GetComponent<Renderer>().sharedMaterial=groundMat;
 float span=Mathf.Max(b.size.x,b.size.z);c.transform.position=b.center+new Vector3(1,.85f,-1).normalized*100;c.transform.LookAt(b.center); float halfX=0,halfY=0; for(int mask=0;mask<8;mask++){var corner=b.center+new Vector3((mask&1)==0?-b.extents.x:b.extents.x,(mask&2)==0?-b.extents.y:b.extents.y,(mask&4)==0?-b.extents.z:b.extents.z);var view=c.transform.InverseTransformPoint(corner)-c.transform.InverseTransformPoint(b.center);halfX=Mathf.Max(halfX,Mathf.Abs(view.x));halfY=Mathf.Max(halfY,Mathf.Abs(view.y));}c.orthographicSize=Mathf.Max(halfY,halfX/c.aspect)*1.12f;
 rt=new RenderTexture(1024,768,24){antiAliasing=4};rt.Create();c.targetTexture=rt;
 Render(c,rt,ref image,Out+"/"+id+"-angle.png");
 if(e.rotatedForFit)model.transform.Rotate(0,90,0,Space.World);
 // Same camera scale, no resizing: yellow rectangle is current 28 x 15 envelope.
 lineMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));lineMat.color=new Color(1f,.74f,.20f);
 var lineGo=new GameObject("Current footprint");SceneManager.MoveGameObjectToScene(lineGo,scene);lineGo.layer=30;var line=lineGo.AddComponent<LineRenderer>();line.sharedMaterial=lineMat;line.widthMultiplier=.12f;line.useWorldSpace=true;line.loop=true;line.positionCount=4;line.SetPositions(new[]{new Vector3(-14,.08f,-7.5f),new Vector3(14,.08f,-7.5f),new Vector3(14,.08f,7.5f),new Vector3(-14,.08f,7.5f)});
 c.orthographicSize=20;c.transform.position=new Vector3(0,100,0);c.transform.rotation=Quaternion.Euler(90,0,0);Render(c,rt,ref image,Out+"/"+id+"-top.png");
 Debug.Log("[BarrackCandidate] id="+id+" prefab="+path+" bounds="+b.size+" fit="+e.uniformFit);
 return e;
 } finally { if(image)UnityEngine.Object.DestroyImmediate(image);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);if(groundMat)UnityEngine.Object.DestroyImmediate(groundMat);if(lineMat)UnityEngine.Object.DestroyImmediate(lineMat); }
 }
 static void Render(Camera c,RenderTexture rt,ref Texture2D image,string output) { c.Render();var old=RenderTexture.active;try{RenderTexture.active=rt;if(!image)image=new Texture2D(1024,768,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1024,768),0,0);image.Apply();File.WriteAllBytes(output,image.EncodeToPNG());}finally{RenderTexture.active=old;} }
}
}
#endif
