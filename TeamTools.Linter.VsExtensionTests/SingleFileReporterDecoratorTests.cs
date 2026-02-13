using NUnit.Framework;
using TeamTools.Common.Linting;
using TeamTools.VisualStudio.SqlExtensionTests.TestingInfrastructure;

namespace TeamTools.VisualStudio.SqlExtensionTests
{
    [Category("Linter.VSExtension")]
    [TestOf(typeof(SingleFileReporterDecorator))]
    public class SingleFileReporterDecoratorTests
    {
        private MockReporter logger;
        private SingleFileReporterDecorator reporter;

        [SetUp]
        public void SetUp()
        {
            logger = new MockReporter();
            reporter = new SingleFileReporterDecorator("dummy", logger);
        }

        [Test]
        public void Test_SingleFileReporterDecorator_PassesReportsToSameMethods()
        {
            reporter.Report("dummy");
            Assert.That(logger.CalledMethods, Does.Contain("Report"), "Report");
            logger.Reset();

            reporter.ReportFailure("dummy");
            Assert.That(logger.CalledMethods, Does.Contain("ReportFailure"), "ReportFailure");
            logger.Reset();

            reporter.ReportViolation(new RuleViolation());
            Assert.That(logger.CalledMethods, Does.Contain("ReportViolation"), "ReportViolation");
            logger.Reset();
        }
    }
}
