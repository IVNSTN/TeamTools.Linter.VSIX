using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;

namespace TeamTools.TSQL.Assistant
{
    internal class ShorthandOrFullNameViolationDetector : TSqlFragmentVisitor
    {
        private static readonly IDictionary<string, string> CorrectWords
            = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly ICollection<string> DateFunctions = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly IDictionary<string, string> TypeReplacements = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static ShorthandOrFullNameViolationDetector()
        {
            // TODO : load from SqlServerMetadata
            CorrectWords.Add("EXECUTE", "EXEC");
            CorrectWords.Add("PROC", "PROCEDURE");
            CorrectWords.Add("TRAN", "TRANSACTION");
            CorrectWords.Add("INTEGER", "INT");
            CorrectWords.Add("DEC", "DECIMAL");
            CorrectWords.Add("OUT", "OUTPUT");

            CorrectWords.Add("HH", "HOUR");
            CorrectWords.Add("N", "MINUTE");
            CorrectWords.Add("MI", "MINUTE");
            CorrectWords.Add("S", "SECOND");
            CorrectWords.Add("SS", "SECOND");
            CorrectWords.Add("MS", "MILLISECOND");
            CorrectWords.Add("MCS", "MICROSECOND");
            CorrectWords.Add("NS", "NANOSECOND");
            CorrectWords.Add("TZ", "TZOFFSET");

            CorrectWords.Add("YY", "YEAR");
            CorrectWords.Add("YYYY", "YEAR");
            CorrectWords.Add("Q", "QUARTER");
            CorrectWords.Add("QQ", "QUARTER");
            CorrectWords.Add("M", "MONTH");
            CorrectWords.Add("MM", "MONTH");
            CorrectWords.Add("WK", "WEEK");
            CorrectWords.Add("WW", "WEEK");
            CorrectWords.Add("Y", "DAYOFYEAR");
            CorrectWords.Add("DY", "DAYOFYEAR");
            CorrectWords.Add("DW", "WEEKDAY");
            CorrectWords.Add("W", "WEEKDAY");
            CorrectWords.Add("D", "DAY");
            CorrectWords.Add("DD", "DAY");
            CorrectWords.Add("ISOWK", "ISO_WEEK");
            CorrectWords.Add("ISOWW", "ISO_WEEK");

            DateFunctions.Add("DATEADD");
            DateFunctions.Add("DATEDIFF");
            DateFunctions.Add("DATEDIFF_BIG");
            DateFunctions.Add("DATENAME");
            DateFunctions.Add("DATE_BUCKET");
            DateFunctions.Add("DATETRUNC");
            DateFunctions.Add("DATEPART");

            TypeReplacements.Add("dbo.TOneChar", "CHAR(1)");
            TypeReplacements.Add("dbo.TShortString", "VARCHAR(20)");
            TypeReplacements.Add("dbo.TUserName", "VARCHAR(20)");
            TypeReplacements.Add("dbo.TIdCode", "VARCHAR(20)");
            TypeReplacements.Add("dbo.TAccount", "VARCHAR(21)");
            TypeReplacements.Add("dbo.TMidleString", "VARCHAR(100)");
            TypeReplacements.Add("dbo.TLongString", "VARCHAR(255)");
            TypeReplacements.Add("dbo.TString8000", "VARCHAR(8000)");
            TypeReplacements.Add("dbo.TSign", "VARBINARY(8000)");
            TypeReplacements.Add("dbo.TIndex", "INT");
            TypeReplacements.Add("dbo.TMoney", "DECIMAL(14, 2)");
            TypeReplacements.Add("dbo.TPrice", "DECIMAL(10, 4)");
            TypeReplacements.Add("dbo.TIMESTAMP", "ROWVERSION");
        }

        public List<ShorthandOrFullnameCodeFix> ShorthandsToFix { get; } = new List<ShorthandOrFullnameCodeFix>();

        public override void Visit(ProcedureStatementBodyBase node)
        {
            int i = node.FirstTokenIndex;
            int lastToken;
            bool codeFixed = false;

            if (node.Parameters?.Count > 0)
            {
                lastToken = node.Parameters[0].FirstTokenIndex;
            }
            else
            if (node.StatementList?.Statements?.Count > 0)
            {
                lastToken = node.StatementList.Statements[0].FirstTokenIndex;
            }
            else
            {
                lastToken = node.LastTokenIndex;
            }

            while (i < lastToken && !codeFixed
                && node.ScriptTokenStream[i].TokenType != TSqlTokenType.Semicolon)
            {
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Proc)
                {
                    codeFixed = true;
                    MakeTokenFix(node.ScriptTokenStream[i], "PROC");
                }
                else
                {
                    i++;
                }
            }
        }

        public override void Visit(DropProcedureStatement node)
        {
            int i = node.FirstTokenIndex;
            int lastToken = node.Objects[0].FirstTokenIndex;
            bool codeFixed = false;

            while (i < lastToken && !codeFixed)
            {
                // catching short version
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Proc)
                {
                    codeFixed = true;
                    MakeTokenFix(node.ScriptTokenStream[i], "PROC");
                }
                else
                {
                    i++;
                }
            }
        }

        public override void Visit(ExecuteStatement node)
        {
            int i = node.FirstTokenIndex;
            int lastToken = node.FirstTokenIndex; // execute is the first word
            bool codeFixed = false;

            while (i <= lastToken && !codeFixed)
            {
                // catching long version
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Execute)
                {
                    codeFixed = true;

                    MakeTokenFix(node.ScriptTokenStream[i], "EXECUTE");
                }
                else
                {
                    i++;
                }
            }
        }

        public override void Visit(TransactionStatement node)
        {
            int i = node.FirstTokenIndex;
            int lastToken = node.Name?.FirstTokenIndex ?? node.LastTokenIndex;
            bool codeFixed = false;

            while (i <= lastToken && !codeFixed)
            {
                // catching short version
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Tran)
                {
                    codeFixed = true;
                    MakeTokenFix(node.ScriptTokenStream[i], "TRAN");
                }
                else
                {
                    i++;
                }
            }
        }

        public override void Visit(DataTypeReference node)
        {
            int i = node.FirstTokenIndex;
            bool codeFixed = false;

            while (i <= node.LastTokenIndex && !codeFixed
                && node.ScriptTokenStream[i].TokenType != TSqlTokenType.Semicolon
                && node.ScriptTokenStream[i].TokenType != TSqlTokenType.Colon
                && node.ScriptTokenStream[i].TokenType != TSqlTokenType.LeftParenthesis)
            {
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Integer)
                {
                    codeFixed = true;
                    MakeTokenFix(node.ScriptTokenStream[i], "INTEGER");
                }
                else
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Identifier
                    && string.Equals(node.ScriptTokenStream[i].Text, "INTEGER", StringComparison.OrdinalIgnoreCase))
                {
                    codeFixed = true;
                    MakeTokenFix(node.ScriptTokenStream[i], "INTEGER");
                }
                else if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Identifier
                    && string.Equals(node.ScriptTokenStream[i].Text, "DEC", StringComparison.OrdinalIgnoreCase))
                {
                    codeFixed = true;
                    MakeTokenFix(node.ScriptTokenStream[i], "DEC");
                }
                else
                {
                    i++;
                }
            }

            // TODO : extract to separate assistant?
            // Type reference can be multiline, implementation currently
            // doesn't support multiline replacements
            if (!codeFixed && node.Name != null
            && node.StartLine == node.ScriptTokenStream[node.LastTokenIndex].Line)
            {
                // TODO : support []?
                string typeFullName = (node.Name.SchemaIdentifier?.Value ?? "dbo")
                    + "." + node.Name.BaseIdentifier.Value;

                if (!string.IsNullOrEmpty(typeFullName)
                && TypeReplacements.ContainsKey(typeFullName))
                {
                    ShorthandsToFix.Add(new ShorthandOrFullnameCodeFix(
                        startLine: node.StartLine,
                        startCol: node.StartColumn,
                        endLine: node.StartLine,
                        endCol: node.ScriptTokenStream[node.LastTokenIndex].Column + node.ScriptTokenStream[node.LastTokenIndex].Text.Length - 1,
                        fixedText: TypeReplacements[typeFullName]));
                }
            }
        }

        public override void Visit(FunctionCall node)
        {
            if (!DateFunctions.Contains(node.FunctionName.Value)
            || node.Parameters.Count == 0)
            {
                return;
            }

            int i = node.Parameters[0].FirstTokenIndex;
            bool codeFixed = false;

            while (i <= node.LastTokenIndex && !codeFixed
                && node.ScriptTokenStream[i].TokenType != TSqlTokenType.Semicolon)
            {
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Identifier
                && CorrectWords.ContainsKey(node.ScriptTokenStream[i].Text))
                {
                    codeFixed = true;
                    MakeTokenFix(node.ScriptTokenStream[i], node.ScriptTokenStream[i].Text);
                }
                else
                {
                    i++;
                }
            }
        }

        private void MakeTokenFix(TSqlParserToken token, string wrongWord)
        {
            string fixedText = CorrectWords[wrongWord];

            ShorthandsToFix.Add(new ShorthandOrFullnameCodeFix(
                startLine: token.Line,
                startCol: token.Column,
                endLine: token.Line,
                endCol: token.Column + token.Text.Length - 1,
                fixedText: fixedText));
        }
    }
}
