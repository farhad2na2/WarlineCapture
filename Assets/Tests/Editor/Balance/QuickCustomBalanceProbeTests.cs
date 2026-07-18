using System.IO;
using NUnit.Framework;
using Game.Runtime;

public sealed class QuickCustomBalanceProbeTests
{
    [Test]
    [Explicit("Opt-in balance probe. Produces a report and is intentionally excluded from normal Jenkins/build validation.")]
    [Category("Balance")]
    public void QuickCustom_Default_Medium_ProducesBalanceReport()
    {
        BalanceReportWriter.ReportPaths paths = QuickCustomBalanceProbe.RunDefaultMediumReport();

        Assert.IsTrue(File.Exists(paths.JsonPath), paths.JsonPath);
        Assert.IsTrue(File.Exists(paths.MarkdownPath), paths.MarkdownPath);
        Assert.Greater(new FileInfo(paths.JsonPath).Length, 0);
        Assert.Greater(new FileInfo(paths.MarkdownPath).Length, 0);
    }

    [Test]
    [Explicit("Opt-in balance probe. Produces a report and is intentionally excluded from normal Jenkins/build validation.")]
    [Category("Balance")]
    public void QuickCustom_Hard_Swarm_ProducesBalanceReport()
    {
        BalanceReportWriter.ReportPaths paths = QuickCustomBalanceProbe.RunHardSwarmReport();

        Assert.IsTrue(File.Exists(paths.JsonPath), paths.JsonPath);
        Assert.IsTrue(File.Exists(paths.MarkdownPath), paths.MarkdownPath);
        Assert.Greater(new FileInfo(paths.JsonPath).Length, 0);
        Assert.Greater(new FileInfo(paths.MarkdownPath).Length, 0);
    }
}
