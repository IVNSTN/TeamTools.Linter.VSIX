using TeamTools.Common.Linting;
using TeamTools.TSQL.Assistant;

namespace TeamTools.VisualStudio.SqlExtension.CodingAssistants
{
    internal class FinalGoAssistantVSAdapter : BaseCodeEditingAssistantVSAdapter
    {
        public FinalGoAssistantVSAdapter(ITextOutputPort outputPort, IVSIdeAdapter ideAdapter) : base(outputPort, ideAdapter)
        {
        }

        public override void ApplyAdjustments()
        {
            RunAssistant(code => new FinalGoAssistant(OutputPort, code));
        }
    }
}
