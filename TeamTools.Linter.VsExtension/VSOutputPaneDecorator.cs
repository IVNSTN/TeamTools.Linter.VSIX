using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using TeamTools.Common.Linting;

namespace TeamTools.VisualStudio.SqlExtension
{
    [Export(typeof(ITextOutputPort))]
    internal class VSOutputPaneDecorator : ITextOutputPort, IVSOutputPaneController
    {
        public static readonly Guid AssistantPaneId = new Guid("8215A068-5779-4281-9A77-FEABCDF06A50");
        public static readonly string AssistantPaneTitle = Properties.Strings.LinterName;

        private readonly IVsOutputWindowPane outputPane;

        public VSOutputPaneDecorator()
        {
            PaneTitle = AssistantPaneTitle;
            PaneID = AssistantPaneId;

            outputPane = GetOutputPane();
        }

        public Guid PaneID { get; }

        public string PaneTitle { get; }

        protected IVsOutputWindowPane OutputPane => GetOutputPane();

        public void WriteLine(string text)
        {
            System.Threading.Tasks.Task.Run(() => WriteLineAsync(text));
        }

        public void Clear()
        {
            System.Threading.Tasks.Task.Run(() => ClearAsync());
        }

        public void Activate()
        {
            // tbd
        }

        private async System.Threading.Tasks.Task WriteLineAsync(string text)
        {
            if (OutputPane is null)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            OutputPane.OutputStringThreadSafe(text + Environment.NewLine);
        }

        private async System.Threading.Tasks.Task ClearAsync()
        {
            if (OutputPane is null)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OutputPane.Clear();
        }

        private IVsOutputWindowPane GetOutputPane()
        {
            if (outputPane != null)
            {
                return outputPane;
            }

            return ThreadHelper.JoinableTaskFactory.Run(() => CreateOutputPaneAsync(PaneID, PaneTitle, true, true));
        }

        private async System.Threading.Tasks.Task<IVsOutputWindowPane> CreateOutputPaneAsync(
            Guid paneGuid,
            string title,
            bool visible,
            bool clearWithSolution)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            outWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);

            if (pane == null)
            {
                // Create a new pane
                var res = outWindow.CreatePane(
                    ref paneGuid,
                    title,
                    Convert.ToInt32(visible),
                    Convert.ToInt32(clearWithSolution));

                if (res != VSConstants.S_OK)
                {
                    Debug.WriteLine($"Failed to create pane: {res}");

                    // TODO : throw?
                    return null;
                }

                // Retrieve the new pane
                res = outWindow.GetPane(ref paneGuid, out pane);
                if (res != VSConstants.S_OK)
                {
                    Debug.WriteLine($"Failed to get pane: {res}");

                    // TODO : throw?
                    return null;
                }
            }

            if (pane != null)
            {
                pane.Clear();
                pane.Activate();
            }

            return pane;
        }
    }
}
