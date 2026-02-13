using NUnit.Framework;
using NUnit.Framework.Internal;
using TeamTools.TSQL.Assistant;

namespace TeamTools.TSQL.AssistantTests
{
    [Category("Assistant")]
    public class BeginEndAssistantTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestBeginEndAssistantPutsBeginEndWhenNeeded()
        {
            string originalText =
            @"
CREATE TRIGGER bs.tr_Objects_D
ON bs.Objects
FOR DELETE
AS
/*
global comment
*/
IF @@ROWCOUNT = 0 /* comment */RETURN;

SET NOCOUNT ON;

SELECT 1; INSERT INTO bs.Objects_D (IdObject) SELECT IdObject FROM deleted;
WHILE 1=1 BREAK
-- comment
GO";
            string fixedText = @"
CREATE TRIGGER bs.tr_Objects_D
ON bs.Objects
FOR DELETE
AS
/*
global comment
*/
BEGIN
IF @@ROWCOUNT = 0 /* comment */
BEGIN
RETURN;
END

SET NOCOUNT ON;

SELECT 1; INSERT INTO bs.Objects_D (IdObject) SELECT IdObject FROM deleted;
WHILE 1=1
BEGIN
 BREAK
END
-- comment
END
GO";

            var assistant = new BeginEndAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void TestBeginEndAssistantDoesNothingIfAllGood()
        {
            string originalText = @"
            CREATE TRIGGER bs.tr_Objects_D
            ON bs.Objects
            FOR DELETE
            AS
            begin
                IF @@ROWCOUNT = 0
                BEGIN
                    RETURN;
                END

                SET NOCOUNT ON;

                INSERT INTO bs.Objects_D (IdObject) SELECT IdObject FROM deleted;

                WHILE 1=1
                BEGIn
                    BREAK
                ENd
                -- comment
            end;
            GO";

            var assistant = new BeginEndAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.False, "no changes");
            Assert.That(assistant.ModifiedCode, Is.Null);
        }
    }
}
