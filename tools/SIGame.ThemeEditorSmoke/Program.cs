using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SIGame;
using SIGame.ViewModel;
using SITheme;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace SIGame.ThemeEditorSmoke;

internal static class Program
{
    private static int _exitCode = 1;

    [STAThread]
    private static int Main(string[] args)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "SIGameThemeEditorSmoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            if (args.Contains("--game-view", StringComparer.OrdinalIgnoreCase))
            {
                return RunGameViewSmoke(tempDirectory);
            }

            var settings = new ThemeSettings();
            var repository = new FileThemeRepository(tempDirectory);
            var controller = new ThemeEditorController(settings, repository, "SIGame");

            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var applierType = typeof(ThemeEditorWindow).Assembly.GetType("SIGame.Implementation.PresentationThemeApplier")
                ?? throw new InvalidOperationException("SIGame WPF theme adapter was not found.");
            var applierConstructor = applierType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single();
            using var applier = (IDisposable)applierConstructor.Invoke([
                settings,
                repository,
                application.Resources,
                Path.Combine(tempDirectory, ".font-cache"),
            ]);
            applierType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public)!.Invoke(applier, null);

            application.Dispatcher.BeginInvoke(() =>
            {
                var window = new ThemeEditorWindow(controller)
                {
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                };
                var webView = (WebView2?)window.FindName("webView")
                    ?? throw new InvalidOperationException("Theme Editor WebView was not created.");
                var navigation = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                ulong? editorNavigationId = null;
                webView.NavigationStarting += (_, args) =>
                {
                    if (args.Uri.StartsWith(controller.Source.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                    {
                        editorNavigationId = args.NavigationId;
                    }
                };
                webView.NavigationCompleted += (_, args) =>
                {
                    if (args.NavigationId == editorNavigationId)
                    {
                        navigation.TrySetResult(args);
                    }
                };

                var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
                timeout.Tick += (_, _) =>
                {
                    timeout.Stop();
                    Console.Error.WriteLine("SIGame Theme Editor smoke test timed out.");
                    window.Close();
                    application.Shutdown(1);
                };

                window.Loaded += async (_, _) =>
                {
                    try
                    {
                        Console.WriteLine("Theme Editor window loaded; waiting for local navigation.");
                        var navigationResult = await navigation.Task.WaitAsync(TimeSpan.FromSeconds(20));

                        if (!navigationResult.IsSuccess)
                        {
                            throw new InvalidOperationException(
                                $"Local Theme Editor navigation failed: {navigationResult.WebErrorStatus}.");
                        }

                        Console.WriteLine("Local editor navigation completed; waiting for editor DOM.");
                        string? header = null;

                    for (var attempt = 0; attempt < 100 && header != "SIGame Theme Editor"; attempt++)
                    {
                        try
                        {
                            var headerJson = await webView.ExecuteScriptAsync(
                                "document.querySelector('.themeEditorHeader strong')?.textContent ?? ''");
                            header = JsonSerializer.Deserialize<string>(headerJson);
                        }
                        catch (InvalidOperationException)
                        {
                            // Navigation may still be switching from about:blank to the local editor page.
                        }

                        if (header != "SIGame Theme Editor")
                        {
                            await Task.Delay(100);
                        }
                    }

                    var sectionCountJson = await webView.ExecuteScriptAsync(
                        "document.querySelectorAll('.themeEditorNavigation button').length");
                    var sectionCount = JsonSerializer.Deserialize<int>(sectionCountJson);
                    Console.WriteLine($"Editor DOM ready: header='{header}', sections={sectionCount}.");

                    if (header != "SIGame Theme Editor" || sectionCount != 5)
                    {
                        throw new InvalidOperationException(
                            $"Unexpected editor DOM: header='{header}', sections={sectionCount}.");
                    }

                    var themeJson = JsonSerializer.SerializeToNode(settings.CreateDefaultPresentationTheme(), new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    })!.AsObject();
                    themeJson["id"] = "sigame-smoke";
                    themeJson["name"] = "SIGame Smoke Theme";
                    var tokens = themeJson["tokens"]!.AsObject();
                    var board = tokens["board"]!.AsObject();
                    board["bordersVisible"] = false;
                    board["plainTextOnly"] = true;
                    var questionText = tokens["typography"]!["questionText"]!.AsObject();
                    questionText["fontSize"] = 37;
                    questionText["autoSize"] = false;
                    questionText["textAlign"] = "right";
                    questionText["textShadowEnabled"] = false;

                    var portableThemeJson = themeJson.ToJsonString();
                    await webView.ExecuteScriptAsync(
                        $"window.chrome.webview.postMessage({{type:'themeChanged',theme:{portableThemeJson}}})");
                    Console.WriteLine("Live theme change posted.");

                    for (var attempt = 0; attempt < 20 && settings.PresentationTheme?.Id != "sigame-smoke"; attempt++)
                    {
                        await Task.Delay(100);
                    }

                    var borderThickness = (Thickness)application.Resources["SIThemeBoardBorderThickness"];
                    var fontSize = (double)application.Resources["SIThemeQuestionTextFontSize"];
                    var autoSize = (bool)application.Resources["SIThemeQuestionTextAutoSize"];
                    var textAlignment = (TextAlignment)application.Resources["SIThemeQuestionTextTextAlignment"];
                    var textEffect = application.Resources["SIThemeQuestionTextTextEffect"] as DropShadowEffect;

                    if (settings.PresentationTheme?.Id != "sigame-smoke"
                        || borderThickness != new Thickness(0)
                        || fontSize != 37
                        || autoSize
                        || textAlignment != TextAlignment.Right
                        || textEffect != null)
                    {
                        throw new InvalidOperationException("Live theme changes were not projected onto native WPF resources.");
                    }

                    await webView.ExecuteScriptAsync(
                        $"window.chrome.webview.postMessage({{type:'saveTheme',theme:{portableThemeJson}}})");
                    Console.WriteLine("Theme save posted.");

                    for (var attempt = 0; attempt < 20 && repository.TryGet("sigame-smoke") == null; attempt++)
                    {
                        await Task.Delay(100);
                    }

                    if (repository.TryGet("sigame-smoke") == null)
                    {
                        throw new InvalidOperationException("Theme Editor did not persist the edited theme.");
                    }

                    _exitCode = 0;
                    Console.WriteLine(
                        $"SIGame Theme Editor smoke test passed: header='{header}', sections={sectionCount}, live WPF theme and save verified.");
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                }
                finally
                {
                    timeout.Stop();
                    window.Close();
                    application.Shutdown(_exitCode);
                }
            };

                timeout.Start();
                window.Show();
            });

            application.Run();
            return _exitCode;
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static int RunGameViewSmoke(string tempDirectory)
    {
        try
        {
            var application = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.InitializeComponent();

            var settings = new ThemeSettings();
            var repository = new FileThemeRepository(tempDirectory);
            var applierType = typeof(ThemeEditorWindow).Assembly.GetType("SIGame.Implementation.PresentationThemeApplier")
                ?? throw new InvalidOperationException("SIGame WPF theme adapter was not found.");
            var applierConstructor = applierType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single();
            using var applier = (IDisposable)applierConstructor.Invoke([
                settings,
                repository,
                application.Resources,
                Path.Combine(tempDirectory, ".font-cache"),
            ]);
            applierType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public)!.Invoke(applier, null);

            _ = new Studia();

            Console.WriteLine("SIGame game view smoke test passed: Studia, Table and player presentation styles loaded.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
