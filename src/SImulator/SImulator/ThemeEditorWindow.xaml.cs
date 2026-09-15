using SImulator.ViewModel.Controllers;
using System.Windows;

namespace SImulator;

/// <summary>
/// Hosts the fully local Theme Editor and its live preview.
/// </summary>
public partial class ThemeEditorWindow : Window
{
    public ThemeEditorWindow(ThemeEditorController controller)
    {
        DataContext = controller;
        InitializeComponent();
    }
}
