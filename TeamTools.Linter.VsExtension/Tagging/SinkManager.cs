using Microsoft.VisualStudio.Shell.TableManager;
using System;

namespace TeamTools.VisualStudio.SqlExtension.Tagging
{
    internal class SinkManager : IDisposable
    {
        private readonly TaggerProvider provider;
        private readonly ITableDataSink sink;

        internal SinkManager(TaggerProvider provider, ITableDataSink sink)
        {
            this.provider = provider;
            this.sink = sink;

            this.provider.AddSinkManager(this);
        }

        public void Dispose()
        {
            this.provider.RemoveSinkManager(this);
        }

        internal void AddFactory(ITableEntriesSnapshotFactory factory)
        {
            this.sink.AddFactory(factory);
        }

        internal void RemoveFactory(ITableEntriesSnapshotFactory factory)
        {
            this.sink.RemoveFactory(factory);
        }

        internal void UpdateSink(ITableEntriesSnapshotFactory factory)
        {
            this.sink.FactorySnapshotChanged(factory);
        }
    }
}
