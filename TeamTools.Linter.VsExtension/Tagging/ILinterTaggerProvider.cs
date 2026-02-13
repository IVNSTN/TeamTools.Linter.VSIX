using System;
using System.Collections.Generic;
using TeamTools.VisualStudio.SqlExtension.Linting;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    public interface ILinterTaggerProvider
    {
        event EventHandler<GeneralErrorArgs> OnError;

        void Accept(string filePath, IEnumerable<LinterMessage> messages);
    }
}
