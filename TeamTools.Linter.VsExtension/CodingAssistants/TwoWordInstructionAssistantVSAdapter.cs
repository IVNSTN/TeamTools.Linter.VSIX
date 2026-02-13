using TeamTools.Common.Linting;
using TeamTools.TSQL.Assistant;

namespace TeamTools.VisualStudio.SqlExtension.CodingAssistants
{
    internal class TwoWordInstructionAssistantVSAdapter : BaseCodeEditingAssistantVSAdapter
    {
        public TwoWordInstructionAssistantVSAdapter(ITextOutputPort outputPort, IVSIdeAdapter ideAdapter) : base(outputPort, ideAdapter)
        {
        }

        public override void ApplyAdjustments()
        {
            RunAssistant(code => new TwoWordInstructionAssistant(OutputPort, code));
        }
    }
}
