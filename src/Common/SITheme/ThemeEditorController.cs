using System.Text.Json;
using Utils.Web;

namespace SITheme;

/// <summary>
/// Connects the local WebView theme editor to application settings and the preset repository.
/// </summary>
public sealed class ThemeEditorController : IWebInterop
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IPresentationThemeSettings _settings;
    private readonly IThemeRepository _themeRepository;
    private readonly string _productName;

    public Uri Source { get; } = new($"file:///{AppDomain.CurrentDomain.BaseDirectory}webtable/theme-editor.html");

    public event Action<string>? SendJsonMessage;

    public event Action<Exception>? Error;

    public ThemeEditorController(
        IPresentationThemeSettings settings,
        IThemeRepository themeRepository,
        string productName = "SImulator")
    {
        _settings = settings;
        _themeRepository = themeRepository;
        _productName = productName;
    }

    public void OnMessage(string webMessageAsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(webMessageAsJson);
            var root = document.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "loaded":
                    SendMessage(new { Type = "editorContext", ProductName = _productName });
                    PublishEditorState();
                    break;

                case "themeChanged":
                    if (TryReadTheme(root, out var changedTheme))
                    {
                        _settings.PresentationTheme = changedTheme;
                    }
                    break;

                case "saveTheme":
                    if (TryReadTheme(root, out var savedTheme))
                    {
                        SaveTheme(savedTheme);
                    }
                    break;

                case "duplicateTheme":
                    if (TryReadTheme(root, out var sourceTheme))
                    {
                        var copyName = root.TryGetProperty("name", out var nameElement)
                            ? nameElement.GetString()
                            : null;

                        DuplicateTheme(sourceTheme, copyName);
                    }
                    break;

                case "importTheme":
                    if (TryReadTheme(root, out var importedTheme))
                    {
                        ActivateTheme(_themeRepository.Import(importedTheme), "Тема импортирована");
                    }
                    break;

                case "loadTheme":
                    LoadTheme(root.GetProperty("id").GetString());
                    break;

                case "deleteTheme":
                    DeleteTheme(root.GetProperty("id").GetString());
                    break;
            }
        }
        catch (Exception exc)
        {
            SendStatus(exc.Message, "error");
            Error?.Invoke(exc);
        }
    }

    private void SaveTheme(SimulatorThemeDocument theme)
    {
        if (theme.Id == "simulator-default")
        {
            var copyName = theme.Name == $"Default {_productName} Theme"
                ? $"My {_productName} Theme"
                : theme.Name;
            ActivateTheme(_themeRepository.Duplicate(theme, copyName), "Тема сохранена как новый пресет");
            return;
        }

        _themeRepository.Save(theme);
        ActivateTheme(theme, "Тема сохранена");
    }

    private void DuplicateTheme(SimulatorThemeDocument theme, string? name)
    {
        var copyName = string.IsNullOrWhiteSpace(name) ? $"{theme.Name} Copy" : name.Trim();
        ActivateTheme(_themeRepository.Duplicate(theme, copyName), "Тема дублирована");
    }

    private void LoadTheme(string? id)
    {
        if (id == "simulator-default")
        {
            _settings.PresentationTheme = null;
            SendTheme(GetDefaultTheme());
            SendStatus("Загружена стандартная тема", "success");
            return;
        }

        if (id == null || _themeRepository.TryGet(id) is not { } theme)
        {
            SendStatus("Тема не найдена", "error");
            return;
        }

        ActivateTheme(theme, "Тема загружена");
    }

    private void DeleteTheme(string? id)
    {
        if (id == null || !_themeRepository.Delete(id))
        {
            SendStatus("Не удалось удалить выбранную тему", "error");
            return;
        }

        if (_settings.PresentationTheme?.Id == id)
        {
            _settings.PresentationTheme = null;
            SendTheme(GetDefaultTheme());
        }

        PublishThemeLibrary();
        SendStatus("Тема удалена", "success");
    }

    private void ActivateTheme(SimulatorThemeDocument theme, string status)
    {
        _settings.PresentationTheme = theme;
        SendTheme(theme);
        PublishThemeLibrary();
        SendStatus(status, "success");
    }

    private void PublishEditorState()
    {
        SendTheme(_settings.PresentationTheme is { } activeTheme && SimulatorThemeValidator.IsValid(activeTheme)
            ? activeTheme
            : GetDefaultTheme());

        PublishThemeLibrary();
    }

    private void PublishThemeLibrary()
    {
        var themes = new[] { GetDefaultTheme() }
            .Concat(_themeRepository.GetAll())
            .Select(theme => new { theme.Id, theme.Name, theme.BasedOn })
            .ToArray();

        SendMessage(new
        {
            Type = "themeLibrary",
            Themes = themes,
            ActiveThemeId = _settings.PresentationTheme?.Id ?? "simulator-default",
        });
    }

    private SimulatorThemeDocument GetDefaultTheme() => _settings.CreateDefaultPresentationTheme();

    private void SendTheme(SimulatorThemeDocument theme) => SendMessage(new
    {
        Type = "applyTheme",
        Theme = theme,
    });

    private void SendStatus(string message, string level) => SendMessage(new
    {
        Type = "editorStatus",
        Message = message,
        Level = level,
    });

    private void SendMessage(object message) =>
        SendJsonMessage?.Invoke(JsonSerializer.Serialize(message, SerializerOptions));

    private static bool TryReadTheme(JsonElement root, out SimulatorThemeDocument theme)
    {
        theme = root.GetProperty("theme").Deserialize<SimulatorThemeDocument>(SerializerOptions)!;

        if (!SimulatorThemeValidator.IsValid(theme))
        {
            throw new InvalidDataException("Тема повреждена или использует неподдерживаемую версию.");
        }

        return true;
    }
}
