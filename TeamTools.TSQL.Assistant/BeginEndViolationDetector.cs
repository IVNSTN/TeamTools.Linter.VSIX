using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;

namespace TeamTools.TSQL.Assistant
{
    internal class BeginEndViolationDetector : TSqlFragmentVisitor
    {
        public IList<BeginEndCodeFix> BeginEndElements { get; } = new List<BeginEndCodeFix>();

        public override void Visit(ProcedureStatementBody node)
        {
            if (node.StatementList.Statements.Count == 1 && node.StatementList.Statements[0] is BeginEndBlockStatement)
            {
                return;
            }

            SurroundWithBeginEnd(
                node,
                SkipComments(node, node.StatementList.Statements[0].FirstTokenIndex),
                SkipComments(node, node.StatementList.Statements[node.StatementList.Statements.Count - 1].LastTokenIndex, true));
        }

        public override void Visit(TriggerStatementBody node)
        {
            if (node.StatementList.Statements.Count == 1 && node.StatementList.Statements[0] is BeginEndBlockStatement)
            {
                return;
            }

            SurroundWithBeginEnd(
                node,
                SkipComments(node, node.StatementList.Statements[0].FirstTokenIndex),
                SkipComments(node, node.StatementList.Statements[node.StatementList.Statements.Count - 1].LastTokenIndex, true));
        }

        public override void Visit(FunctionStatementBody node)
        {
            if (node.StatementList.Statements.Count == 1 && node.StatementList.Statements[0] is BeginEndBlockStatement)
            {
                return;
            }

            SurroundWithBeginEnd(
                node,
                SkipComments(node, node.StatementList.Statements[0].FirstTokenIndex),
                SkipComments(node, node.StatementList.Statements[node.StatementList.Statements.Count - 1].LastTokenIndex, true));
        }

        public override void Visit(IfStatement node)
        {
            if ((null != node.ThenStatement) && !(node.ThenStatement is BeginEndBlockStatement))
            {
                SurroundWithBeginEnd(node, node.ThenStatement.FirstTokenIndex, node.ThenStatement.LastTokenIndex);
            }

            if ((null != node.ElseStatement) && !(node.ElseStatement is BeginEndBlockStatement) && !(node.ElseStatement is IfStatement))
            {
                SurroundWithBeginEnd(node, node.ElseStatement.FirstTokenIndex, node.ElseStatement.LastTokenIndex);
            }
        }

        public override void Visit(WhileStatement node)
        {
            if (node.Statement is BeginEndBlockStatement)
            {
                return;
            }

            SurroundWithBeginEnd(node, node.Statement.FirstTokenIndex, node.Statement.LastTokenIndex);
        }

        private int SkipComments(TSqlFragment node, int tokenIndex, bool stepForward = false)
        {
            int finalToken = tokenIndex + (stepForward ? 1 : 0);
            while (finalToken > 0 && finalToken < node.ScriptTokenStream.Count
                && (node.ScriptTokenStream[finalToken].TokenType == TSqlTokenType.WhiteSpace
                || node.ScriptTokenStream[finalToken].TokenType == TSqlTokenType.SingleLineComment
                || node.ScriptTokenStream[finalToken].TokenType == TSqlTokenType.MultilineComment))
            {
                finalToken++;
            }

            // eof is a separate token so no need to check if we are on either
            // whitespace or comment token in the end
            if (finalToken > tokenIndex
                && !(node.ScriptTokenStream[finalToken].TokenType == TSqlTokenType.WhiteSpace
                || node.ScriptTokenStream[finalToken].TokenType == TSqlTokenType.SingleLineComment
                || node.ScriptTokenStream[finalToken].TokenType == TSqlTokenType.MultilineComment))
            {
                finalToken--;
            }

            return finalToken;
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

        private void CalcEndOfPriorExpressionPosition(TSqlFragment node, int firstToken, out int line, out int col)
        {
            if (firstToken > 1)
            {
                firstToken--;

                while (firstToken > 0 && (node.ScriptTokenStream[firstToken].TokenType == TSqlTokenType.WhiteSpace))
                {
                    firstToken--;
                }

                // if not a whitespace then need to step back
                if (node.ScriptTokenStream[firstToken].TokenType != TSqlTokenType.WhiteSpace)
                {
                    firstToken++;
                }
            }

            line = node.ScriptTokenStream[firstToken].Line;
            col = node.ScriptTokenStream[firstToken].Column;
        }

        private void SurroundWithBeginEnd(TSqlFragment node, int firstToken, int lastToken)
        {
            int line, col;

            CalcEndOfPriorExpressionPosition(node, firstToken, out line, out col);
            BeginEndElements.Add(new BeginEndCodeFix(line, col, BeginOrEnd.BeginWord));

            CalcEndOfExpressionPosition(node.ScriptTokenStream[lastToken], out line, out col);
            BeginEndElements.Add(new BeginEndCodeFix(line, col, BeginOrEnd.EndWord));
        }
    }
}
