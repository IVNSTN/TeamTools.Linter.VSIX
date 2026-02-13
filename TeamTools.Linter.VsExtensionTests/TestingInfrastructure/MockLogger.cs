using System.Collections.Generic;
using TeamTools.Common.Linting;

namespace TeamTools.VisualStudio.SqlExtensionTests.TestingInfrastructure
{
    public class MockLogger : ITextOutputPort
    {
        public List<string> Log { get; } = new List<string>();

        public void WriteLine(string message)
        {
            Log.Add(message);
        }

        public void Reset()
        {
            Log.Clear();
        }
    }
}
