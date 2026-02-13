using System.Collections.Generic;
using TeamTools.Common.Linting;

namespace TeamTools.VisualStudio.SqlExtensionTests.TestingInfrastructure
{
    public class MockVcs : IVcsAccessor
    {
        private readonly List<string> files = new List<string>();

        public MockVcs(params string[] files)
        {
            this.files.AddRange(files);
        }

        public int GetModifiedFilesCallCount { get; private set; } = 0;

        public IEnumerable<string> GetModifiedFiles(string folder, string mainBranch)
        {
            GetModifiedFilesCallCount++;
            return files;
        }
    }
}
