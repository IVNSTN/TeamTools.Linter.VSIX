using System;
using System.Diagnostics.CodeAnalysis;

namespace TeamTools.VisualStudio.SqlExtension
{
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1649", Justification = "Reviewed")]

    public interface IVSIdeAdapter
    {
        object GetVSSelectedPackageItem();

        object GetGlobalService(Type type);
    }

    public interface IVSOutputPaneController
    {
        void Activate();

        void Clear();
    }
}
