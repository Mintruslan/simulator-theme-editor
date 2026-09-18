using Microsoft.Web.WebView2.Core;
using SITheme;
using System;
using System.Windows;

namespace SImulator;

/// <summary>
/// Hosts the fully local Theme Editor and its live preview.
/// </summary>
public partial class ThemeEditorWindow : Window
{
    private readonly Uri _source;
    private bool _isClosed;

    public ThemeEditorWindow(ThemeEditorController controller)
    {
        DataContext = controller;
        _source = controller.Source;
        InitializeComponent();

        if (webView.CoreWebView2 != null)
        {
            NavigateToEditor();
        }
        else
        {
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        }
    }

    private void WebView_CoreWebView2InitializationCompleted(
        object? sender,
        CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            Dispatcher.BeginInvoke(NavigateToEditor);
        }
    }

    private async void NavigateToEditor()
    {
        // Some WebView2 Runtime versions abort a file navigation that starts while
        // the initial about:blank navigation is still settling.
        await System.Threading.Tasks.Task.Delay(1000);

        if (!_isClosed)
        {
            webView.Source = _source;
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _isClosed = true;
        webView.CoreWebView2InitializationCompleted -= WebView_CoreWebView2InitializationCompleted;
        webView.Dispose();
    }
}
