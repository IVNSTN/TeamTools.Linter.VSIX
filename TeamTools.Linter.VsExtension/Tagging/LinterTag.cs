using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using TeamTools.VisualStudio.SqlExtension.Linting;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    internal class LinterTag : IErrorTag
    {
        internal LinterTag(LinterMessage message, EventHandler<RuleHandlingErrorArgs> errorHandler)
        {
            if (errorHandler != null)
            {
                OnError += errorHandler;
            }

            ErrorType = SeverityConverter.ConvertToErrorType(message.MessageSeverity);
            ToolTipContent = GetToolTipContent(message);
        }

        public event EventHandler<RuleHandlingErrorArgs> OnError;

        public string ErrorType { get; }

        public object ToolTipContent { get; }

        public object GetToolTipContent(LinterMessage message)
        {
            string descr = $"{message.RuleId}: {message.Message}";
            Lazy<string> descrFromFile = default;
            // TODO : load and show some additional tips and hints as detailed info
            string[] details = null;

            if (!string.IsNullOrEmpty(message.DocumentationFilePath) && File.Exists(message.DocumentationFilePath))
            {
                try
                {
                    descrFromFile = new Lazy<string>(() => GetRuleDescr(message.DocumentationFilePath));
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new RuleHandlingErrorArgs(e, message.RuleId, "Failed reading rule descr"));
                }
            }
            else
            {
                return descr;
            }

            var uc = new RuleTooltipControl();
            uc.DataContext = new
            {
                icon = SeverityConverter.ConvertToTagIcon(message.MessageSeverity),
                ruleId = message.RuleId,
                message = message.Message,
                explanation = descrFromFile?.Value ?? descr,
                takeAttention = details,
                takeAttentionVisibility = details is null ? Visibility.Hidden : Visibility.Visible,
                link = message.DocumentationLink,
                docsLinkVisibility = string.IsNullOrEmpty(message.DocumentationLink) ? Visibility.Collapsed : Visibility.Visible,
            };

            return uc;
        }

        private static string GetRuleDescr(string filePath)
        {
            using TextReader file = new StreamReader(filePath);
            string descr = file.ReadToEnd();

            // TODO : maybe analyze markdown and extract more info, get rid of semi-html markup
            var m = Regex.Match(descr, "<p\\s+id=\"descr\">(.*?)<\\/p>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
            if (m.Success)
            {
                descr = m.Groups[1].Value.Trim(Environment.NewLine.ToCharArray());
            }

            return descr;
        }
    }
}
