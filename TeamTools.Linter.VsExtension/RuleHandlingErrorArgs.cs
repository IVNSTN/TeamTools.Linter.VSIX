using System;

namespace TeamTools.VisualStudio.SqlExtension
{
    internal class RuleHandlingErrorArgs : EventArgs
    {
        public RuleHandlingErrorArgs(Exception e, string ruleID, string descr)
        {
            Err = e;
            RuleID = ruleID;
            Descr = descr;
        }

        public Exception Err { get; }

        public string RuleID { get; }

        public string Descr { get; }
    }
}
