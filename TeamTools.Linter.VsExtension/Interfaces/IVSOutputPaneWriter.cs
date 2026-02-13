using System;

namespace LintExtension
{
    public interface IVSOutputPaneWriter
    {
        void WriteLine(string text);
    }

    public interface IVSOutputPaneController
    {
        void Activate();

        void Clear();
    }

    public interface IVSIdeAdapter
    {
        object GetVSSelectedPackageItem();

        object GetGlobalService(Type type);
    }
}
