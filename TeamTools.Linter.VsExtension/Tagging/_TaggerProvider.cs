using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

using TeamTools.VisualStudio.SqlExtension.Linting;

using Task = System.Threading.Tasks.Task;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    [Export(typeof(IViewTaggerProvider))]
    [Export(typeof(ILinterTaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType("text")]
    [ContentType("projection")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    public sealed class TaggerProvider : IViewTaggerProvider, ITableDataSource, ILinterTaggerProvider, IDisposable
    {
        private readonly List<SinkManager> managers = new List<SinkManager>();
        private readonly TaggerManager taggers = new TaggerManager();
        private readonly ILinterService linter;
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private ITableManager tableManager;

        [ImportingConstructor]
        public TaggerProvider(
            [Import] ITableManagerProvider tableManagerProvider,
            [Import] ITextDocumentFactoryService textDocumentFactoryService,
            [Import] ILinterService linter)
        {
            tableManager = tableManagerProvider
                .GetTableManager(StandardTables.ErrorsTable);

            this.textDocumentFactoryService = textDocumentFactoryService;

            var columns = new[]
            {
                StandardTableColumnDefinitions.BuildTool,
                StandardTableColumnDefinitions.Column,
                StandardTableColumnDefinitions.DetailsExpander,
                StandardTableColumnDefinitions.DocumentName,
                StandardTableColumnDefinitions.ErrorCategory,
                StandardTableColumnDefinitions.ErrorCode,
                StandardTableColumnDefinitions.ErrorSeverity,
                StandardTableColumnDefinitions.ErrorSource,
                StandardTableColumnDefinitions.Line,
                StandardTableColumnDefinitions.Text,
            };

            tableManager.AddSource(this, columns);

            this.linter = (ILinterService)linter;
            if (this.linter != null)
            {
                this.linter.OnMessageReported += Accept;
            }
        }

        public event EventHandler<GeneralErrorArgs> OnError;

        // TODO : These names should come from config or some metadata
        public string DisplayName => Properties.Strings.LinterName;

        public string Identifier => Properties.Strings.LinterName.Replace(" ", "");

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        [Import]
        private IContentTypeRegistryService ContentTypeRegistry { get; set; }

        public void Accept(string filePath, IEnumerable<LinterMessage> messages)
        {
            try
            {
                UpdateMessages(filePath, messages);
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, new GeneralErrorArgs(e, "Failed preparing linting error messages for ErrorList"));
            }
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer)
            where T : ITag
        {
            if (buffer != textView.TextBuffer || typeof(IErrorTag) != typeof(T))
            {
                return null;
            }

            if (false == TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out var document))
            {
                return null;
            }

            var filePath = document.FilePath;
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

            // TODO: settings/options/list of supported file types from plugin
            if (extension != ".sql")
            {
                return null;
            }

            lock (taggers)
            {
                if (taggers.TryGetValue(filePath, out var tagger))
                {
                    if (tagger.CurrentSnapshot != buffer.CurrentSnapshot)
                    {
                        // TODO : Need something better. This crutch is for taggers generated
                        // on the fly if file was linted without tab opened. Simple solution
                        // for mismatching snapshots - just analyze again.
                        tagger.Dispose();
                    }
                    else
                    {
                        return tagger as ITagger<T>;
                    }
                }

                var tg = new LinterTagger(this, buffer, document);
                tg.OnError += HandleError;

                return tg as ITagger<T>;
            }
        }

        public void Dispose()
        {
            tableManager.RemoveSource(this);
            tableManager = null;

            if (linter != null)
            {
                linter.OnMessageReported -= Accept;
            }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            return new SinkManager(this, sink);
        }

        public void UpdateMessages(string filePath, IEnumerable<LinterMessage> messages)
        {
            lock (taggers)
            {
                if (false == taggers.TryGetValue(filePath, out var tagger))
                {
                    var doc = LoadTextDocument(filePath);
                    if (doc != null)
                    {
                        tagger = new LinterTagger(this, doc.TextBuffer, doc);
                        tagger.OnError += HandleError;
                    }
                    else
                    {
                        return;
                    }
                }

                tagger.UpdateMessages(messages);
            }
        }

        internal void AddSinkManager(SinkManager manager)
        {
            lock (managers)
            {
                managers.Add(manager);

                foreach (var tagger in taggers.Values)
                {
                    manager.AddFactory(tagger.Factory);
                }
            }
        }

        internal async Task AddTaggerAsync(LinterTagger tagger, Func<Task> callback)
        {
            lock (managers)
            {
                taggers.Add(tagger);

                foreach (var manager in managers)
                {
                    manager.AddFactory(tagger.Factory);
                }
            }

            await callback();
        }

        internal async Task AnalyzeAsync(string filePath, CancellationToken token)
        {
            lock (taggers)
            {
                if (false == taggers.Exists(filePath))
                {
                    return;
                }
            }

            await linter.InitializeAsync(token).ConfigureAwait(false);
            await linter.LintFileAsync(filePath, token).ConfigureAwait(false);
        }

        internal void RemoveSinkManager(SinkManager manager)
        {
            lock (managers)
            {
                managers.Remove(manager);
            }
        }

        internal void RemoveTagger(LinterTagger tagger)
        {
            lock (managers)
            {
                taggers.Remove(tagger);

                foreach (var manager in managers)
                {
                    manager.RemoveFactory(tagger.Factory);
                }
            }
        }

        internal void Rename(string oldPath, string newPath)
        {
            lock (taggers)
            {
                taggers.Rename(oldPath, newPath);
            }
        }

        internal void UpdateAllSinks(ITableEntriesSnapshotFactory factory)
        {
            lock (managers)
            {
                foreach (var manager in managers)
                {
                    manager.UpdateSink(factory);
                }
            }
        }

        private bool TryGetTextDocument(ITextBuffer buffer, out ITextDocument document)
        {
            return this.textDocumentFactoryService.TryGetTextDocument(buffer, out document);
        }

        private ITextDocument LoadTextDocument(string filePath)
        {
            return textDocumentFactoryService.CreateAndLoadTextDocument(filePath, ContentTypeRegistry.GetContentType("SQL Server Tools"));
        }

        private void HandleError(object sender, GeneralErrorArgs args)
        {
            OnError?.Invoke(sender, args);
        }
    }
}
