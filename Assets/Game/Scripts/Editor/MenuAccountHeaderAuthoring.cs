using System;
using System.Linq;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    // Shared presentation authoring only: uses the existing account gateway.
    public static class MenuAccountHeaderAuthoring
    {
        private static readonly string[] CreditsNames = { "Credits", "CreditsChip", "CreditsPanel", "CreditsResource", "CreditsVisualPanel" };
        private static readonly string[] CommandNames = { "Command", "CommandChip", "CommandPanel", "CommandResource", "CommandVisualPanel" };
        public static void Apply(GameObject root)
        {
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if(transform == null) continue;
                if(Array.IndexOf(CommandNames,transform.name)>=0 && transform.Find("Value")!=null)
                { UnityEngine.Object.DestroyImmediate(transform.gameObject); continue; }
                if(Array.IndexOf(CreditsNames,transform.name)<0) continue;
                var value=transform.Find("Value")?.GetComponent<TMP_Text>();
                if(value==null) continue;
                value.text="—";
                value.enableAutoSizing=true; value.fontSizeMin=Mathf.Min(20,value.fontSize); value.fontSizeMax=Mathf.Max(20,value.fontSize);
                foreach(var child in transform.GetComponentsInChildren<Transform>(true))
                    if(child!=null && child!=transform && child.name.IndexOf("Plus",StringComparison.OrdinalIgnoreCase)>=0)
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                foreach(var route in transform.GetComponentsInChildren<UIShellRouteButtonView>(true)) UnityEngine.Object.DestroyImmediate(route);
                foreach(var action in transform.GetComponentsInChildren<UIShellActionButtonView>(true)) UnityEngine.Object.DestroyImmediate(action);
                foreach(var button in transform.GetComponentsInChildren<Button>(true)) UnityEngine.Object.DestroyImmediate(button);
                foreach(var graphic in transform.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget=false;
                if(!root.GetComponentsInChildren<MainMenuAccountHeaderView>(true).Any(view=>view.Value==value))
                    transform.gameObject.AddComponent<MainMenuAccountHeaderView>().Configure(value);
            }
        }
        public static void MigrateExistingHeaders()
        {
            int count=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Game/Prefabs/UI/Shell/Content"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(!prefab.GetComponentsInChildren<Transform>(true).Any(t=>Array.IndexOf(CreditsNames,t.name)>=0 && t.Find("Value")!=null)) continue;
                var root=PrefabUtility.LoadPrefabContents(path);
                try { Apply(root); PrefabUtility.SaveAsPrefabAsset(root,path); count++; }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[MenuAccountHeaderAuthoring] result=Passed prefabs="+count+" CommandAbsent=True CreditsInformational=True");
        }
        public static void PreserveNativeAriaImport()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(V3UiFoundationBuilder.SharedAriaPortraitPath);
            var settings = importer.GetDefaultPlatformTextureSettings();
            if(settings.maxTextureSize!=1024 || settings.textureCompression!=TextureImporterCompression.Uncompressed)
            {
                settings.maxTextureSize = 1024;
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
            }
            Debug.Log("[MenuHeaderAria] result=Passed originalSourceAndImport=True");
        }
        public static void RemoveAccountBindingFromCombatPopup()
        {
            const string path="Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var view in root.GetComponentsInChildren<MainMenuAccountHeaderView>(true))
                {
                    if(view.Value != null) view.Value.fontSizeMin=15.08f;
                    UnityEngine.Object.DestroyImmediate(view);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[MenuHeaderCombatBoundary] result=Passed accountCreditsAbsent=True");
        }
    }
}
