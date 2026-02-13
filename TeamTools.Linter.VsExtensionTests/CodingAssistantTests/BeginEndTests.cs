using NUnit.Framework;
using TeamTools.Common.Linting;
using TeamTools.VisualStudio.SqlExtension.CodingAssistants;
using TeamTools.VisualStudio.SqlExtensionTests.TestingInfrastructure;

namespace TeamTools.VisualStudio.SqlExtensionTests.CodingAssistantTests
{
    [Category("Linter.VSExtension")]
    public sealed class BeginEndTests
    {
        [Test]
        public void TestBeginEndAssistantModifiesCodeAsExpected()
        {
            var reporter = new MockLogger();
            var assistant = new MockBeginEndAssistantVSAdapter(reporter);
            assistant.LoadOriginalText("IF @a = @b PRINT 1");
            assistant.ApplyAdjustments();

            Assert.That(reporter.Log, Has.Count.EqualTo(1));
            Assert.That(reporter.Log[0].Replace("\r\n", " "), Is.EqualTo("IF @a = @b BEGIN  PRINT 1 END"));
        }

        private class MockBeginEndAssistantVSAdapter : BeginEndAssistantVSAdapter
        {
            private string originalCode = "";

            public MockBeginEndAssistantVSAdapter(ITextOutputPort outputPort) : base(outputPort, null)
            {
            }

            public void LoadOriginalText(string originalCode)
            {
                this.originalCode = originalCode;
            }

            protected override void ApplyChanges(ContentsInfo context, string modifiedCode)
            {
                OutputPort.WriteLine(modifiedCode);
            }

            protected override ContentsInfo GetActiveTabContents()
            {
                return new ContentsInfo { Text = originalCode, View = default };
            }
        }
    }
}
