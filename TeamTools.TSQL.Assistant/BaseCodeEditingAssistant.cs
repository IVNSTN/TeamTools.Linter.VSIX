using TeamTools.Common.Linting;

namespace TeamTools.TSQL.Assistant
{
    public abstract class BaseCodeEditingAssistant
    {
        public BaseCodeEditingAssistant(ITextOutputPort outputPort, string code)
        {
            OriginalCode = code;
            OutputPort = outputPort;
        }

        public string ModifiedCode { get; protected set; }

        public bool HasChanges { get; protected set; }

        protected ITextOutputPort OutputPort { get; }

        protected string OriginalCode { get; }

        public abstract bool Parse();
    }
}
