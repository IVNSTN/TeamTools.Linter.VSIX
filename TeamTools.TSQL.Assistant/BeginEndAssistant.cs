using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamTools.Common.Linting;

namespace TeamTools.TSQL.Assistant
{
    public sealed class BeginEndAssistant : BaseCodeEditingAssistant
    {
        public BeginEndAssistant(ITextOutputPort outputPort, string code) : base(outputPort, code)
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

            var codeModifier = new BeginEndViolationDetector();
            sql.Accept(codeModifier);

            HasChanges = codeModifier.BeginEndElements.Count > 0;
            if (HasChanges)
            {
                RebuildCodeWithBeginEndBlocks(codeModifier.BeginEndElements);
            }

            return true;
        }

        // TODO : extract from concrete assistants
        private void PerformModifications(IOrderedEnumerable<BeginEndCodeFix> blocks, IList<string> code)
        {
            // TODO : if THROW is the first statement after BEGIN then append semicolon after BEGIN
            foreach (var block in blocks)
            {
                int lineIndex = block.Line - 1;
                var line = code[lineIndex];
                if (block.Col > 1 && block.Col < line.Length)
                {
                    code[lineIndex] = line.Substring(0, block.Col - 1);
                }

                lineIndex++;
                code.Insert(lineIndex, block.Element == BeginOrEnd.BeginWord ? "BEGIN" : "END");
                if (block.Col > 1 && block.Col < line.Length)
                {
                    lineIndex++;
                    code.Insert(lineIndex, line.Substring(block.Col - 1, line.Length - (block.Col - 1)));
                }
            }
        }

        // TODO : extract from concrete assistants
        private void RebuildCodeWithBeginEndBlocks(IList<BeginEndCodeFix> blocks)
        {
            var modifiedCodeLines = new List<string>(OriginalCode.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            var reversePositionList = blocks.OrderByDescending(b => b.Line).ThenByDescending(b => b.Col);
            PerformModifications(reversePositionList, modifiedCodeLines);

            ModifiedCode = string.Join(Environment.NewLine, modifiedCodeLines);
        }
    }
}
