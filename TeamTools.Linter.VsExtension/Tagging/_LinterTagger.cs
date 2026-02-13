using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TeamTools.VisualStudio.SqlExtension.Linting;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    internal class LinterTagger : ITagger<IErrorTag>, IDisposable
    {
        private readonly ITextBuffer buffer;
        private readonly ITextDocument document;
        private readonly TaggerProvider provider;

        private ITextSnapshot currentSnapshot;
        private NormalizedSnapshotSpanCollection dirtySpans;
        private CancellationTokenSource source;

        internal LinterTagger(
            TaggerProvider provider,
            ITextBuffer buffer,
            ITextDocument document)
        {
            this.provider = provider;
            this.buffer = buffer;
            this.document = document;

            currentSnapshot = buffer.CurrentSnapshot;
            dirtySpans = new NormalizedSnapshotSpanCollection();

            FilePath = document.FilePath;
            Factory = new LinterSnapshotFactory(new LinterSnapshot(FilePath, 0, new List<LinterMessageMarker>()));

            document.FileActionOccurred += OnFileActionOccurred;
            // TODO : buffer.PostChanged?
            buffer.ChangedLowPriority += OnBufferChange;

            InitializeAsync();
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public event EventHandler<GeneralErrorArgs> OnError;

        internal ITextSnapshot CurrentSnapshot => currentSnapshot;

        internal LinterSnapshotFactory Factory { get; }

        internal string FilePath { get; private set; }

        internal LinterSnapshot Snapshot { get; set; }

        public void Dispose()
        {
            document.FileActionOccurred -= OnFileActionOccurred;
            buffer.ChangedLowPriority -= OnBufferChange;

            provider.RemoveTagger(this);
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // TODO: smth went wrong. IntersectsWith throws exception when the snapshot versions are different
            if (0 == spans.Count || null == Snapshot || false == Snapshot.Markers.Any())
            {
                return Enumerable.Empty<ITagSpan<IErrorTag>>();
            }

            var version = spans[0].Snapshot.Version;

            try
            {
                // FIXME : collection refers to different snapshots
                return Snapshot.Markers
                    .Where(marker => version == marker.Span.Snapshot.Version && spans.IntersectsWith(marker.Span))
                    .Select(marker =>
                    {
                        var tg = new LinterTag(marker.Message, HandleError);
                        return new TagSpan<IErrorTag>(marker.Span, tg);
                    });
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, new GeneralErrorArgs(e, "Failed making tags"));
            }

            return Enumerable.Empty<ITagSpan<IErrorTag>>();
        }

        internal void UpdateMessages(IEnumerable<LinterMessage> messages)
        {
            var oldSnapshot = Factory.CurrentSnapshot;

            var markers = ProcessMessages(messages)
                .Where(m => m.Range != null)
                .Select(CreateMarker);
            var newSnapshot = new LinterSnapshot(FilePath, oldSnapshot.VersionNumber + 1, markers);

            SnapToNewSnapshot(newSnapshot);
        }

        private static LinterMessageRange GetRange(SnapshotPoint start, SnapshotPoint end, int line)
        {
            if (start > end)
            {
                throw new ArgumentOutOfRangeException($"start ({start.Position}) greater than end ({end.Position}) for line {line}");
            }

            return new LinterMessageRange
            {
                Start = start,
                End = end,
            };
        }

        private async Task AnalyzeAsync(string filePath)
        {
            Cancel();

            // TODO: magic 10, take it out somehow
            source = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await provider.AnalyzeAsync(filePath, source.Token)
                .ConfigureAwait(false);
        }

        private void Cancel()
        {
            try
            {
                source?.Cancel();
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, new GeneralErrorArgs(e, "Failed cancelling tagging task"));
            }
            finally
            {
                source?.Dispose();
                source = null;
            }
        }

        private LinterMessageMarker CreateMarker(LinterMessage message)
        {
            var start = new SnapshotPoint(currentSnapshot, message.Range.Start);
            var end = new SnapshotPoint(currentSnapshot, message.Range.End);

            return new LinterMessageMarker(message, new SnapshotSpan(start, end));
        }

        private async Task InitializeAsync()
        {
            await provider.AddTaggerAsync(this, () => AnalyzeAsync(FilePath));
        }

        private void OnBufferChange(object sender, TextContentChangedEventArgs e)
        {
            Cancel();

            UpdateDirtySpans(e);

            var newSnapshot = TranslateWarningSpans();

            SnapToNewSnapshot(newSnapshot);
        }

        private async void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            switch (e.FileActionType)
            {
                case FileActionTypes.ContentSavedToDisk:
                case FileActionTypes.ContentLoadedFromDisk:
                    await AnalyzeAsync(FilePath).ConfigureAwait(false);
                    break;

                case FileActionTypes.DocumentRenamed:
                    Rename(FilePath, e.FilePath);
                    break;

                default:
                    // TODO: process unrecognized action
                    break;
            }
        }

        private IEnumerable<LinterMessage> ProcessMessages(IEnumerable<LinterMessage> messages)
        {
            // TODO: the reporter's job must involve some code from here.
            foreach (var message in messages)
            {
                var lineCount = currentSnapshot.LineCount;
                var lineNumber = message.StartLine > 0 ? message.StartLine - 1 : message.StartLine;
                var colNumber = message.StartColumn > 0 ? message.StartColumn - 1 : message.StartColumn;

                if (lineNumber > lineCount)
                {
                    // FIXME : all other messages must be handled respectfully
                    throw new ArgumentOutOfRangeException($"line ({lineNumber}) greater than line count ({lineCount})");
                }

                var startLine = currentSnapshot.GetLineFromLineNumber(lineNumber);
                var startPosition = startLine.Start.Add(colNumber);

                if (message.Length.HasValue)
                {
                    var endPosition = startPosition.Add(message.Length.Value);

                    message.Range = GetRange(startPosition, endPosition, lineNumber);
                }
                else if (message.EndLine.HasValue && message.EndColumn.HasValue)
                {
                    var endLine = currentSnapshot.GetLineFromLineNumber(message.EndLine.Value - 1);
                    var endPosition = endLine.Start.Add(message.EndColumn.Value - 1);

                    message.Range = GetRange(startPosition, endPosition, message.EndLine.Value - 1);
                }
                else
                {
                    var lineText = startLine.GetText();
                    var end = startLine.End;

                    if (!string.IsNullOrWhiteSpace(lineText) && colNumber > 0)
                    {
                        var match = Regex.Match(lineText.Substring(colNumber), "^[\t ]*$|[^\\s\\/\\\\\\(\\)\"\':,\\.;<>~!@#\\$%\\^&\\*\\|\\+=\\[\\]\\{\\}`\\?\\-…]+");
                        if (match.Success)
                        {
                            end = startPosition.Add(match.Index).Add(match.Length);
                        }
                    }

                    message.Range = GetRange(startPosition, end, lineNumber);
                }

                yield return message;
            }
        }

        private void Rename(string oldPath, string newPath)
        {
            provider.Rename(oldPath, newPath);

            FilePath = newPath;
        }

        private void SnapToNewSnapshot(LinterSnapshot snapshot)
        {
            var factory = Factory;

            factory.UpdateMarkers(snapshot);

            provider.UpdateAllSinks(factory);

            UpdateMarkers(currentSnapshot, snapshot);

            Snapshot = snapshot;
        }

        private LinterSnapshot TranslateWarningSpans()
        {
            var oldSnapshot = Factory.CurrentSnapshot;

            var newWarnings = oldSnapshot.Markers
                .Select(marker => marker.CloneAndTranslateTo(currentSnapshot))
                .Where(clone => null != clone);

            return new LinterSnapshot(FilePath, oldSnapshot.VersionNumber + 1, newWarnings);
        }

        // TODO: check next two methods to fix ONSAVE
        private void UpdateDirtySpans(TextContentChangedEventArgs e)
        {
            currentSnapshot = e.After;

            var newDirtySpans = dirtySpans.CloneAndTrackTo(e.After, SpanTrackingMode.EdgeInclusive);

            newDirtySpans = e.Changes
                .Aggregate(newDirtySpans, (current, change) => NormalizedSnapshotSpanCollection.Union(
                    current, new NormalizedSnapshotSpanCollection(e.After, change.NewSpan)));

            dirtySpans = newDirtySpans;
        }

        private void UpdateMarkers(ITextSnapshot currentSnapshot, LinterSnapshot snapshot)
        {
            var oldSnapshot = Snapshot;

            var handler = TagsChanged;
            if (null == handler)
            {
                return;
            }

            var start = int.MaxValue;
            var end = int.MinValue;

            if (null != oldSnapshot && 0 < oldSnapshot.Count)
            {
                start = oldSnapshot.Markers
                    .Select(marker => marker.Span.Start.TranslateTo(currentSnapshot, PointTrackingMode.Negative))
                    .Min();
                end = oldSnapshot.Markers
                    .Select(marker => marker.Span.End.TranslateTo(currentSnapshot, PointTrackingMode.Positive))
                    .Max();
            }

            if (0 < snapshot.Count)
            {
                start = Math.Min(start, snapshot.Markers.Select(marker => marker.Span.Start.Position)
                    .Min());
                end = Math.Max(end, snapshot.Markers.Select(marker => marker.Span.End.Position)
                    .Max());
            }

            if (start > end)
            {
                return;
            }

            handler(this, new SnapshotSpanEventArgs(new SnapshotSpan(
                currentSnapshot, Span.FromBounds(start, end))));
        }

        private void HandleError(object sender, RuleHandlingErrorArgs args)
        {
            OnError?.Invoke(this, new GeneralErrorArgs(args.Err, $"Rule {args.RuleID} error: {args.Descr}"));
        }
    }
}
