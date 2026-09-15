using NUnit.Framework;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.Theming;
using System.Text.Json;

namespace SImulator.ViewModel.Tests;

public sealed class ThemeEditorControllerTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private string _testDirectory = null!;
    private AppSettings _settings = null!;
    private FileThemeRepository _repository = null!;
    private ThemeEditorController _controller = null!;
    private List<string> _messages = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "SImulatorThemeEditorTests", Guid.NewGuid().ToString("N"));
        _settings = new AppSettings();
        _repository = new FileThemeRepository(_testDirectory);
        _controller = new ThemeEditorController(_settings, _repository);
        _messages = [];
        _controller.SendJsonMessage += _messages.Add;
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Test]
    public void LoadedPublishesActiveThemeAndLocalLibrary()
    {
        _controller.OnMessage("{\"type\":\"loaded\"}");

        var messageTypes = _messages.Select(ReadMessageType).ToArray();

        Assert.That(messageTypes, Is.EqualTo(new[] { "applyTheme", "themeLibrary" }));
    }

    [Test]
    public void ThemeChangedUpdatesActiveThemeWithoutSavingPreset()
    {
        var theme = CreateTheme("live-preview", "Live Preview");

        SendThemeMessage("themeChanged", theme);

        Assert.Multiple(() =>
        {
            Assert.That(_settings.PresentationTheme?.Id, Is.EqualTo("live-preview"));
            Assert.That(_repository.GetAll(), Is.Empty);
        });
    }

    [Test]
    public void SavingBuiltInThemeCreatesEditableCopy()
    {
        var theme = SimulatorDefaultTheme.Create(_settings.SIUISettings);

        SendThemeMessage("saveTheme", theme);

        Assert.Multiple(() =>
        {
            Assert.That(_settings.PresentationTheme, Is.Not.Null);
            Assert.That(_settings.PresentationTheme!.Id, Is.Not.EqualTo("simulator-default"));
            Assert.That(_settings.PresentationTheme.BasedOn, Is.EqualTo("simulator-default"));
            Assert.That(_repository.GetAll(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void DeletingActivePresetFallsBackToDefaultTheme()
    {
        var theme = CreateTheme("broadcast", "Broadcast");
        _repository.Save(theme);
        _settings.PresentationTheme = theme;

        _controller.OnMessage("{\"type\":\"deleteTheme\",\"id\":\"broadcast\"}");

        Assert.Multiple(() =>
        {
            Assert.That(_settings.PresentationTheme, Is.Null);
            Assert.That(_repository.TryGet("broadcast"), Is.Null);
            Assert.That(_messages.Select(ReadMessageType), Does.Contain("applyTheme"));
        });
    }

    private void SendThemeMessage(string type, SimulatorThemeDocument theme) =>
        _controller.OnMessage(JsonSerializer.Serialize(new { type, theme }, SerializerOptions));

    private static string? ReadMessageType(string message)
    {
        using var document = JsonDocument.Parse(message);
        return document.RootElement.GetProperty("type").GetString();
    }

    private static SimulatorThemeDocument CreateTheme(string id, string name)
    {
        var defaultTheme = SimulatorDefaultTheme.Create("#FFFFFF", "#0A0E30", "Standard");

        return new SimulatorThemeDocument
        {
            Id = id,
            Name = name,
            BasedOn = defaultTheme.Id,
            Assets = defaultTheme.Assets,
            Tokens = defaultTheme.Tokens,
        };
    }
}
