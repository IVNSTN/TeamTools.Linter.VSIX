using NUnit.Framework;
using TeamTools.Common.Linting.Infrastructure;

namespace TeamTools.VisualStudio.SqlExtensionTests
{
    [Category("Linter.VSExtension")]
    public class RoutinesTests
    {
        [Test]
        public void TestGitRelativePathBuilder()
        {
            string relativePath = GitCommandFactory.GetFolderRelativePathToGitRoot(@"c:\git\reports", @"c:\git\reports\577-П");
            Assert.That(relativePath, Is.EqualTo("577-П"));

            relativePath = GitCommandFactory.GetFolderRelativePathToGitRoot(@"c:\git\db\", @"c:\git\db");
            Assert.That(relativePath, Is.EqualTo(""));
        }
    }
}
