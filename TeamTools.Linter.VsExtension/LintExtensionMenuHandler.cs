using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using TeamTools.Common.Linting;
using TeamTools.Common.Linting.Infrastructure;
using TeamTools.VisualStudio.SqlExtension.CodingAssistants;
using TeamTools.VisualStudio.SqlExtension.Linting;

namespace TeamTools.VisualStudio.SqlExtension
{
    /// <summary>
    /// Command handler.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class LintExtensionMenuHandler
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandIdProject = 0x0100;
        public const int CommandIdFolder = 0x0500;
        public const int CommandIdFile = 0x0200;
        public const int CommandIdTab = 0x0300;
        public const int CommandIdSpellingHelper = 0x0400;

        /// <summary>
        /// Command menu group (command set GUID).
        /// https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/ide-defined-commands-for-extending-project-systems?view=vs-2019 .
        /// </summary>
        public static readonly Guid CommandSetProject = new Guid("c5fe6472-33f3-40e4-b201-8a8b76cb3b81");
        public static readonly Guid CommandSetFolder = new Guid("230501BA-B459-4CC5-8FDA-245D0015CAC4");
        public static readonly Guid CommandSetFile = new Guid("230501BA-B459-4CC5-8FDA-245D0015CAC3");
        public static readonly Guid CommandSetTab = new Guid("230CDDC9-A01E-484C-B4A3-170910687A82");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;
        private BeginEndAssistantVSAdapter beginEnd;
        private TwoWordInstructionAssistantVSAdapter twoWord;
        private ShorthandOrFullNameAssistantVSAdapter shorthand;
        private FinalGoAssistantVSAdapter finalGo;
        private LintingAssistantVSAdapter linting;
        private VSIdeAdapter ide;

        /// <summary>
        /// Initializes a new instance of the <see cref="LintExtensionMenuHandler"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private LintExtensionMenuHandler(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static LintExtensionMenuHandler Instance { get; private set; }

        private ILinterService Linter { get; set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private AsyncPackage ServiceProvider => package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package, ITextOutputPort output)
        {
            Instance = new LintExtensionMenuHandler(package);
            await Instance.InitializeAsync(output);
        }

        private static string GetAssemblyLocalPathFrom(Type type)
        {
            string codebase = type.Assembly.CodeBase;
            var uri = new Uri(codebase, UriKind.Absolute);
            return uri.LocalPath;
        }

        private async System.Threading.Tasks.Task InitializeAsync(ITextOutputPort output)
        {
            var svc = await ServiceProvider.GetServiceAsync(typeof(IMenuCommandService));

            if (!(svc is OleMenuCommandService commandService))
            {
                return;
            }

            MakeMenu((menuItem) => commandService.AddCommand(menuItem));

            ide = new VSIdeAdapter();

            // TODO : composition of unknown number of assistants
            beginEnd = new BeginEndAssistantVSAdapter(output, ide);
            twoWord = new TwoWordInstructionAssistantVSAdapter(output, ide);
            shorthand = new ShorthandOrFullNameAssistantVSAdapter(output, ide);
            finalGo = new FinalGoAssistantVSAdapter(output, ide);

            linting = new LintingAssistantVSAdapter(
                package,
                output,
                output as IVSOutputPaneController, // TODO : Not good hardcoded cast. Pass something specific or change approach.
                ide,
                new AssemblyWrapper(),
                new FileSystemWrapper(),
                GetAssemblyLocalPathFrom(typeof(LintPackage)));
        }

        private OleMenuCommand MakeMenuItem(Guid commandSetGuid, int commandId, EventHandler menuActionHandler, Action<object, EventArgs> getEnabledCallback)
        {
            CommandID menuCommandID = new CommandID(commandSetGuid, commandId);
            OleMenuCommand menuItem = new OleMenuCommand(menuActionHandler, menuCommandID);
            if (null != getEnabledCallback)
            {
                menuItem.BeforeQueryStatus += new EventHandler(getEnabledCallback);
            }

            return menuItem;
        }

        private void MakeMenu(Action<OleMenuCommand> callback)
        {
            // Lint this project
            callback(MakeMenuItem(CommandSetProject, CommandIdProject, ProjectMenuItemCallback, null));
            // Lint this folder
            callback(MakeMenuItem(CommandSetFolder, CommandIdFolder, FolderMenuItemCallback, null));
            // Lint this file
            callback(MakeMenuItem(CommandSetFile, CommandIdFile, FileMenuItemCallback, BeforeGetStatus));
            // Lint this tab
            callback(MakeMenuItem(CommandSetTab, CommandIdTab, TabMenuItemCallback, BeforeGetStatus));
            // Adjust spelling on open tab header
            callback(MakeMenuItem(CommandSetTab, CommandIdSpellingHelper, SpellingAssistantCallback, BeforeGetStatus));
        }

        private void BeforeGetStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand cmd)
            {
                return;
            }

            cmd.Enabled = GetFileMenuItemEnabled();
        }

        private bool GetFileMenuItemEnabled()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            object selectedItem = ide.GetVSSelectedPackageItem();

            if (selectedItem is ProjectItem selectedFile)
            {
                string fileExt = Path.GetExtension(selectedFile.Properties?.Item("FullPath").Value.ToString());
                return !string.IsNullOrEmpty(fileExt);
            }

            return false;
        }

        private void ProjectMenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(DTE));

            linting.RunLintingOnAllModifiedFiles(
            (msg, i, n) =>
            {
                // ThreadHelper.ThrowIfNotOnUIThread();
                // FIXME : callback is fired not in UI thread; put progress to queue first?
                dte2?.StatusBar.Progress(true, msg, i, n);
            });
        }

        private void FileMenuItemCallback(object sender, EventArgs e)
        {
            ServiceProvider.JoinableTaskFactory.RunAsyncAsVsTask(
                VsTaskRunContext.UIThreadBackgroundPriority,
                async (token) =>
                {
                    await FileMenuItemCallbackAsync(token);
                    return false;
                });
        }

        private async System.Threading.Tasks.Task FileMenuItemCallbackAsync(CancellationToken cancellation)
        {
            Linter = (ILinterService)(await ServiceProvider.GetServiceAsync(typeof(ILinterService)));
            if (Linter is null)
            {
                // TODO : throw?
                return;
            }

            var filePath = await linting.GetVSSelectedFilePathAsync(cancellation);

            await Linter.LintFileAsync(filePath, cancellation);
        }

        private void FolderMenuItemCallback(object sender, EventArgs e)
        {
            ServiceProvider.JoinableTaskFactory.RunAsyncAsVsTask(
                VsTaskRunContext.UIThreadBackgroundPriority,
                async (token) =>
                {
                    await FolderMenuItemCallbackAsync(token);
                    return false;
                });
        }

        private async System.Threading.Tasks.Task FolderMenuItemCallbackAsync(CancellationToken cancellation)
        {
            Linter = (ILinterService)(await ServiceProvider.GetServiceAsync(typeof(ILinterService)));
            if (Linter is null)
            {
                // TODO : throw?
                return;
            }

            var filePath = await linting.GetVSSelectedFilePathAsync(cancellation);

            await Linter.LintFolderAsync(filePath, cancellation);

            /* TODO : get progress bar update back
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(DTE));

            linting.RunLintingOnSelectedFolder(
            (msg, i, n) =>
            {
                // ThreadHelper.ThrowIfNotOnUIThread();
                // FIXME : callback is fired not in UI thread; put progress to queue first?
                dte2?.StatusBar.Progress(true, msg, i, n);
            });
            */
        }

        // TODO: No need anymore since tab is automatically linted on open and on edit?
        private void TabMenuItemCallback(object sender, EventArgs e)
        {
            ServiceProvider.JoinableTaskFactory.RunAsyncAsVsTask(
                VsTaskRunContext.UIThreadBackgroundPriority,
                async (token) =>
                {
                    await TabMenuItemCallbackAsync(token);
                    return false;
                });
        }

        private async System.Threading.Tasks.Task TabMenuItemCallbackAsync(CancellationToken cancellation)
        {
            Linter = (ILinterService)(await ServiceProvider.GetServiceAsync(typeof(ILinterService)));
            if (Linter is null)
            {
                // TODO : throw?
                return;
            }

            var filePath = await linting.GetVSSelectedFilePathAsync(cancellation);
            var reader = await linting.GetActiveTabTextAsync(cancellation);

            await Linter.LintFileTabAsync(filePath, reader, cancellation).ConfigureAwait(true);
        }

        private void SpellingAssistantCallback(object sender, EventArgs e)
        {
            ServiceProvider.JoinableTaskFactory.RunAsyncAsVsTask(
                VsTaskRunContext.UIThreadBackgroundPriority,
                async (token) =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);

                    // TODO : bring it back-> OutputPane?.Clear();
                    finalGo.ApplyAdjustments();
                    beginEnd.ApplyAdjustments();
                    twoWord.ApplyAdjustments();
                    shorthand.ApplyAdjustments();

                    return true;
                });
        }
    }
}
