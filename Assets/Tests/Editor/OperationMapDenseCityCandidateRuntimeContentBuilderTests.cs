using System;
using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapDenseCityCandidateRuntimeContentBuilderTests
{
    public static void RunFocusedValidation()
    {
        var suite = new OperationMapDenseCityCandidateRuntimeContentBuilderTests();
        Action[] tests =
        {
            suite.MeasureEntityContent_SeparatesArchiveAndMetadataBytes,
            suite.MeasureEntityContent_RejectsMissingCatalog,
            suite.MeasureEntityContent_ReportsMultipleArchivesForFailClosedCaller
        };
        for (int i = 0; i < tests.Length; i++)
            tests[i]();
        Debug.Log(
            $"[DenseCityRuntimeContentByteInventoryValidation] result=Passed tests={tests.Length}");
    }

    [Test]
    public void MeasureEntityContent_SeparatesArchiveAndMetadataBytes()
    {
        WithContentDirectory(
            root =>
            {
                string catalog = WriteBytes(root, "catalog.bin", 7);
                WriteBytes(root, "scene.archive", 19);
                WriteBytes(root, "metadata/header.bin", 5);

                OperationMapDenseCityCandidateRuntimeContentBuilder.EntityContentBuildResult
                    result =
                        OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureEntityContent(
                            root,
                            catalog);

                Assert.That(result.ArchiveCount, Is.EqualTo(1));
                Assert.That(result.ArchiveBytes, Is.EqualTo(19));
                Assert.That(result.MetadataBytes, Is.EqualTo(12));
                Assert.That(result.TotalBytes, Is.EqualTo(31));
            });
    }

    [Test]
    public void MeasureEntityContent_RejectsMissingCatalog()
    {
        WithContentDirectory(
            root =>
            {
                WriteBytes(root, "scene.archive", 19);
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureEntityContent(
                        root,
                        Path.Combine(root, "missing-catalog.bin")));
                Assert.That(exception.Message, Does.Contain("catalog is missing"));
            });
    }

    [Test]
    public void MeasureEntityContent_ReportsMultipleArchivesForFailClosedCaller()
    {
        WithContentDirectory(
            root =>
            {
                string catalog = WriteBytes(root, "catalog.bin", 3);
                WriteBytes(root, "a.archive", 11);
                WriteBytes(root, "nested/b.archive", 13);

                OperationMapDenseCityCandidateRuntimeContentBuilder.EntityContentBuildResult
                    result =
                        OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureEntityContent(
                            root,
                            catalog);

                Assert.That(result.ArchiveCount, Is.EqualTo(2));
                Assert.That(result.ArchiveBytes, Is.EqualTo(24));
                Assert.That(result.MetadataBytes, Is.EqualTo(3));
                Assert.That(result.TotalBytes, Is.EqualTo(27));
            });
    }

    private static string WriteBytes(string root, string relativePath, int count)
    {
        string path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, new byte[count]);
        return path;
    }

    private static void WithContentDirectory(Action<string> action)
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "WarlineDenseRuntimeContentByteTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            action(root);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
