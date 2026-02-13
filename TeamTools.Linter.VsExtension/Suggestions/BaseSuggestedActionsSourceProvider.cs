using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System.ComponentModel.Composition;

namespace TeamTools.VisualStudio.SqlExtension.Suggestions
{
    internal abstract class BaseSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import(typeof(ITextStructureNavigatorSelectorService))]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null || textView == null)
            {
                return null;
            }

            return MakeSuggestedActionsSource(textView, textBuffer);
        }

        protected abstract ISuggestedActionsSource MakeSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer);
    }
}
