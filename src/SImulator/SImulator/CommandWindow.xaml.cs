using SImulator.ViewModel;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.Theming;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SImulator;

/// <summary>
/// Provides interaction logic for CommandWindow.xaml.
/// </summary>
public partial class CommandWindow : Window
{
    private readonly AppSettings _settings;
    private readonly IThemeRepository _themeRepository;

    /// <summary>
    /// Application version.
    /// </summary>
    public string? Version => App.ProductVersion.ToString(3);

    public CommandWindow(AppSettings settings, IThemeRepository themeRepository)
    {
        _settings = settings;
        _themeRepository = themeRepository;
        InitializeComponent();
    }

    private void OpenThemeEditor_Click(object sender, RoutedEventArgs e)
    {
        var editor = new ThemeEditorWindow(new ThemeEditorController(_settings, _themeRepository))
        {
            Owner = this,
        };

        editor.Show();
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        if (DataContext != null)
        {
            var result = await ((MainViewModel)DataContext).RaiseStop();
            e.Cancel = !result;
        }
    }

    private async void Button_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            await mainViewModel.OnButtonsLeftAsync();
        }
    }

    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
        e.Accepted = (ViewModel.Core.PlayerKeysModes)e.Item != ViewModel.Core.PlayerKeysModes.Com;
    }
}
