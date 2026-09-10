using System;
using Game.Configs;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private static string wideResultOwner;
        private static int wideResultStage;

        private static bool CaptureWideResult(string owner)
        {
            if(wideResultOwner!=owner) {wideResultOwner=owner;wideResultStage=0;}
            if(wideResultStage>0 && Time.frameCount-frame<10)return false;
            switch(wideResultStage)
            {
                case 0:
                    GameLocalization.SetLocale("fa-IR",false);
                    MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);break;
                case 1:
                    if(!ResultVisible())return false;
                    ScreenCapture.CaptureScreenshot(Output+"/"+owner+"-fa-20x9.png");break;
                case 2:GameLocalization.SetLocale("en",false);break;
                case 3:
                    if(!ResultVisible())return false;
                    ScreenCapture.CaptureScreenshot(Output+"/"+owner+"-en-20x9.png");break;
                case 4:MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);break;
                default:return ResultVisible();
            }
            wideResultStage++;frame=Time.frameCount;return false;
        }

        private static void ValidateResultViewport(MissionResultPopupView view)
        {
            var layout=view.GetComponentInChildren<MainMenuV3SectionLayoutView>();
            if(layout==null)throw new InvalidOperationException("M04 result has no responsive composition");
            var canvas=view.GetComponentInParent<Canvas>().rootCanvas;
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var viewport=canvas.pixelRect;
            var corners=new Vector3[4];((RectTransform)layout.transform).GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                if(point.x<viewport.xMin-1 || point.y<viewport.yMin-1 || point.x>viewport.xMax+1 || point.y>viewport.yMax+1)
                    throw new InvalidOperationException($"M04 result outside {viewport}: {point}");
            }
        }
    }
}
