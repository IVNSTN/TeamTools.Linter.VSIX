using System;
using System.Collections.Generic;
using TeamTools.Common.Linting;

namespace TeamTools.VisualStudio.SqlExtensionTests.TestingInfrastructure
{
    public class MockReporter : IReporter
    {
        private readonly ICollection<string> calledMethods = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> events = new List<string>();

        public ICollection<string> CalledMethods => calledMethods;

        public List<string> Events => events;

        public void Report(string msg)
        {
            calledMethods.Add("Report");
            events.Add(msg);
        }

        public void ReportFailure(string error)
        {
            calledMethods.Add("ReportFailure");
            events.Add(error);
        }

        public void ReportViolation(RuleViolation violation)
        {
            calledMethods.Add("ReportViolation");
            events.Add(violation.Text);
        }

        public void Reset()
        {
            calledMethods.Clear();
            events.Clear();
        }
    }
}
