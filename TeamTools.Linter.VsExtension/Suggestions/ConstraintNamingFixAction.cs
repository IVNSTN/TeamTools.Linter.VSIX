using Microsoft.VisualStudio.Text;

namespace TeamTools.VisualStudio.SqlExtension.Suggestions
{
    internal class ConstraintNamingFixAction : BaseSuggestedAction
    {
        private const string DisplayTextTemplate = "Bring constraint '{0}' to right notation";

        public ConstraintNamingFixAction(ITrackingSpan span) : base(span, DisplayTextTemplate)
        {
        }

        protected override string ModifyText(string originalText)
        {
            return "asfdasdasfd"; // TODO : tbd
        }
    }
}
