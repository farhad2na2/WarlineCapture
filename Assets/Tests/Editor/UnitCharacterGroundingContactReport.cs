using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using Game.Configs;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using UnityEditor;
using NUnit.Framework;
using UnityEngine;

public sealed partial class UnitCharacterGroundingValidationTests
{
    [Test]
    public void CharacterLocomotionProfiles_PlantFeetWithoutConstantHover()
    {
        RunLocomotionContactReport();
    }

    public static void RunLocomotionContactReport()
    {
        var rows=new List<string>();
        var samplerRoot=new GameObject("Animation contact sampler");
        var sampler=samplerRoot.AddComponent<MaterialAnimatorAuthoring>();
        try
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab Unit_Chr_",new[]{CharacterPrefabFolder}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var authoring=root.GetComponent<UnitGridAuthoring>();if(authoring==null)continue;
                    var model=ResolveModelRoot(root,authoring);
                    var index=model.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
                    if(index==null||index.animator==null)continue;
                    var source=index.animator.GetComponent<MaterialAnimatorAuthoring>();if(source==null)continue;
                    sampler.bonesCount=source.bonesCount;
                    for(int i=0;i<authoring.AnimationOrder.Count && i+1<source.animations.Count;i++)
                    {
                        var kind=authoring.AnimationOrder[i];if(kind!=UnitAnimationKind.Idle && kind!=UnitAnimationKind.Run && kind!=UnitAnimationKind.RunAim && kind!=UnitAnimationKind.Walk)continue;
                        var animation=source.animations[i+1];float min=float.PositiveInfinity,max=float.NegativeInfinity;
                        for(int frame=0;frame<animation.frames;frame+=Mathf.Max(1,animation.frames/20))
                        {
                            var pose=animation;pose.start+=frame*sampler.bonesCount;pose.frames=1;sampler.animations=new List<MaterialAnimatorBake>{default,pose};
                            if(TryGetGpuAnimatedMinY(root.transform,model,sampler,out float y,out _)){min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
                        }
                        NUnit.Framework.Assert.That(authoring.AnimationGroundOffsets,Is.Not.Null);
                        float contact=min+authoring.AnimationGroundOffsets[i+1];
                        NUnit.Framework.Assert.That(contact,Is.GreaterThanOrEqualTo(-.005f),path+" "+kind+" sinks below the road");
                        NUnit.Framework.Assert.That(contact,Is.LessThan(.025f),path+" "+kind+" retains a constant hover gap");
                        rows.Add($"{Path.GetFileNameWithoutExtension(path)} {kind} offset={authoring.AnimationGroundOffsets[i+1]:F4} footMin={min:F4} footMax={max:F4} contactMin={min+authoring.AnimationGroundOffsets[i+1]:F4} contactMax={max+authoring.AnimationGroundOffsets[i+1]:F4}");
                    }
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(127), "Canonical locomotion clips must remain covered.");
            File.WriteAllLines(Path.Combine(Path.GetTempPath(), "warline-unit-locomotion-contact.txt"),rows);
            Debug.Log("[UnitLocomotionContact] result=Passed clips="+rows.Count);
        }
        finally{Object.DestroyImmediate(samplerRoot);}
    }
}
