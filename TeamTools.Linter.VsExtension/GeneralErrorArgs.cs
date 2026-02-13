using System;

namespace TeamTools.VisualStudio.SqlExtension
{
    public class GeneralErrorArgs : EventArgs
    {
        public GeneralErrorArgs(Exception err, string details = default)
        {
            Err = err;
            Details = details;
        }

        public Exception Err { get; }

        public string Details { get; }
    }
}
