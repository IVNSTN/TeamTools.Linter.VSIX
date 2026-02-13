using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamTools.Common.Linting;
using TeamTools.Common.Linting.Infrastructure;
using TeamTools.VisualStudio.SqlExtension.Linting;

namespace TeamTools.VisualStudio.SqlExtension.CodingAssistants
{
    [ExcludeFromCodeCoverage]
    internal class LintingAssistantVSAdapter
    {
        private const string LinterConfigFileName = "DefaultConfig.json";
        private readonly IAsyncServiceProvider serviceProvider;
        private readonly ITextOutputPort outputPort;
        private readonly IVSOutputPaneController outputPaneController;
        private readonly LintingAssistant linter;
        private readonly IVSIdeAdapter ideAdapter;

        public LintingAssistantVSAdapter(
            IAsyncServiceProvider serviceProvider,
            ITextOutputPort outputPort,
            IVSOutputPaneController paneController,
            IVSIdeAdapter ideAdapter,
            IAssemblyWrapper assemblyWrapper,
            IFileSystemWrapper fileSystemWrapper,
            string assemblyPath)
        {
            this.serviceProvider = serviceProvider;
            this.outputPort = outputPort;
            this.outputPaneController = paneController;
            this.ideAdapter = ideAdapter;

            // TODO : also LinterService creates LintingAssistant
            // thus MakeLinter and linter instantiation happens twice
            this.linter = new LintingAssistant(
                this.outputPort,
                assemblyWrapper,
                fileSystemWrapper,
                new AppConfigLoader(fileSystemWrapper, assemblyWrapper),
                new GitDecorator(new GitCommandFactory()),
                Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", LinterConfigFileName));
        }

        // TODO : integrate into ErrorList, use TaggerProvider
        public void RunLintingOnAllModifiedFiles(Action<string, int, int> progressCallback = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string solutionDir = GetVSSolutionPath();
            if (string.IsNullOrEmpty(solutionDir))
            {
                return;
            }

            PerformVSAction(() =>
            {
                // TODO : This is the last call to linter instance in this class
                // Migrate to LinterService and remove LintingAssistant instantiation
                // from this class
                linter.LintModifiedFiles(solutionDir, progressCallback);
            });
        }

        public async Task<string> GetVSSelectedFilePathAsync(CancellationToken cancellation)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellation);

            object selectedItem = ideAdapter.GetVSSelectedPackageItem();
            if (!(selectedItem is ProjectItem selectedFile))
            {
                return default;
            }

            var filePathNode = selectedFile.Properties?.Item("FullPath");
            if (filePathNode != null)
            {
                return filePathNode.Value.ToString();
            }

            return GetFilePathViaDte();
        }

        public async Task<TextReader> GetActiveTabTextAsync(CancellationToken cancellation)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellation);

            return new StringReader(ActiveTabContentsHelper.GetText(ideAdapter));
        }

        // TODO : async
        private void PerformVSAction(Action act, bool activateAfter = false)
        {
            try
            {
                outputPaneController.Clear();
                act();
            }
            catch (Exception e)
            {
                outputPort.WriteLine(e.Message);
            }
            finally
            {
                if (activateAfter)
                {
                    outputPaneController.Activate();
                }
            }
        }

        private string GetFilePathViaDte()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!(ideAdapter.GetGlobalService(typeof(DTE)) is DTE dte))
            {
                return default;
            }

            if (dte.ActiveDocument != null)
            {
                return dte.ActiveDocument.FullName;
            }

            if (dte.SelectedItems.Count == 1)
            {
                var sel = dte.SelectedItems.Item(1);
                var projectItem = sel.ProjectItem;
                var project = projectItem.ContainingProject;

                if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                {
                    var itemFolder = new FileInfo(projectItem.Properties?.Item("FullPath").ToString()).Directory;
                    var selectedFile = itemFolder.GetFiles(sel.Name).First();
                    return selectedFile.FullName;
                }

                return Path.Combine(Path.GetDirectoryName(project.FullName), sel.Name);
            }

            return default;
        }

        private string GetVSSolutionPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!(ideAdapter.GetGlobalService(typeof(DTE)) is DTE dte))
            {
                return default;
            }

            return Path.GetDirectoryName(dte.Solution.FullName);
        }
    }
}
