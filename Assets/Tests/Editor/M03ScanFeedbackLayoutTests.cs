#if UNITY_EDITOR
using System;
using Game.UI.Runtime;
using Game.Configs;
using Game.UI.Contracts;
using Game.Tactical.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M03ScanFeedbackLayoutTests
{
    [Test]
    public void ScanFeedbackFitsItsPanelInBothLanguages()
    {
        string previousLocale = GameLocalization.CurrentLocaleCode;
        var root = PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        try
        {
            var view = root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            Assert.That(view, Is.Not.Null);
            foreach (var message in new[] { "Scan: no vehicles. Keep holding.", "Scan: 12 vehicles. Keep holding.",
                "اسکن: خودرویی نیست؛ دفاع رو ادامه بده.", "اسکن: ۱۲ خودرو پیدا شد؛ دفاع رو ادامه بده." })
            {
                GameLocalization.SetLocale(message.StartsWith("Scan:") ? "en" : "fa-IR", false);
                view.ApplyTransientCommandFeedback(MatchHudCommandFeedbackModel.ShowTransient(message, CommandFeedbackSeverity.Ready, 8f), 0f);
                Canvas.ForceUpdateCanvases();
                var label = view.FeedbackText;
                var panel = (RectTransform)view.FeedbackPanel.transform;
                var rect = label.rectTransform;
                Assert.That(rect.anchoredPosition.x + rect.rect.width, Is.LessThanOrEqualTo(panel.rect.width - 15f), message);
                Assert.That(label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x,
                    Is.LessThanOrEqualTo(rect.rect.width), message);
            }
            Debug.Log("[M03ScanLayout] result=Passed EN/FA zero/contact messages fit panel and text bounds");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); GameLocalization.SetLocale(previousLocale, false); }
    }
}
#endif
