using NUnit.Framework;
using TeamTools.TSQL.Assistant;

namespace TeamTools.TSQL.AssistantTests
{
    [Category("Assistant")]
    internal class FinalGoAssistantTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void FinalGoAssistantPutsGoToTheEndOfScript()
        {
            string originalText =
            @"-- comment";
            string fixedText = @"-- comment
GO
";

            var assistant = new FinalGoAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void FinalGoAssistantDoesNothingIfGoExists()
        {
            string originalText =
            @"
BEGIN
    PRINT '1'
END
GO";
            var assistant = new FinalGoAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.False, "has changes");
        }

        [Test]
        public void FinalGoAssistantDoesNothingIfScriptIsEmpty()
        {
            string originalText =
            @"
              
";
            var assistant = new FinalGoAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.False, "has changes");
        }
    }
}
