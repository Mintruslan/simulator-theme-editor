using System.Text.Json;

namespace SImulator.ViewModel.Theming;

/// <summary>
/// Stores each user theme as an independent JSON file in a local directory.
/// </summary>
public sealed class FileThemeRepository : IThemeRepository
{
    public const string ThemeFileExtension = ".theme.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string _themesDirectory;

    public FileThemeRepository(string themesDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themesDirectory);
        _themesDirectory = Path.GetFullPath(themesDirectory);
    }

    public IReadOnlyList<SimulatorThemeDocument> GetAll()
    {
        if (!Directory.Exists(_themesDirectory))
        {
            return [];
        }

        var themes = new List<SimulatorThemeDocument>();

        foreach (var filePath in Directory.EnumerateFiles(_themesDirectory, $"*{ThemeFileExtension}")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                themes.Add(ReadDocument(filePath));
            }
            catch (Exception exc) when (exc is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
            {
                // A broken preset must not prevent the rest of the local library from loading.
            }
        }

        return themes;
    }

    public SimulatorThemeDocument? TryGet(string id)
    {
        if (!SimulatorThemeValidator.IsValidId(id))
        {
            return null;
        }

        var filePath = GetThemeFilePath(id);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            return ReadDocument(filePath);
        }
        catch (Exception exc) when (exc is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return null;
        }
    }

    public void Save(SimulatorThemeDocument theme)
    {
        ValidateUserTheme(theme);
        WriteDocument(theme, GetThemeFilePath(theme.Id));
    }

    public SimulatorThemeDocument Duplicate(SimulatorThemeDocument source, string name)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!SimulatorThemeValidator.IsValid(source))
        {
            throw new InvalidDataException("The source theme is invalid or unsupported.");
        }

        var duplicate = CloneWithIdentity(source, CreateThemeId(), name.Trim(), source.Id);
        Save(duplicate);
        return duplicate;
    }

    public bool Delete(string id)
    {
        if (!SimulatorThemeValidator.IsValidId(id) || id == "simulator-default")
        {
            return false;
        }

        var filePath = GetThemeFilePath(id);

        if (!File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        return true;
    }

    public SimulatorThemeDocument Import(string sourceFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        return Import(ReadDocument(sourceFilePath));
    }

    public SimulatorThemeDocument Import(SimulatorThemeDocument theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (!SimulatorThemeValidator.IsValid(theme))
        {
            throw new InvalidDataException("The imported theme is invalid or unsupported.");
        }

        var imported = theme;

        if (imported.Id == "simulator-default" || File.Exists(GetThemeFilePath(imported.Id)))
        {
            imported = CloneWithIdentity(imported, CreateThemeId(), imported.Name, imported.Id);
        }

        Save(imported);
        return imported;
    }

    public void Export(SimulatorThemeDocument theme, string destinationFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);

        if (!SimulatorThemeValidator.IsValid(theme))
        {
            throw new InvalidDataException("The theme is invalid or unsupported.");
        }

        WriteDocument(theme, destinationFilePath);
    }

    private string GetThemeFilePath(string id) => Path.Combine(_themesDirectory, $"{id}{ThemeFileExtension}");

    private static SimulatorThemeDocument ReadDocument(string filePath)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var theme = JsonSerializer.Deserialize<SimulatorThemeDocument>(stream, SerializerOptions);

        if (!SimulatorThemeValidator.IsValid(theme))
        {
            throw new InvalidDataException($"Theme file '{filePath}' is invalid or unsupported.");
        }

        return theme!;
    }

    private static void WriteDocument(SimulatorThemeDocument theme, string filePath)
    {
        var absolutePath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(absolutePath)
            ?? throw new InvalidOperationException("The theme destination directory could not be resolved.");

        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(absolutePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, theme, SerializerOptions);
            }

            File.Move(temporaryPath, absolutePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void ValidateUserTheme(SimulatorThemeDocument theme)
    {
        if (!SimulatorThemeValidator.IsValid(theme))
        {
            throw new InvalidDataException("The theme is invalid or unsupported.");
        }

        if (theme.Id == "simulator-default")
        {
            throw new InvalidOperationException("The built-in default theme is read-only. Duplicate it before editing.");
        }
    }

    private static string CreateThemeId() => $"theme-{Guid.NewGuid():N}";

    private static SimulatorThemeDocument CloneWithIdentity(
        SimulatorThemeDocument source,
        string id,
        string name,
        string? basedOn)
    {
        var serialized = JsonSerializer.Serialize(source, SerializerOptions);
        var clone = JsonSerializer.Deserialize<SimulatorThemeDocument>(serialized, SerializerOptions)
            ?? throw new InvalidDataException("The theme could not be cloned.");

        return new SimulatorThemeDocument
        {
            SchemaVersion = clone.SchemaVersion,
            Id = id,
            Name = name,
            BasedOn = basedOn,
            Assets = clone.Assets,
            Tokens = clone.Tokens,
        };
    }
}
