using NUnit.Framework;
using TeamTools.TSQL.Assistant;

namespace TeamTools.TSQL.AssistantTests
{
    [Category("Assistant")]
    public class TwoWordInstructionAsisstantTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestTwoWordInstructionAsisstantFixesTranStatements()
        {
            string originalText = @"
BEGIN /* my */ TRAN
COMMIT;
delete dbo.tbl; /* comment */ ROLLBACK -- comment
TRAN /* asdf */
adsf;";
            string fixedText = @"
BEGIN /* my */ TRANSACTION
COMMIT TRANSACTION;
delete dbo.tbl; /* comment */ ROLLBACK -- comment
TRANSACTION /* asdf */
adsf;";

            var assistant = new TwoWordInstructionAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void TestTwoWordInstructionAsisstantFixesCommitAsLastWord()
        {
            string originalText = @"COMMIT";
            string fixedText = @"COMMIT TRANSACTION";

            var assistant = new TwoWordInstructionAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void TestTwoWordInstructionAsisstantDoesNothingIfAllTranStatementsAreGood()
        {
            string originalText = @"
BEGIN /* my */ TRANSACTION;
COMMIT TRANSACTION adsf;
ROLLBACK
TRANSACTION
adsf;";

            var assistant = new TwoWordInstructionAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.False, "no changes");
            Assert.That(assistant.ModifiedCode, Is.Null);
        }

        [Test]
        public void TestTwoWordInstructionAsisstantFixesJoins()
        {
            string originalText = @"
SELECT *
FROM dbo.t1
JOIN dbo.t2
ON a = b
    lefT /*comment*/OUTER JOIN dbo.t3
on c=d
/* comment */ full OUTER HASH
--comment
join   -- comment
dbo.t4 on e=d right OUTER LOOP join dbo.t5 on f = g cross join
dbo.t6
;";
            string fixedText = @"
SELECT *
FROM dbo.t1
INNER JOIN dbo.t2
ON a = b
    lefT /*comment*/ JOIN dbo.t3
on c=d
/* comment */ full  HASH
--comment
join   -- comment
dbo.t4 on e=d right  LOOP join dbo.t5 on f = g cross join
dbo.t6
;";

            var assistant = new TwoWordInstructionAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void TestTwoWordInstructionAsisstantDoesNothingIfAllJoinsAreGood()
        {
            string originalText = @"
SELECT *
FROM dbo.t1
INNER JOIN dbo.t2
ON a = b
    lefT /*comment*/ JOIN dbo.t3
on c=d
full
--comment
join   -- comment
dbo.t4 on e=d right  LOOP join dbo.t5 on f = g cross join
dbo.t6
;";

            var assistant = new TwoWordInstructionAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.False, "no changes");
            Assert.That(assistant.ModifiedCode, Is.Null);
        }

        [Test]
        public void TestTwoWordInstructionAsisstantAddsWithToHints()
        {
            string originalText = @"
SELECT *
FROM dbo.[t1](NOLOCK)
INNER JOIN dbo.t2 as t2
(TABLOCKX)
ON a = b
CROSS JOIN dbo.t3";

            string fixedText = @"
SELECT *
FROM dbo.[t1] WITH (NOLOCK)
INNER JOIN dbo.t2 as t2 WITH 
(TABLOCKX)
ON a = b
CROSS JOIN dbo.t3";

            var assistant = new TwoWordInstructionAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }
    }
}
