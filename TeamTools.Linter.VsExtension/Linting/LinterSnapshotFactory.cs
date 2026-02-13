using Microsoft.VisualStudio.Shell.TableManager;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    internal class LinterSnapshotFactory : TableEntriesSnapshotFactoryBase
    {
        internal LinterSnapshotFactory(LinterSnapshot snapshot)
        {
            this.CurrentSnapshot = snapshot;
        }

        public override int CurrentVersionNumber => this.CurrentSnapshot.VersionNumber;

        internal LinterSnapshot CurrentSnapshot { get; private set; }

        public override ITableEntriesSnapshot GetCurrentSnapshot()
        {
            return this.CurrentSnapshot;
        }

        public override ITableEntriesSnapshot GetSnapshot(int versionNumber)
        {
            var snapshot = this.CurrentSnapshot;

            return versionNumber == snapshot.VersionNumber ? snapshot : null;
        }

        internal void UpdateMarkers(LinterSnapshot snapshot)
        {
            this.CurrentSnapshot = snapshot;
        }
    }
}
