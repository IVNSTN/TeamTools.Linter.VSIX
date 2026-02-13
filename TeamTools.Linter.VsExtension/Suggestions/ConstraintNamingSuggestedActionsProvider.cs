using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace TeamTools.VisualStudio.SqlExtension.Suggestions
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Test Suggested Actions")]
    [ContentType("SQL Server Tools")]
    internal class ConstraintNamingSuggestedActionsProvider : BaseSuggestedActionsSourceProvider, ISuggestedActionsSourceProvider
    {
        protected override ISuggestedActionsSource MakeSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return null; // TODO : finish
            // return new ConstraintNamingSuggestedActionsSource(this, textView, textBuffer);
        }
    }
}
