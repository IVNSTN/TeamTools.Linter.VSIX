using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace TeamTools.VisualStudio.SqlExtension
{
    internal class VSIdeAdapter : IVSIdeAdapter
    {
        public object GetVSSelectedPackageItem()
        {
            object selectedObject = null;

            IVsMonitorSelection monitorSelection =
                    (IVsMonitorSelection)Package.GetGlobalService(
                    typeof(SVsShellMonitorSelection));

            monitorSelection.GetCurrentSelection(
                out IntPtr hierarchyPointer,
                out uint fileItemId,
                out IVsMultiItemSelect multiItemSelect,
                out IntPtr selectionContainerPointer);

            // TODO : handle multiItemSelect
            if (Marshal.GetTypedObjectForIUnknown(hierarchyPointer, typeof(IVsHierarchy)) is IVsHierarchy selectedHierarchy)
            {
                ErrorHandler.ThrowOnFailure(
                    selectedHierarchy.GetProperty(
                        fileItemId,
                        (int)__VSHPROPID.VSHPROPID_ExtObject,
                        out selectedObject));
            }

            return selectedObject;
        }

        public object GetGlobalService(Type type)
        {
            return Package.GetGlobalService(type);
        }
    }
}
