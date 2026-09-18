using NUnit.Framework;
using SImulator.ViewModel.Model;
using SITheme;
using System.Text.Json;

namespace SImulator.ViewModel.Tests;

public sealed class SimulatorDefaultThemeTests
{
    [Test]
    public void CreatePreservesLegacyColorsButUsesBundledFont()
    {
        var theme = SimulatorDefaultTheme.Create("#112233", "#445566", "Inter");

        Assert.Multiple(() =>
        {
            Assert.That(theme.SchemaVersion, Is.EqualTo(SimulatorThemeDocument.CurrentSchemaVersion));
            Assert.That(theme.Id, Is.EqualTo("simulator-default"));
            Assert.That(theme.Tokens.Global.TextColor, Is.EqualTo("#112233"));
            Assert.That(theme.Tokens.Global.BackgroundColor, Is.EqualTo("#445566"));
            Assert.That(theme.Tokens.Typography.QuestionText.FontFamily, Is.EqualTo("Standard"));
            Assert.That(theme.Tokens.Typography.QuestionText.AutoSize, Is.True);
            Assert.That(theme.Tokens.Typography.QuestionText.TextShadowEnabled, Is.True);
            Assert.That(theme.Tokens.Board.BordersVisible, Is.True);
            Assert.That(theme.Tokens.Board.PlainTextOnly, Is.False);
        });
    }

    [Test]
    public void CreateMapsLegacyDefaultFontToBundledStandardFont()
    {
        var theme = SimulatorDefaultTheme.Create("White", "#0A0E30", "_Default");

        Assert.That(theme.Tokens.Typography.QuestionText.FontFamily, Is.EqualTo("Standard"));
    }

    [Test]
    public void ActiveThemeSurvivesApplicationSettingsJsonRoundTrip()
    {
        var settings = new AppSettings
        {
            PresentationTheme = SimulatorDefaultTheme.Create("#F0F0F0", "#102040", "Standard")
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.Multiple(() =>
        {
            Assert.That(restored?.PresentationTheme, Is.Not.Null);
            Assert.That(restored!.PresentationTheme!.Tokens.Global.BackgroundColor, Is.EqualTo("#102040"));
            Assert.That(restored.PresentationTheme.Tokens.Typography.QuestionText.FontFamily, Is.EqualTo("Standard"));
            Assert.That(restored.PresentationTheme.Tokens.Typography.QuestionText.AutoSize, Is.True);
            Assert.That(restored.PresentationTheme.Tokens.Typography.QuestionText.TextShadowEnabled, Is.True);
            Assert.That(restored.PresentationTheme.Tokens.Board.BordersVisible, Is.True);
            Assert.That(restored.PresentationTheme.Tokens.Board.PlainTextOnly, Is.False);
        });
    }

    [Test]
    public void EmbeddedFontThemeIsValid()
    {
        var theme = CreateFontTheme("Broadcast Sans", "data:font/woff2;base64,AA==");

        Assert.That(SimulatorThemeValidator.IsValid(theme), Is.True);
    }

    [Test]
    public void RemoteFontThemeIsInvalid()
    {
        var theme = CreateFontTheme("Broadcast Sans", "https://example.com/font.woff2");

        Assert.That(SimulatorThemeValidator.IsValid(theme), Is.False);
    }

    [Test]
    public void UnregisteredSystemFontIsInvalid()
    {
        var theme = CreateFontTheme("Arial", "data:font/woff2;base64,AA==");
        theme.Assets.Fonts.Clear();

        Assert.That(SimulatorThemeValidator.IsValid(theme), Is.False);
    }

    private static SimulatorThemeDocument CreateFontTheme(string fontFamily, string source) => new()
    {
        Id = "font-theme",
        Name = "Font Theme",
        Assets = new SimulatorThemeAssets
        {
            Fonts = new Dictionary<string, string>
            {
                [fontFamily] = source,
            },
        },
        Tokens = new SimulatorThemeTokens
        {
            Typography = new SimulatorTypographyThemeTokens
            {
                QuestionText = new SimulatorTypographyToken
                {
                    FontFamily = fontFamily,
                    FontSize = 72,
                },
            },
        },
    };
}
