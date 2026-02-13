namespace TeamTools.TSQL.Assistant
{
    public class ShorthandOrFullnameCodeFix
    {
        public ShorthandOrFullnameCodeFix(int startLine, int startCol, int endLine, int endCol, string fixedText)
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
