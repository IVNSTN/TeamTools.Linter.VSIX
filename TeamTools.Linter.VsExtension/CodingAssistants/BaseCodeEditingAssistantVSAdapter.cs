using Microsoft.VisualStudio.Text.Editor;
using System;
using TeamTools.Common.Linting;
using TeamTools.TSQL.Assistant;

namespace TeamTools.VisualStudio.SqlExtension.CodingAssistants
{
    internal abstract class BaseCodeEditingAssistantVSAdapter
    {
        public BaseCodeEditingAssistantVSAdapter(ITextOutputPort outputPort, IVSIdeAdapter ideAdapter)
        {
            OutputPort = outputPort;
            IdeAdapter = ideAdapter;
        }

        protected ITextOutputPort OutputPort { get; }

        protected IVSIdeAdapter IdeAdapter { get; }

        public abstract void ApplyAdjustments();

        protected void RunAssistant(Func<string, BaseCodeEditingAssistant> assistantFactory)
        {
            var context = GetActiveTabContents();
            if (context is null || string.IsNullOrWhiteSpace(context.Text))
            {
                return;
            }

            var assistant = assistantFactory(context.Text);
            if (assistant is null || !assistant.Parse())
            {
                return;
            }

            if (!assistant.HasChanges)
            {
                return;
            }

            ApplyChanges(context, assistant.ModifiedCode);
        }

        protected virtual void ApplyChanges(ContentsInfo context, string modifiedCode)
        {
            ActiveTabContentsHelper.SetText(context.View, modifiedCode);
        }

        protected virtual ContentsInfo GetActiveTabContents()
        {
            var textView = ActiveTabContentsHelper.GetTextViewOfActiveDocument(IdeAdapter);
            if (textView is null)
            {
                return default;
            }

            return new ContentsInfo
            {
                View = textView,
                Text = textView.TextSnapshot.GetText(),
            };
        }

        protected class ContentsInfo
        {
            public ITextView View { get; set; }

            public string Text { get; set; }
        }
    }
}
