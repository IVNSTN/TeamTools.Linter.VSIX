using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.IO;
using TeamTools.Common.Linting;

namespace TeamTools.TSQL.Assistant
{
    public class FinalGoAssistant : BaseCodeEditingAssistant
    {
        public FinalGoAssistant(ITextOutputPort outputPort, string code) : base(outputPort, code)
        {
        }

        // TODO : refactor
        // TODO : extract from concrete assistants
        public override bool Parse()
        {
            var parser = new TSql150Parser(true); // TODO : compatibility from appSettings + factory
            var reader = new StringReader(OriginalCode);
            var sql = parser.Parse(reader, out var errors);

            if (errors.Count > 0)
            {
                OutputPort.WriteLine("Error parsing file:");
                OutputPort.WriteLine(string.Join("\r\n", errors));
                return false;
            }

            int i = sql.LastTokenIndex - 1;

            while (i >= 0 && sql.ScriptTokenStream[i].TokenType != TSqlTokenType.Go
            && !HasChanges)
            {
                if (sql.ScriptTokenStream[i].TokenType != TSqlTokenType.WhiteSpace)
                {
                    HasChanges = true;
                    ModifiedCode = OriginalCode + Environment.NewLine + "GO" + Environment.NewLine;
                }

                i--;
            }

            return true;
        }
    }
}
