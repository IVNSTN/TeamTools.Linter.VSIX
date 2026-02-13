namespace TeamTools.TSQL.Assistant
{
    public enum BeginOrEnd
    {
        /// <summary>
        /// Block opening `BEGIN` keyword.
        /// </summary>
        BeginWord,

        /// <summary>
        /// Block closing `END` keyword.
        /// </summary>
        EndWord,
    }

    public class BeginEndCodeFix
    {
        public BeginEndCodeFix(int line, int col, BeginOrEnd element)
        {
            Line = line;
            Col = col;
            Element = element;
        }

        public int Line { get; }

        public int Col { get; }

        public BeginOrEnd Element { get; }
    }
}
