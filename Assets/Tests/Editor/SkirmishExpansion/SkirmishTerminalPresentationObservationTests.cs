#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishTerminalPresentationObservationTests
    {
        private const string Attempt = "terminal-proof-fixture-attempt";
        private static SkirmishTerminalPresentationObservation.Proof Result(SkirmishOutcome outcome)
        {
            string title = UiLocalizedText.CatalogLabel(SkirmishTerminalPresentationObservation.ExpectedResultTitle(outcome));
            return new SkirmishTerminalPresentationObservation.Proof { attempt=Attempt, nativeOutcome=outcome.ToString(),
                locale=UiShellRuntimeGateway.Localization.CurrentLocaleCode, frame=17, rendered=true, copyMatches=true,
                ownerPath="Canvas/SkirmishResult/CampaignStyleResult", titlePath="Canvas/SkirmishResult/CampaignStyleResult/Outcome/Title",
                expectedTitle=title, actualTitle=title };
        }
        private static SkirmishTerminalPresentationObservation.Proof Menu()
        {
            string title=UiLocalizedText.CatalogLabel("SKIRMISH");
            return new SkirmishTerminalPresentationObservation.Proof { attempt=Attempt, frame=32,
                locale=UiShellRuntimeGateway.Localization.CurrentLocaleCode, rendered=true, presented=true, copyMatches=true, pushHistory=true,
                ownerPath="Canvas/Card_Skirmish", controlPath="Canvas/Card_Skirmish/Hotspot", titlePath="Canvas/Card_Skirmish/Title",
                intent=UiShellRouteIntent.OpenMenuRoute.ToString(), route=UIRoute.QuickCustomSetup.ToString(), expectedTitle=title, actualTitle=title };
        }

        [Test] public static void ExactNativeOutcomeHeading()
        {
            foreach (var outcome in new[] { SkirmishOutcome.Victory, SkirmishOutcome.Defeat, SkirmishOutcome.Draw })
            {
                Assert.That(SkirmishTerminalPresentationObservation.AcceptResult(Result(outcome),Attempt,outcome),Is.True);
                foreach (var other in new[] { SkirmishOutcome.Victory, SkirmishOutcome.Defeat, SkirmishOutcome.Draw })
                    if (other != outcome) Assert.That(SkirmishTerminalPresentationObservation.AcceptResult(Result(outcome),Attempt,other),Is.False);
            }
            Assert.That(SkirmishTerminalPresentationObservation.AcceptResult(Result(SkirmishOutcome.Victory),Attempt,SkirmishOutcome.None),Is.False);
        }

        [Test] public static void ResultRefusesForeignHiddenAndUnboundCopy()
        {
            Action<SkirmishTerminalPresentationObservation.Proof>[] corruptions = {
                p=>p.attempt="foreign", p=>p.nativeOutcome="Defeat", p=>p.frame=0, p=>p.rendered=false,
                p=>p.copyMatches=false, p=>p.locale="foreign", p=>p.expectedTitle="wrong", p=>p.actualTitle="wrong",
                p=>p.titlePath="Canvas/SkirmishResult/CampaignStyleResult/Status/Label", p=>p.ownerPath="Canvas/UnrelatedResult" };
            foreach (var corrupt in corruptions)
            {
                var proof=Result(SkirmishOutcome.Victory); corrupt(proof);
                Assert.That(SkirmishTerminalPresentationObservation.AcceptResult(proof,Attempt,SkirmishOutcome.Victory),Is.False);
            }
        }

        [Test] public static void ExactLocalizedMenuBindingAndRefusals()
        {
            Assert.That(SkirmishTerminalPresentationObservation.AcceptMenu(Menu(),Attempt),Is.True);
            Action<SkirmishTerminalPresentationObservation.Proof>[] corruptions = {
                p=>p.attempt="foreign", p=>p.frame=0, p=>p.rendered=false, p=>p.presented=false, p=>p.copyMatches=false,
                p=>p.pushHistory=false, p=>p.locale="foreign", p=>p.actualTitle="wrong", p=>p.expectedTitle="wrong",
                p=>p.ownerPath="Canvas/Card_Campaign", p=>p.controlPath="Canvas/Card_Skirmish/UnboundButton",
                p=>p.titlePath="Canvas/Unrelated/Title", p=>p.intent=UiShellRouteIntent.OpenSettings.ToString(), p=>p.route=UIRoute.Settings.ToString() };
            foreach (var corrupt in corruptions)
            {
                var proof=Menu(); corrupt(proof);
                Assert.That(SkirmishTerminalPresentationObservation.AcceptMenu(proof,Attempt),Is.False);
            }
        }

        [Test] public static void MissingNativePresentationAndLocalizedShape()
        {
            Assert.That(SkirmishTerminalPresentationObservation.AcceptMenu(null,Attempt),Is.False);
            Assert.That(SkirmishTerminalPresentationObservation.AcceptResult(null,Attempt,SkirmishOutcome.Victory),Is.False);
            var missing=SkirmishTerminalPresentationObservation.ReadResult(null,Attempt,SkirmishOutcome.Victory);
            Assert.That(SkirmishTerminalPresentationObservation.AcceptResult(missing,Attempt,SkirmishOutcome.Victory),Is.False);
            const string fa="پیروزی";
            Assert.That(SkirmishTerminalPresentationObservation.CopyMatches(fa,fa),Is.True);
            Assert.That(SkirmishTerminalPresentationObservation.CopyMatches(V3LocalizedTextBindingView.ShapeForRendering(fa),fa),Is.True);
            Assert.That(SkirmishTerminalPresentationObservation.CopyMatches("شکست",fa),Is.False);
        }

        public static void RunFocusedValidation()
        {
            ExactNativeOutcomeHeading(); ResultRefusesForeignHiddenAndUnboundCopy();
            ExactLocalizedMenuBindingAndRefusals(); MissingNativePresentationAndLocalizedShape();
            NativeSessionAbsenceIncludesDuplicateAndDisabledOwners();
            AcceptedCandidateRetainsItsOwnPointAndAmbiguityRefuses();
            Debug.Log("[SkirmishTerminalPresentationObservationTests] result=Passed methods=6 nativeTerminalAndReturnJourney=Pending");
        }

        [Test] public static void AcceptedCandidateRetainsItsOwnPointAndAmbiguityRefuses()
        {
            var valid=Menu(); valid.pointX=123; valid.pointY=456;
            var rejected=Menu(); rejected.actualTitle="Wrong visible title"; rejected.pointX=900; rejected.pointY=800;
            var selected=SkirmishTerminalPresentationObservation.SelectUniqueMenu(new[] { valid, rejected },Attempt);
            Assert.That(selected,Is.SameAs(valid));
            Assert.That(selected.pointX,Is.EqualTo(123)); Assert.That(selected.pointY,Is.EqualTo(456));
            Assert.That(SkirmishTerminalPresentationObservation.SelectUniqueMenu(new[] { valid, Menu() },Attempt),Is.Null);
            Assert.That(SkirmishTerminalPresentationObservation.SelectUniqueMenu(new[] { rejected },Attempt),Is.Null);
        }

        [Test] public static void NativeSessionAbsenceIncludesDuplicateAndDisabledOwners()
        {
            using var world = new World("TerminalPresentationObservationFixture");
            var em=world.EntityManager;
            Assert.That(SkirmishTerminalPresentationObservation.NativeSessionCount(em), Is.Zero);
            var first=em.CreateEntity(typeof(SkirmishMatchState));
            var second=em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(Disabled));
            Assert.That(SkirmishTerminalPresentationObservation.NativeSessionCount(em), Is.EqualTo(2));
            em.DestroyEntity(first);
            Assert.That(SkirmishTerminalPresentationObservation.NativeSessionCount(em), Is.EqualTo(1));
            em.DestroyEntity(second);
            Assert.That(SkirmishTerminalPresentationObservation.NativeSessionCount(em), Is.Zero);
        }
    }
}
#endif
