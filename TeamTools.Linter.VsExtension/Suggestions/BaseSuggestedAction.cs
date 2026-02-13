using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TeamTools.VisualStudio.SqlExtension.Suggestions
{
    internal abstract class BaseSuggestedAction : ISuggestedAction
    {
        private readonly ITrackingSpan span;
        private readonly ITextSnapshot snapshot;
        private readonly string display;

        public BaseSuggestedAction(ITrackingSpan span, string displayTextTemplate)
        {
            this.span = span;
            this.snapshot = this.span.TextBuffer.CurrentSnapshot;
            string originalText = this.span.GetText(snapshot);
            this.ModifiedText = ModifyText(originalText);
            this.display = string.Format(displayTextTemplate, originalText, ModifiedText);
        }

        public string ModifiedText { get; }

        public bool HasActionSets { get { return false; } }

        public string DisplayText { get { return display; } }

        public ImageMoniker IconMoniker { get { return default; } }

        public string IconAutomationText { get { return null; } }

        public string InputGestureText { get { return null; } }

        public bool HasPreview { get { return true; } }

        public void Dispose()
        {
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock
            {
                Padding = new Thickness(5),
            };
            textBlock.Inlines.Add(new Run { Text = ModifiedText });
            return Task.FromResult<object>(textBlock);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            span.TextBuffer.Replace(span.GetSpan(snapshot), ModifiedText);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        protected abstract string ModifyText(string originalText);
    }
}
