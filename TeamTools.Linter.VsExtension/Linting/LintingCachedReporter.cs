using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using TeamTools.Common.Linting;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    internal class LintingCachedReporter : IReporter
    {
        private const string MessageTemplate = "{0}({1},{2}): {5} {3} - {4}";
        private const string TotalsTemplate = "Errors: {0}";
        private readonly IList<RuleViolation> violations = new List<RuleViolation>();

        public int ViolationCount => Violations.Count;

        public IReadOnlyCollection<RuleViolation> Violations => new ReadOnlyCollection<RuleViolation>(violations);

        public void ReportViolation(RuleViolation violation)
        {
            if (violation is null)
            {
                Debug.WriteLine("unexpected NULL violation caught");
                return;
            }

            violations.Add(violation);
        }

        public void Report(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                Debug.WriteLine("unexpected NULL violation caught");
                return;
            }

            violations.Add(new RuleViolation { Text = msg, RuleId = "Failure" });
        }

        public void ReportFailure(string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                Debug.WriteLine("unexpected NULL violation caught");
                return;
            }

            violations.Add(new RuleViolation { Text = error, RuleId = "Failure" });
        }

        public void Dump(ITextOutputPort outputPort, bool withTotals = false)
        {
            // TODO : async?
            var sortedViolations = Violations
                .Where(v => v != null) // crutch for some strange situations
                .OrderBy(v => v.FileName)
                .ThenBy(v => v.Line)
                .ThenBy(v => v.Column)
                .ThenBy(v => v.RuleId)
                .ThenBy(v => v.Text);

            foreach (var violation in sortedViolations)
            {
                if (string.IsNullOrEmpty(violation.FileName))
                {
                    outputPort.WriteLine(violation.Text);
                }
                else
                {
                    outputPort.WriteLine(string.Format(
                        MessageTemplate,
                        violation.FileName,
                        violation.Line.ToString(),
                        violation.Column.ToString(),
                        violation.RuleId,
                        violation.Text,
                        violation.SeverityName));
                }
            }

            if (withTotals)
            {
                outputPort.WriteLine(string.Format(TotalsTemplate, Violations.Count.ToString()));
            }
        }

        public void Reset() => violations.Clear();
    }
}
