//------------------------------------------------------------------------------
// <copyright file="LintPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TeamTools.Common.Linting;
using TeamTools.VisualStudio.SqlExtension.Linting;
using TeamTools.VisualStudio.SqlExtension.Tagging;
using Task = System.Threading.Tasks.Task;

namespace TeamTools.VisualStudio.SqlExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.2", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideService(typeof(ILinterService), IsAsyncQueryable = true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideBindingPath]
    [Guid(LintPackage.PackageGuidString)]
    [ExcludeFromCodeCoverage]
    public sealed class LintPackage : AsyncPackage
    {
        /// <summary>
        /// LintPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "83202475-351d-41bb-a06c-2f541ec39b1d";

        static LintPackage()
        {
            // otherwise it does not find embedded dlls from Resources dir
            var cat = new AggregateCatalog();
            string extensionPath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            foreach (string subdir in GetEmbeddedAssemblyPaths(extensionPath))
            {
                cat.Catalogs.Add(new DirectoryCatalog(subdir));
            }

            cat.Catalogs.Add(new AssemblyCatalog(typeof(LintPackage).Assembly));
            var container = new CompositionContainer(cat);
            var batch = new CompositionBatch();
            batch.AddPart(container);
            container.Compose(batch);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LintExtensionMenuHandler"/> class.
        /// </summary>
        public LintPackage()
        {
            // So MEF imports can be satisfied. Because Packages are not a part of MEF.
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            componentModel.DefaultCompositionService.SatisfyImportsOnce(this);

            if (Linter != null)
            {
                Linter.OnError += HandleError;
                Linter.OnMessageReported += LogViolation;
            }

            if (Tagger != null)
            {
                Tagger.OnError += HandleError;
            }
        }

        [Import]
        private ILinterService Linter { get; set; }

        [Import]
        private ILinterTaggerProvider Tagger { get; set; }

        [Import]
        private ITextOutputPort OutputPane { get; set; }

        // Initialization of the package; this method is called right after the package is sited, so this is the place
        // where you can put all the initialization code that rely on services provided by VisualStudio.
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await base.InitializeAsync(cancellationToken, progress);

            this.AddService(typeof(ILinterService), CreateLinterServiceAsync);

            await LintExtensionMenuHandler.InitializeAsync(this, OutputPane).ConfigureAwait(false);
        }

        // TODO : what about Resources\DefaultConfig.json ???
        private static IEnumerable<string> GetEmbeddedAssemblyPaths(string extensionPath)
        {
            yield return Path.Combine(extensionPath, @"Resources\Plugins\Assistant");
            yield return Path.Combine(extensionPath, @"Resources\Plugins\SSDTLinter");
            yield return Path.Combine(extensionPath, @"Resources\Plugins\TSQLLinter");
        }

        private async Task<object> CreateLinterServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            // Linter comes from MEF now
            if (Linter is null)
            {
                // TODO : throw?
                return null;
            }

            await ((ILinterService)Linter).InitializeAsync(cancellationToken).ConfigureAwait(false);

            return Linter;
        }

        private void HandleError(object sender, GeneralErrorArgs args)
        {
            Task.Run(() => HandleErrorAsync(sender, args));
        }

        private async Task HandleErrorAsync(object sender, GeneralErrorArgs args)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                OutputPane.WriteLine($"{sender.GetType()} reported an error:");

                foreach (var e in ExpandException(args.Err))
                {
                    OutputPane.WriteLine($"Error {e.GetType().Name}: {e.Message}");
                }
            }
            catch (TaskCanceledException)
            {
                // dummy
            }
        }

        private IEnumerable<Exception> ExpandException(Exception e)
        {
            if (e is AggregateException ag)
            {
                foreach (var ee in ag.Flatten().InnerExceptions)
                {
                    yield return ee;
                }
            }
            else
            {
                yield return e;
            }
        }

        private void LogViolation(string filename, IEnumerable<LinterMessage> messages)
        {
            Task.Run(() => LogViolationAsync(filename, messages));
        }

        private async Task LogViolationAsync(string filename, IEnumerable<LinterMessage> messages)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                foreach (var msg in messages)
                {
                    OutputPane.WriteLine(msg.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                // dummy
            }
            catch (Exception e)
            {
                await HandleErrorAsync(this, new GeneralErrorArgs(e)).ConfigureAwait(false);
            }
        }
    }
}
