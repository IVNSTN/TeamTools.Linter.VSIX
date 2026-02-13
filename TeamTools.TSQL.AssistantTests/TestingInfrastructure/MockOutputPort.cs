using System.Collections.Generic;
using TeamTools.Common.Linting;

namespace TeamTools.TSQL.AssistantTests
{
    public class MockOutputPort : ITextOutputPort
    {
        public List<string> Log { get; } = new List<string>();

        public void WriteLine(string text)
        {
            Log.Add(text);
        }

        public void Activate()
        {
            // dummy
        }
    }
}
