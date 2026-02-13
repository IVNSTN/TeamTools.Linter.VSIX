using System.Diagnostics.CodeAnalysis;

namespace TeamTools.TSQL.Assistant
{
    public enum TwoWordInstructionType
    {
        /// <summary>
        /// Represents code fix for join expression spelling
        /// e.g.
        /// * JOIN -> INNER JOIN
        /// * LEFT OUTER JOIN -> LEFT JOIN
        /// </summary>
        Join,

        /// <summary>
        /// Represents code fix for transaction statement spelling
        /// e.g. COMMIT -> COMMIT TRANSACTION
        /// </summary>
        Tran,
    }

    [ExcludeFromCodeCoverage]
    public class TwoWordInstructionCodeFix
    {
        public TwoWordInstructionCodeFix(int startLine, int startCol, int endLine, int endCol, string fixedText)
        {
            StartLine = startLine;
            StartCol = startCol;
            EndLine = endLine;
            EndCol = endCol;
            FixedText = fixedText;
        }

        public int StartLine { get; }

        public int StartCol { get; }

        public int EndLine { get; }

        public int EndCol { get; }

        public string FixedText { get; }
    }
}
