using TeamTools.Common.Linting;
using TeamTools.TSQL.Assistant;

namespace TeamTools.VisualStudio.SqlExtension.CodingAssistants
{
    internal class BeginEndAssistantVSAdapter : BaseCodeEditingAssistantVSAdapter
    {
        public BeginEndAssistantVSAdapter(ITextOutputPort outputPort, IVSIdeAdapter ideAdapter) : base(outputPort, ideAdapter)
        {
        }

        public override void ApplyAdjustments()
        {
            RunAssistant(code => new BeginEndAssistant(OutputPort, code));
        }
    }
}
