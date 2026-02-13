using Microsoft.VisualStudio.Text;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    internal class LinterMessageMarker
    {
        internal LinterMessageMarker(LinterMessage message, SnapshotSpan span)
        {
            this.Message = message;
            this.Span = span;
        }

        internal LinterMessage Message { get; }

        internal SnapshotSpan Span { get; }

        internal LinterMessageMarker Clone()
        {
            return new LinterMessageMarker(this.Message, this.Span);
        }

        internal LinterMessageMarker CloneAndTranslateTo(ITextSnapshot newSnapshot)
        {
            var newSpan = this.Span.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive);

            return this.Span.Length == newSpan.Length
                ? new LinterMessageMarker(this.Message, newSpan)
                : null;
        }
    }
}
