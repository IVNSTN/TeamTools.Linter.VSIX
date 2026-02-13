using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamTools.Common.Linting;
using TeamTools.Common.Linting.Infrastructure;
using TeamTools.VisualStudio.SqlExtension.Tagging;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    [Export(typeof(ILinterService))]
    public class LinterService : ILinterService
    {
        // TODO : take from config file
        private static readonly string BaseDocsUri = @"https://github.com/IVNSTN/TeamTools.Linter.TSQL/tree/main/TeamTools.TSQL.Linter/Resources/Docs";
        // TODO : take from config file
        private static readonly string FallbackLanguage = "en-us";

        private readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
        private readonly string configPath;
        private readonly DocsLinkBuilder docsLinkBuilder;
        private LintingAssistant linterPluginDecorator;

        private LinterService()
        {
            string codebase = typeof(LintPackage).Assembly.CodeBase;
            var uri = new Uri(codebase, UriKind.Absolute);
            configPath = Path.Combine(Path.GetDirectoryName(uri.LocalPath), "Resources", "DefaultConfig.json");

            docsLinkBuilder = new DocsLinkBuilder(BaseDocsUri, FallbackLanguage);
        }

        public event LinterMessageReportedEvent OnMessageReported;

        public event EventHandler<GeneralErrorArgs> OnError;

        [Import]
        public ITextOutputPort OutputPort { get; set; }

        public async Task InitializeAsync(CancellationToken cancellation)
        {
            if (linterPluginDecorator != null)
            {
                return;
            }

            await Task.Run(
                () =>
                {
                    // TODO: DI, please come and save us
                    var assemblyWrapper = new AssemblyWrapper();
                    var fileSystemWrapper = new FileSystemWrapper();

                    // TODO : also LintingAssistantVSAdapter creates LintingAssistant
                    // thus linter instance is created twice.
                    // Finish migration to LinverService, don't create linter, don't load plugins in LintingAssistant
                    // or get rid of LintingAssistant completely
                    linterPluginDecorator = new LintingAssistant(
                        OutputPort,
                        assemblyWrapper,
                        fileSystemWrapper,
                        new AppConfigLoader(fileSystemWrapper, assemblyWrapper),
                        new GitDecorator(new GitCommandFactory()),
                        configPath);
                },
                cancellation);
        }

        public async Task LintFileAsync(string filePath, CancellationToken token)
        {
            await LintAsync(filePath, token);
        }

        public async Task LintFileTabAsync(string filePath, TextReader reader, CancellationToken token)
        {
            await LintAsync(filePath, token, reader);
        }

        public async Task LintFolderAsync(string folderPath, CancellationToken token)
        {
            var files = await linterPluginDecorator.GetFilesToLintAsync(token, folderPath);

            try
            {
                foreach (var f in files)
                {
                    await LintAsync(f, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // dummy
            }
        }

        protected async Task LintAsync(string filePath, CancellationToken token, TextReader src = null)
        {
            try
            {
                await mutex.WaitAsync(token).ConfigureAwait(false);

                try
                {
                    token.ThrowIfCancellationRequested();

                    if (linterPluginDecorator is null)
                    {
                        await InitializeAsync(token).ConfigureAwait(false);
                    }

                    // TODO: smth like LintingMessageReporter
                    // FIXME: when folder is linted messages are duplicated in
                    // VS output panel. In other scenarios no such thing happen.
                    var reporter = new LintingCachedReporter();

                    if (src is null)
                    {
                        using TextReader reader = new StreamReader(filePath);
                        linterPluginDecorator.LintFile(filePath, reader, reporter);
                    }
                    else
                    {
                        linterPluginDecorator.LintFile(filePath, src, reporter);
                    }

                    token.ThrowIfCancellationRequested();

                    IEnumerable<LinterMessage> messages;

                    if (reporter.ViolationCount < 1)
                    {
                        messages = Enumerable.Empty<LinterMessage>();
                    }
                    else
                    {
                        var fileProjInfo = await ItemProjectLocator.GetProjectInfoAsync(filePath, token).ConfigureAwait(false);

                        // TODO: refactor
                        messages = reporter.Violations.Select(v =>
                        {
                            var docs = docsLinkBuilder.Build(v.RuleId);

                            return new LinterMessage
                            {
                                StartColumn = v.Column,
                                StartLine = v.Line,
                                Length = v.FragmentLength,
                                RuleId = v.RuleId,
                                Source = v.FileName,
                                FilePath = filePath,
                                ProjectName = fileProjInfo.ProjectName,
                                ProjectGuid = fileProjInfo.ProjectGuid,
                                Message = v.Text,
                                MessageSeverity = v.ViolationSeverity,
                                DocumentationLink = docs.WebLink,
                                DocumentationFilePath = docs.ResourceFilePath,
                            };
                        });

                        OnMessageReported?.Invoke(filePath, messages);
                    }
                }
                catch (OperationCanceledException)
                {
                    // dummy
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new GeneralErrorArgs(e));
                }
                finally
                {
                    mutex.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // dummy
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, new GeneralErrorArgs(e));
            }
        }
    }
}
