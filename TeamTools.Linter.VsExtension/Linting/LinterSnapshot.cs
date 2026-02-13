using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TeamTools.VisualStudio.SqlExtension.Tagging;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    internal class LinterSnapshot : WpfTableEntriesSnapshotBase
    {
        private readonly string filePath;
        private readonly IList<LinterMessageMarker> markers;
        private readonly IReadOnlyCollection<LinterMessageMarker> readonlyMarkers;

        internal LinterSnapshot(string filePath, int versionNumber, IEnumerable<LinterMessageMarker> markers)
        {
            this.filePath = filePath;
            this.VersionNumber = versionNumber;

            this.markers = new List<LinterMessageMarker>(markers);
            this.readonlyMarkers = new ReadOnlyCollection<LinterMessageMarker>(this.markers);
        }

        public override int Count => markers.Count;

        public override int VersionNumber { get; }

        internal IEnumerable<LinterMessageMarker> Markers => readonlyMarkers;

        public override bool TryGetValue(int index, string columnName, out object content)
        {
            content = null;

            if (index < 0 || this.markers.Count <= index)
            {
                return false;
            }

            var marker = this.markers[index];

            switch (columnName)
            {
                case StandardTableKeyNames.BuildTool:
                    // TODO : take name from project config / resource file
                    content = "TSQL Linter";
                    return true;

                case StandardTableKeyNames.Column:
                    var position = marker.Span.Start;
                    var line = position.GetContainingLine();
                    content = position.Position - line.Start.Position;
                    return true;

                case StandardTableKeyNames.DocumentName:
                    content = filePath;
                    return null != content;

                case StandardTableKeyNames.ErrorCodeToolTip:
                    content = null; // TODO : something useful can be shown here
                    return null != content;

                case StandardTableKeyNames.HelpLink:
                    content = marker.Message.DocumentationLink;
                    return null != content;

                case StandardTableKeyNames.ErrorCode:
                    content = marker.Message.RuleId;
                    return true;

                case StandardTableKeyNames.ErrorSeverity:
                    content = SeverityConverter.ConvertToErrorCategory(marker.Message.MessageSeverity);
                    return true;

                case StandardTableKeyNames.ErrorSource:
                    content = marker.Message.Source;
                    return null != content;

                case StandardTableKeyNames.Line:
                    content = marker.Span.Start.GetContainingLine().LineNumber;
                    return true;

                case StandardTableKeyNames.ProjectGuid:
                    content = marker.Message.ProjectGuid;
                    return null != content;

                case StandardTableKeyNames.ProjectName:
                    content = marker.Message.ProjectName;
                    return null != content;

                case StandardTableKeyNames.Text:
                    content = marker.Message.Message;
                    return null != content;

                default:
                    return false;
            }
        }

        public override bool CanCreateDetailsContent(int index)
        {
            return !string.IsNullOrEmpty(markers[index].Message.Explanation);
        }

        public override bool TryCreateDetailsStringContent(int index, out string content)
        {
            // TODO: a good place to get suggestions for fixing
            content = null; // "Сюда можно вывести пояснение";
            return null != content;
        }
    }
}
