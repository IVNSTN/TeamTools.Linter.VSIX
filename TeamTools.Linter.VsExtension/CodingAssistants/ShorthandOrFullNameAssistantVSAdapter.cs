using TeamTools.Common.Linting;
using TeamTools.TSQL.Assistant;

namespace TeamTools.VisualStudio.SqlExtension.CodingAssistants
{
    internal class ShorthandOrFullNameAssistantVSAdapter : BaseCodeEditingAssistantVSAdapter
    {
        public ShorthandOrFullNameAssistantVSAdapter(ITextOutputPort outputPort, IVSIdeAdapter ideAdapter) : base(outputPort, ideAdapter)
        {
        }

        public override void ApplyAdjustments()
        {
            RunAssistant(code => new ShorthandOrFullNameAssistant(OutputPort, code));
        }
    }
}
