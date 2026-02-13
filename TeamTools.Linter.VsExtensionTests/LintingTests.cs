using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using TeamTools.Common.Linting;
using TeamTools.Common.Linting.Infrastructure;
using TeamTools.Common.Linting.Interfaces;
using TeamTools.VisualStudio.SqlExtension.Linting;
using TeamTools.VisualStudio.SqlExtensionTests.TestingInfrastructure;

namespace TeamTools.VisualStudio.SqlExtensionTests
{
    [Category("Linter.VSExtension")]
    public sealed class LintingTests
    {
        [Test]
        public void TestCachedReporterDumpsAllSorted()
        {
            var reporter = new LintingCachedReporter();
            var outputPort = new MockLogger();

            reporter.Report("c");
            reporter.Report("a");
            reporter.Report("d");
            reporter.Report("b");
            reporter.Dump(outputPort);

            var fullLog = string.Join(";", outputPort.Log);
            Assert.That(fullLog, Is.EqualTo("a;b;c;d"));
        }

        [Test]
        public void TestLintingAssistantGoesToVcsToLintDiff()
        {
            var output = new MockLogger();
            var vcs = new MockVcs("test.file");
            var linter = new MockAssistant(
                output,
                new AssemblyWrapper(),
                new StubFileSystem("test.file", "another.file"),
                new MockConfigLoader(),
                vcs,
                "dummy.cfg");

            linter.LintModifiedFiles("dummy", null);

            Assert.That(vcs.GetModifiedFilesCallCount, Is.EqualTo(1));
        }

        /* TODO : test LinterService
        [Test]
        public void TestLintingAssistantNeverGoesToVcsForFileOrFolder()
        {
            var output = new MockLogger();
            var vcs = new MockVcs("test.file");
            var rpt = new MockReporter();
            var linter = new MockAssistant(
                output,
                new AssemblyWrapper(),
                new StubFileSystem("test.file", "another.file"),
                new MockConfigLoader(),
                vcs,
                "dummy.cfg");

            linter.LintFile("dummy", null);
            Assert.That(vcs.GetModifiedFilesCallCount, Is.EqualTo(0));

            linter.LintFolder("dummy", rpt);
            Assert.That(vcs.GetModifiedFilesCallCount, Is.EqualTo(0));
        }

        [Test]
        public void TestLintingAssistantLintsAllFolder()
        {
            var output = new MockLogger();
            var rpt = new MockReporter();
            var vcs = new MockVcs("test.file");
            var linter = new MockAssistant(
                output,
                new AssemblyWrapper(),
                new StubFileSystem("test.file", "another.file"),
                new MockConfigLoader(),
                vcs,
                "dummy.cfg");

            linter.LintFolder("dummy", rpt);

            // TODO : return verbose logging
            // Assert.AreEqual(4, output.Log.Count);
            // Assert.Contains("test.file", output.Log);
            // Assert.Contains("another.file", output.Log);
            Assert.That(rpt.Events, Has.Count.EqualTo(2));
            Assert.That(rpt.Events, Does.Contain("test.file"));
            Assert.That(rpt.Events, Does.Contain("another.file"));
        }
        */

        /* TODO : fix text
        [Test]
        public void TestLintingAssistantWritesToOutputPort()
        {
            var output = new MockLogger();
            var vcs = new MockVcs("test.file");
            var linter = new MockAssistant(
                output,
                new AssemblyWrapper(),
                new StubFileSystem("test.file", "another.file"),
                new MockConfigLoader(),
                vcs,
                "dummy.cfg");

            linter.LintModifiedFiles("dummy", null);

            Assert.That(output.Log, Has.Count.EqualTo(1));
            Assert.That(output.Log[0], Is.EqualTo("test.file"));
        }
        */

        private class MockAssistant : LintingAssistant
        {
            public MockAssistant(
                ITextOutputPort outputPort,
                IAssemblyWrapper assemblyWrapper,
                IFileSystemWrapper fileSystemWrapper,
                IAppConfigLoader configLoader,
                IVcsAccessor vcs,
                string configPath) : base(outputPort, assemblyWrapper, fileSystemWrapper, configLoader, vcs, configPath)
            {
            }

            protected override ILinterHandler MakeLinter(IAssemblyWrapper asmWrapper, IReporter reporter, IAppConfigLoader cfgLoader, string cfgPath, string currentCulture)
            {
                return new MockLinterHandler();
            }
        }

        private class MockLinterHandler : ILinterHandler
        {
            public ICollection<string> LintedFiles { get; } = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            public void RunOnFiles(IEnumerable<string> files, Action<string> reportVerbose, IReporter pluginRepoter)
            {
                foreach (var f in files)
                {
                    pluginRepoter.Report(f);
                    LintedFiles.Add(f);
                }
            }

            public void RunOnFileSource(string file, TextReader source, IReporter pluginRepoter)
            {
                pluginRepoter.Report(file);
                LintedFiles.Add(file);
            }
        }

        private class MockConfigLoader : IAppConfigLoader
        {
            public IDictionary<string, PluginInfo> Plugins => new Dictionary<string, PluginInfo>();

            public ICollection<string> IgnoredFolders => new List<string>();

            public ICollection<string> IgnoredExtensions => new List<string>();

            public string MainBranch => "main";

            public void LoadFromFile(string filePath)
            {
                // dummy;
            }
        }
    }
}
