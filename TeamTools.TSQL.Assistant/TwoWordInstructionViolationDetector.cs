using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;

namespace TeamTools.TSQL.Assistant
{
    internal class TwoWordInstructionViolationDetector : TSqlFragmentVisitor
    {
        public IList<TwoWordInstructionCodeFix> TwoWordInstructionsToFix { get; } = new List<TwoWordInstructionCodeFix>();

        public override void Visit(BeginTransactionStatement node)
        {
            FixTransactionStatement(node);
        }

        public override void Visit(CommitTransactionStatement node)
        {
            FixTransactionStatement(node);
        }

        public override void Visit(RollbackTransactionStatement node)
        {
            FixTransactionStatement(node);
        }

        public override void Visit(SaveTransactionStatement node)
        {
            FixTransactionStatement(node);
        }

        public override void Visit(QualifiedJoin node)
        {
            string finalText = "";
            bool codeFixed = false;
            bool joinTypeFound = false;
            bool joinFound = false;

            // take closest to join definition token range
            // and skip referenced table identifiers
            int i = node.FirstTableReference.LastTokenIndex
                + (node.ScriptTokenStream[node.FirstTableReference.LastTokenIndex].TokenType == TSqlTokenType.Identifier ? 1 : 0);
            int lastToken = node.SecondTableReference.FirstTokenIndex
                - (node.ScriptTokenStream[node.SecondTableReference.FirstTokenIndex].TokenType == TSqlTokenType.Identifier ? 1 : 0);

            // skip comments and spaces
            while (i < lastToken && (node.ScriptTokenStream[i].TokenType == TSqlTokenType.WhiteSpace
            || node.ScriptTokenStream[i].TokenType == TSqlTokenType.WhiteSpace))
            {
                i++;
            }

            while (i < lastToken && (node.ScriptTokenStream[lastToken].TokenType == TSqlTokenType.WhiteSpace
             || node.ScriptTokenStream[lastToken].TokenType == TSqlTokenType.WhiteSpace))
            {
                lastToken--;
            }

            int startLine = node.ScriptTokenStream[i].Line,
                startCol = node.ScriptTokenStream[i].Column;

            while (i <= lastToken && !codeFixed && !joinFound)
            {
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Left
                || node.ScriptTokenStream[i].TokenType == TSqlTokenType.Right
                || node.ScriptTokenStream[i].TokenType == TSqlTokenType.Full
                || node.ScriptTokenStream[i].TokenType == TSqlTokenType.Inner
                || node.ScriptTokenStream[i].TokenType == TSqlTokenType.Cross)
                {
                    // narrowing token range
                    startLine = node.ScriptTokenStream[i].Line;
                    startCol = node.ScriptTokenStream[i].Column;
                    finalText = "";

                    joinTypeFound = true;
                }

                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Join)
                {
                    joinFound = true;
                    if (!joinTypeFound)
                    {
                        finalText += "INNER ";
                        codeFixed = true;
                    }

                    finalText += node.ScriptTokenStream[i].Text;
                }
                else
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Outer)
                {
                    // skip OUTER keyword
                    codeFixed = true;
                }
                else
                {
                    finalText += node.ScriptTokenStream[i].Text;
                    i++;
                }
            }

            if (!codeFixed)
            {
                return;
            }

            CalcEndOfExpressionPosition(node.ScriptTokenStream[i], out int endLine, out int endCol);

            TwoWordInstructionsToFix.Add(new TwoWordInstructionCodeFix(
                startLine: startLine,
                startCol: startCol,
                endLine: endLine,
                endCol: endCol,
                fixedText: finalText));
        }

        // WITH keyword for hints
        public override void Visit(NamedTableReference node)
        {
            if (node.TableHints.Count == 0)
            {
                return;
            }

            var firstHint = node.TableHints[0];
            var startToken = node.SchemaObject.LastTokenIndex;

            // copy-pasted from HintSyntaxRule
            int i = firstHint.FirstTokenIndex - 1;
            while (i >= startToken && (
                firstHint.ScriptTokenStream[i].TokenType == TSqlTokenType.WhiteSpace
                || firstHint.ScriptTokenStream[i].TokenType == TSqlTokenType.LeftParenthesis))
            {
                i--;
            }

            if (i <= 0)
            {
                return;
            }

            if (firstHint.ScriptTokenStream[i].TokenType == TSqlTokenType.With)
            {
                return;
            }

            i++;

            TwoWordInstructionsToFix.Add(new TwoWordInstructionCodeFix(
                startLine: firstHint.ScriptTokenStream[i].Line,
                startCol: firstHint.ScriptTokenStream[i].Column,
                endLine: firstHint.ScriptTokenStream[i].Line,
                endCol: firstHint.ScriptTokenStream[i].Column + firstHint.ScriptTokenStream[i].Text.Length - 1,
                fixedText: " WITH " + firstHint.ScriptTokenStream[i].Text));
        }

        private void CalcEndOfExpressionPosition(TSqlParserToken token, out int line, out int col)
        {
            col = token.Column;
            line = token.Line;

            using (var reader = new StringReader(token.Text))
            {
                string textLine = reader.ReadLine();
                // first line is the same line where token started
                if (!string.IsNullOrEmpty(textLine))
                {
                    col += textLine.Length - 1;
                }

                while ((textLine = reader.ReadLine()) != null)
                {
                    col = textLine.Length;
                    line++;
                }
            }
        }

        private void FixTransactionStatement(TransactionStatement node)
        {
            string tranKeyword = "TRANSACTION";
            string finalText = "";
            int startLine = node.ScriptTokenStream[node.FirstTokenIndex].Line,
                startCol = node.ScriptTokenStream[node.FirstTokenIndex].Column;

            int i = node.FirstTokenIndex;
            int lastToken = node.Name != null ? node.Name.FirstTokenIndex - 1 : node.LastTokenIndex;
            bool codeFixed = false;
            bool tranFound = false;
            while (i <= lastToken && !codeFixed && !tranFound)
            {
                // wrong spelling or missing keyword
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Tran)
                {
                    finalText += tranKeyword;
                    codeFixed = true;
                }
                else if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Semicolon)
                {
                    finalText += string.Format(" {0};", tranKeyword);
                    codeFixed = true;
                }
                else if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.Transaction)
                {
                    tranFound = true;
                }
                else
                {
                    finalText += node.ScriptTokenStream[i].Text;
                    i++;
                }
            }

            // in case of COMMIT written as a single word
            if (node.FirstTokenIndex == node.LastTokenIndex && !tranFound && !codeFixed)
            {
                codeFixed = true;
                finalText += " " + tranKeyword;
                i = node.LastTokenIndex;
            }

            if (!codeFixed)
            {
                return;
            }

            CalcEndOfExpressionPosition(node.ScriptTokenStream[i], out int endLine, out int endCol);

            TwoWordInstructionsToFix.Add(new TwoWordInstructionCodeFix(
                startLine: startLine,
                startCol: startCol,
                endLine: endLine,
                endCol: endCol,
                fixedText: finalText));
        }
    }
}
