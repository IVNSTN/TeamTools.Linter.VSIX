using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    public delegate void LinterMessageReportedEvent(string filename, IEnumerable<LinterMessage> msg);

    public interface ILinterService
    {
        event LinterMessageReportedEvent OnMessageReported;

        event EventHandler<GeneralErrorArgs> OnError;

        Task LintFileAsync(string filePath, CancellationToken token);

        Task LintFolderAsync(string folderPath, CancellationToken token);

        Task LintFileTabAsync(string filePath, TextReader reader, CancellationToken token);

        Task InitializeAsync(CancellationToken cancellation);
    }
}
