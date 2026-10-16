using System;
using Game.Runtime;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    public sealed class VehicleRepairRulesTests
    {
        [Test] public void FullQuarterCostsTwentyAndTakesTwentySimulationSeconds()
        {Assert.IsTrue(VehicleRepairRules.TryQuote(100,200,out var p));Assert.AreEqual(20,p.Materials);Assert.AreEqual(20*TimeSpan.TicksPerSecond,p.DurationTicks);Assert.AreEqual(50,VehicleRepairRules.CompletionHeal(p.FractionNumerator,p.FractionDenominator,100,200));}
        [Test] public void SmallMissingHealthProratesWithCeilingCostAndDuration()
        {Assert.IsTrue(VehicleRepairRules.TryQuote(199,200,out var p));Assert.AreEqual(1,p.Materials);Assert.AreEqual(4000000,p.DurationTicks);Assert.AreEqual(1,VehicleRepairRules.CompletionHeal(p.FractionNumerator,p.FractionDenominator,199,200));}
        [Test] public void DestructionNoRefundAndCancellationUsesExactWorkBoundary()
        {Assert.AreEqual(20,VehicleRepairRules.Refund(20,0,false));Assert.AreEqual(15,VehicleRepairRules.Refund(20,1,false));Assert.AreEqual(0,VehicleRepairRules.Refund(20,0,true));Assert.AreEqual(0,VehicleRepairRules.Refund(1,1,false));}
        [Test] public void HealthUpgradeRetainsAcceptedFractionAndDoesNotChargeAgain()
        {Assert.IsTrue(VehicleRepairRules.TryQuote(100,200,out var p));Assert.IsFalse(VehicleRepairRules.TookDamage(100,200,150,300));Assert.AreEqual(75,VehicleRepairRules.CompletionHeal(p.FractionNumerator,p.FractionDenominator,150,300));Assert.AreEqual(20,p.Materials);}
        [Test] public void IncomingDamageIsDetectedIncludingDuringHealthUpgrade()
        {Assert.IsTrue(VehicleRepairRules.TookDamage(100,200,99,200));Assert.IsTrue(VehicleRepairRules.TookDamage(100,200,149,300));Assert.IsTrue(VehicleRepairRules.TookDamage(100,200,0,200));}
        [Test] public void CompletionClampsToMissingHealthWithoutRevival()
        {Assert.AreEqual(1,VehicleRepairRules.CompletionHeal(1,4,199,200));Assert.AreEqual(0,VehicleRepairRules.CompletionHeal(1,4,0,200));Assert.AreEqual(0,VehicleRepairRules.CompletionHeal(1,4,200,200));}
        [Test] public void InvalidHealthAndFractionsNeverQuoteOrHeal()
        {foreach(var hp in new[]{-1,0,100,101})Assert.IsFalse(VehicleRepairRules.TryQuote(hp,100,out _));Assert.AreEqual(0,VehicleRepairRules.CompletionHeal(2,4,1,100));Assert.AreEqual(0,VehicleRepairRules.CompletionHeal(1,0,1,100));}
        [Test] public void ExtremeIntegerHealthDoesNotOverflowQuoteOrDamageMath()
        {Assert.IsTrue(VehicleRepairRules.TryQuote(int.MaxValue-1,int.MaxValue,out var p));Assert.AreEqual(1,p.Materials);Assert.GreaterOrEqual(p.DurationTicks,1);Assert.AreEqual(1,VehicleRepairRules.CompletionHeal(p.FractionNumerator,p.FractionDenominator,int.MaxValue-1,int.MaxValue));Assert.IsFalse(VehicleRepairRules.TookDamage(int.MaxValue,int.MaxValue,int.MaxValue,int.MaxValue));}
    }
}
