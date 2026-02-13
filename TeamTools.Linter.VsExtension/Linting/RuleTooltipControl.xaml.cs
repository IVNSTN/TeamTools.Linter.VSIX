using Microsoft.VisualStudio.PlatformUI;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TeamTools.VisualStudio.SqlExtension.Linting
{
    public partial class RuleTooltipControl : UserControl
    {
        public RuleTooltipControl()
        {
            InitializeComponent();
            VSColorTheme.ThemeChanged += VSColorThemeChanged;

            UpdateColors();
        }

        private static System.Windows.Media.Color ColorFromARGB(Color value)
        {
            return System.Windows.Media.Color.FromArgb(value.A, value.R, value.G, value.B);
        }

        private void VSColorThemeChanged(ThemeChangedEventArgs e)
        {
            UpdateColors();
        }

        private void UpdateColors()
        {
            Resources["TextFontColor"] = new System.Windows.Media.SolidColorBrush(ColorFromARGB(VSColorTheme.GetThemedColor(CommonControlsColors.TextBoxTextColorKey)));
            Resources["HyperlinkFontColor"] = Resources["TextFontColor"];
            Resources["UnderlineColor"] = Resources["TextFontColor"];
        }

        private void DocsLink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(((Hyperlink)e.Source).NavigateUri.AbsoluteUri);
        }
    }
}
