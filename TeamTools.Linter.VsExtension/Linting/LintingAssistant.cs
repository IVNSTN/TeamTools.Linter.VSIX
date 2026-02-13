using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamTools.Common.Linting;
using TeamTools.Common.Linting.Interfaces;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    // TODO: migrate to LinterService
    internal class LintingAssistant
    {
        private const string StartingLintingMsgTemplate = "Linting {0} files...";
        private const string MainBranch = "master"; // TODO : take from config
        private readonly ITextOutputPort outputPort;
        private readonly IFileSystemWrapper fileSystemWrapper;
        private readonly IAppConfigLoader configLoader;
        private readonly IVcsAccessor vcs;
        private readonly ILinterHandler linter;

        public LintingAssistant(
            ITextOutputPort outputPort,
            IAssemblyWrapper assemblyWrapper,
            IFileSystemWrapper fileSystemWrapper,
            IAppConfigLoader configLoader,
            IVcsAccessor vcs,
            string configPath)
        {
            this.outputPort = outputPort ?? throw new ArgumentNullException(nameof(outputPort));
            this.fileSystemWrapper = fileSystemWrapper ?? throw new ArgumentNullException(nameof(fileSystemWrapper));
            this.configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
            this.vcs = vcs ?? throw new ArgumentNullException(nameof(vcs));

            linter = MakeLinter(
                assemblyWrapper,
                new LogReporter(outputPort),
                configLoader,
                configPath,
                CultureInfo.CurrentUICulture.Name);
        }

        public void LintFile(string filePath, TextReader src, IReporter reporter)
        {
            PerformLinting(filePath, src, reporter);
        }

        public void LintModifiedFiles(string folderPath, Action<string, int, int> progressCallback)
        {
            PerformLinting(folderPath, true, progressCallback);
        }

        public async Task<IList<string>> GetFilesToLintAsync(CancellationToken token, string folderPath, bool diffOnly = false)
        {
            return await Task.Run<IList<string>>(
                () => GetFilesToLintFromFolder(folderPath, diffOnly, configLoader.IgnoredFolders, configLoader.IgnoredExtensions),
                token);
        }

        // TODO : turn into factory method injected in constructor or something alike
        // and remove assemblyWrapper from constructor attributes
        protected virtual ILinterHandler MakeLinter(IAssemblyWrapper asmWrapper, IReporter reporter, IAppConfigLoader cfgLoader, string cfgPath, string currentCulture)
        {
            // TODO : load only once?
            // TODO: config service, watch for changes, reload
            cfgLoader.LoadFromFile(cfgPath);
            var linter = new PluginHandler(asmWrapper);
            linter.LoadPlugins(cfgLoader.Plugins, msg => reporter?.Report(msg), reporter, currentCulture);

            return linter;
        }

        private void PerformLinting(string file, TextReader src, IReporter reporter)
        {
            // TODO : System.Threading.Tasks.Task.Run(async () =>
            linter.RunOnFileSource(file, src, reporter);
        }

        // TODO : make it async
        private void PerformLinting(string folderPath, bool diffOnly, IReporter reporter, Action<string, int, int> progressCallback = null)
        {
            try
            {
                var filesToLint = GetFilesToLintFromFolder(folderPath, diffOnly, configLoader.IgnoredFolders, configLoader.IgnoredExtensions);
                outputPort.WriteLine(string.Format(StartingLintingMsgTemplate, filesToLint.Count.ToString()));

                // TODO : System.Threading.Tasks.Task.Run(async () =>
                // FIXME : ThreadHelper.JoinableTaskFactory is broken
                DoRunLinting(linter, filesToLint, reporter, progressCallback);
            }
            catch (Exception e)
            {
                outputPort.WriteLine(e.Message);
            }
        }

        private void PerformLinting(string folderPath, bool diffOnly, Action<string, int, int> progressCallback = null)
        {
            var reporter = new LintingCachedReporter();
            try
            {
                PerformLinting(folderPath, diffOnly, reporter, progressCallback);
            }
            catch (Exception e)
            {
                outputPort.WriteLine(e.Message);
            }
            finally
            {
                if (diffOnly)
                {
                    // TODO : finish migrating to async reporting to ErrorList window
                    // for diff linting as well. Other scenarios are already there.
                    reporter.Dump(outputPort);
                }
            }
        }

        // TODO : make it async
        private IList<string> GetFilesToLintFromFolder(string folderPath, bool diffOnly, ICollection<string> ignoredFolders, ICollection<string> ignoredExtensions)
        {
            IEnumerable<string> files = fileSystemWrapper.GetAllFilesFromDirectory(folderPath, ignoredFolders, ignoredExtensions);
            if (diffOnly)
            {
                var diffFiles = vcs.GetModifiedFiles(folderPath, MainBranch).Select(f =>
                    f.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
                // git is CaseSensitive but Windows is not. so ignoring character case
                files = files.Intersect(diffFiles, StringComparer.OrdinalIgnoreCase);
            }

            return files.Distinct().OrderBy(f => f).ToList();
        }

        private void DoRunLinting(ILinterHandler linter, IList<string> filesToLint, IReporter reporter, Action<string, int, int> progressCallback)
        {
            int n = filesToLint.Count;
            int i = 0;

            // TODO : rethink
            Action<string> callback;
            if (progressCallback is null)
            {
                callback = null;
            }
            else
            {
                callback = (msg) =>
                {
                    if (msg.StartsWith("starting with file", StringComparison.OrdinalIgnoreCase))
                    {
                        progressCallback.Invoke("Linting", Interlocked.Increment(ref i), n);
                    }
                };
            }

            try
            {
                linter.RunOnFiles(filesToLint, msg => callback(msg), reporter);
            }
            finally
            {
                if (n > 0)
                {
                    progressCallback?.Invoke("Linting", n, n);
                }
            }
        }

        private class LogReporter : IReporter
        {
            private readonly ITextOutputPort outputPort;

            public LogReporter(ITextOutputPort outputPort)
            {
                this.outputPort = outputPort;
            }

            public void Report(string msg)
            {
                outputPort?.WriteLine(msg);
            }

            public void ReportFailure(string error)
            {
                outputPort?.WriteLine(error);
            }

            public void ReportViolation(RuleViolation violation)
            {
                outputPort?.WriteLine(violation.ToString());
            }
        }
    }
}
