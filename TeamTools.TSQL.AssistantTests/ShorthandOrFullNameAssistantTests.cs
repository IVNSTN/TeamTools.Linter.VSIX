using NUnit.Framework;
using TeamTools.TSQL.Assistant;

namespace TeamTools.TSQL.AssistantTests
{
    [Category("Assistant")]
    internal class ShorthandOrFullNameAssistantTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ShorthandOrFullNameAssistantDoesNothingIfAllGood()
        {
            string originalText = @"
BEGIN TRANSACTION;
GO
CREATE PROCEDURE dbo.my_proc
    @id INT
AS
BEGIN
    EXEC ('test');
    IF @@ERROR <> 0
        ROLLBACK TRANSACTION;
END
GO
EXEC dbo.my_proc;
go
DROP PROCEDURE dbo.my_proc;
GO
COMMIT TRANSACTION;
";

            var assistant = new ShorthandOrFullNameAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.False, "no changes");
            Assert.That(assistant.ModifiedCode, Is.Null);
        }

        [Test]
        public void ShorthandOrFullNameAssistantFixesTranStatements()
        {
            string originalText = @"
BEGIN /* my */ TRAN;
COMMIT;
delete dbo.tbl; /* comment */ ROLLBACK -- comment
TRANSACTION /* asdf */
adsf;";
            string fixedText = @"
BEGIN /* my */ TRANSACTION;
COMMIT;
delete dbo.tbl; /* comment */ ROLLBACK -- comment
TRANSACTION /* asdf */
adsf;";

            var assistant = new ShorthandOrFullNameAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void ShorthandOrFullNameAssistantFixesExec()
        {
            string originalText = @"
EXECUTE ('test') AS USER='dbo';
EXECUTE sp_executesql N'adsf';
/* comment */ EXECUTE
@my_proc; EXECUTE dbo.test; -- comment
";
            string fixedText = @"
EXEC ('test') AS USER='dbo';
EXEC sp_executesql N'adsf';
/* comment */ EXEC
@my_proc; EXEC dbo.test; -- comment
";

            var assistant = new ShorthandOrFullNameAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void ShorthandOrFullNameAssistantFixesTypeNames()
        {
            string originalText = @"
DECLARE @i INTEGER,
    @d DEC(2,1)
";
            string fixedText = @"
DECLARE @i INT,
    @d DECIMAL(2,1)
";

            var assistant = new ShorthandOrFullNameAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void ShorthandOrFullNameAssistantFixesProc()
        {
            string originalText = @"
CREATE PROC dbo.foo
AS;
go
ALTER
    /* comment */ PROC dbo.bar
WITH EXECUTE AS OWNER
AS
BEGIN
    SELECT 1
END;
GO
SELECT 'asdf'; DROP PROC dbo.far; -- comment
;";
            string fixedText = @"
CREATE PROCEDURE dbo.foo
AS;
go
ALTER
    /* comment */ PROCEDURE dbo.bar
WITH EXECUTE AS OWNER
AS
BEGIN
    SELECT 1
END;
GO
SELECT 'asdf'; DROP PROCEDURE dbo.far; -- comment
;";

            var assistant = new ShorthandOrFullNameAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void ShorthandOrFullNameAssistantFixesDateParts()
        {
            string originalText = @"SELECT DATEADD(DD, @dt, 1), DATEPART(MS, GETDATE())";
            string fixedText = @"SELECT DATEADD(DAY, @dt, 1), DATEPART(MILLISECOND, GETDATE())";

            var assistant = new ShorthandOrFullNameAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }

        [Test]
        public void ShorthandOrFullNameAssistantReplacesDeprecatedDatatypeName()
        {
            string originalText = @"DECLARE @acc dbo.TAccount, @num tprice, @n NUMERIC(18,3)";
            string fixedText = @"DECLARE @acc VARCHAR(21), @num DECIMAL(10, 4), @n NUMERIC(18,3)";

            var assistant = new ShorthandOrFullNameAssistant(new MockOutputPort(), originalText);
            Assert.That(assistant.Parse(), Is.True, "parsed");
            Assert.That(assistant.HasChanges, Is.True, "has changes");
            Assert.That(assistant.ModifiedCode, Is.EqualTo(fixedText), "got expected code");
        }
    }
}
