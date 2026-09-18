using System.Windows.Controls;

namespace SIGame.View
{
    /// <summary>
    /// Логика взаимодействия для ThemeSettingsPage.xaml
    /// </summary>
    public partial class ThemeSettingsPage : Page
    {
        public ThemeSettingsPage()
        {
            InitializeComponent();
        }

        private void OpenThemeEditor_Click(object sender, System.Windows.RoutedEventArgs e) =>
            ((App)System.Windows.Application.Current).OpenThemeEditor(System.Windows.Window.GetWindow(this));
    }
}
