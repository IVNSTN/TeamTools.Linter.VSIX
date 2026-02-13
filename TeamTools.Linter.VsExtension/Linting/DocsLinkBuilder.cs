using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TeamTools.VisualStudio.SqlExtension.Tagging;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    public class DocsLinkBuilder
    {
        // TODO : nope, take from plugin config
        private static readonly string RelativeBaseDocsPath = @"Resources\Plugins\TSQLLinter\Resources\Docs";
        private readonly string baseDocsUri;
        private readonly string fallbackLanguage;
        private readonly string vsixRootPath;

        public DocsLinkBuilder(string baseDocsUri, string fallbackLanguage)
        {
            this.baseDocsUri = baseDocsUri;
            this.fallbackLanguage = fallbackLanguage;

            vsixRootPath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public DocsLink Build(string ruleId)
        {
            string url = default;

            string currentLang = CultureInfo.CurrentUICulture.Name.ToLowerInvariant();
            string ruleLang = DetectAvailableRuleDocLang(ruleId, currentLang, out string path);

            if (!string.IsNullOrEmpty(baseDocsUri) && !string.IsNullOrEmpty(ruleLang))
            {
                url = MakeDocsAbsoluteUrl(ruleLang, ruleId);
            }

            return new DocsLink
            {
                WebLink = url,
                ResourceFilePath = path,
            };
        }

        private string DetectAvailableRuleDocLang(string ruleId, string lang, out string ruleDocsPath)
        {
            // Trying to locate in current UI languages
            ruleDocsPath = MakeDocsAbsoluteFilePath(lang, ruleId);

            if (File.Exists(ruleDocsPath))
            {
                return lang;
            }

            // If not found in that language - trying to locate in default (fallback) language
            ruleDocsPath = MakeDocsAbsoluteFilePath(fallbackLanguage, ruleId);

            return File.Exists(ruleDocsPath) ? fallbackLanguage : default;
        }

        // TODO : nope, it should path within specific plugin folder
        // taken from extension's DefaultConfig.json
        private string MakeDocsAbsoluteFilePath(string lang, string ruleId)
        {
            return Path.Combine(vsixRootPath, RelativeBaseDocsPath, lang, MakeDocsFileName(ruleId));
        }

        private string MakeDocsAbsoluteUrl(string lang, string ruleId)
        {
            return new Uri(baseDocsUri).Append(lang).Append(MakeDocsFileName(ruleId)).AbsoluteUri;
        }

        private string MakeDocsFileName(string ruleId)
        {
            return $"{ruleId}.md";
        }

        public sealed class DocsLink
        {
            public string WebLink { get; set; }

            public string ResourceFilePath { get; set; }
        }
    }
}
