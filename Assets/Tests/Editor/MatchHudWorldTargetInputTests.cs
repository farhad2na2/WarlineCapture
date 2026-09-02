#if UNITY_EDITOR
using System;
using System.Reflection;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudWorldTargetInputTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            new MatchHudWorldTargetInputTests()
                .FullScreenFooterOwner_DoesNotConsumeWorldTargetClicks();
            Debug.Log("[MatchHudWorldTargetInputValidation] result=Passed worldTargets=Move,Attack");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudWorldTargetInputValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void FullScreenFooterOwner_DoesNotConsumeWorldTargetClicks()
    {
        GameObject footer = new("FooterContent", typeof(RectTransform));
        try
        {
            RectTransform footerRect = footer.GetComponent<RectTransform>();
            footerRect.sizeDelta = new Vector2(1672f, 941f);
            MatchOverlayCommandControlsView controls =
                footer.AddComponent<MatchOverlayCommandControlsView>();

            Button move = CreateButton(footerRect, "MoveCommand", new Vector2(-70f, -410f));
            Button attack = CreateButton(footerRect, "AttackCommand", new Vector2(70f, -410f));
            SetField(controls, "moveButton", move);
            SetField(controls, "attackButton", attack);

            Vector2 groundTarget = WorldToScreenPoint(footerRect, new Vector2(-260f, 30f));
            Vector2 hostileTarget = WorldToScreenPoint(footerRect, new Vector2(260f, 130f));
            Vector2 moveButtonPoint = WorldToScreenPoint(move.transform as RectTransform, Vector2.zero);
            Vector2 attackButtonPoint = WorldToScreenPoint(attack.transform as RectTransform, Vector2.zero);

            Assert.IsFalse(
                controls.ContainsScreenPoint(groundTarget),
                "M1 Move destination clicks must pass through the full-screen footer owner.");
            Assert.IsFalse(
                controls.ContainsScreenPoint(hostileTarget),
                "M1 hostile target clicks must pass through the full-screen footer owner.");
            Assert.IsTrue(controls.ContainsScreenPoint(moveButtonPoint));
            Assert.IsTrue(controls.ContainsScreenPoint(attackButtonPoint));
            Assert.AreEqual("None", controls.DescribeScreenPointHit(groundTarget));
            Assert.AreEqual("None", controls.DescribeScreenPointHit(hostileTarget));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(footer);
        }
    }

    private static Button CreateButton(RectTransform parent, string name, Vector2 position)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(120f, 80f);
        rect.anchoredPosition = position;
        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = gameObject.GetComponent<Image>();
        return button;
    }

    private static Vector2 WorldToScreenPoint(RectTransform rect, Vector2 localPoint) =>
        RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(localPoint));

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, name);
        field.SetValue(target, value);
    }
}
#endif
