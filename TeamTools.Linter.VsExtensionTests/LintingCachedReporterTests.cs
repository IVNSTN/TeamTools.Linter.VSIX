using NUnit.Framework;
using TeamTools.Common.Linting;
using TeamTools.VisualStudio.SqlExtension.Linting;
using TeamTools.VisualStudio.SqlExtensionTests.TestingInfrastructure;

namespace TeamTools.VisualStudio.SqlExtensionTests
{
    [Category("Linter.VSExtension")]
    [TestOf(typeof(LintingCachedReporter))]
    public sealed class LintingCachedReporterTests
    {
        private LintingCachedReporter cachedReporter;

        [SetUp]
        public void SetUp()
        {
            cachedReporter = new LintingCachedReporter();
        }

        [Test]
        public void Test_LintingCachedReporterTests_ResetCleansCache()
        {
            var v = new RuleViolation() { Text = "one" };
            cachedReporter.ReportViolation(v);

            v = new RuleViolation { Text = "two" };
            cachedReporter.ReportViolation(v);
            Assert.That(cachedReporter.ViolationCount, Is.EqualTo(2));

            cachedReporter.Reset();
            Assert.That(cachedReporter.ViolationCount, Is.EqualTo(0));
        }

        [Test]
        public void Test_LintingCachedReporterTests_NullViolationDoesnotBreak()
        {
            Assert.DoesNotThrow(() => { cachedReporter.Report(null); }, "Report");
            Assert.DoesNotThrow(() => { cachedReporter.ReportFailure(null); }, "ReportFailure");
            Assert.DoesNotThrow(() => { cachedReporter.ReportViolation(null); }, "ReportViolation");
        }

        [Test]
        public void Test_LintingCachedReporterTests_DumpsEmAll()
        {
            var outPort = new MockLogger();

            var v = new RuleViolation() { Text = "one" };
            cachedReporter.ReportViolation(v);

            v = new RuleViolation { Text = "two" };
            cachedReporter.ReportViolation(v);

            cachedReporter.ReportFailure("three");

            cachedReporter.Dump(outPort, true);

            // 3 violations + totals
            Assert.That(outPort.Log, Has.Count.EqualTo(4));
        }
    }
}
