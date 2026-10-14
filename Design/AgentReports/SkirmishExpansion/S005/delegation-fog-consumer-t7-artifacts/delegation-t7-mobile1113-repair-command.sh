rtk proxy unity command eval --project-path /Volumes/T7/Projects/WarlineCapture-MenuUiUx --code 'if(Game.Editor.ExistingEditorValidation.IsRunning || UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) throw new System.InvalidOperationException("Require idle T7");
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Mobile quality repair requires an idle Editor.");
        const string mobilePath = "Assets/Settings/Mobile_RPAsset.asset";
        const string mobileGuid = "5e6cbd92db86f4b18aec3ed561671858";
        if (UnityEditor.AssetDatabase.AssetPathToGUID(mobilePath) != mobileGuid)
            throw new System.InvalidOperationException("Accepted Mobile pipeline GUID changed.");
        var pipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(mobilePath);
        if (pipeline == null || new UnityEditor.SerializedObject(pipeline).FindProperty("m_MSAA").intValue != 1)
            throw new System.InvalidOperationException("Accepted Mobile pipeline must have MSAA disabled (one sample).");
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (assets.Length != 1) throw new System.InvalidOperationException("Expected one native QualitySettings asset.");
        var so = new UnityEditor.SerializedObject(assets[0]);
        var tiers = so.FindProperty("m_QualitySettings");
        UnityEditor.SerializedProperty mobile = null;
        for (int i = 0; i < tiers.arraySize; i++)
        {
            var tier = tiers.GetArrayElementAtIndex(i);
            if (tier.FindPropertyRelative("name").stringValue != "Mobile") continue;
            if (mobile != null) throw new System.InvalidOperationException("Duplicate Mobile tier.");
            mobile = tier.Copy();
        }
        if (mobile == null) throw new System.InvalidOperationException("Missing Mobile tier.");
        var rp = mobile.FindPropertyRelative("customRenderPipeline");
        var aa = mobile.FindPropertyRelative("antiAliasing");
        if (rp.objectReferenceValue != null && rp.objectReferenceValue != pipeline)
            throw new System.InvalidOperationException("Unexpected Mobile override; preserve it for review.");
        if (aa.intValue != 0 && aa.intValue != 4)
            throw new System.InvalidOperationException("Unexpected Mobile MSAA; preserve it for review.");
        int beforeAa = aa.intValue;
        string beforePipeline = rp.objectReferenceValue != null ? UnityEditor.AssetDatabase.GetAssetPath(rp.objectReferenceValue) : "null";
        rp.objectReferenceValue = pipeline;
        aa.intValue = 0;
        so.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.EditorUtility.SetDirty(assets[0]);
        // Parent must capture full PRE/POST source manifests. This is the same native
        // project-settings persistence boundary as the existing refinement repair.
        UnityEditor.AssetDatabase.SaveAssets();
        if(UnityEngine.QualitySettings.names[UnityEngine.QualitySettings.GetQualityLevel()] != "Mobile") throw new System.InvalidOperationException("Preserve other current tier");
        UnityEngine.QualitySettings.renderPipeline = (UnityEngine.Rendering.RenderPipelineAsset)pipeline;
        UnityEngine.QualitySettings.antiAliasing = 0;
        var verify = new UnityEditor.SerializedObject(assets[0]);
        var after = verify.FindProperty("m_QualitySettings");
        for (int i = 0; i < after.arraySize; i++)
        {
            var tier = after.GetArrayElementAtIndex(i);
            if (tier.FindPropertyRelative("name").stringValue != "Mobile") continue;
            if (tier.FindPropertyRelative("antiAliasing").intValue != 0 ||
                tier.FindPropertyRelative("customRenderPipeline").objectReferenceValue != pipeline)
                throw new System.InvalidOperationException("Native Mobile settings did not persist in memory.");
        }
        string raw = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../ProjectSettings/QualitySettings.asset"));
        int start = raw.IndexOf("name: Mobile", System.StringComparison.Ordinal);
        int end = raw.IndexOf("\n  - serializedVersion:", start + 1, System.StringComparison.Ordinal);
        string block = end >= 0 ? raw.Substring(start, end - start) : raw.Substring(start);
        if (!block.Contains("antiAliasing: 0") || !block.Contains("guid: " + mobileGuid))
            throw new System.InvalidOperationException("Native Mobile settings did not persist to disk.");
        UnityEngine.Debug.Log("[S005MobileQualityRepair] result=Passed beforeAA=" + beforeAa + " beforePipeline=" + beforePipeline +
            " mobileAA=0 mobilePipeline=" + mobilePath + " urpMsaa=1 pcFallbackNotSelected=1 otherSourceMutationRequiresPostManifestCheck=1");
        return "Repaired native Mobile AA0 and accepted explicit pipeline; lifecycle validation remains pending.";' --format json > /private/tmp/delegation-t7-mobile1113-repair-result.json
