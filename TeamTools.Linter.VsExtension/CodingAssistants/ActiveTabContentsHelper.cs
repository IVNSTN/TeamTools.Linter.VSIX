using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Diagnostics.CodeAnalysis;

namespace TeamTools.VisualStudio.SqlExtension.CodingAssistants
{
    [ExcludeFromCodeCoverage]
    internal static class ActiveTabContentsHelper
    {
        public static ITextView GetTextViewOfActiveDocument(IVSIdeAdapter ide)
        {
            var componentModel = (IComponentModel)ide.GetGlobalService(typeof(SComponentModel));
            var textManager = (IVsTextManager)ide.GetGlobalService(typeof(SVsTextManager));
            ErrorHandler.ThrowOnFailure(textManager.GetActiveView(0, null, out IVsTextView activeView));
            var editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var textView = editorAdapter.GetWpfTextView(activeView);

            if (textView.TextBuffer.ContentType.TypeName.Equals("Sql Server tools", StringComparison.OrdinalIgnoreCase))
            {
                return textView;
            }
            else
            {
                return null;
            }
        }

        public static string GetText(IVSIdeAdapter ide)
        {
            var textView = GetTextViewOfActiveDocument(ide);
            if (textView == null)
            {
                return null;
            }

            return textView.TextSnapshot.GetText();
        }

        public static void SetText(ITextView textView, string text)
        {
            textView.TextBuffer.Replace(new Span(0, textView.TextSnapshot.Length), text);
        }
    }
}
