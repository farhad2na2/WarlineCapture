#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishSagaShaderSettings
    {
        public static void Ensure()
        {
            var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if(settings.Length==0) throw new InvalidOperationException("GraphicsSettings asset unavailable.");
            var serialized=new SerializedObject(settings[0]);
            var shaders=serialized.FindProperty("m_AlwaysIncludedShaders");
            if(shaders==null||!shaders.isArray) throw new InvalidOperationException("Always Included Shaders unavailable.");
            bool changed=false;
            foreach(string name in new[]{"TheatrePlate","DistrictMultiply"})
            {
                var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Game/Rendering/Shaders/"+name+".shader");
                if(shader==null||shader.name!="Game/UI/"+name) throw new InvalidOperationException("Exact theatre shader missing: "+name);
                bool found=false;
                for(int i=0;i<shaders.arraySize;i++)
                    if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader) { found=true; break; }
                if(found) continue;
                int index=shaders.arraySize++;
                shaders.GetArrayElementAtIndex(index).objectReferenceValue=shader;
                changed=true;
            }
            if(changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings[0]);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("[SagaUiShaderInclusion] result=Passed shaders=2");
        }
    }
}
#endif
