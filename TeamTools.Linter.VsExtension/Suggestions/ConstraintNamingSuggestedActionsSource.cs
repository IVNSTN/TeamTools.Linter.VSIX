using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;

namespace TeamTools.VisualStudio.SqlExtension.Suggestions
{
    internal class ConstraintNamingSuggestedActionsSource : BaseSuggestedActionsSource
    {
        public ConstraintNamingSuggestedActionsSource(
            ConstraintNamingSuggestedActionsProvider actionsSourceProvider,
            ITextView textView,
            ITextBuffer textBuffer) : base(actionsSourceProvider, textView, textBuffer)
        {
        }

        protected override IEnumerable<ISuggestedAction> ActionFactoryMethod(ITrackingSpan trackingSpan)
        {
            yield return new ConstraintNamingFixAction(trackingSpan);
        }
    }
}
