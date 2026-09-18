using NUnit.Framework;
using SITheme;

namespace SImulator.ViewModel.Tests;

public sealed class FileThemeRepositoryTests
{
    private string _testDirectory = null!;
    private FileThemeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "SImulatorThemeTests", Guid.NewGuid().ToString("N"));
        _repository = new FileThemeRepository(_testDirectory);
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
    public void SaveAndLoadRoundTripsATheme()
    {
        var theme = CreateUserTheme("studio-blue", "Studio Blue");

        _repository.Save(theme);

        var loaded = _repository.TryGet(theme.Id);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Name, Is.EqualTo("Studio Blue"));
            Assert.That(loaded.Tokens.Global.BackgroundColor, Is.EqualTo("#0A0E30"));
            Assert.That(_repository.GetAll().Select(item => item.Id), Is.EqualTo(new[] { "studio-blue" }));
        });
    }

    [Test]
    public void DuplicateCreatesAnIndependentUserPreset()
    {
        var source = CreateUserTheme("studio-blue", "Studio Blue");

        var duplicate = _repository.Duplicate(source, "Studio Blue Copy");

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Id, Is.Not.EqualTo(source.Id));
            Assert.That(duplicate.Name, Is.EqualTo("Studio Blue Copy"));
            Assert.That(duplicate.BasedOn, Is.EqualTo(source.Id));
            Assert.That(_repository.TryGet(duplicate.Id), Is.Not.Null);
            Assert.That(duplicate.Tokens, Is.Not.SameAs(source.Tokens));
        });
    }

    [Test]
    public void ImportDoesNotOverwriteAnExistingTheme()
    {
        var source = CreateUserTheme("studio-blue", "Studio Blue");
        _repository.Save(source);

        var exportPath = Path.Combine(_testDirectory, "exports", "studio-blue.theme.json");
        _repository.Export(source, exportPath);

        var imported = _repository.Import(exportPath);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Id, Is.Not.EqualTo(source.Id));
            Assert.That(imported.BasedOn, Is.EqualTo(source.Id));
            Assert.That(_repository.GetAll(), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void BrokenFilesAreSkippedWithoutBreakingTheLibrary()
    {
        _repository.Save(CreateUserTheme("valid-theme", "Valid Theme"));
        File.WriteAllText(Path.Combine(_testDirectory, "broken.theme.json"), "not-json");

        var themes = _repository.GetAll();

        Assert.That(themes.Select(theme => theme.Id), Is.EqualTo(new[] { "valid-theme" }));
    }

    [Test]
    public void UnsafeThemeIdIsRejectedBeforeWriting()
    {
        var theme = CreateUserTheme("../outside", "Unsafe Theme");

        Assert.That(() => _repository.Save(theme), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(_testDirectory), Is.False);
    }

    [Test]
    public void BuiltInDefaultThemeCannotBeDeletedOrOverwritten()
    {
        var defaultTheme = SimulatorDefaultTheme.Create("#FFFFFF", "#0A0E30", "Standard");

        Assert.Multiple(() =>
        {
            Assert.That(() => _repository.Save(defaultTheme), Throws.TypeOf<InvalidOperationException>());
            Assert.That(_repository.Delete(defaultTheme.Id), Is.False);
        });
    }

    private static SimulatorThemeDocument CreateUserTheme(string id, string name)
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
