using System;
using TeamTools.Common.Linting;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    // TODO: hm... I have a dislike for this class.
    public class LinterMessage
    {
        public int StartColumn { get; set; }

        public int? EndColumn { get; set; }

        public int StartLine { get; set; }

        public int? EndLine { get; set; }

        public int? Length { get; set; }

        public string RuleId { get; set; }

        public string Message { get; set; }

        public string Explanation { get; set; }

        public string Example { get; set; }

        public string DocumentationLink { get; set; }

        public string DocumentationFilePath { get; set; }

        public string Source { get; set; }

        public string FilePath { get; set; }

        public string ProjectName { get; set; }

        public Guid? ProjectGuid { get; set; }

        public LinterMessageRange Range { get; set; }

        public Severity MessageSeverity { get; set; }

        public string SeverityName => SeverityConverter.ConvertToString(MessageSeverity);

        public override string ToString()
            => $"{FilePath}({StartLine},{StartColumn}): {SeverityName} {RuleId} : {Message}";
    }
}
