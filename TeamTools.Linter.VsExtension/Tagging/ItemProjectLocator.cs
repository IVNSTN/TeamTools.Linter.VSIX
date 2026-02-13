using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    internal static class ItemProjectLocator
    {
        public static async System.Threading.Tasks.Task<ItemProjectInfo> GetProjectInfoAsync(string filePath, CancellationToken cancellation)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte2 = (DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var item = dte2?.Solution?.FindProjectItem(filePath);
            var info = new ItemProjectInfo();

            if (item is null || cancellation.IsCancellationRequested)
            {
                return info;
            }

            info.ProjectName = item.ContainingProject.Name;

            var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));

            if (solution != null
            && VSConstants.S_OK == solution.GetProjectOfUniqueName(item.ContainingProject.FullName, out IVsHierarchy hierarchy))
            {
                if (hierarchy != null && VSConstants.S_OK == hierarchy.GetGuidProperty(
                                VSConstants.VSITEMID_ROOT,
                                (int)__VSHPROPID.VSHPROPID_ProjectIDGuid,
                                out Guid projectGuid))
                {
                    info.ProjectGuid = projectGuid;
                    info.ProjectGuidString = projectGuid.ToString();
                }
            }

            return info;
        }

        public class ItemProjectInfo
        {
            public string ProjectName { get; set; } = null;

            public string ProjectGuidString { get; set; } = null;

            public Guid? ProjectGuid { get; set; }
        }
    }
}
