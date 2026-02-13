using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Adornments;
using TeamTools.Common.Linting;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    public static class SeverityConverter
    {
        public static string ConvertToErrorType(Severity severity)
        {
            return severity switch
            {
                Severity.Warning => PredefinedErrorTypeNames.Warning,
                Severity.Info => PredefinedErrorTypeNames.HintedSuggestion,
                _ => PredefinedErrorTypeNames.CompilerError,
            };
        }

        public static __VSERRORCATEGORY ConvertToErrorCategory(Severity severity)
        {
            return severity switch
            {
                Severity.Warning => __VSERRORCATEGORY.EC_WARNING,
                Severity.Info => __VSERRORCATEGORY.EC_MESSAGE,
                _ => __VSERRORCATEGORY.EC_ERROR,
            };
        }

        public static ImageMoniker ConvertToTagIcon(Severity severity)
        {
            return severity switch
            {
                Severity.Warning => KnownMonikers.StatusWarning,
                Severity.Info => KnownMonikers.StatusInformation,
                _ => KnownMonikers.StatusError,
            };
        }
    }
}
