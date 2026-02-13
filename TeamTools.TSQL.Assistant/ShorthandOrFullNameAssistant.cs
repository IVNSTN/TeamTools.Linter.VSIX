using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamTools.Common.Linting;

namespace TeamTools.TSQL.Assistant
{
    public class ShorthandOrFullNameAssistant : BaseCodeEditingAssistant
    {
        public ShorthandOrFullNameAssistant(ITextOutputPort outputPort, string code) : base(outputPort, code)
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

            var codeModifier = new ShorthandOrFullNameViolationDetector();
            sql.Accept(codeModifier);

            HasChanges = codeModifier.ShorthandsToFix.Count > 0;
            if (HasChanges)
            {
                FixShorthands(codeModifier.ShorthandsToFix);
            }

            return true;
        }

        // TODO : extract from concrete assistants
        private void PerformModifications(IOrderedEnumerable<ShorthandOrFullnameCodeFix> blocks, IList<string> code)
        {
            foreach (var block in blocks)
            {
                // splitting fixed code into multiple lines
                var fixedCode = new List<string>();
                using (var reader = new StringReader(block.FixedText))
                {
                    string fixedLine;
                    while ((fixedLine = reader.ReadLine()) != null)
                    {
                        fixedCode.Add(fixedLine);
                    }
                }

                // processing original code lines
                for (int lineIndex = block.EndLine; lineIndex >= block.StartLine; lineIndex--)
                {
                    string line = code[lineIndex - 1], linePrefix = "", lineSuffix = "", lineFix = "";
                    int fixIndex;

                    if (lineIndex == block.StartLine && block.StartCol > 1)
                    {
                        linePrefix = line.Substring(0, block.StartCol - 1);
                    }

                    if (lineIndex == block.EndLine && block.EndCol < line.Length)
                    {
                        lineSuffix = line.Substring(block.EndCol, line.Length - block.EndCol);
                    }

                    if ((fixIndex = fixedCode.Count - (block.EndLine - lineIndex) - 1) >= 0)
                    {
                        lineFix = fixedCode[fixIndex];
                    }

                    code[lineIndex - 1] = linePrefix + lineFix + lineSuffix;
                }
            }
        }

        // TODO : extract from concrete assistants
        private void FixShorthands(IList<ShorthandOrFullnameCodeFix> items)
        {
            var modifiedCodeLines = new List<string>(OriginalCode.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            var reversePositionList = items.OrderByDescending(b => b.StartLine).ThenByDescending(b => b.StartCol);
            PerformModifications(reversePositionList, modifiedCodeLines);

            ModifiedCode = string.Join(Environment.NewLine, modifiedCodeLines);
        }
    }
}
