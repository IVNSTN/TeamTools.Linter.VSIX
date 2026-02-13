using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TeamTools.VisualStudio.SqlExtension.Suggestions
{
    internal abstract class BaseSuggestedActionsSource : ISuggestedActionsSource
    {
        private const string SuggestedActionsCategory = "Linter suggestions";
        private readonly BaseSuggestedActionsSourceProvider factory;
        private readonly ITextBuffer textBuffer;
        private readonly ITextView textView;

        public BaseSuggestedActionsSource(BaseSuggestedActionsSourceProvider actionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            this.factory = actionsSourceProvider;
            this.textBuffer = textBuffer;
            this.textView = textView;
        }

#pragma warning disable CS0067
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore CS0067

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            if (TryGetWordUnderCaret(out TextExtent extent) && extent.IsSignificant)
            {
                ITrackingSpan trackingSpan = range.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
                return new SuggestedActionSet[]
                {
                    new SuggestedActionSet(
                        SuggestedActionsCategory,
                        ActionFactoryMethod(trackingSpan),
                        null,
                        SuggestedActionSetPriority.Medium,
                        extent.Span),
                };
            }

            return Enumerable.Empty<SuggestedActionSet>();
        }

        public virtual Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
#pragma warning disable VSTHRD105
            return Task.Factory.StartNew(
                () =>
                {
                    if (TryGetWordUnderCaret(out TextExtent extent))
                    {
                        // don't display the action if the extent has whitespace
                        return extent.IsSignificant;
                    }

                    return false;
                });
#pragma warning restore VSTHRD105
        }

        protected abstract IEnumerable<ISuggestedAction> ActionFactoryMethod(ITrackingSpan trackingSpan);

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            ITextCaret caret = textView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default;
                return false;
            }

            ITextStructureNavigator navigator = factory.NavigatorService.GetTextStructureNavigator(textBuffer);

            wordExtent = navigator.GetExtentOfWord(point);
            return true;
        }
    }
}
