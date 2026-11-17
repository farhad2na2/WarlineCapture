#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Pure tests for the ARIA queue request file an open Editor reads when run_aria_queue.py --reuse drives it.</summary>
    public sealed class SkirmishAriaQueueRequestTests
    {
        private const string Root = "/Volumes/T7/Projects/WarlineCapture";
        private const string QueuePath = "/private/tmp/logs/queue.csv";
        private static readonly DateTime Now = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

        private static string Json(int version = 1, string id = "req-1", string project = Root, string queue = QueuePath,
            bool menu = true, bool validation = true, string created = "2026-11-17T11:59:30Z") =>
            "{\n  \"version\": " + version + ",\n  \"requestId\": \"" + id + "\",\n  \"project\": \"" + project +
            "\",\n  \"queue\": \"" + queue + "\",\n  \"menuJourney\": " + (menu ? "true" : "false") +
            ",\n  \"validationLibrary\": " + (validation ? "true" : "false") + ",\n  \"createdUtc\": \"" + created + "\"\n}\n";

        private static string Check(string json, Func<string, bool> exists = null, string root = Root)
        {
            string parseError = SkirmishAriaQueueRequest.Parse(json, out var request);
            if (parseError != null) return parseError;
            return SkirmishAriaQueueRequest.Validate(request, root, Now, exists ?? (_ => true));
        }

        [Test]
        public void WellFormedRequestIsAcceptedAndCarriesTheFlags()
        {
            Assert.IsNull(SkirmishAriaQueueRequest.Parse(Json(menu: true, validation: false), out var request));
            Assert.IsNull(SkirmishAriaQueueRequest.Validate(request, Root, Now, _ => true));
            Assert.IsTrue(request.menuJourney);
            Assert.IsFalse(request.validationLibrary);
            Assert.AreEqual(QueuePath, request.queue);
            Assert.AreEqual("req-1", request.requestId);
        }

        [Test]
        public void MalformedOrEmptyTextIsRefusedWithoutThrowing()
        {
            Assert.AreEqual("requestEmpty", Check(""));
            Assert.AreEqual("requestEmpty", Check("   "));
            Assert.AreEqual("requestUnparseable", Check("{ not json"));
            // A truncated (half-written) file must be refused, never half-applied.
            Assert.AreEqual("requestUnparseable", Check(Json().Substring(0, 40)));
        }

        [Test]
        public void MissingFieldsAndUnsupportedVersionAreRefused()
        {
            StringAssert.StartsWith("requestVersionUnsupported", Check(Json(version: 2)));
            StringAssert.StartsWith("requestVersionUnsupported", Check("{}"));
            Assert.AreEqual("requestIdMissing", Check(Json(id: "")));
            StringAssert.StartsWith("requestProjectMismatch", Check(Json(project: "")));
            Assert.AreEqual("requestQueueNotAbsolute", Check(Json(queue: "queue.csv")));
            Assert.AreEqual("requestQueueNotAbsolute", Check(Json(queue: "")));
        }

        [Test]
        public void RequestForAnotherCheckoutIsRefusedAndTrailingSeparatorsAreIgnored()
        {
            StringAssert.StartsWith("requestProjectMismatch:request=/Volumes/T7/Projects/WarlineCapture-wt/harness",
                Check(Json(project: "/Volumes/T7/Projects/WarlineCapture-wt/harness")));
            Assert.IsNull(Check(Json(project: Root + "/")));
            Assert.IsNull(Check(Json(), root: Root + "/"));
        }

        [Test]
        public void QueueFileMustExist()
        {
            Assert.AreEqual("requestQueueMissing:" + QueuePath, Check(Json(), _ => false));
        }

        [Test]
        public void StaleFutureAndMalformedTimestampsAreRefused()
        {
            Assert.IsNull(Check(Json(created: "2026-11-17T11:45:01Z")), "just inside the 15 minute window");
            StringAssert.StartsWith("requestStale:ageSeconds=", Check(Json(created: "2026-11-17T11:00:00Z")));
            StringAssert.StartsWith("requestFromFuture", Check(Json(created: "2026-11-17T12:10:00Z")));
            Assert.IsNull(Check(Json(created: "2026-11-17T12:00:30Z")), "small clock skew is tolerated");
            Assert.AreEqual("requestCreatedUtcInvalid", Check(Json(created: "yesterday")));
            Assert.AreEqual("requestCreatedUtcInvalid", Check(Json(created: "")));
        }

        [Test]
        public void RequestPathsAreFixedUnderTheProjectAndConsumedNamesAreSafe()
        {
            Assert.AreEqual("Temp/aria-queue/request.json", SkirmishAriaQueueRequest.RelativePath);
            Assert.AreEqual(Path.Combine(Root, "Temp", "aria-queue", "request.json"), SkirmishAriaQueueRequest.PathFor(Root));
            string consumed = SkirmishAriaQueueRequest.ConsumedPath("/p/request.json", "20261117T120000Z-ab12");
            Assert.AreEqual("/p/request.json.consumed-20261117T120000Z-ab12", consumed);
            StringAssert.DoesNotContain("/", SkirmishAriaQueueRequest.ConsumedPath("request.json", "a/b").Substring("request.json".Length));
            Assert.AreEqual("request.json.consumed-unknown", SkirmishAriaQueueRequest.ConsumedPath("request.json", ""));
        }

        [Test]
        public void LogLinesNameTheRequestAndTheReason()
        {
            SkirmishAriaQueueRequest.Parse(Json(), out var request);
            Assert.AreEqual("[SkirmishAriaQueue] request=File id=req-1 menuJourney=1 validationLibrary=1 queue=" + QueuePath,
                SkirmishAriaQueueRequest.FormatAccepted(request));
            Assert.AreEqual("[SkirmishAriaQueue] request=Refused reason=requestStale:ageSeconds=3600",
                SkirmishAriaQueueRequest.FormatRefused("requestStale:ageSeconds=3600"));
        }
    }
}
#endif
